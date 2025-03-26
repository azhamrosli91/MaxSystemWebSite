using Azure.Identity;
using Microsoft.Graph;
using System.Net.Http.Headers;

namespace MaxSys.Helpers
{
    public class UserProfileService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public UserProfileService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        //public async Task<byte[]> GetUserPhotoAsync(string accessToken)
        //{

        //    var scopes = new[] { "https://graph.microsoft.com/.default" };
        //    var tenantId = "tenantId";
        //    var clientId = "appId";
        //    var clientSecret = "secret";

        //    var options = new TokenCredentialOptions
        //    {
        //        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        //    };

        //    var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);

        //    var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

        //    try
        //    {
        //        var user = await graphClient.Users["userId"].GetAsync();
              
        //        // Get the user's photo
        //        var photoStream = await graphClient.Users["userId"].Photo.Content.GetAsync();
        //        if (photoStream != null)
        //        {
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                await photoStream.CopyToAsync(memoryStream);
        //                return memoryStream.ToArray();
        //            }
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine($"Error fetching user photo: {exception.Message}");
        //        return null;
        //    }
        //}

        public async Task<byte[]> GetUserPhotoAsync(string userId)
        {
            try
            {
                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var tenantId = _configuration["AzureAd:TenantId"];
                var clientId = _configuration["AzureAd:ClientId"];
                var clientSecret = _configuration["AzureAd:ClientSecret"];
                var clientSecretCredential = new ClientSecretCredential(
                                tenantId, clientId, clientSecret);
                var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
                var users = await graphClient.Users.GetAsync();
                var photo = await graphClient.Users[userId].Photo.Content.GetAsync();
                if (photo != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await photo.CopyToAsync(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
                return null;

                //return File(photo, "image/jpeg");

                // Configure TokenCredentialOptions
                // var options = new TokenCredentialOptions
                // {
                //     AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                // };

                // // Use ClientSecretCredential for authentication
                // string _tenantId = _configuration["AzureAd:TenantId"];
                // string _clientId = _configuration["AzureAd:ClientId"];
                // string _clientSecret = _configuration["AzureAd:ClientSecret"];

                // var clientSecretCredential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret, options);

                // // Define scopes for the Graph API
                // var scopes = new[] { "https://graph.microsoft.com/.default" };

                // // Initialize GraphServiceClient
                // var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                // // Get user info (optional step, demonstrates additional capabilities)
                ////var user = await graphClient.Users[userId].GetAsync();
                // var user = await graphClient.Users[userId].GetAsync();
                // Console.WriteLine($"Fetched user: {user.DisplayName}");


                // // Get the user's photo
                // var photoStream = await graphClient.Users[userId].Photo.Content.GetAsync();
                // if (photoStream != null)
                // {
                //     using (var memoryStream = new MemoryStream())
                //     {
                //         await photoStream.CopyToAsync(memoryStream);
                //         return memoryStream.ToArray();
                //     }
                // }
                // else
                // {
                //     Console.WriteLine("No photo available for the specified user.");
                //     return null;
                // }


            }
            catch (Exception ex)
            {
                // Log the exception for troubleshooting
                Console.WriteLine($"Error fetching user photo: {ex.Message}");
                return null;
            }
        }
        //var client = _httpClientFactory.CreateClient();
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //    var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/photo/$value");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        return await response.Content.ReadAsByteArrayAsync();
        //    }

        //    throw new Exception("Unable to retrieve photo. Status code: " + response.StatusCode);
        //}
    }
}
