using Base.Model;
using BaseSQL.Interface;
using BaseWebApi.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using static Microsoft.Graph.Constants;


namespace MaxSys.Helpers
{
    public class BaseController : Controller
    {
        public readonly IConfiguration _configuration;
        public readonly IWebApi _webApi;
        public readonly IDapper _dapper;
        public readonly IAuthenticator _authenticator;
        public string USER_ID { get; set; }
        public string ID_MM_USER { get; set; }

        public string NAME { get; set; }
        public string EMAIL { get; set; }
        public string USER_TOKEN { get; set; }
        public int ID_MM_COMPANY { get; set; }
        public string MM_COMPANY_NAME { get; set; }
        public string COMPANY_CODE { get; set; }
        public int ACCESS_LEVEL { get; set; }
        public string WORK_NO { get; set; }
        public string PIC_PROFILE { get; set; }
        public string POSTCODE { get; set; }
        public string CITY { get; set; }
        public string STATE { get; set; }
        public string COUNTRY { get; set; }
        public string SOLAT_ZONE { get; set; }
        public string PROFILE_IMAGE { get; set; }
        public string connectionString { get; set; }

        public BaseController(IConfiguration configuration, IWebApi webApi, IDapper dapper, IAuthenticator authenticator)
        {
            _configuration = configuration;
            _webApi = webApi;
            _dapper = dapper;
            _authenticator = authenticator;

            if (User != null)
            {
                NAME = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
                EMAIL = User.Identity?.Name;
            }

            connectionString = _configuration["ConnectionString_Dummy"] ?? "";
            _dapper.SetConnectionString(connectionString);
            _authenticator.SetConnectionString(connectionString);
        }
        public async void setBaseConnectionString(string SystemName, string URL, bool isDummy = false)
        {
            if (isDummy == false)
            {
                connectionString = await _webApi.Get_ConnectionString(_configuration[SystemName], _configuration[URL]);
            }
            else
            {
                connectionString = _configuration["ConnectionString_Dummy"] ?? "";
            }
            _dapper.SetConnectionString(connectionString);
            _authenticator.SetConnectionString(connectionString);
        }

        public void SetConnectionString(string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<List<T>> GetAll_Dropdown<T>(string CATEGORY)
        {
            try
            {
                // Assuming EMAIL is a variable accessible in this method
                (bool status, string message, List<T> data) stateDDL = await _dapper.GetDropdownList<T>(EMAIL, CATEGORY);

                if (stateDDL.status && stateDDL.data != null && stateDDL.data.Count > 0)
                {
                    return stateDDL.data; // Return the retrieved list
                }
                else
                {
                    return new List<T>(); // Return an empty list if no data found
                }
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                Console.WriteLine(ex.Message);
                return new List<T>(); // Return an empty list on exception
            }
        }

        public bool USE_TOKEN()
        {
            if (!string.IsNullOrEmpty(USER_TOKEN))
            {

                return _authenticator.IsTokenExpired(USER_TOKEN);
            }
            else
            {
                return false;
            }
        }

    }
}
