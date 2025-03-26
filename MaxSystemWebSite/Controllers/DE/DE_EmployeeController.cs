using Base.Model;
using BaseSQL.Interface;
using BaseWebApi.Interface;
using E_Template.Controllers.MM;
using MaxSys.Models.DE;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using MaxSys.Helpers;
using MaxSys.Interface;

namespace MaxSys.Controllers.DE
{
    public class DE_EmployeeController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<DE_EmployeeController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IWebHostEnvironment _environment;
        public DE_EmployeeController(ILogger<DE_EmployeeController> logger, IConfiguration configuration, IWebApi webApi,
            IDapper dapper, IJWTToken jWTToken, ISQL sql,
            HtmlEncoder htmlEncoder, IAuthenticator authenticator, IWebHostEnvironment environment)
        : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _jwtToken = jWTToken;
            _SQL = sql;
            _htmlEncoder = htmlEncoder;
            _environment = environment;
        }

        public IActionResult Index()
        {
            ViewBag.ID = 2;
            return View();
        }
        public async Task<IActionResult> Detail(int id = 0) 
        {

            DE_EMPLOYEE model = new DE_EMPLOYEE();
            model.Page_Type = BaseModel.Enum.BaseEnumType.Page_Type.New;
            model.POSTCODE = "14000";
            model.GENDER = 0;
            model.STATE = "Pulau Pinang";
            model.COUNTRY = "MY";
            model.PHONE_1_CODE = "+60";
            model.PHONE_2_CODE = "+60";
            model.ddlSTATE = await GetAll_Dropdown<Setting_DropDown>("STATE_MY");
            model.ddlCOUNTRY = await GetAll_Dropdown<Setting_DropDown>("COUNTRY");
            model.ddlSTATUS = await GetAll_Dropdown<Setting_DropDown>("STATUS");
            model.ddlPHONE_CODE = await GetAll_Dropdown<Setting_DropDown>("PHONE_CODE");
            model.ddlRACE = await GetAll_Dropdown<Setting_DropDown>("RACE");
            model.ddlRELIGION = await GetAll_Dropdown<Setting_DropDown>("RELIGION");
            model.ddlGENDER = await GetAll_Dropdown<Setting_DropDown>("GENDER");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Post(DE_EMPLOYEE model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Data not found" });
                }
                //string PathFolder = $"document/{ID_MM_COMPANY.ToString()}/event";
                //(bool success, string newPath, string message) fileMove = CommonMethod.MoveFile(model.PHOTO_URL, PathFolder);

                //string PhotoUrl = "";
                //if (fileMove.success == false)
                //{
                //    return Json(new { success = false, message = "Maaf gagal memuat naik fail. Sila cuba lagi." });
                //}
                //else
                //{
                //    PhotoUrl = fileMove.newPath;
                //}

                //model.ID_MM_COMPANY = ID_MM_COMPANY;
                //var obj = new
                //{
                //    USER_ID = EMAIL,
                //    ID_MM_COMPANY = model.ID_MM_COMPANY,
                //    ID_DE_EVENT = model.ID_DE_EVENT,
                //    EVENT_DATE_START = model.EVENT_DATE_START,
                //    EVENT_DATE_END = model.EVENT_DATE_END,
                //    NAME = model.NAME,
                //    DESCRIPTION = model.DESCRIPTION,
                //    LOCATION = model.LOCATION,
                //    PHOTO_URL = PhotoUrl,
                //    STATUS = model.STATUS
                //};
                //(bool status, string message, ReturnSQL model) data = await _dapper.PSP_COMMON_DAPPER_SINGLE<ReturnSQL>("PSP_DE_EVENT_MAINT", System.Data.CommandType.StoredProcedure, obj);
                //if (data.status == true && data.model != null)
                //{
                //    return Json(new { success = true, message = "Successfully saved data." });
                //}
                //else
                //{
                //    return Json(new { success = false, message = data.message });
                //}
                return Json(new { success = false, message = "" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            var files = HttpContext.Request.Form.Files;
            if (files != null && files.Count > 0)
            {
                var uploadedFilesInfo = new List<object>();

                try
                {
                    // Define the permanent path for the document folder
                    var documentPath = Path.Combine(_environment.WebRootPath, "Temp");

                    // Ensure the document directory exists
                    if (!Directory.Exists(documentPath))
                    {
                        Directory.CreateDirectory(documentPath);
                    }
                    var tempFilePath = "";
                    var returnPath = "";

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            // Create a unique file name using GUID
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            returnPath = "/Temp/" + fileName;
                            tempFilePath = Path.Combine(documentPath, fileName);

                            // Save the file temporarily
                            using (var stream = new FileStream(tempFilePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                        }
                    }

                    // Return success response with all uploaded files info
                    return Json(new { success = true, message = "Files uploaded successfully!", filePath = returnPath });
                }
                catch (Exception ex)
                {
                    // Return error response in case of exception
                    return Json(new { success = false, message = "File upload failed: " + ex.Message });
                }
            }

            // Return error response if no files were selected
            return Json(new { success = false, message = "No files selected." });
        }
    }
}
