using Base.Model;
using BaseSQL.Interface;
using BaseWebApi.Interface;
using MaxSys.Interface;
using MaxSys.Models.MM;
using E_Template.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartTemplateCore.Models.Common;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using MaxSys.Helpers;
using MaxSys.Interface;
using MaxSys.Models;
using LoginViewModel = Base.Model.LoginViewModel;
using Org.BouncyCastle.Asn1.Crmf;

namespace MaxSys.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<HomeController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IDapper_Oracle _dapper_Oracle;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IWebApi webApi, 
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

        [HttpGet]
        [NoSessionExpire]
        [AllowAnonymous]
        public async Task<IActionResult> TestPage() 
        {
            return View();
        }

        [HttpGet]
        [NoSessionExpire]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string ReturnUrl = "")
        {
            //reset cookies
            Response.Cookies.Delete("ACL_JSON");
            Response.Cookies.Delete("USER_ID");
            Response.Cookies.Delete("USER_NAME");
            Response.Cookies.Delete("COMPANY_CODE");
            Response.Cookies.Delete("USER_EMAIL");
            Response.Cookies.Delete("USER_ID_NAME");
            Response.Cookies.Delete("SOLAT_ZONE");
            Response.Cookies.Delete("JWTToken");
            Response.Cookies.Delete("JWTRefreshToken");
            Response.Cookies.Delete("ACCESS_LEVEL"); 
            Response.Cookies.Append("ReturnUrl", ReturnUrl ?? "", new CookieOptions { Expires = DateTime.Now.AddMinutes(15) });

            AuthenticatorModel authenticatorModel = new AuthenticatorModel();
            authenticatorModel.RETURN_URL = ReturnUrl ?? "";
            return View(authenticatorModel);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AuthenticatorModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.LOGIN_ID) == true || string.IsNullOrEmpty(model.PASSWORD))
                {
                    ViewBag.Validate = false;
                    return Json(new { success = false, redirectUrl = "/Home/Index" });
                }

                var modelData = new LoginViewModel
                {
                    Email = model.LOGIN_ID,
                    Password = model.PASSWORD,
                    RememberMe = true
                };

                (bool success, string message, Base.Model.Login_Response userModel) returnAuth = await _authenticator.LOGIN(modelData);

                if (returnAuth.success == true)
                {
                    
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,  // Set to true for production with HTTPS
                        Expires = DateTime.UtcNow.AddYears(30),
                        Path = "/",  // Makes the cookie accessible across the entire app
                        SameSite = SameSiteMode.Strict  // Prevents the cookie from being sent in cross-site requests
                    };

                    Response.Cookies.Append("USER_TOKEN", returnAuth.userModel.JWTToken, cookieOptions);
                    Response.Cookies.Append("EMAIL", returnAuth.userModel.Email, cookieOptions);
                    Response.Cookies.Append("USER_ID", returnAuth.userModel.User_ID, cookieOptions);
                    Response.Cookies.Append("NAME", returnAuth.userModel.Name, cookieOptions);
                    //Response.Cookies.Append("ID_MM_USER", mm_user.user.ID_MM_USER.ToString(), cookieOptions);
                    //Response.Cookies.Append("ID_MM_COMPANY", mm_company.com.ID_MM_COMPANY.ToString(), cookieOptions);
                    //Response.Cookies.Append("MM_COMPANY_NAME", mm_company.com.MM_COMPANY_NAME, cookieOptions);
                    //Response.Cookies.Append("POSTCODE", mm_company.com.POSTCODE, cookieOptions);
                    //Response.Cookies.Append("CITY", mm_company.com.CITY, cookieOptions);
                    //Response.Cookies.Append("STATE", mm_company.com.STATE, cookieOptions);
                    //Response.Cookies.Append("COUNTRY", mm_company.com.COUNTRY, cookieOptions);
                    //Response.Cookies.Append("SOLAT_ZONE", mm_company.com.SOLAT_ZONE, cookieOptions);
                    //Response.Cookies.Append("COMPANY_CODE", mm_company.com.CODE, cookieOptions);
                     Response.Cookies.Append("ACCESS_LEVEL", "3", cookieOptions);


                    
                    //if (!string.IsNullOrEmpty(mm_user.user.PROFILE_IMAGE))
                    //{
                    //    // Get the full physical path of the file
                    //    var fullPath = Path.Combine(_environment.WebRootPath, mm_user.user.PROFILE_IMAGE.TrimStart('/'));

                    //    // Check if the file exists
                    //    if (System.IO.File.Exists(fullPath))
                    //    {
                    //        Response.Cookies.Append("PROFILE_IMAGE", mm_user.user.PROFILE_IMAGE, cookieOptions);
                    //    }
                    //    else {
                    //        Response.Cookies.Append("PROFILE_IMAGE", "/Images/user-avatar.png", cookieOptions);
                    //    }        
                    //}
                    //else {
                    //    Response.Cookies.Append("PROFILE_IMAGE", "/Images/user-avatar.png", cookieOptions);
                    //}

                    Response.Cookies.Append("AUTH_TYPE", "JWT", cookieOptions);

                    Response.Cookies.Append("jwt", returnAuth.userModel.JWTToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,  // Set to true for production with HTTPS
                        Expires = DateTime.UtcNow.AddYears(30),
                        Path = "/",  // Makes the cookie accessible across the entire app
                        SameSite = SameSiteMode.Strict  // Prevents the cookie from being sent in cross-site requests
                    });

                    ViewBag.Validate = true;

                    if (!string.IsNullOrWhiteSpace(model.RETURN_URL))
                        return Json(new { success = true, redirectUrl = model.RETURN_URL });


                    return Json(new { success = true, redirectUrl = "/Dashboard/Index" });
                }
                else
                {

                    ViewBag.Validate = false;
                    return Json(new { success = false, redirectUrl = "/Home/Index" });
                }



            }
            catch (Exception ex)
            {
                ViewBag.Validate = false;
            }
            return Json(new { success = false, redirectUrl = "/Home/Index" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(string authenticationType = "JWT")
        {
            if (HttpContext.Request.Cookies.ContainsKey("jwt"))
            {
                Response.Cookies.Delete("jwt");
            }
            Response.Cookies.Append("jwt", "", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire immediately
            });

            var deleteCookieOptions = new CookieOptions
            {
                Path = "/",          // Match the path used during append
                HttpOnly = true,     // Ensure this matches the original cookie's settings
                Secure = true        // Match Secure setting if the original cookie was Secure
            };
            Response.Cookies.Delete("ACL_JSON", deleteCookieOptions);
            Response.Cookies.Delete("USER_TOKEN", deleteCookieOptions);
            Response.Cookies.Delete("EMAIL", deleteCookieOptions);
            Response.Cookies.Delete("USER_ID", deleteCookieOptions);
            Response.Cookies.Delete("ID_MM_USER", deleteCookieOptions);
            Response.Cookies.Delete("NAME", deleteCookieOptions);
            Response.Cookies.Delete("ID_MM_COMPANY", deleteCookieOptions);
            Response.Cookies.Delete("MM_COMPANY_NAME", deleteCookieOptions);
            Response.Cookies.Delete("WORK_NO", deleteCookieOptions);
            Response.Cookies.Delete("AUTH_TYPE", deleteCookieOptions);
            Response.Cookies.Delete("SOLAT_ZONE", deleteCookieOptions);
            Response.Cookies.Delete("COMPANY_CODE", deleteCookieOptions);
            Response.Cookies.Delete("ACCESS_LEVEL", deleteCookieOptions);
            Response.Cookies.Delete("PROFILE_IMAGE", deleteCookieOptions);

            Response.Cookies.Append("ReturnUrl", "", new CookieOptions { Expires = DateTime.Now.AddMinutes(-1) });

            // Sign out from both Microsoft Identity and local session
            HttpContext.SignOutAsync();

            if (authenticationType == "OPENID")
            {

                //return SignOut(new AuthenticationProperties
                //{
                //    RedirectUri = "/"

                //}, OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                {
                    RedirectUri = "/"
                });


                await HttpContext.SignOutAsync("Cookies");

                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Sidebar()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Chatbot([FromBody] ChatRequest request)
        {
            try
            {
                var client = new HttpClient();
                string apiKey = _configuration["ChatGPT"];
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var body = new
                {
                    model = "gpt-3.5-turbo",
                    messages = request.Messages,
                    temperature = 0.7
                };

                var response = await client.PostAsync(
                    "https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);
                    string reply = data.choices[0].message.content;
                    return Ok(reply);
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                return StatusCode(500, $"OpenAI error: {errorBody}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
            
        }

        public async Task<IActionResult> AuditTrail(int ID = 0, string TableName = "", string ReturnURL = "")
        {
            if (string.IsNullOrEmpty(TableName)) {
                return View();
            }

            List<SqlParameter> _pMssql = new List<SqlParameter>();
            _pMssql.Add(new SqlParameter("@TABLE", TableName));
            _pMssql.Add(new SqlParameter("@KEY_VALUE", ID));

            (bool status, string message, DataTable dt)  result = await _SQL.PSP_COMMON_SQL("PSP_GET_AUDIT_TRAIL", CommandType.StoredProcedure,BaseSQL.Enum.BaseEnum.ExecutionType.ExecuteReader, _pMssql);

            if (result.status == false) { return Redirect(ReturnURL); }
            List<string> keyNames = result.dt.Columns.Cast<DataColumn>()
            .Select(column => column.ColumnName)
            .ToList();

            string jsonResult = JsonConvert.SerializeObject(result);

            AuditTrailModel dataModel = new AuditTrailModel
            {
                JsonData = jsonResult,
                ListData = keyNames
            };

            ViewBag.JsonResult = dataModel.JsonData;
            ViewBag.KeyNames = dataModel.ListData;

            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetSidebar()
        {
            List<AclResource> sidebarItems = null;
            (bool success, string message, List<AclResource> item) side_bar = await _dapper.PSP_COMMON_DAPPER<AclResource>("PSP_ACL_RESOURCE", CommandType.StoredProcedure, new { USER_ID  , ID_MM_COMPANY });

            if (side_bar.success == true && side_bar.item != null)
            {
                sidebarItems = side_bar.item;
            }
            else
            {
                string filePath = $"{Directory.GetCurrentDirectory()}\\wwwroot\\json\\sidebar.json";
                string jsonText = "";
                if (System.IO.File.Exists(filePath))
                {
                    jsonText = System.IO.File.ReadAllText(filePath);
                }

                // Deserialize JSON to a list of objects
                sidebarItems = JsonConvert.DeserializeObject<List<AclResource>>(jsonText);
            }

            // Filter based on userAccessLevel
            var filteredSidebar = sidebarItems
                .Where(item => item.ACCESS_LEVEL <= ACCESS_LEVEL && item.LAYER == 1)
                .OrderBy(item => item.SEQ)
                .ToList();

            if (filteredSidebar != null)
            {
                foreach (var item in filteredSidebar)
                {
                    if (item.ListChild == null)
                    {
                        item.ListChild = new List<AclResource>();

                        var childSidebar = sidebarItems
                            .Where(data => data.ACCESS_LEVEL <= ACCESS_LEVEL && data.RESOURCE_PARENT_ID == item.ID_ACL_RESOURCE)
                            .OrderBy(data => data.SEQ)
                            .ToList();

                        if (childSidebar != null && childSidebar.Count > 0)
                        {
                            item.ListChild = childSidebar;
                        }
                    }
                }
            }

            // Serialize back to JSON
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var filteredJson = JsonConvert.SerializeObject(filteredSidebar, settings);

            return Json(filteredJson);
        }

        //public async Task<IActionResult> GetSidebar()
        //{
        //    List<AclResource> sidebarItems = null;
        //    (bool success, string message, List<AclResource> item) side_bar = await _dapper.PSP_COMMON_DAPPER<AclResource>("PSP_ACL_RESOURCE", CommandType.StoredProcedure, new { USER_ID = EMAIL });

        //    if (side_bar.success == true && side_bar.item != null)
        //    {
        //        sidebarItems = side_bar.item;
        //    }
        //    else
        //    {
        //        string filePath = $"{Directory.GetCurrentDirectory()}\\wwwroot\\json\\sidebar.json";
        //        string jsonText = "";
        //        if (System.IO.File.Exists(filePath))
        //        {
        //            jsonText = System.IO.File.ReadAllText(filePath);
        //        }

        //        Deserialize JSON to a list of objects
        //       sidebarItems = JsonConvert.DeserializeObject<List<AclResource>>(jsonText);
        //    }


        //    Filter based on userAccessLevel
        //    var filteredSidebar = sidebarItems
        //        .Where(item => item.ACCESS_LEVEL <= ACCESS_LEVEL && item.LAYER == 1)
        //        .ToList();

        //    if (filteredSidebar != null)
        //    {

        //        foreach (var item in filteredSidebar)
        //        {
        //            if (item.ListChild == null)
        //            {
        //                item.ListChild = new List<AclResource>();

        //                var childSidebar = sidebarItems
        //                             .Where(data => data.ACCESS_LEVEL <= ACCESS_LEVEL && data.RESOURCE_PARENT_ID == item.ID_ACL_RESOURCE)
        //                             .ToList();
        //                if (childSidebar != null && childSidebar.Count > 0)
        //                {
        //                    item.ListChild = childSidebar;
        //                }
        //            }
        //        }
        //    }
        //    Serialize back to JSON
        //    var settings = new JsonSerializerSettings
        //    {
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        //    };

        //    var filteredJson = JsonConvert.SerializeObject(filteredSidebar, settings);



        //    return Json(filteredJson);

        //}

        [HttpGet]
        [NoSessionExpire]
        public IActionResult RenderComponent(string ComponentName = "")
        {
            
            return ViewComponent(ComponentName);
        }

        [HttpPost]
        public string GetEcryptstring(string key = "")
        {
            var success = true;
            var message = "";

            string base64String = CommonMethod.ConvertStringTo64base(key);

            var data = new { success, message, data = base64String };
            string returnJson = JsonConvert.SerializeObject(data);
            return returnJson;
        }

        [HttpPost]
        public string GetDecryptstring(string key = "")
        {
            var success = true;
            var message = "";

            string text = CommonMethod.Convert64baseToString(key);

            var data = new { success, message, data = text };
            string returnJson = JsonConvert.SerializeObject(data);
            return returnJson;
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        void InitializeGraph(E_Template.Helpers.Settings settings)
        {
            GraphHelper.InitializeGraphForAppAuth(settings);
        }

        [AllowAnonymous]
        public async Task<string> GetSettingSetup()
        {
            try
            {
                (bool success, string message, Setting_Setup model) setup = await _dapper.PSP_COMMON_DAPPER_SINGLE<Setting_Setup>("PSP_SETTING_SETUP", CommandType.StoredProcedure, new { USER_ID });

                return JsonConvert.SerializeObject(new { success = setup.success, message = setup.message, data = setup.model });

            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, message = ex.Message });
            }

        }
    }
}
