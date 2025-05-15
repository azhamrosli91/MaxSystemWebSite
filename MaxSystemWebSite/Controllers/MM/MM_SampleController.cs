using Base.Model;
using BaseSQL.Interface;
using BaseWebApi.Interface;
using Dapper;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSystemWebSite.Models.EMAIL;
using MaxSystemWebSite.Models.MM;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System.Data;
using System.Text.Encodings.Web;
using System.Web.Helpers;

namespace MaxSystemWebSite.Controllers.MM
{
    public class MM_SampleController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<MM_SampleController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IDapper_Oracle _dapper_Oracle;
        private readonly UserProfileService _userProfileService;
        private readonly ISharePoint _sharePoint;

        public MM_SampleController(ILogger<MM_SampleController> logger, IConfiguration configuration, IWebApi webApi,
           IDapper dapper, IAuthenticator authenticator, UserProfileService userProfileService, ISharePoint sharePoint)
       : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _userProfileService = userProfileService;
            _sharePoint = sharePoint;
        }

        public IActionResult SampleForm()
        {
            MM_SAMPLE Model = new MM_SAMPLE();

            return View(Model);
        }

        [HttpPost]
        public async Task<JsonResult> ExecuteSubmission(MM_SAMPLE Model)
        {
            var error = false;
            var msg = "Successfully saved data.";

            try
            {
                //TBC
                var a = Model.REMARK;
            }
            catch (Exception ex)
            {
                //await ErrorLog_Add_V3(System.Reflection.MethodBase.GetCurrentMethod().Name, ex, UserID_Name);
                error = true;
                msg = ex.Message;
            }

            return Json(new { error = error, msg = msg });
        }


    }
}
