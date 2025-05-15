using Azure.Identity;
using Microsoft.Graph;
using MaxSys.Interface;
using MaxSystemWebSite.Models.SETTING;
using E_Template.Helpers;

namespace MaxSystemWebSite.Helpers.Graph
{
    public class GH_UserProfile : IUserProfile
    {
        // Settings object
        private static Settings? _settings;

        // Client secret credential
        private static ClientSecretCredential? _clientSecretCredential;
        // Client configured with application authentication
        private static GraphServiceClient? _appClient;

        public void InitGraph(SETTING_EMAIL settings)
        {

            // Ensure that required properties are not null
            if (string.IsNullOrEmpty(settings.TENANT_ID) ||
                string.IsNullOrEmpty(settings.CLIENT_ID) ||
                string.IsNullOrEmpty(settings.CLIENT_SECRET))
            {
                throw new ArgumentException("Invalid Graph API settings");
            }

            // Initialize the credential using client secret
            _clientSecretCredential = new ClientSecretCredential(
                settings.TENANT_ID,
                settings.CLIENT_ID,
                settings.CLIENT_SECRET
            );

            // Initialize Graph client with the credential and desired scopes
            _appClient = new GraphServiceClient(_clientSecretCredential, new[] { settings.GRAPH_USER });
        }

        #region "USER PROFILE"
        public async Task<(bool success, string message, string? base64Image)> GetUserProfilePhotoAsync(string userEmail)
        {
            try
            {
                if (_appClient == null)
                {
                    return (false, "Graph has not been initialized for app authentication", null);
                }

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return (false, "User email is required", null);
                }

                // Call the Graph API to get the user's photo
                var photoStream = await _appClient.Users[userEmail].Photo.Content.GetAsync();

                if (photoStream == null)
                {
                    return (false, "No profile photo found", null);
                }

                using (var memoryStream = new MemoryStream())
                {
                    await photoStream.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);
                    var mimeType = "image/jpeg"; // Default Microsoft Graph format

                    return (true, "Profile photo retrieved successfully", $"data:{mimeType};base64,{base64Image}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public Task<(bool success, string message, string? base64Image)> GetUserList()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
