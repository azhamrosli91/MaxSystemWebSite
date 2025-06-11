using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Helpers;
using MaxSys.Interface;
using Microsoft.AspNetCore.Mvc;
using SmartTemplateCore.Models.Common;
using System.Text.Encodings.Web;

namespace MaxSystemWebSite.Controllers.Ai_Agent
{
    public class ConnectorController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<ConnectorController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;

        public ConnectorController(ILogger<ConnectorController> logger, IConfiguration configuration, IWebApi webApi,
           IDapper dapper, IAuthenticator authenticator, UserProfileService userProfileService)
            : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
