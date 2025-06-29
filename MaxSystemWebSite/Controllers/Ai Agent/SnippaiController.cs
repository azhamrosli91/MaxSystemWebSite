﻿using BaseSQL.Interface;
using BaseWebApi.Interface;
using Dapper;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Wordprocessing;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSys.Models;
using MaxSystemWebSite.Controllers.PMO;
using MaxSystemWebSite.Models.MCP;
using MaxSystemWebSite.Models.SETTING;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartTemplateCore.Models.Common;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
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
        private readonly IEmail _emailService;



        public SnippaiController(ILogger<SnippaiController> logger, IConfiguration configuration, IWebApi webApi,
           IDapper dapper, IAuthenticator authenticator, UserProfileService userProfileService, IEmail emailService)
            : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }
        // Extract of updated SnippaiController with streaming + code-box rendering
        // Extract of updated SnippaiController with streaming + code-box rendering + MCP execution
        //[HttpPost]
        //public async Task Chatbot([FromBody] SnippaiChatRequest request)
        //{
        //    Response.ContentType = "text/plain";
        //    Response.Headers.Add("Cache-Control", "no-cache");
        //    Response.Headers.Add("X-Accel-Buffering", "no");

        //    string apiKey = _configuration["ChatGPT:SecretKey"];
        //    string assistantId = _configuration["ChatGPT:AssistantId"];
        //    string threadId = request.TheadID;

        //    // Step 1: Try from cookie
        //    if (string.IsNullOrWhiteSpace(threadId) && Request.Cookies.ContainsKey("chat_thread_id"))
        //    {
        //        threadId = Request.Cookies["chat_thread_id"];
        //    }

        //    // Step 2: If still null or empty, create new thread and store in cookie
        //    if (string.IsNullOrWhiteSpace(threadId))
        //    {
        //        threadId = await CreateNewThread(apiKey);

        //        // Set cookie to expire in 7 days
        //        Response.Cookies.Append("chat_thread_id", threadId, new CookieOptions
        //        {
        //            HttpOnly = true,
        //            Secure = true,
        //            SameSite = SameSiteMode.Lax,
        //            Expires = DateTimeOffset.UtcNow.AddDays(7)
        //        });
        //    }

        //    string userMessage = request.Messages.LastOrDefault()?.content?.Trim();
        //    if (string.IsNullOrWhiteSpace(userMessage))
        //    {
        //        await Response.WriteAsync("\u274C No user message found.");
        //        await Response.Body.FlushAsync();
        //        return;
        //    }

        //    await PostUserMessageToThread(threadId, userMessage, apiKey);
        //    string runId = await TriggerRun(threadId, assistantId, apiKey);

        //    var client = CreateHttpClientWithProxy(apiKey);
        //    while (true)
        //    {
        //        var response = await client.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
        //        var json = await response.Content.ReadAsStringAsync();
        //        dynamic result = JsonConvert.DeserializeObject(json);
        //        string status = result.status;

        //        if (status == "completed") break;
        //        if (status == "failed" || status == "cancelled")
        //        {
        //            await Response.WriteAsync($"\u274C Assistant run ended with status: {status}");
        //            await Response.Body.FlushAsync();
        //            return;
        //        }

        //        await Task.Delay(1000);
        //    }

        //    var streamResponse = await client.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages", HttpCompletionOption.ResponseHeadersRead);
        //    var streamJson = await streamResponse.Content.ReadAsStringAsync();
        //    dynamic messageList = JsonConvert.DeserializeObject(streamJson);

        //    string lastContent = "";
        //    foreach (var message in messageList.data)
        //    {
        //        if ((string)message.role == "assistant")
        //        {
        //            lastContent = message.content[0].text.value;
        //            break;
        //        }
        //    }


        //    if (!string.IsNullOrWhiteSpace(lastContent))
        //    {
        //        bool insideCode = false;
        //        string codeLang = "plaintext";
        //        var codeBuilder = new StringBuilder();
        //        var lines = lastContent.Split("\n");
        //        foreach (var line in lines)
        //        {
        //            if (line.TrimStart().StartsWith("```"))
        //            {
        //                if (!insideCode)
        //                {
        //                    codeLang = line.Trim().Length > 3 ? line.Trim().Substring(3) : "plaintext";
        //                    insideCode = true;
        //                    await Response.WriteAsync($"<div class=\"code-box\"><div class=\"code-box-header\"><div class=\"title\">{codeLang}</div><div class=\"buttons\"><button class=\"btn-copy\">Copy</button></div></div><pre><code class=\"language-{codeLang}\">");
        //                }
        //                else
        //                {
        //                    insideCode = false;
        //                    await Response.WriteAsync(WebUtility.HtmlEncode(codeBuilder.ToString()));
        //                    await Response.WriteAsync("</code></pre></div>");
        //                    codeBuilder.Clear();
        //                }
        //                continue;
        //            }

        //            if (insideCode)
        //                codeBuilder.AppendLine(line);
        //            else
        //            {
        //                await Response.WriteAsync(WebUtility.HtmlEncode(line) + "<br/>");
        //            }
        //            await Response.Body.FlushAsync();
        //        }

        //        // === MCP Execution from Assistant Response ===
        //        if (lastContent.Trim().StartsWith("{"))
        //        {
        //            try
        //            {
        //                dynamic mcp = JsonConvert.DeserializeObject(lastContent);
        //                string action = mcp?.action;
        //                string connStrRaw = mcp?.connection_string;
        //                string connStr = ExtractConnectionStringFromHtml(connStrRaw);

        //                if (string.IsNullOrWhiteSpace(action))
        //                {
        //                    await Response.WriteAsync("❌ Missing `action` in MCP payload.");
        //                    await Response.Body.FlushAsync();
        //                    return;
        //                }

        //                if (string.IsNullOrWhiteSpace(connStr))
        //                {
        //                    await Response.WriteAsync("🔐 Please provide a valid SQL Server `connection_string`.");
        //                    await Response.Body.FlushAsync();
        //                    return;
        //                }

        //                using var connection = new SqlConnection(connStr);
        //                await connection.OpenAsync();

        //                if (action == "DB_QUERY_EXECUTE")
        //                {
        //                    string sql = mcp.sql;
        //                    var result = await connection.QueryAsync(sql);
        //                    var rows = result.ToList();
        //                    if (rows.Count == 0)
        //                    {
        //                        await Response.WriteAsync("<p><i>No data returned.</i></p>");
        //                        await Response.Body.FlushAsync();
        //                        return;
        //                    }

        //                    var html = new StringBuilder();
        //                    html.Append(@"<div class='code-box'><div class='code-box-header'><div class='title'>table</div><div class='buttons'><button onclick='downloadExcel()' class='btn-export'><i class='fas fa-file-export'></i> Export</button></div></div><div class='table-responsive' style='max-height:400px; overflow:auto; padding:10px;'><table class='table table-bordered table-sm mb-0'><thead class='table-light'><tr>");

        //                    foreach (var col in ((IDictionary<string, object>)rows[0]).Keys)
        //                    {
        //                        html.Append($"<th>{WebUtility.HtmlEncode(col)}</th>");
        //                    }
        //                    html.Append("</tr></thead><tbody>");

        //                    foreach (var row in rows)
        //                    {
        //                        html.Append("<tr>");
        //                        foreach (var val in (IDictionary<string, object>)row)
        //                        {
        //                            html.Append($"<td>{WebUtility.HtmlEncode(val.Value?.ToString() ?? "")}</td>");
        //                        }
        //                        html.Append("</tr>");
        //                    }

        //                    html.Append(@"</tbody></table></div></div>");
        //                    await Response.WriteAsync(html.ToString());
        //                    await Response.Body.FlushAsync();
        //                    return;
        //                }

        //                if (action == "DB_INSERT_UPDATE")
        //                {
        //                    string spName = mcp.sp_name;
        //                    var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcp.@params.ToString());
        //                    var parameters = new DynamicParameters();
        //                    foreach (var pair in paramDict)
        //                    {
        //                        parameters.Add(pair.Key, pair.Value);
        //                    }
        //                    var result = await connection.QueryAsync(spName, parameters, commandType: CommandType.StoredProcedure);
        //                    string json = JsonConvert.SerializeObject(result, Formatting.Indented);
        //                    await Response.WriteAsync($"<div class='code-box'><div class='code-box-header'><div class='title'>json</div><div class='buttons'><button class='btn-copy'>Copy</button></div></div><pre><code class='language-json'>{WebUtility.HtmlEncode(json)}</code></pre></div>");
        //                    await Response.Body.FlushAsync();
        //                    return;
        //                }

        //                await Response.WriteAsync("⚠ Unknown action provided in MCP payload.");
        //                await Response.Body.FlushAsync();
        //                return;
        //            }
        //            catch (Exception ex)
        //            {
        //                await Response.WriteAsync($"❌ MCP Error: {WebUtility.HtmlEncode(ex.Message)}");
        //                await Response.Body.FlushAsync();
        //                return;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        await Response.WriteAsync("⚠️ No assistant reply found.");
        //    }

        //    await Response.Body.FlushAsync();
        //}

        private HttpClient CreateHttpClientWithProxy(string apiKey)
        {
            string proxyAddr = _configuration["Proxy:Address"];
            string proxyUser = _configuration["Proxy:Username"];
            string proxyPass = _configuration["Proxy:Password"];
            string isRequireProxy = _configuration["Proxy:IsRequireProxy"];

            HttpClientHandler handler;

            if (!string.IsNullOrWhiteSpace(proxyAddr) && isRequireProxy == "true")
            {
                handler = new HttpClientHandler
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
            }
            else
            {
                handler = new HttpClientHandler();
            }

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // ✅ Required for Assistants API
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
           // client.DefaultRequestHeaders.Add("Content-Type","application /json");

            return client;
        }
        private async Task<string> CreateNewThread(string apiKey)
        {
            var client = CreateHttpClientWithProxy(apiKey);
            var response = await client.PostAsync("https://api.openai.com/v1/threads", null);
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json);
            return result.id;
        }

        [HttpPost]
        public async Task<IActionResult> StartNewConversation()
        {
            string old_thread = Request.Cookies["chat_thread_id"];
            string apiKey = _configuration["ChatGPT:SecretKey"];
            var threadId = await CreateNewThread(apiKey);

            // Set cookie here (this is correct)
            Response.Cookies.Append("chat_thread_id", threadId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Json(new { threadId, old_thread });
        }

        [HttpPost]
        public async Task WebSearch([FromBody] WebSearchRequest request)
        {
            string apiKey = _configuration["ChatGPT:SecretKey"];
            string url = request.Query;

            if (string.IsNullOrWhiteSpace(url))
            {
                Response.ContentType = "text/plain";
                await Response.WriteAsync("Invalid or missing URL.");
                await Response.Body.FlushAsync();
                return;
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
                input = new[] { new { role = "user", content = url } },
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
                Response.ContentType = "text/plain";
                await Response.WriteAsync($"HTTP Request Error: {ex.Message}");
                await Response.Body.FlushAsync();
                return;
            }

            if (apiResponse == null)
            {
                Response.ContentType = "text/plain";
                await Response.WriteAsync("No response received.");
                await Response.Body.FlushAsync();
                return;
            }

            string responseText = await apiResponse.Content.ReadAsStringAsync();

            ApiResponse emailParams = JsonConvert.DeserializeObject<ApiResponse>(responseText);

            string extractedText = emailParams.Output?
                .FirstOrDefault(o => o.Type == "message")?
                .Content?
                .FirstOrDefault(c => c.Type == "output_text")?
                .Text;

            Response.ContentType = "text/html";

            if (string.IsNullOrEmpty(extractedText))
            {
                await Response.WriteAsync(extractedText);
            }
            else
            {
                await Response.WriteAsync("No output text found.");
            }

            await Response.Body.FlushAsync();
        }

        [HttpPost]
        [Route("Snippai/post-assistant-message")]
        public IActionResult PostAssistantMessageToThread([FromBody] string message)
        {
            string threadId = null;

            if (Request.Cookies.ContainsKey("chat_thread_id"))
            {
                threadId = Request.Cookies["chat_thread_id"];
            }

            _threadMessages[threadId].Add(new MessageChatBot("assistant", message));

            return Ok(new { status = "Message added" });
        }

        private async Task PostUserMessageToThread(string threadId, string message, string apiKey)
        {
            var client = CreateHttpClientWithProxy(apiKey);
            var payload = new { role = "user", content = message };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            await client.PostAsync($"https://api.openai.com/v1/threads/{threadId}/messages", content);
        }

        private async Task<string> TriggerRun(string threadId, string assistantId, string apiKey)
        {
            var client = CreateHttpClientWithProxy(apiKey);
            var payload = new { assistant_id = assistantId };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs", content);
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json);
            return result.id;
        }

        private async Task<string> PollRunUntilCompleted(string threadId, string runId, string apiKey)
        {
            var client = CreateHttpClientWithProxy(apiKey);

            while (true)
            {
                var response = await client.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode || !contentType?.Contains("json") == true)
                {
                    _logger.LogError("Invalid response from OpenAI: {StatusCode} | Body: {Body}", response.StatusCode, json);
                    throw new Exception("Invalid response from OpenAI: " + json);
                }

                dynamic result = JsonConvert.DeserializeObject(json);
                string status = result.status;

                if (status == "completed") return "completed";
                if (status == "failed" || status == "cancelled") return status;

                await Task.Delay(1000);
            }
        }


        private async Task<string> GetLatestAssistantMessage(string threadId, string apiKey)
        {
            var client = CreateHttpClientWithProxy(apiKey);
            var response = await client.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json);

            foreach (var message in result.data)
            {
                if ((string)message.role == "assistant")
                    return message.content[0].text.value;
            }

            return "⚠️ No assistant reply found.";
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

            //string threadId = request.TheadID ?? Guid.NewGuid().ToString();
            string threadId = request.TheadID;

            // Step 1: Try from cookie
            if (string.IsNullOrWhiteSpace(threadId) && Request.Cookies.ContainsKey("chat_thread_id"))
            {
                threadId = Request.Cookies["chat_thread_id"];
            }

            // Step 2: If still null or empty, create new thread and store in cookie
            if (string.IsNullOrWhiteSpace(threadId))
            {
                threadId = await CreateNewThread(apiKey);

                // Set cookie to expire in 7 days
                Response.Cookies.Append("chat_thread_id", threadId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }
            if (!_threadMessages.ContainsKey(threadId))
            {
                _threadMessages[threadId] = new List<MessageChatBot>
                {
                    new MessageChatBot("system", systemPrompt),
                    new MessageChatBot("user", "my name is "+ NAME +" and my email is "+ EMAIL)
                };
            }

            var transformedMessages = new List<MessageChatBot>();
            foreach (var msg in request.Messages)
            {
                if (!string.IsNullOrWhiteSpace(msg.content) && IsBase64Image(msg.content.Trim(), out string mimeType))
                {
                    transformedMessages.Add(new MessageChatBot("user", JsonConvert.SerializeObject(new
                    {
                        type = "input_image",
                        image_url = $"data:{mimeType};base64,{msg.content.Trim()}",
                         detail= "high"
                    })));
                }
                else
                {
                    transformedMessages.Add(msg);
                }
            }

            _threadMessages[threadId].AddRange(transformedMessages);
            //_threadMessages[threadId].AddRange(request.Messages);

            // === MCP Instruction Execution ===
            //var latestMessage = request.Messages.LastOrDefault()?.content?.Trim();
            //if (!string.IsNullOrWhiteSpace(latestMessage) && latestMessage.StartsWith("{"))
            //{
            //    try
            //    {
            //        dynamic mcp = JsonConvert.DeserializeObject(latestMessage);
            //        string action = mcp?.action;
            //        //string connStr = mcp?.connection_string;
            //        string connStrRaw = mcp?.connection_string;
            //        string connStr = ExtractConnectionStringFromHtml(connStrRaw);

            //        if (string.IsNullOrWhiteSpace(action))
            //        {
            //            await Response.WriteAsync("❌ Missing `action` in MCP payload.");
            //            await Response.Body.FlushAsync();
            //            return;
            //        }

            //        if (string.IsNullOrWhiteSpace(connStr))
            //        {
            //            await Response.WriteAsync("🔐 Please provide a valid SQL Server `connection_string` before executing.");
            //            await Response.Body.FlushAsync();
            //            return;
            //        }

            //        using var connection = new SqlConnection(connStr);
            //        await connection.OpenAsync();

            //        if (action == "DB_QUERY_EXECUTE")
            //        {
            //            string sql = mcp.sql;
            //            var result = await connection.QueryAsync(sql);
            //            string json = JsonConvert.SerializeObject(result, Formatting.Indented);



            //            await Response.WriteAsync($@"<pre><code class=""language-json"">{WebUtility.HtmlEncode(json)}</code></pre>");

            //            await Response.Body.FlushAsync();
            //            return;
            //        }

            //        if (action == "DB_INSERT_UPDATE")
            //        {
            //            string spName = mcp.sp_name;
            //            var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcp.@params.ToString());

            //            var parameters = new DynamicParameters();
            //            foreach (var pair in paramDict)
            //            {
            //                parameters.Add(pair.Key, pair.Value);
            //            }

            //            var result = await connection.QueryAsync(spName, parameters, commandType: CommandType.StoredProcedure);

            //            string json = JsonConvert.SerializeObject(result, Formatting.Indented);

            //            await Response.WriteAsync($@"<pre><code class=""language-json"">{WebUtility.HtmlEncode(json)}</code></pre>");
            //            await Response.Body.FlushAsync();
            //            return;
            //        }


            //        await Response.WriteAsync("⚠ Unknown action provided in MCP payload.");
            //        await Response.Body.FlushAsync();
            //        return;
            //    }
            //    catch (Exception ex)
            //    {
            //        await Response.WriteAsync($"❌ MCP Error: {WebUtility.HtmlEncode(ex.Message)}");
            //        await Response.Body.FlushAsync();
            //        return;
            //    }
            //}

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
            var handler = new HttpClientHandler();
            //var handler = string.IsNullOrWhiteSpace(proxyAddr)
            //    ? new HttpClientHandler()
            //    : new HttpClientHandler
            //    {
            //        Proxy = new WebProxy
            //        {
            //            Address = new Uri(proxyAddr),
            //            BypassProxyOnLocal = false,
            //            UseDefaultCredentials = false,
            //            Credentials = new NetworkCredential(proxyUser, proxyPass)
            //        },
            //        UseProxy = true
            //    };

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
            bool isMCP = false;
            StringBuilder codeBuffer = new();
            StringBuilder fullLineBuffer = new();
            string currentLang = "plaintext";
            string pendingBacktick = "";

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                var jsonLine = line.Substring("data: ".Length);
               
                if (jsonLine == "[DONE]") 
                {
                    try
                    {
                        var aiReplyText = fullAssistantReply.ToString().Trim();

                        //if (aiReplyText.StartsWith("[MCP]"))
                        //{
                        //    isMCP = false;
                        //    dynamic mcp = JsonConvert.DeserializeObject(aiReplyText.Replace("[MCP]",""));
                        //    string action = mcp?.action;

                        //    if (string.IsNullOrWhiteSpace(action))
                        //    {
                        //        await Response.WriteAsync("❌ MCP action missing from ChatGPT output.");
                        //        await Response.Body.FlushAsync();
                        //        return;
                        //    }



                        //    if (action == "DB_QUERY_EXECUTE")
                        //    {
                        //        string connStr = mcp?.connection_string;

                        //        if (string.IsNullOrWhiteSpace(connStr))
                        //        {
                        //            await Response.WriteAsync("🔐 MCP request from ChatGPT requires `connection_string`.");
                        //            await Response.Body.FlushAsync();
                        //            return;
                        //        }


                        //        using var connection = new SqlConnection(connStr);
                        //        await connection.OpenAsync();

                        //        string sql = mcp.sql;
                        //        var result = await connection.QueryAsync(sql);
                        //        string json = JsonConvert.SerializeObject(result, Formatting.Indented);

                        //        var rows = result.ToList();
                        //        if (rows.Count == 0)
                        //        {
                        //            await Response.WriteAsync("<p><i>No data returned.</i></p>");
                        //            await Response.Body.FlushAsync();
                        //            return;
                        //        }

                        //        // Build HTML table
                        //        var html = new StringBuilder();
                        //        // Start code-box wrapper
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



                        //        // Get headers from first row
                        //        foreach (var col in ((IDictionary<string, object>)rows[0]).Keys)
                        //        {
                        //            html.Append($"<th>{WebUtility.HtmlEncode(col)}</th>");
                        //        }
                        //        html.Append("</tr></thead><tbody>");

                        //        foreach (var row in rows)
                        //        {
                        //            html.Append("<tr>");
                        //            foreach (var val in (IDictionary<string, object>)row)
                        //            {
                        //                html.Append($"<td>{WebUtility.HtmlEncode(val.Value?.ToString() ?? "")}</td>");
                        //            }
                        //            html.Append("</tr>");
                        //        }

                        //        html.Append(@"
                        //              </tbody>
                        //            </table>
                        //          </div>
                        //        </div>

                        //        <script>
                        //        function downloadExcel() {
                        //            const payload = {
                        //                filename: 'MCP_Result',
                        //                data: " + JsonConvert.SerializeObject(rows) + @"
                        //            };

                        //            fetch('/Snippai/ExportExcel', {
                        //                method: 'POST',
                        //                headers: {
                        //                    'Content-Type': 'application/json'
                        //                },
                        //                body: JSON.stringify(payload)
                        //            })
                        //            .then(resp => resp.blob())
                        //            .then(blob => {
                        //                const link = document.createElement('a');
                        //                link.href = window.URL.createObjectURL(blob);
                        //                link.download = payload.filename + '.xlsx';
                        //                link.click();
                        //            })
                        //            .catch(err => alert('Export failed: ' + err));
                        //        }
                        //        </script>");


                        //        await Response.WriteAsync(html.ToString());
                        //        await Response.Body.FlushAsync();
                        //        return;
                        //    }

                        //    if (action == "DB_INSERT_UPDATE")
                        //    {
                        //        string spName = mcp.sp_name;
                        //        var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcp.@params.ToString());

                        //        var parameters = new DynamicParameters();
                        //        foreach (var pair in paramDict)
                        //        {
                        //            parameters.Add(pair.Key, pair.Value);
                        //        }

                        //        string connStr = mcp?.connection_string;

                        //        if (string.IsNullOrWhiteSpace(connStr))
                        //        {
                        //            await Response.WriteAsync("🔐 MCP request from ChatGPT requires `connection_string`.");
                        //            await Response.Body.FlushAsync();
                        //            return;
                        //        }


                        //        using var connection = new SqlConnection(connStr);
                        //        await connection.OpenAsync();

                        //        var result = await connection.QueryAsync(spName, parameters, commandType: CommandType.StoredProcedure);
                        //        string json = JsonConvert.SerializeObject(result, Formatting.Indented);

                        //        await Response.WriteAsync($@"<pre><code class=""language-json"">{WebUtility.HtmlEncode(json)}</code></pre>");
                        //        await Response.Body.FlushAsync();
                        //        return;
                        //    }

                        //    if (action == "SEND_EMAIL")
                        //    {
                        //        string cleanedJson = aiReplyText.Replace("[MCP]", "");
                        //        JObject jObject = JObject.Parse(cleanedJson);

                        //        JToken paramsToken = jObject["params"];
                        //        MCP_EmailParams emailParams = paramsToken.ToObject<MCP_EmailParams>();
                        //        string SenderEmail = EMAIL;

                        //        List<Recipient> ListReceipt = new List<Recipient>();
                        //        List<Recipient> ListCc = new List<Recipient>();
                        //        List<Recipient> ListBcc = new List<Recipient>();

                        //        if (emailParams != null)
                        //        {
                        //            if (emailParams.Recipient != null)
                        //            {
                        //                foreach (var item in emailParams.Recipient)
                        //                {
                        //                    ListReceipt.Add(new Recipient
                        //                    {
                        //                        EmailAddress = new EmailAddress
                        //                        {
                        //                            Address = item.EmailAddress.Address,
                        //                            Name = item.EmailAddress.Name
                        //                        }
                        //                    });
                        //                }
                        //            }

                        //            if (emailParams.Cc != null)
                        //            {
                        //                foreach (var item in emailParams.Cc)
                        //                {
                        //                    ListCc.Add(new Recipient
                        //                    {
                        //                        EmailAddress = new EmailAddress
                        //                        {
                        //                            Address = item.EmailAddress.Address,
                        //                            Name = item.EmailAddress.Name
                        //                        }
                        //                    });
                        //                }
                        //            }

                        //            if (emailParams.Bcc != null)
                        //            {
                        //                foreach (var item in emailParams.Bcc)
                        //                {
                        //                    ListBcc.Add(new Recipient
                        //                    {
                        //                        EmailAddress = new EmailAddress
                        //                        {
                        //                            Address = item.EmailAddress.Address,
                        //                            Name = item.EmailAddress.Name
                        //                        }
                        //                    });
                        //                }
                        //            }

                        //            var modelTemp = new Emai_TemplateSent
                        //            {
                        //                Recipient = ListReceipt,
                        //                CC = ListCc,
                        //                BCC = ListBcc,
                        //                Subject = emailParams.Subject,
                        //                subTemplate = emailParams.Body,
                        //                WORD_REPLACE = new List<(string ori, string replace)>
                        //        {
                        //            ("[APPLICATION_NAME]", emailParams.Subject),
                        //            ("[EMAIL_SUBJECT]", emailParams.Subject),
                        //            ("[EMAIL_BODY]", emailParams.Body),
                        //            ("[HELP_DESK_EMAIL]", "hr@maxsys.com.my")
                        //        },
                        //                Attachments = new List<Emai_TemplateSent.EmailAttachment>()
                        //            };

                        //            modelTemp.mainTemplate = await modelTemp.EmailBodyTemplate();
                        //            modelTemp.bodyContent = modelTemp.mainTemplate.Replace("[BODY]", modelTemp.subTemplate);
                        //            var wordResult = modelTemp.WordReplacer(modelTemp.bodyContent);
                        //            if (wordResult.Item1)
                        //            {
                        //                modelTemp.bodyContent = wordResult.Item2;
                        //            }
                        //            SETTING_EMAIL settingEmail = new SETTING_EMAIL();

                        //            settingEmail.TENANT_ID = _configuration["Settings:TenantId"];
                        //            settingEmail.CLIENT_ID = _configuration["Settings:ClientId"];
                        //            settingEmail.CLIENT_SECRET = _configuration["Settings:ClientSecret"];
                        //            settingEmail.GRAPH_USER = _configuration.GetSection("Settings:GraphUserScopes").Get<string[]>()[0];



                        //            modelTemp.Setting_Setup = new Setting_Setup();
                        //            modelTemp.Setting_Setup.SMTP_ACCOUNT = SenderEmail;

                        //            _emailService.InitGraph(settingEmail);

                        //            (bool status, string message) result = await _emailService.SendEmailAsync(modelTemp);

                        //            if (!result.status)
                        //            {
                        //                await Response.WriteAsync($"Failed to send email.{result.message}");
                        //            }
                        //            await Response.WriteAsync($"Your Email subject: \n {emailParams.Subject}");
                        //            await Response.WriteAsync($"\nYour Email body: \n {emailParams.Body}");
                        //            await Response.WriteAsync($"\nEmail sent");
                        //            await Response.Body.FlushAsync();
                        //            return;
                        //        }
                        //    }

                        //if (action == "WEB_SEARCH")
                        //{
                        //    string? url = mcp?.query;

                        //    if (string.IsNullOrWhiteSpace(url))
                        //    {
                        //        await Response.WriteAsync("Invalid or missing URL.");
                        //        await Response.Body.FlushAsync();
                        //        return;
                        //    }

                        //    var tools = new[]
                        //        {
                        //            new { type = "web_search_preview" }
                        //            };


                        //    string instructions =
                        //    "You are a web search agent. The user will provide a URL or a query. " +
                        //    "You must perform a web search using the provided input and return a summary of the results. " +
                        //    "The output must be in valid HTML only. Use HTML tags such as <div>, <h1>, <h2>, <p>, <a>, and <ul>. " +
                        //    "Wrap everything in an outer <div> tag. Do not include any explanations or comments outside of the HTML. " +
                        //    "Do not say 'Here's the result' or similar. Just return clean HTML, suitable for embedding directly into a webpage." +
                        //    "Return only the HTML. Do not include any Markdown, plain text, or commentary. Start your response with <div>.";



                        //    var payload = new
                        //    {
                        //        model = "gpt-4.1-mini",
                        //        input = new[]
                        //        {
                        //                new { role = "user", content = url }
                        //            },
                        //        instructions = instructions,
                        //        temperature = temperature,
                        //        tools = tools
                        //    };

                        //    using var httpClient = new HttpClient();
                        //    httpClient.DefaultRequestHeaders.Authorization =
                        //        new AuthenticationHeaderValue("Bearer", apiKey);

                        //    string jsonPayload = JsonConvert.SerializeObject(payload);
                        //    var contentWeb = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        //    HttpResponseMessage apiResponse = null;

                        //    try
                        //    {
                        //        apiResponse = await httpClient.PostAsync("https://api.openai.com/v1/responses", contentWeb);
                        //    }
                        //    catch (HttpRequestException ex)
                        //    {
                        //        await Response.WriteAsync($"HTTP Request Error: {ex.Message}");
                        //        return;
                        //    }

                        //    if (apiResponse == null)
                        //    {
                        //        await Response.WriteAsync("No response received.");
                        //        return;
                        //    }

                        //    string responseText = await apiResponse.Content.ReadAsStringAsync();

                        //    //JObject jObject = JObject.Parse(responseText);
                        //    //JToken paramsToken = jObject["params"];

                        //    ApiResponse emailParams = JsonConvert.DeserializeObject<ApiResponse>(responseText);
                        //    //Console.WriteLine(emailParams);

                        //    string extractedText = emailParams.Output
                        //        ?.FirstOrDefault(o => o.Type == "message")
                        //        ?.Content?
                        //        .FirstOrDefault(c => c.Type == "output_text")
                        //        ?.Text;

                        //    if (!string.IsNullOrEmpty(extractedText))
                        //    {
                        //        await Response.WriteAsync(extractedText);
                        //    }
                        //    else
                        //    {
                        //        await Response.WriteAsync("No output text found.");
                        //    }


                        //    await Response.WriteAsync(extractedText);
                        //    await Response.Body.FlushAsync();
                        //    return;
                        //}

                        //    await Response.WriteAsync("⚠ Unknown MCP action from ChatGPT.");
                        //    await Response.Body.FlushAsync();
                        //}
                        await Response.WriteAsync("!![DONE]!!");
                        await Response.Body.FlushAsync();

                    }
                    catch (Exception ex)
                    {
                        await Response.WriteAsync($"❌ MCP execution error from ChatGPT output: {WebUtility.HtmlEncode(ex.Message)}");
                        await Response.Body.FlushAsync();
                    }

                    break;
                }

                dynamic parsed = JsonConvert.DeserializeObject(jsonLine);
                string content = parsed?.choices[0]?.delta?.content;
                if (string.IsNullOrEmpty(content)) continue;

                fullAssistantReply.Append(content);
                fullLineBuffer.Append(content);

                //if (fullAssistantReply.ToString().Contains("[MCP]"))
                //{
                //    isMCP = true;
                //}
                //            if (isMCP ==false)
                //            { 

                //                //string xx = "";
                //                string combined = fullLineBuffer.ToString();

                //                //if (!insideCodeBlock)
                //                //{
                //                //    pendingBacktick += content;

                //                //    if (pendingBacktick.EndsWith("`")) {
                //                //        continue;
                //                //    }
                //                //    if (pendingBacktick.EndsWith("``"))
                //                //    {
                //                //        continue;
                //                //    }
                //                //    if (pendingBacktick.EndsWith("```"))
                //                //    {
                //                //        continue;
                //                //    }




                //                //    var startIdx = pendingBacktick.ToString().Contains("```");
                //                //    if (startIdx == true)
                //                //    {
                //                //        insideCodeBlock = true;
                //                //        codeBuffer.Clear();

                //                //        //var remaining = pendingBacktick.Substring(startIdx + 6).TrimStart();
                //                //        //var parts = remaining.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                //                //        currentLang =  "plaintext";

                //                //        pendingBacktick = "";
                //                //        fullLineBuffer.Clear();
                //                //        continue;
                //                //    }

                //                //    if (pendingBacktick.Length > 4)
                //                //    {
                //                //        await Response.WriteAsync(pendingBacktick);
                //                //        await Response.Body.FlushAsync();
                //                //        pendingBacktick = "";
                //                //    }
                //                //    else if (insideCodeBlock == false && (!pendingBacktick.Contains("[MCP]") || !pendingBacktick.Contains("[M") 
                //                //        || !pendingBacktick.Contains("[MC") || !pendingBacktick.Contains("[MCP"))) 
                //                //    {
                //                //        await Response.WriteAsync(pendingBacktick);
                //                //        await Response.Body.FlushAsync();
                //                //        pendingBacktick = "";
                //                //    }

                //                //    continue;
                //                //}

                ////                if (insideCodeBlock)
                ////                {
                ////                    codeBuffer.Append(content);

                ////                    if (codeBuffer.ToString().Contains("```"))
                ////                    {
                ////                        var cleanCode = codeBuffer.ToString();
                ////                        var endIndex = cleanCode.IndexOf("```", StringComparison.Ordinal);
                ////                        cleanCode = cleanCode.Substring(0, endIndex);

                ////                        // Match pattern ```||> [|lang|]
                ////                        var match = Regex.Match(fullAssistantReply.ToString(), @"``` *?(?<lang>[^\s]+)");
                ////                        if (match.Success)
                ////                        {
                ////                            currentLang = match.Groups["lang"].Value.Trim().ToLower();
                ////                        }

                ////                        string tmpID = $"tmpbox_{Guid.NewGuid().ToString()}";
                ////                        string codeBoxHtml = $@"
                ////<div class=""code-box"">
                ////  <div class=""code-box-header"">
                ////    <div class=""title"">{currentLang}</div>
                ////    <div class=""buttons""><button class=""btn-copy"">Copy</button></div>
                ////  </div>
                ////  <pre><code class=""language-{currentLang}"" id=""{tmpID}"">{WebUtility.HtmlEncode(cleanCode)}</code></pre>
                ////</div>
                ////<script>
                ////  setTimeout(() => {{
                ////    const codeBlock = document.getElementById('{tmpID}');
                ////    if (codeBlock) hljs.highlightElement(codeBlock);
                ////  }}, 100);
                ////</script>";
                ////                        await Response.WriteAsync(codeBoxHtml);
                ////                        await Response.Body.FlushAsync();


                ////                        codeBuffer.Clear();
                ////                        pendingBacktick = "";
                ////                        insideCodeBlock = false;
                ////                        fullLineBuffer.Clear();
                ////                        continue;
                ////                    }

                ////                    continue;
                ////                }

                //                await Response.WriteAsync(content);
                //                await Response.Body.FlushAsync();
                //                fullLineBuffer.Clear();
                //            }



                await Response.WriteAsync(WebUtility.HtmlEncode(content));
                await Response.Body.FlushAsync();
                fullLineBuffer.Clear();
            }

            _threadMessages[threadId].Add(new MessageChatBot("assistant", fullAssistantReply.ToString()));

            //foreach (var message in _threadMessages[threadId])
            //{
            //    Console.WriteLine($"[{message.role}] {message.content}");
            //}
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
        private string ExtractBase64FromImgTag(string input)
        {
            // Extract src="data:image/...;base64,..." using regex
            var match = Regex.Match(input, @"data:(image\/[a-zA-Z0-9.+-]+);base64,([A-Za-z0-9+/=\r\n]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return $"data:{match.Groups[1].Value};base64,{match.Groups[2].Value.Replace("\n", "").Replace("\r", "").Replace(" ", "")}";
            }

            return null;
        }

        private bool IsBase64Image(string input, out string mimeType)
        {
            mimeType = "image/jpeg"; // default fallback

            if (string.IsNullOrWhiteSpace(input))
                return false;

            try
            {
                // Clean up
                input = input.Trim()
                             .Replace(" ", "")
                             .Replace("\r", "")
                             .Replace("\n", "");
                input = ExtractBase64FromImgTag(input);

                // Extract MIME type and base64 data
                var match = Regex.Match(input, @"^data:(image\/[a-zA-Z0-9.+-]+);base64,(.*)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    mimeType = match.Groups[1].Value;
                    input = match.Groups[2].Value;
                }

              
                // Validate base64 characters
                if (!Regex.IsMatch(input, @"^[a-zA-Z0-9\+/]*={0,2}$"))
                    return false;

                byte[] data = Convert.FromBase64String(input);

                // Check magic bytes
                if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xD8)
                {
                    mimeType = "image/jpeg";
                    return true;
                }
                if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 &&
                    data[2] == 0x4E && data[3] == 0x47)
                {
                    mimeType = "image/png";
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }


    }
}
