﻿using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSystemWebSite.Models.EMAIL;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace MaxSystemWebSite.Controllers.MM
{
    public class MM_LeaveController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<MM_LeaveController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IDapper_Oracle _dapper_Oracle;
        private readonly UserProfileService _userProfileService;
        private readonly ISharePoint _sharePoint;

        public MM_LeaveController(ILogger<MM_LeaveController> logger, IConfiguration configuration, IWebApi webApi,
           IDapper dapper, IAuthenticator authenticator, UserProfileService userProfileService, ISharePoint sharePoint)
       : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _userProfileService = userProfileService;
            _sharePoint = sharePoint;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaveHistory(string email)
        {
            (bool success, string message, List<SP_LeaveHistory> data) dataReturn = await _sharePoint.GetLeaveHistory(_configuration["MaxSystem_SharePoint:MainPoint"], _configuration["MaxSystem_SharePoint:HistoryLeaveRecordID"], email);

            return Json(new { success = dataReturn.success, msg = dataReturn.message, data = dataReturn.data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLeaveHistory()
        {
            (bool success, string message, List<SP_LeaveHistory> data) dataReturn = await _sharePoint.GetAllLeaveHistory(_configuration["MaxSystem_SharePoint:MainPoint"], _configuration["MaxSystem_SharePoint:HistoryLeaveRecordID"]);

            return Json(new { success = dataReturn.success, msg = dataReturn.message, data = dataReturn.data });
        }

        public async Task<IActionResult> GetLoadLeaveInformation(string email) 
        {
            (bool success, string message, SP_LeaveInformation data) dataReturn = await _sharePoint.GetLeaveInformation(_configuration["MaxSystem_SharePoint:MainPoint"], _configuration["MaxSystem_SharePoint:LeaveBalance"], email);

            return Json(new { success = dataReturn.success, msg = dataReturn.message, data = dataReturn.data });
        }

        [HttpGet]
        public async Task<IActionResult> GetPublicHoliday()
        {
            (bool success, string message, List<SP_PublicHoliday> data) dataReturn = await _sharePoint.GetPublicHoliday(_configuration["MaxSystem_SharePoint:MainPoint"], _configuration["MaxSystem_SharePoint:PublicHoliday"]);

            return Json(new { success = dataReturn.success, msg = dataReturn.message, data = dataReturn.data });
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarData()
        {
            bool success = true;
            string message = "";

            (bool success, string message, List<SP_LeaveHistory> data) dataLeave = await _sharePoint.GetAllLeaveHistory(_configuration["MaxSystem_SharePoint:MainPoint"], _configuration["MaxSystem_SharePoint:HistoryLeaveRecordID"]);
            if (!dataLeave.success)
            {
                message += !string.IsNullOrEmpty(dataLeave.message) ? dataLeave.message : "Fail to obtain leave information.";
            }

            (bool success, string message, List<SP_PublicHoliday> data) dataHoliday = await _sharePoint.GetPublicHoliday(_configuration["MaxSystem_SharePoint:MainPoint"], _configuration["MaxSystem_SharePoint:PublicHoliday"]);
            if (!dataHoliday.success)
            {
                message += !string.IsNullOrEmpty(dataHoliday.message) ? dataHoliday.message : "Fail to obtain holiday information.";
            }

            if (!dataLeave.success && !dataHoliday.success)
            {
                success = false;
            }

            return Json(new { success = success, msg = message, l_data = dataLeave.data, h_data = dataHoliday.data });
        }

    }
}
