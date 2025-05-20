using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSystemWebSite.Controllers.PMO;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartTemplateCore.Models.Common;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using Dapper;
using System.Text.RegularExpressions;

namespace MaxSystemWebSite.Controllers.Ai_Agent
{
    public class SnippaiController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<SnippaiController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private static Dictionary<string, List<MessageChatBot>> _threadMessages = new();


        public SnippaiController(ILogger<SnippaiController> logger, IConfiguration configuration, IWebApi webApi,
           IDapper dapper, IAuthenticator authenticator, UserProfileService userProfileService)
            : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task Chatbot([FromBody] SnippaiChatRequest request)
        {
            Response.ContentType = "text/plain";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no"); // for nginx streaming

            string apiKey = _configuration["ChatGPT:SecretKey"];
            string modelType = "gpt-4.1-mini";
            double temperature = double.TryParse(_configuration["ChatGPT:Temperature"], out var value) ? value : 0.7;

            string promptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template", "SnippaiPrompt.txt");
            string systemPrompt = System.IO.File.Exists(promptPath)
                ? await System.IO.File.ReadAllTextAsync(promptPath)
                : "You are a helpful assistant.";

            string threadId = request.TheadID ?? Guid.NewGuid().ToString();

            if (!_threadMessages.ContainsKey(threadId))
            {
                _threadMessages[threadId] = new List<MessageChatBot>
        {
            new MessageChatBot("system", systemPrompt)
        };
            }

            _threadMessages[threadId].AddRange(request.Messages);

            // === MCP Instruction Execution ===
            var latestMessage = request.Messages.LastOrDefault()?.content?.Trim();
            if (!string.IsNullOrWhiteSpace(latestMessage) && latestMessage.StartsWith("{"))
            {
                try
                {
                    dynamic mcp = JsonConvert.DeserializeObject(latestMessage);
                    string action = mcp?.action;
                    //string connStr = mcp?.connection_string;
                    string connStrRaw = mcp?.connection_string;
                    string connStr = ExtractConnectionStringFromHtml(connStrRaw);

                    if (string.IsNullOrWhiteSpace(action))
                    {
                        await Response.WriteAsync("❌ Missing `action` in MCP payload.");
                        await Response.Body.FlushAsync();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(connStr))
                    {
                        await Response.WriteAsync("🔐 Please provide a valid SQL Server `connection_string` before executing.");
                        await Response.Body.FlushAsync();
                        return;
                    }

                    using var connection = new SqlConnection(connStr);
                    await connection.OpenAsync();

                    if (action == "DB_QUERY_EXECUTE")
                    {
                        string sql = mcp.sql;
                        var result = await connection.QueryAsync(sql);
                        string json = JsonConvert.SerializeObject(result, Formatting.Indented);



                        await Response.WriteAsync($@"<pre><code class=""language-json"">{WebUtility.HtmlEncode(json)}</code></pre>");

                        await Response.Body.FlushAsync();
                        return;
                    }

                    if (action == "DB_INSERT_UPDATE")
                    {
                        string spName = mcp.sp_name;
                        var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcp.@params.ToString());

                        var parameters = new DynamicParameters();
                        foreach (var pair in paramDict)
                        {
                            parameters.Add(pair.Key, pair.Value);
                        }

                        var result = await connection.QueryAsync(spName, parameters, commandType: CommandType.StoredProcedure);

                        string json = JsonConvert.SerializeObject(result, Formatting.Indented);

                        await Response.WriteAsync($@"<pre><code class=""language-json"">{WebUtility.HtmlEncode(json)}</code></pre>");
                        await Response.Body.FlushAsync();
                        return;
                    }

                    await Response.WriteAsync("⚠ Unknown action provided in MCP payload.");
                    await Response.Body.FlushAsync();
                    return;
                }
                catch (Exception ex)
                {
                    await Response.WriteAsync($"❌ MCP Error: {WebUtility.HtmlEncode(ex.Message)}");
                    await Response.Body.FlushAsync();
                    return;
                }
            }

            // === Fallback: OpenAI ChatGPT Streaming ===
            var body = new
            {
                model = modelType,
                messages = _threadMessages[threadId],
                temperature = temperature,
                stream = true
            };

            string proxyAddr = _configuration["Proxy:Address"];
            string proxyUser = _configuration["Proxy:Username"];
            string proxyPass = _configuration["Proxy:Password"];

            var handler = string.IsNullOrWhiteSpace(proxyAddr)
                ? new HttpClientHandler()
                : new HttpClientHandler
                {
                    Proxy = new WebProxy
                    {
                        Address = new Uri(proxyAddr),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(proxyUser, proxyPass)
                    },
                    UseProxy = true
                };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var fullAssistantReply = new StringBuilder();
            bool insideCodeBlock = false;
            StringBuilder codeBuffer = new();
            StringBuilder fullLineBuffer = new();
            string currentLang = "plaintext";
            string pendingBacktick = "";

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                var jsonLine = line.Substring("data: ".Length);
                if (jsonLine == "[DONE]") break;

                dynamic parsed = JsonConvert.DeserializeObject(jsonLine);
                string content = parsed?.choices[0]?.delta?.content;
                if (string.IsNullOrEmpty(content)) continue;

                fullAssistantReply.Append(content);
                fullLineBuffer.Append(content);
                string combined = fullLineBuffer.ToString();

                if (!insideCodeBlock)
                {
                    pendingBacktick += content;
                    if (pendingBacktick.EndsWith("```"))
                    {
                        insideCodeBlock = true;
                        codeBuffer.Clear();

                        var afterBacktick = pendingBacktick.Length > 3 ? pendingBacktick.Substring(3).TrimStart() : "";
                        var parts = afterBacktick.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        currentLang = parts.Length > 0 && parts[0].Length <= 15 && !parts[0].Contains(" ")
                            ? parts[0].Trim().ToLower()
                            : "plaintext";

                        pendingBacktick = "";
                        fullLineBuffer.Clear();
                        continue;
                    }
                    else if (pendingBacktick.Length > 5)
                    {
                        await Response.WriteAsync(pendingBacktick);
                        await Response.Body.FlushAsync();
                        pendingBacktick = "";
                    }
                    continue;
                }

                if (insideCodeBlock)
                {
                    codeBuffer.Append(content);
                    if (codeBuffer.ToString().EndsWith("```"))
                    {
                        var cleanCode = codeBuffer.ToString();
                        cleanCode = cleanCode.Substring(0, cleanCode.Length - 3);

                        await Response.WriteAsync($@"
<div class=""code-box"">
  <div class=""code-box-header"">
    <div class=""title"">{currentLang}</div>
    <div class=""buttons""><button class=""btn-copy"">Copy</button></div>
  </div>
  <pre><code class=""language-{currentLang}"">{WebUtility.HtmlEncode(cleanCode)}</code></pre>
</div>");
                        await Response.Body.FlushAsync();

                        codeBuffer.Clear();
                        insideCodeBlock = false;
                        fullLineBuffer.Clear();
                        continue;
                    }
                    continue;
                }

                await Response.WriteAsync(content);
                await Response.Body.FlushAsync();
                fullLineBuffer.Clear();
            }

            _threadMessages[threadId].Add(new MessageChatBot("assistant", fullAssistantReply.ToString()));
            try
            {
                var aiReplyText = fullAssistantReply.ToString().Trim();

                if (aiReplyText.StartsWith("{"))
                {
                    dynamic mcp = JsonConvert.DeserializeObject(aiReplyText);
                    string action = mcp?.action;
                    string connStr = mcp?.connection_string;

                    if (string.IsNullOrWhiteSpace(action))
                    {
                        await Response.WriteAsync("❌ MCP action missing from ChatGPT output.");
                        await Response.Body.FlushAsync();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(connStr))
                    {
                        await Response.WriteAsync("🔐 MCP request from ChatGPT requires `connection_string`.");
                        await Response.Body.FlushAsync();
                        return;
                    }

                    using var connection = new SqlConnection(connStr);
                    await connection.OpenAsync();

                    if (action == "DB_QUERY_EXECUTE")
                    {
                        string sql = mcp.sql;
                        var result = await connection.QueryAsync(sql);
                        string json = JsonConvert.SerializeObject(result, Formatting.Indented);

                        var rows = result.ToList();
                        if (rows.Count == 0)
                        {
                            await Response.WriteAsync("<p><i>No data returned.</i></p>");
                            await Response.Body.FlushAsync();
                            return;
                        }

                        // Build HTML table
                        var html = new StringBuilder();
                        // Start code-box wrapper
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



                        // Get headers from first row
                        foreach (var col in ((IDictionary<string, object>)rows[0]).Keys)
                        {
                            html.Append($"<th>{WebUtility.HtmlEncode(col)}</th>");
                        }
                        html.Append("</tr></thead><tbody>");

                        foreach (var row in rows)
                        {
                            html.Append("<tr>");
                            foreach (var val in (IDictionary<string, object>)row)
                            {
                                html.Append($"<td>{WebUtility.HtmlEncode(val.Value?.ToString() ?? "")}</td>");
                            }
                            html.Append("</tr>");
                        }

                        html.Append(@"
                              </tbody>
                            </table>
                          </div>
                        </div>

                        <script>
                        function downloadExcel() {
                            const payload = {
                                filename: 'MCP_Result',
                                data: " + JsonConvert.SerializeObject(rows) + @"
                            };

                            fetch('/Snippai/ExportExcel', {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json'
                                },
                                body: JSON.stringify(payload)
                            })
                            .then(resp => resp.blob())
                            .then(blob => {
                                const link = document.createElement('a');
                                link.href = window.URL.createObjectURL(blob);
                                link.download = payload.filename + '.xlsx';
                                link.click();
                            })
                            .catch(err => alert('Export failed: ' + err));
                        }
                        </script>");


                        await Response.WriteAsync(html.ToString());
                        await Response.Body.FlushAsync();
                        return;
                    }

                    if (action == "DB_INSERT_UPDATE")
                    {
                        string spName = mcp.sp_name;
                        var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcp.@params.ToString());

                        var parameters = new DynamicParameters();
                        foreach (var pair in paramDict)
                        {
                            parameters.Add(pair.Key, pair.Value);
                        }

                        var result = await connection.QueryAsync(spName, parameters, commandType: CommandType.StoredProcedure);
                        string json = JsonConvert.SerializeObject(result, Formatting.Indented);

                        await Response.WriteAsync($@"<pre><code class=""language-json"">{WebUtility.HtmlEncode(json)}</code></pre>");
                        await Response.Body.FlushAsync();
                        return;
                    }

                    await Response.WriteAsync("⚠ Unknown MCP action from ChatGPT.");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"❌ MCP execution error from ChatGPT output: {WebUtility.HtmlEncode(ex.Message)}");
                await Response.Body.FlushAsync();
            }

        }
        public static string ExtractConnectionStringFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";

            // Remove all HTML tags
            string raw = Regex.Replace(html, "<.*?>", "");

            // Decode HTML entities if any (&nbsp;, &quot;, etc.)
            string decoded = WebUtility.HtmlDecode(raw);

            // Optional: trim to clean up spaces or line breaks
            return decoded.Trim();
        }
        [HttpPost]
        public IActionResult ExportExcel([FromBody] ExcelExportRequest request)
        {
            var data = request.Data ?? new List<Dictionary<string, object>>();
            var filename = string.IsNullOrWhiteSpace(request.Filename) ? "MCP_Result" : request.Filename;

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Export");

            if (data.Count > 0)
            {
                var headers = data[0].Keys.ToList();
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                for (int r = 0; r < data.Count; r++)
                {
                    var row = data[r];
                    for (int c = 0; c < headers.Count; c++)
                    {
                        var value = row[headers[c]];
                        worksheet.Cell(r + 2, c + 1).Value = value?.ToString() ?? "";
                    }
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{filename}.xlsx");
        }


    }
}
