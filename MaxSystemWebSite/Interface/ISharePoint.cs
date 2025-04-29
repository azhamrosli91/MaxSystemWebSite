using MaxSystemWebSite.Models.SETTING;
using MaxSystemWebSite.Models.EMAIL;
using Microsoft.Graph.Models;


namespace MaxSys.Interface
{
    public interface ISharePoint
    {
        void InitGraph(SETTING_EMAIL settings);
        Task<(bool success, string message, ListCollectionResponse? data)> GetList(string _siteID);
        Task<(bool success, string message, List<SP_EmployeeInformation> data)> GetEmployeeInformation(string _siteID, string listID);
        Task<(bool success, string message, SP_EmployeeInformation data)> GetEmployeeInformationDetail();

    }
}
