using Azure.Core;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Microsoft.Graph.Users.Item.SendMail;
using MaxSys.Models;

namespace E_Template.Helpers
{
    public class GraphHelper
    {
        // Settings object
        private static Settings? _settings;
        // Client secret credential
        private static ClientSecretCredential? _clientSecretCredential;
        // Client configured with application authentication
        private static GraphServiceClient? _appClient;

        public static void InitializeGraphForAppAuth(Settings settings)
        {
            _settings = settings;

            // Initialize the credential using client secret
            _clientSecretCredential = new ClientSecretCredential(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret
            );

            // Initialize Graph client with the credential and desired scopes
            _appClient = new GraphServiceClient(_clientSecretCredential, _settings.GraphUserScopes);
        }

        public static async Task<string> GetAppTokenAsync()
        {
            // Ensure credential isn't null
            _ = _clientSecretCredential ??
                throw new NullReferenceException("Graph has not been initialized for app auth");

            // Ensure scopes isn't null
            _ = _settings?.GraphUserScopes ??
                throw new ArgumentNullException("Argument 'scopes' cannot be null");

            // Request token with given scopes
            var context = new TokenRequestContext(_settings.GraphUserScopes);
            var response = await _clientSecretCredential.GetTokenAsync(context);
            return response.Token;
        }

        public static Task<User?> GetUserAsync(string userId)
        {
            // Ensure client isn't null
            _ = _appClient ??
                throw new NullReferenceException("Graph has not been initialized for app auth");

            return _appClient.Users[userId]
                .GetAsync((config) =>
                {
                    config.QueryParameters.Select = new[] { "displayName", "mail", "userPrincipalName" };
                });
        }

        public static async Task<bool> SendEmailAsync(Emai_Template model)
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ??
                    throw new NullReferenceException("Graph has not been initialized for app auth");

                if (model == null)
                {
                    return false;
                }

                model.mainTemplate = await model.EmailBodyTemplate();
                model.bodyContent = model.mainTemplate.ToString().Replace("[BODY]", model.subTemplate);
                (bool success, string template) returnTemp = model.WordReplacer(model.bodyContent);
                if (returnTemp.success == false)
                {
                    return false;
                }
                model.bodyContent = returnTemp.template;

                // Create the email message
                var message = new Message
                {
                    Subject = model.Subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html, // Change to HTML content type
                        Content = model.bodyContent
                    },
                    ToRecipients = model.Recipient ?? new List<Recipient>(),
                    CcRecipients = model.CC ?? new List<Recipient>(),
                    BccRecipients = model.BCC ?? new List<Recipient>()
                };


                // Create the request body
                var requestBody = new SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = false
                };

                // Send the email
                if (model.Setting_Setup != null && !string.IsNullOrEmpty(model.Setting_Setup.SMTP_ACCOUNT)) {  
                    await _appClient.Users[model.Setting_Setup.SMTP_ACCOUNT].SendMail.PostAsync(requestBody);
                    return true;
                }

                return false;

            }
            catch (Exception ex)
            {
                return false;
            }
        }


    }
}
