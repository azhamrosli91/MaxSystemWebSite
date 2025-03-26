using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Interface;
using MaxSys.Models.DE;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Encodings.Web;
using MaxSys.Helpers;
using MaxSys.Interface;

namespace MaxSys.Controllers.Dashboard
{
    [AllowAnonymous]
    public class DashboardController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<DashboardController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IDapper_Oracle _dapper_Oracle;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        public DashboardController(ILogger<DashboardController> logger, IConfiguration configuration, IWebApi webApi,
            IDapper dapper, IJWTToken jWTToken, ISQL sql,
            IDapper_Oracle dapper_Oracle, HtmlEncoder htmlEncoder, IAuthenticator authenticator, IEmailService emailService, IWebHostEnvironment environment)
        : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _jwtToken = jWTToken;
            _SQL = sql;
            _htmlEncoder = htmlEncoder;
            _dapper_Oracle = dapper_Oracle;
            _emailService = emailService;
            _environment = environment;
        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            ViewBag.ID = 2;
            return View();
        }

        public async Task<string> Get(string search = "-")
        {
            try
            {
                if (search == "-")
                {
                    var currentDate = DateTime.Now;
                    search = $"{currentDate.Year}-{currentDate.Month:D2}";
                }
                else if (search == "all")
                {
                    search = "";
                }


                (bool status, string message, List<DE_EMPLOYEE> model) manager = await _dapper.PSP_COMMON_DAPPER<DE_EMPLOYEE>("PSP_DE_EMPLOYEE", System.Data.CommandType.StoredProcedure, new { USER_ID, ID_MM_COMPANY });

                return JsonConvert.SerializeObject(new { success = manager.status, message = manager.message, data = manager.model });

            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, message = ex.Message });
            }

        }
    }
}
