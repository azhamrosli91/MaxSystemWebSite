namespace SmartTemplateCore.Models.Common
{
    public class Login_Request
    {
        public string LoginID { get; set; }
        public string LoginPass { get; set; }
        public string SystemName { get; set; }
        public string CompanyCode { get; set; }
        public string DatabaseType { get; set; }
    }
}
