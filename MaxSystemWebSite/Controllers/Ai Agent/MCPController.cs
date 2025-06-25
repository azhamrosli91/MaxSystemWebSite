using BaseSQL.Interface;
using BaseWebApi.Interface;
using Dapper;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSys.Models;
using MaxSystemWebSite.Helpers.Graph;
using MaxSystemWebSite.Models.MCP;
using MaxSystemWebSite.Models.SETTING;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartTemplateCore.Models.Common;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using static System.Net.WebRequestMethods;

namespace MaxSystemWebSite.Controllers.Ai_Agent
{
    [Route("api/[controller]")]
    [ApiController]
    public class MCPController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<MCPController> _logger;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private static Dictionary<string, List<MessageChatBot>> _threadMessages = new();
        private readonly IEmail _emailService;
        private readonly HttpClient _httpClient;


        public MCPController(
            ILogger<MCPController> logger,
            IConfiguration configuration,
            IWebApi webApi,
            IDapper dapper,
            ISQL sQL,
            IAuthenticator authenticator,
            UserProfileService userProfileService,
            IEmail emailService,
            HttpClient httpClient)
            : base(configuration, webApi, dapper, authenticator)
        {
            _SQL = sQL;
            _logger = logger;
            _emailService = emailService;
            _httpClient = httpClient;
        }

        [HttpPost("handle")]
        public async Task<IActionResult> MCP_Handle([FromBody] string valueparam)
        {
            string action = null;
            JObject jsonObject = null; // <-- Declare here

            try
            {
                jsonObject = JObject.Parse(valueparam);
                action = jsonObject["action"]?.ToString();
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid JSON format: " + ex.Message);
            }

            if (string.IsNullOrEmpty(action))
                return BadRequest("Missing 'action' property in JSON");

            switch (action)
            {
                case "DB_QUERY_EXECUTE":
                    (bool status, string message, string dt) data = await DB(valueparam);
                    return Ok(data.dt);

                case "WEB_SEARCH":
                    (bool status, string message, string dt) webdata = await WebSearch(valueparam);
                    return Ok(webdata.dt);

                case "SEND_EMAIL":
                    (bool status, string message, string dt) emaildata = await SendEmail(valueparam);
                    return Ok(emaildata.dt);

                case "REPORTING":
                    (bool status, string message, string dt) reportdata = await Reporting(valueparam);
                    return Ok(reportdata.dt);

                case "HTTP_REST":
                    (bool status, string message, string dt) httpdata = await HTTPrest(valueparam);
                    return Ok(httpdata.dt);

                case "READ_EMAIL":
                    (bool status, string message, string dt) reademaildata = await ReadEmail(valueparam);
                    return Ok(reademaildata.dt);

                default:
                    return BadRequest("Unknown Command: " + action);
            }
        }
        public async Task<(bool status, string message, string dt)> ReadEmail(string valueparam)
        {

            SETTING_EMAIL settingEmail = new SETTING_EMAIL
            {
                TENANT_ID = _configuration["AzureAd:TenantId"],
                CLIENT_ID = _configuration["AzureAd:ClientId"],
                CLIENT_SECRET = _configuration["AzureAd:ClientSecrectValue"],
                GRAPH_USER = _configuration.GetSection("Settings:GraphUserScopes").Get<string[]>()[0]
            };

            _emailService.InitGraph(settingEmail);


            //(bool status, string message) result = await _emailService.SendEmailAsync(modelTemp);
            var (found, msg, userId) = await _emailService.GetUserIdByEmailAsync(EMAIL);

            if (!found || string.IsNullOrEmpty(userId))
            {
                return (false, msg, msg);
            }

            var (success, message, emails) = await _emailService.GetEmailList(userId);
            if (!success)
            {
                return (false, message, message);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(emails);
            return (true, "Emails retrieved", json);
        }
        public async Task<(bool status, string message, string dt)> HTTPrest(string valueparam)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(valueparam);
                var root = jsonDoc.RootElement;

                string method = root.GetProperty("method").GetString()?.ToLower();
                string url = root.GetProperty("url").GetString();

                // Build headers
                var headers = new HttpRequestMessage();
                if (root.TryGetProperty("header", out JsonElement headerElement) && headerElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var header in headerElement.EnumerateObject())
                    {
                        headers.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
                    }
                }

                HttpResponseMessage response;

                if (method == "get")
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    foreach (var h in headers.Headers) request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    response = await _httpClient.SendAsync(request);
                }
                else if (method == "post")
                {
                    string body = "{}";
                    if (root.TryGetProperty("body", out JsonElement bodyElement))
                    {
                        body = bodyElement.GetRawText();
                    }

                    var content = new StringContent(body, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = content
                    };
                    foreach (var h in headers.Headers) request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    response = await _httpClient.SendAsync(request);
                }
                else
                {
                    return (false, "Unsupported method", "");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                return (response.IsSuccessStatusCode, response.StatusCode.ToString(), responseContent);
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}", "");
            }
        }  
        public async Task<(bool status, string message, string dt)> Reporting(string valueparam)
        {
            return (true, "report executed", valueparam);
        }
        public async Task<(bool status, string message, string dt)> SendEmail(string valueparam)
        {
            try
            {
                string cleanedJson = valueparam.Replace("[MCP]", "");
                JObject jObject = JObject.Parse(cleanedJson);

                JToken paramsToken = jObject["params"];
                MCP_EmailParams emailParams = paramsToken.ToObject<MCP_EmailParams>();
                string SenderEmail = EMAIL;

                // ✅ Decode HTML entities
                var decodedBody = WebUtility.HtmlDecode(WebUtility.HtmlDecode(emailParams.Body));

                Console.WriteLine(decodedBody);
                List<Recipient> ListReceipt = new List<Recipient>();
                List<Recipient> ListCc = new List<Recipient>();
                List<Recipient> ListBcc = new List<Recipient>();

                if (emailParams != null)
                {
                    if (emailParams.Recipient != null)
                    {
                        foreach (var item in emailParams.Recipient)
                        {
                            ListReceipt.Add(new Recipient
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = item.EmailAddress.Address,
                                    Name = item.EmailAddress.Name
                                }
                            });
                        }
                    }

                    if (emailParams.Cc != null)
                    {
                        foreach (var item in emailParams.Cc)
                        {
                            ListCc.Add(new Recipient
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = item.EmailAddress.Address,
                                    Name = item.EmailAddress.Name
                                }
                            });
                        }
                    }

                    if (emailParams.Bcc != null)
                    {
                        foreach (var item in emailParams.Bcc)
                        {
                            ListBcc.Add(new Recipient
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = item.EmailAddress.Address,
                                    Name = item.EmailAddress.Name
                                }
                            });
                        }
                    }

                    var modelTemp = new Emai_TemplateSent
                    {
                        Recipient = ListReceipt,
                        CC = ListCc,
                        BCC = ListBcc,
                        Subject = emailParams.Subject,
                        subTemplate = decodedBody,
                        WORD_REPLACE = new List<(string ori, string replace)>
                    {
                    ("[APPLICATION_NAME]", emailParams.Subject),
                    ("[EMAIL_SUBJECT]", emailParams.Subject),
                    ("[EMAIL_BODY]", decodedBody),
                    ("[HELP_DESK_EMAIL]", "hr@maxsys.com.my")
                    },
                        Attachments = new List<Emai_TemplateSent.EmailAttachment>()
                    };

                    modelTemp.mainTemplate = await modelTemp.EmailBodyTemplate();
                    modelTemp.bodyContent = modelTemp.mainTemplate.Replace("[BODY]", modelTemp.subTemplate);
                    var wordResult = modelTemp.WordReplacer(modelTemp.bodyContent);
                    if (wordResult.Item1)
                    {
                        modelTemp.bodyContent = wordResult.Item2;
                    }

                    SETTING_EMAIL settingEmail = new SETTING_EMAIL
                    {
                        TENANT_ID = _configuration["Settings:TenantId"],
                        CLIENT_ID = _configuration["Settings:ClientId"],
                        CLIENT_SECRET = _configuration["Settings:ClientSecret"],
                        GRAPH_USER = _configuration.GetSection("Settings:GraphUserScopes").Get<string[]>()[0]
                    };

                    modelTemp.Setting_Setup = new Setting_Setup
                    {
                        SMTP_ACCOUNT = SenderEmail
                    };

                    _emailService.InitGraph(settingEmail);

                    //Console.WriteLine("Sending Email with Body:");
                    //Console.WriteLine(modelTemp.bodyContent);
                    //modelTemp.bodyContent = WebUtility.HtmlDecode(modelTemp.bodyContent);
                    (bool status, string message) result = await _emailService.SendEmailAsync(modelTemp);

                    if (!result.status)
                    {
                        return (false, $"Failed to send email. {result.message}", "");
                    }

                    string responseMessage = $"Your Email subject: \n{emailParams.Subject}" +
                                             $"\nYour Email body: \n{decodedBody}" +
                                             $"\nEmail sent";

                    return (true, "Email sent", responseMessage);
                }

                return (false, "Email parameters were null", "");
            }
            catch (Exception ex)
            {
                return (false, "Exception in SendEmail: " + ex.Message, "");
            }
        }
        public async Task<(bool status, string message, string dt)> WebSearch(string valueparam)
        {
            string apiKey = _configuration["ChatGPT:SecretKey"];
            dynamic mcp = JsonConvert.DeserializeObject(valueparam.Replace("[MCP]", ""));

            string query = mcp.query;

            if (string.IsNullOrWhiteSpace(query))
            {
                return (false, "invalid query", "");
            }

            var tools = new[] { new { type = "web_search_preview" } };

            string instructions =
                "You are a web search agent. The user will provide a URL or a query. " +
                "You must perform a web search using the provided input and return a summary of the results. " +
                "The output must be in valid HTML only. Use HTML tags such as <div>, <h1>, <h2>, <p>, <a>, and <ul>. " +
                "Wrap everything in an outer <div> tag. Do not include any explanations or comments outside of the HTML. " +
                "Do not say 'Here's the result' or similar. Just return clean HTML, suitable for embedding directly into a webpage." +
                "Return only the HTML. Do not include any Markdown, plain text, or commentary. Start your response with <div>.";

            var payload = new
            {
                model = "gpt-4.1-mini",
                input = new[] { new { role = "user", content = query } },
                instructions = instructions,
                temperature = 0.3,
                tools = tools
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var contentWeb = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage apiResponse;

            try
            {
                apiResponse = await httpClient.PostAsync("https://api.openai.com/v1/responses", contentWeb);
            }
            catch (HttpRequestException ex)
            {

                return(false, ex.Message, "");
            }

            if (apiResponse == null)
            {
                return(false,"agent failed to response", "");
            }

            string responseText = await apiResponse.Content.ReadAsStringAsync();

            ApiResponse emailParams = JsonConvert.DeserializeObject<ApiResponse>(responseText);

            string extractedText = emailParams.Output?
                .FirstOrDefault(o => o.Type == "message")?
                .Content?
                .FirstOrDefault(c => c.Type == "output_text")?
                .Text;

            return (true,"okay", extractedText);
        }
        //private async Task<(bool status, string message, string dt)> DB(string valueparam)
        //{
        //    try
        //    {
        // Deserialize your MCP payload
        //dynamic mcp = JsonConvert.DeserializeObject(valueparam.Replace("[MCP]", ""));
        //string connStr = "";     // e.g. "Server=db.internal.local;Port=1433;User Id=...;"
        //string sshHost = "10.252.133.21";                // e.g. "bastion.example.com"
        //int sshPort = 8080;        // default to 22
        //string sshUser = "muhamadazham.rosli.q9@mail.toray";                // your SSH user
        //string sshPass = "YsTe6266";            // or use a private key
        //string dbHost = "10.201.1.5";
        //int dbPort = 49818;
        //int localPort = 49818;      // choose any free port

        //if (string.IsNullOrWhiteSpace(connStr))
        //    return (false, "No Connection string found", "");

        //// 1) Establish SSH connection and port‐forward
        //using var ssh = new Renci.SshNet.SshClient(sshHost, sshPort, sshUser, sshPass);
        //ssh.Connect();

        //var forwardedPort = new Renci.SshNet.ForwardedPortLocal(
        //    "127.0.0.1",               // bind to localhost
        //    (uint)localPort,           // local port
        //    dbHost,                    // remote DB host
        //    (uint)dbPort               // remote DB port
        //);
        //ssh.AddForwardedPort(forwardedPort);
        //forwardedPort.Start();

        //// 2) Modify your connection string to hit your local tunnel
        ////    You can either rebuild from scratch or replace the "Server=" part.
        //var builder = new SqlConnectionStringBuilder(connStr)
        //{
        //    DataSource = $"127.0.0.1,{localPort}"
        //};

        //// 3) Open SQL connection over tunnel
        //var dt = new DataTable();
        //using (var conn = new SqlConnection(builder.ConnectionString))
        //using (var cmd = new SqlCommand((string)mcp.sql, conn))
        //{
        //    conn.Open();
        //    using var reader = await cmd.ExecuteReaderAsync();
        //    dt.Load(reader);
        //}

        //// 4) Teardown SSH tunnel
        //forwardedPort.Stop();
        //ssh.Disconnect();

        //        if (dt == null || dt.Rows.Count == 0)
        //        {
        //            return (false, "No data found", "");
        //        }

        //        // Convert to List<Dictionary<string, string>>
        //        var rowsList = new List<Dictionary<string, string>>();

        //        foreach (DataRow dr in dt.Rows)
        //        {
        //            var rowDict = new Dictionary<string, string>();
        //            foreach (DataColumn col in dt.Columns)
        //            {
        //                rowDict[col.ColumnName] = dr[col.ColumnName]?.ToString() ?? string.Empty;
        //            }
        //            rowsList.Add(rowDict);
        //        }

        //        var encodedJson = JsonConvert.SerializeObject(rowsList);

        //        var html = new StringBuilder();
        //        html.Append(@"
        //        <div class='code-box'>
        //          <div class='code-box-header'>
        //            <div class='title'>table</div>
        //            <div class='buttons'>
        //              <button onclick='downloadExcel()' class='btn-export'><i class='fas fa-file-export'></i> Export</button>
        //            </div>
        //          </div>
        //          <div class='table-responsive' style='max-height:400px; overflow:auto; padding:10px;'>
        //            <table class='table table-bordered table-sm mb-0'>
        //              <thead class='table-light'><tr>");

        //        foreach (DataColumn col in dt.Columns)
        //        {
        //            html.Append($"<th>{WebUtility.HtmlEncode(col.ColumnName)}</th>");
        //        }

        //        html.Append("</tr></thead><tbody>");

        //        foreach (DataRow row in dt.Rows)
        //        {
        //            html.Append("<tr>");
        //            foreach (DataColumn col in dt.Columns)
        //            {
        //                html.Append($"<td>{WebUtility.HtmlEncode(row[col.ColumnName]?.ToString() ?? "")}</td>");
        //            }
        //            html.Append("</tr>");
        //        }

        //        html.Append($@"
        //                      </tbody>
        //                    </table>
        //                  </div>
        //                </div>

        //                <script>
        //                function downloadExcel() {{
        //                    const payload = {{
        //                        filename: 'MCP_Result',
        //                        data: {encodedJson}
        //                    }};

        //                    fetch('/Snippai/ExportExcel', {{
        //                        method: 'POST',
        //                        headers: {{
        //                            'Content-Type': 'application/json'
        //                        }},
        //                        body: JSON.stringify(payload)
        //                    }})
        //                    .then(resp => resp.blob())
        //                    .then(blob => {{
        //                        const link = document.createElement('a');
        //                        link.href = window.URL.createObjectURL(blob);
        //                        link.download = payload.filename + '.xlsx';
        //                        link.click();
        //                    }})
        //                    .catch(err => alert('Export failed: ' + err));
        //                }}
        //                </script>");

        //        return (true, $"Success", html.ToString());

        //    }
        //    catch (Exception ex)
        //    {
        //        return (false, $"Error: {ex.Message}", "");
        //    }
        //}
        private async Task<(bool status, string message, string dt)> DB(string valueparam)
        {
            try
            {
                // Simulate DB call (replace this with actual DB call using ISQL or IDapper)
                //await Task.Delay(10); // Simulated async work

                dynamic mcp = JsonConvert.DeserializeObject(valueparam.Replace("[MCP]", ""));
                string connStr = mcp?.connection_string;

                if (string.IsNullOrWhiteSpace(connStr))
                {
                    return (false, "No Connection string found", "");
                }



                string sql = mcp.sql;
                DataTable dt = new DataTable();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                    }
                }

                if (dt == null || dt.Rows.Count == 0)
                {
                    return (false, "No data found", "");
                }

                // Convert to List<Dictionary<string, string>>
                var rowsList = new List<Dictionary<string, string>>();

                foreach (DataRow dr in dt.Rows)
                {
                    var rowDict = new Dictionary<string, string>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        rowDict[col.ColumnName] = dr[col.ColumnName]?.ToString() ?? string.Empty;
                    }
                    rowsList.Add(rowDict);
                }

                var encodedJson = JsonConvert.SerializeObject(rowsList);

                var html = new StringBuilder();
                html.Append(@"
                <div class='code-box'>
                  <div class='code-box-header'>
                    <div class='title'>table</div>
                    <div class='buttons'>
                      <button onclick='downloadExcel()' class='btn-export'><i class='fas fa-file-export'></i> Export</button>
                    </div>
                  </div>
                  <div class='table-responsive' style='max-height:400px; overflow:auto; padding:10px;'>
                    <table class='table table-bordered table-sm mb-0'>
                      <thead class='table-light'><tr>");

                foreach (DataColumn col in dt.Columns)
                {
                    html.Append($"<th>{WebUtility.HtmlEncode(col.ColumnName)}</th>");
                }

                html.Append("</tr></thead><tbody>");

                foreach (DataRow row in dt.Rows)
                {
                    html.Append("<tr>");
                    foreach (DataColumn col in dt.Columns)
                    {
                        html.Append($"<td>{WebUtility.HtmlEncode(row[col.ColumnName]?.ToString() ?? "")}</td>");
                    }
                    html.Append("</tr>");
                }

                html.Append($@"
                              </tbody>
                            </table>
                          </div>
                        </div>

                        <script>
                        function downloadExcel() {{
                            const payload = {{
                                filename: 'MCP_Result',
                                data: {encodedJson}
                            }};

                            fetch('/Snippai/ExportExcel', {{
                                method: 'POST',
                                headers: {{
                                    'Content-Type': 'application/json'
                                }},
                                body: JSON.stringify(payload)
                            }})
                            .then(resp => resp.blob())
                            .then(blob => {{
                                const link = document.createElement('a');
                                link.href = window.URL.createObjectURL(blob);
                                link.download = payload.filename + '.xlsx';
                                link.click();
                            }})
                            .catch(err => alert('Export failed: ' + err));
                        }}
                        </script>");

                return (true, $"Success", html.ToString());
            }
            catch (Exception ex)
            {
                // Log error if needed (_logger.LogError(ex, "DB error"))
                return (false, $"Error: {ex.Message}", "");
            }
        }


        private string Email() => "Hello Email";
    }
}
