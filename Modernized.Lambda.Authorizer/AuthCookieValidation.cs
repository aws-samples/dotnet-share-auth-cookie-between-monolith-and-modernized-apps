using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Modernized.ApiGateway.LambdaAuthorizer.Error;
using Modernized.ApiGateway.LambdaAuthorizer.Services;
using System;
using System.Collections.Generic;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Modernized.ApiGateway.LambdaAuthorizer
{
    public class AuthCookieValidation
    {
        // Attributes of the Shared cookie eco-system.
        private const string _sharedAppNameKey = "SharedAppName";
        private const string _sharedSchemeNameKey = "SharedSchemeName";
        private string _sharedAppNameValue;
        private string _sharedSchemeNameValue;
        private string _sharedAuthCookie = string.Empty;

        // FYI: You can read more about the authorizerPayloadFormat request/response payload here: https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-lambda-authorizer.html 
        public bool FunctionHandler(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
        {
            bool isAuthorized = false;

            try
            {   
                EnsurePreRequisites(input.Headers);

                // Validate the Auth cookie
                var isAuthCookieValid = ValidateAuthCookie(_sharedAuthCookie);
                
                isAuthorized = isAuthCookieValid ? true : false;

                #region Version 1
                // Prepare the Auth validation response
                //APIGatewayCustomAuthorizerPolicy policy = new APIGatewayCustomAuthorizerPolicy
                //{
                //    Version = "2012-10-17",
                //    Statement = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement>()
                //};

                //policy.Statement.Add(new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement
                //{
                //    Action = new HashSet<string>(new string[] { "execute-api:Invoke" }),
                //    Effect = isAuthorized ? "Allow" : "Deny",
                //    Resource = new HashSet<string>(new string[] { input.RequestContext.Http.Path }) //MethodArn 

                //});

                //return new APIGatewayCustomAuthorizerResponse
                //{
                //    PrincipalID = isAuthorized ? "TBD" : "TBD",
                //    PolicyDocument = policy
                //};
                #endregion

            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedException)
                    throw;

                // log the exception and return a 401
                LambdaLogger.Log(ex.ToString());

                //throw new UnauthorizedException();
            } 
            
            return isAuthorized;
        }

        /// <summary>
        /// Helps ensure the required information (e.g. Cookie, Envrionment variables etc.) before performing any business logic.                
        /// </summary>
        private void EnsurePreRequisites(IDictionary<string, string> headers)
        {               
            headers.TryGetValue("Cookie", out _sharedAuthCookie);
                
            _sharedAppNameValue = Environment.GetEnvironmentVariable(_sharedAppNameKey);

            _sharedSchemeNameValue = Environment.GetEnvironmentVariable(_sharedSchemeNameKey);

            if (string.IsNullOrEmpty(_sharedAppNameValue))
                throw new Exception($"Ensure the,{_sharedAppNameKey}, environment variable is defined.");

            if (string.IsNullOrEmpty(_sharedAppNameValue))
                throw new Exception($"Ensure the,{_sharedSchemeNameKey}, environment variable is defined.");                   
        }

        /// <summary>
        /// Helper function needed to validate Authentication.
        /// </summary>
        private bool ValidateAuthCookie(string cookie)
        {
            bool isAuthCookieValid = false;

            // Short circuit it.
            if (string.IsNullOrEmpty(cookie))
                return isAuthCookieValid;

            // Try to decrypt the cookie.
            var dataProtector = GetDataProtector();
            try
            {
                dataProtector.Unprotect(cookie);  // TODO::Enhancements:: 1/ Load the plain text form of the unprotected data into a strongly typed Identity object, 2/ validate additional identity attributes etc.
                isAuthCookieValid = true;
            }
            catch (Exception ex) // commaon failure are caused by: invalid cookie, invalid encryption key etc.)
            {
                // Log exception
                LambdaLogger.Log($"Error:decrypting the cookie::[Message]::{ex.Message}, ::[InnerException]:: {ex.InnerException.Message}, ::[StackTracek]::{ex.StackTrace}");

                isAuthCookieValid = false;
            }

            return isAuthCookieValid;
        }

        /// <summary>
        /// Create an instance of the DataProtector that would perform the encrypted cookie validation.
        /// WHY? We need an implementation of the IDataProtector to use the 'Unprotect' feature.  Moreover, AFAIK, going through the .NET Core DI is about the only straight forward way to get hold off it.
        /// </summary>
        private IDataProtector GetDataProtector()
        {
            // Get the DI going
            var services = new ServiceCollection();

            // Register the Data Protection and its configurations
            services.AddDataProtection()
                //.PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"C:\SharedCookieAppKey")) // FYI: the same key must be shared by all the Apps sharing this cookie.
                .SetApplicationName(_sharedAppNameValue) // FYI: the App name value (e.g. "SharedCookieApp") must be same across all the Apps sharing this cookie.                
            #region 
                // FYI: comment this region and uncomment the 'PersistKeysToFileSystem' to make shared cookie auth work without any central repository (e.g. AWS Parameter store)
                .Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
                {
                    return new ConfigureOptions<KeyManagementOptions>(options =>
                    {
                        options.XmlRepository = new CustomPersistKeysToAWSParameterStore();
                    });
                });
            #endregion

            var dataProvider = services.BuildServiceProvider().GetDataProtectionProvider();

            var dataProtector =
                dataProvider.CreateProtector(
                    "Microsoft.AspNetCore.Authentication.Cookies." +
                    "CookieAuthenticationMiddleware",
                    _sharedSchemeNameValue, // FYI: this auth scheme that you choose (e.g. "Identity.Application") must be same across the shared cookie apps.
                    "v2");

            return dataProtector;
        }
    }
}
