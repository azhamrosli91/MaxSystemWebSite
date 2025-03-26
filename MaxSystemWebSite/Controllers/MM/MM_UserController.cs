using Base.Model;
using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Models.MM;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;
using System;
using System.Text.Encodings.Web;
using System.Web.Helpers;
using MaxSys.Helpers;
using MaxSys.Interface;
using Microsoft.AspNetCore.Http;

namespace E_Template.Controllers.MM
{
    public class MM_UserController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<MM_UserController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IWebHostEnvironment _environment;
        public MM_UserController(ILogger<MM_UserController> logger, IConfiguration configuration, IWebApi webApi,
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
            ViewBag.ID = 9;
            //ViewBag.ID = 2009; //adam
            MM_USER model = new MM_USER();
            return View(model);
        }
        public async Task<IActionResult> Detail(int id = 0)
        {
            if (id == 0) {
                return RedirectToAction("Index");
            }
            MM_USER model = new MM_USER();

            // Fetch MM_USER data from the database using a stored procedure.
            (bool status, string message, MM_USER model) MM_USER =
                await _dapper.PSP_COMMON_DAPPER_SINGLE<MM_USER>(
                    "PSP_USER_LOGIN_BYID",
                    System.Data.CommandType.StoredProcedure,
                    new { USER_ID, ID_MM_USER = id, ID_MM_COMPANY }
                );

            if (MM_USER.status == false || MM_USER.model == null)
            {
               return RedirectToAction("Index");
            }
            else
            {
                model = MM_USER.model; // Assign the fetched data to the model.
                ViewBag.Page_Type = "Kemaskini";
            }

            model.ddlROLE = await GetAll_Dropdown<Setting_DropDown>("MM_USER");

           

            return View(model);
        }

        public async Task<string> Get()
        {
            try
            {
                (bool status, string message, List<MM_USER> model) manager = await _dapper.PSP_COMMON_DAPPER<MM_USER>("PSP_MM_USER_ID_LIST", System.Data.CommandType.StoredProcedure, new { USER_ID, ID_MM_COMPANY });

                return JsonConvert.SerializeObject(new { success = manager.status, message = manager.message, data = manager.model });

            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, message = ex.Message });
            }

        }

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
        [HttpPost]
        public async Task<string> DeleteAttachmentAtTemp(string fileName)
        {
            var success = true;
            var message = "";

            try
            {
                var tempPath = Path.Combine(_environment.WebRootPath, "Temp");

                // Remove the "Temp/" prefix from the fileName
                fileName = fileName.TrimStart('/');

                // Construct the physical file path
                string physicalFilePath = Path.Combine(_environment.WebRootPath, fileName);

                // Check if the file exists
                if (System.IO.File.Exists(physicalFilePath))
                {
                    // Delete the file
                    System.IO.File.Delete(physicalFilePath);
                }

                message = "Successfully deleted file";
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message.ToString();
            }
            var data = new { success, message };
            string returnJson = JsonConvert.SerializeObject(data);
            return returnJson;
        }
    }
}
