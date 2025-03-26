using Azure;
using Base.Model;
using BaseSQL.Interface;
using MaxSys.Models.MM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Graph.Models;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Security.Claims;
using System.Web;

namespace MaxSys.Helpers
{
    public class SessionExpireAttribute : ActionFilterAttribute
    {
        //private readonly ILogger<SessionExpireAttribute> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDapper _dapper;
        private readonly IAuthenticator _authenticator;
        public SessionExpireAttribute(IConfiguration configuration, IDapper dapper, IAuthenticator authenticator)
        {
            _configuration = configuration;
            _dapper = dapper;
            _authenticator = authenticator;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as BaseController;
            if (controller != null)
            {
                if (_configuration["isTest"].ToLower() == "true")
                {
                    controller.setBaseConnectionString("", "ConnectionAPI_URL", true);
                }
                else
                {
                    controller.setBaseConnectionString("SystemName", "ConnectionAPI_URL", false);
                }
            }


            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor != null)
            {
                var hasAllowAnonymous = actionDescriptor.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
                var hasNoSessionExpire = actionDescriptor.MethodInfo.GetCustomAttributes(typeof(NoSessionExpireAttribute), true).Any();

                if (hasAllowAnonymous || hasNoSessionExpire)
                {
                    return;
                }
            }

            var cookies = context.HttpContext.Request.Cookies;
            string USER_TOKEN = "";
            string EMAIL = "";
            string NAME = "";
            string USER_ID = "";
            string ACCESS_LEVEL = "1";
            string ID_MM_USER = "";
            string ID_MM_COMPANY = "0";
            string redirectTo = "~/Home/Index";
            string PIC_PROFILE = "";
            string STATE = "";
            string COUNTRY = "";
            string POSTCODE = "";
            string CITY = "";
            string MM_COMPANY_NAME = "";
            string SOLAT_ZONE = "";
            string COMPANY_CODE = "";
            string PROFILE_IMAGE = "";

            var currentUrl = context.HttpContext.Request.Path;

            if (context.HttpContext.Request.Cookies.TryGetValue("PROFILE_IMAGE", out var PROFILE_IMAGEcookieValue))
            {
                if (IsCookieExpired(PROFILE_IMAGEcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("PROFILE_IMAGE");
                    PROFILE_IMAGE = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    PROFILE_IMAGE = PROFILE_IMAGEcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("ACCESS_LEVEL", out var ACCESS_LEVELcookieValue))
            {
                if (IsCookieExpired(ACCESS_LEVELcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("ACCESS_LEVEL");
                    ACCESS_LEVEL = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    ACCESS_LEVEL = ACCESS_LEVELcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("COMPANY_CODE", out var COMPANY_CODEcookieValue))
            {
                if (IsCookieExpired(COMPANY_CODEcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("COMPANY_CODE");
                    COMPANY_CODE = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    COMPANY_CODE = COMPANY_CODEcookieValue;
                }
            }


            if (context.HttpContext.Request.Cookies.TryGetValue("SOLAT_ZONE", out var SOLAT_ZONEcookieValue))
            {
                if (IsCookieExpired(SOLAT_ZONEcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("SOLAT_ZONE");
                    SOLAT_ZONE = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    SOLAT_ZONE = SOLAT_ZONEcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("MM_COMPANY_NAME", out var MM_COMPANY_NAMEYcookieValue))
            {
                if (IsCookieExpired(MM_COMPANY_NAMEYcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("MM_COMPANY_NAME");
                    MM_COMPANY_NAME = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    MM_COMPANY_NAME = MM_COMPANY_NAMEYcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("CITY", out var CITYcookieValue))
            {
                if (IsCookieExpired(CITYcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("CITY");
                    CITY = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    CITY = CITYcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("POSTCODE", out var POSTCODEcookieValue))
            {
                if (IsCookieExpired(POSTCODEcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("POSTCODE");
                    POSTCODE = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    POSTCODE = POSTCODEcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("COUNTRY", out var COUNTRYcookieValue))
            {
                if (IsCookieExpired(COUNTRYcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("COUNTRY");
                    COUNTRY = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    COUNTRY = COUNTRYcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("STATE", out var STATEcookieValue))
            {
                if (IsCookieExpired(STATEcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("STATE");
                    STATE = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    STATE = STATEcookieValue;
                }
            }

            if (context.HttpContext.Request.Cookies.TryGetValue("USER_TOKEN", out var cookieValue))
            {
                if (IsCookieExpired(cookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("USER_TOKEN");
                    USER_TOKEN = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    USER_TOKEN = cookieValue;
                }
            }
            if (context.HttpContext.Request.Cookies.TryGetValue("EMAIL", out var emailcookieValue))
            {
                if (IsCookieExpired(emailcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("EMAIL");
                    EMAIL = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    EMAIL = emailcookieValue;
                }
            }
            if (context.HttpContext.Request.Cookies.TryGetValue("NAME", out var namecookieValue))
            {
                if (IsCookieExpired(namecookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("NAME");
                    NAME = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    NAME = namecookieValue;
                }
            }
            if (context.HttpContext.Request.Cookies.TryGetValue("USER_ID", out var userIDcookieValue))
            {
                if (IsCookieExpired(userIDcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("USER_ID");
                    USER_ID = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    USER_ID = userIDcookieValue;
                }
            }
            if (context.HttpContext.Request.Cookies.TryGetValue("ID_MM_USER", out var userDataIDcookieValue))
            {
                if (IsCookieExpired(userDataIDcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("ID_MM_USER");
                    ID_MM_USER = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    ID_MM_USER = userDataIDcookieValue;
                }
            }
            if (context.HttpContext.Request.Cookies.TryGetValue("ID_MM_COMPANY", out var ID_MM_COMPANYcookieValue))
            {
                if (IsCookieExpired(ID_MM_COMPANYcookieValue))
                {
                    //_logger.LogWarning($"Session is expired.");
                    context.HttpContext.Response.Cookies.Delete("ID_MM_COMPANY");
                    ID_MM_COMPANY = "";


                    redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";

                    context.Result = new RedirectResult(redirectTo);
                }
                else
                {
                    ID_MM_COMPANY = ID_MM_COMPANYcookieValue;
                }
            }

            if (controller != null)
            {

                if (USER_TOKEN != "")
                {
                    //controller.ACL_JSON = USER_TOKEN;
                    //controller.MM_EMPLOYEE_ID = MM_EMPLOYEE_ID;
                    //bool status = await controller.USE_TOKEN();

                    // if (currentUrl != "/Home/Login" && currentUrl != "/" && currentUrl != "")
                    // {
                    //     redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";
                    //     context.Result = new RedirectResult(redirectTo);
                    // }
                    controller.USER_ID = USER_ID;
                    controller.ID_MM_USER = ID_MM_USER;
                    controller.NAME = NAME;
                    controller.EMAIL = EMAIL;
                    controller.ACCESS_LEVEL = Convert.ToInt32(ACCESS_LEVEL);
                    controller.USER_TOKEN = USER_TOKEN;
                    controller.PIC_PROFILE = PIC_PROFILE;
                    controller.ID_MM_COMPANY = Convert.ToInt32(ID_MM_COMPANY);
                    controller.MM_COMPANY_NAME = MM_COMPANY_NAME;
                    controller.POSTCODE = POSTCODE;
                    controller.CITY = CITY;
                    controller.STATE = STATE;
                    controller.COUNTRY = COUNTRY;
                    controller.SOLAT_ZONE = SOLAT_ZONE;
                    controller.COMPANY_CODE = COMPANY_CODE;
                    controller.PROFILE_IMAGE = PROFILE_IMAGE;

                    bool status = controller.USE_TOKEN();

                    if (status == false && currentUrl != "/Home/Index")
                    {
                        redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";
                        context.Result = new RedirectResult(redirectTo);
                    }

                }
                else
                {
                    if (currentUrl != "/Home/Index")
                    {
                        redirectTo = $"{redirectTo}?ReturnUrl={HttpUtility.UrlEncode(currentUrl)}";
                        context.Result = new RedirectResult(redirectTo);
                    }
                }

                //controller.Setting_Setup = this.setting_Setup;
            }

            base.OnActionExecuting(context);
        }
        private bool IsCookieExpired(string cookieValue)
        {
            // Parse the cookie value or handle it as needed
            // For example, if your cookie value contains a creation timestamp:
            if (DateTime.TryParse(cookieValue, out var creationDate))
            {
                // Set the fixed expiration duration (e.g., 15 minutes)
                var expirationDuration = TimeSpan.FromMinutes(15);
                var expirationDate = creationDate + expirationDuration;
                return expirationDate < DateTime.Now;
            }
            // Handle other cases if necessary
            return false; // Default behavior if parsing fails
        }
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            //_logger.LogInformation($"Action {context.ActionDescriptor.DisplayName} executed in {executionTime} ms.");
        }
        private void writeCookies(ActionExecutingContext context, BaseController controller ) 
        {
            if (!string.IsNullOrEmpty(controller.EMAIL))
            {
                context.HttpContext.Response.Cookies.Append("EMAIL", controller.EMAIL, new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                });
            }

            if (!string.IsNullOrEmpty(controller.USER_ID))
            {
                context.HttpContext.Response.Cookies.Append("USER_ID", controller.USER_ID, new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                });
            }

            if (!string.IsNullOrEmpty(controller.ID_MM_USER))
            {
                context.HttpContext.Response.Cookies.Append("ID_MM_USER", controller.ID_MM_USER, new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                });
            }

            if (!string.IsNullOrEmpty(controller.NAME))
            {
                context.HttpContext.Response.Cookies.Append("NAME", controller.NAME, new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                });
            }

            if (!string.IsNullOrEmpty(controller.ID_MM_COMPANY.ToString()))
            {
                context.HttpContext.Response.Cookies.Append("ID_MM_COMPANY", controller.ID_MM_COMPANY.ToString(), new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                });
            }
            if (!string.IsNullOrEmpty(controller.WORK_NO.ToString()))
            {
                context.HttpContext.Response.Cookies.Append("WORK_NO", controller.WORK_NO.ToString(), new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                });
            }
            context.HttpContext.Response.Cookies.Append("AUTH_TYPE", "OPENID", new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(30),
            });

        }
    }
}
