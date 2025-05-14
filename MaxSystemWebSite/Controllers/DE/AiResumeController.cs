using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSystemWebSite.Controllers.MM;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace MaxSystemWebSite.Controllers.DE
{
    public class AiResumeController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<AiResumeController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IDapper_Oracle _dapper_Oracle;
        private readonly UserProfileService _userProfileService;
        private readonly ISharePoint _sharePoint;
        private readonly IHttpClientFactory _clientFactory;

        public AiResumeController(ILogger<AiResumeController> logger, IConfiguration configuration, IWebApi webApi,
           IDapper dapper, IAuthenticator authenticator, UserProfileService userProfileService, ISharePoint sharePoint, IHttpClientFactory clientFactory)
            : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _userProfileService = userProfileService;
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {

            return View();
        }
        public IActionResult History()
        {
            return View();
        }

        public IActionResult ShortListed()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadProxy([FromForm] IFormFile file, [FromForm] string jobDesc)
        {
            if (file == null || string.IsNullOrWhiteSpace(jobDesc))
            {
                return BadRequest("Missing file or job description");
            }

            var client = _clientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            // ✅ Correct field name for API
            content.Add(new StringContent(jobDesc), "job_desc");

            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = file.FileName
            };
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync("https://airesumeapi-amfpg4anayejfbdx.southeastasia-01.azurewebsites.net/evaluate-resume", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            return Content(responseBody, "application/json");
        }

    }
}
