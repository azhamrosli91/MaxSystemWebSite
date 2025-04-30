using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BaseSQL.Interface;
using System.Data;
using System.Security.Claims;
using BaseWebApi.Interface;
using System.Text.Encodings.Web;
using MaxSys.Interface;
using MaxSys.Helpers;
using E_Template.Helpers;
using System.Text;
using MaxSys.Models.MM;
using Base.Model;
using Microsoft.AspNet.Identity.EntityFramework;
using MaxSystemWebSite.Models.SETTING;

namespace MaxSys.Controllers
{
    [Authorize]
    [AllowAnonymous]
    [NoSessionExpire]
    public class AccountController : BaseController
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly ILogger<AccountController> _logger;
        private readonly IActionResult _result;
        private readonly IJWTToken _jwtToken;
        private readonly ISQL _SQL;
        private readonly IDapper_Oracle _dapper_Oracle;
        private readonly UserProfileService _userProfileService;
        private readonly IEmailService _emailService;
        private readonly IUserProfile _userProfile;

        public AccountController(ILogger<AccountController> logger, IConfiguration configuration, IWebApi webApi,
            IDapper dapper, IJWTToken jWTToken, ISQL sql,
            IDapper_Oracle dapper_Oracle, HtmlEncoder htmlEncoder, IAuthenticator authenticator, UserProfileService userProfileService, IEmailService emailService, IUserProfile userProfile)
        : base(configuration, webApi, dapper, authenticator) // Call the base constructor
        {
            _logger = logger;
            _jwtToken = jWTToken;
            _SQL = sql;
            _htmlEncoder = htmlEncoder;
            _dapper_Oracle = dapper_Oracle;
            _userProfileService = userProfileService;
            _emailService = emailService;
            _userProfile = userProfile;
        }

        [AllowAnonymous]
        [NoSessionExpire]
        public IActionResult Sign()
        {
            // Set the return URL to redirect after successful sign-in
            var returnUrl = Url.Action("SignIn", "Account");

            // Trigger the OpenID Connect authentication process and pass the return URL
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
           // return Challenge( OpenIdConnectDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SignIn()
        {

            if (!User.Identity.IsAuthenticated) {
                return RedirectToAction("Unauthorize", "Errors", new { message = "User.Identity.IsAuthenticated = false" });
            }

            var claims = User.Claims.Select(c => new { c.Type, c.Value });

            if (claims == null) {
                return RedirectToAction("Unauthorize", "Errors");
            }

            

            string email = claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ??
                                           claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            (bool success, string message, MM_USER emp) employee = _dapper.PSP_COMMON_DAPPER_SINGLE_SYNC<MM_USER>
                                  ("PSP_USER_BYEMAIL", CommandType.StoredProcedure, new { EMAIL = email });

            if (employee.success == false || employee.emp == null)
            {
                return RedirectToAction("Unauthorize", "Errors", new { message = "employee is null or " + employee.message });
            }


            IdentityUser identityUser = new IdentityUser();
            identityUser.Id = Guid.NewGuid().ToString();
            identityUser.UserName = employee.emp.NAME;
            identityUser.Email = employee.emp.EMAIL;

            string token = _authenticator.GenerateToken(identityUser);


            Response.Cookies.Append("ReturnUrl", "", new CookieOptions { Expires = DateTime.Now.AddMinutes(-1) });


            var cookieOptions = new CookieOptions
            {
                Path = "/",           // Ensure a consistent path
                SameSite = SameSiteMode.Unspecified // Specify how cookies should be handled
            };

            Response.Cookies.Append("USER_TOKEN", token, cookieOptions);
            Response.Cookies.Append("EMAIL", employee.emp.EMAIL, cookieOptions);
            Response.Cookies.Append("ID_MM_USER", identityUser.Id, cookieOptions);
            Response.Cookies.Append("NAME", employee.emp.NAME, cookieOptions);
            Response.Cookies.Append("AUTH_TYPE", "OPENID", cookieOptions);

            // Trigger the OpenID Connect authentication process and pass the return URL
            return RedirectToAction("Index", "MM_UserProfile");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("signout-oidc")]
        public IActionResult SigingOut(string page = "")
        {
            return RedirectToAction("Login", "Home");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignOut(string page = "")
        {
            return RedirectToAction("Login", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        [NoSessionExpire]
        public async Task<IActionResult> GetProfileImage(string email = "")
        {
            var photoBytes = await _userProfileService.GetUserPhotoAsync(email);

            if (photoBytes == null)
            {
                return Json(new { success = false, msg = "no profile photo." });
            }
            // Convert photo bytes to Base64
            var base64String = Convert.ToBase64String(photoBytes);
            string base64Image = $"data:image/jpeg;base64,{base64String}";

            return Json(new { success = true, msg = "Profile photo retrieved.", data = base64Image });
        }


        [HttpPost]
        [AllowAnonymous]
        [NoSessionExpire]
        public async Task<IActionResult> ForgotPasswordVerify(ChangePasswordViewModel model) 
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Data not found" });
                }

                if (string.IsNullOrEmpty(model.Email))
                {
                    return Json(new { success = false, message = "Sila masukkan email anda." });
                }

                if (string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmPassword))
                {
                    return Json(new { success = false, message = "Sila masukkan kata laluan anda." });
                }


                byte[] data = Convert.FromBase64String(model.NewPassword);
                // Convert byte array to a normal string
                string normalString = Encoding.UTF8.GetString(data);
                model.NewPassword = normalString;

                byte[] data2 = Convert.FromBase64String(model.ConfirmPassword);
                // Convert byte array to a normal string
                string normalString2 = Encoding.UTF8.GetString(data2);
                model.ConfirmPassword = normalString2;


                (bool success, string message) returnAuth = await _authenticator.CHANGE_PASSWORD(model);

                if (returnAuth.success == false)
                {
                    return Json(new { success = false, message = "Failed to change password" });
                }
                else {
                    return Json(new { success = true, message = $"Successfully changes password." });
                }

            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message });
            }
        }
       

        [HttpGet]
        [AllowAnonymous]
        [NoSessionExpire]
        public async Task<IActionResult> ForgotPassword(string Email = "")
        {
            ChangePasswordViewModel model = new ChangePasswordViewModel();
            model.Email = Email;
            return View(model);
        }

        //void InitializeGraph(E_Template.Helpers.Settings settings)
        //{
        //    GraphHelper.InitializeGraphForAppAuth(settings);
        //}
    }
}
