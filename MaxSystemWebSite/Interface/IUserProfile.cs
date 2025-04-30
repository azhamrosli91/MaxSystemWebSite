using MaxSystemWebSite.Models.SETTING;

namespace MaxSys.Interface
{
    public interface IUserProfile
    {
        void InitGraph(SETTING_EMAIL settings);
        Task<(bool success, string message, string? base64Image)> GetUserProfilePhotoAsync(string userEmail);
    }
}
