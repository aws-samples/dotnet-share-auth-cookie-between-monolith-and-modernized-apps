using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Modernized.Backend.ServiceB.Services
{
    public class CustomPersistKeysToAWSParameterStore : IXmlRepository
    {
        // NOTE: set the region here to match the region used when you created        
        private readonly RegionEndpoint region;
        private readonly GetParameterRequest paramRequest;
        private  AWSCredentials awsCredentials; 

        // Represents the data protection key name, whose value is used to encrypt/decrypt the shared cookie.
        private const string appKeyParamStoreName = "SharedCookieAppKey";

        public CustomPersistKeysToAWSParameterStore()
        {
            // region = RegionEndpoint.USEast1;
            paramRequest = new GetParameterRequest()
            {
                Name = appKeyParamStoreName
            };

            // LoadAwsCredentials();
        }

        private void LoadAwsCredentials()
        {
            var sharedFile = new SharedCredentialsFile();
            // FYI: Ensure you've an AWS profile defined within the '~\.aws\credentials' file and reference that name here (e.g. development).
            sharedFile.TryGetProfile("development", out var profile);
            AWSCredentialsFactory.TryGetAWSCredentials(profile, sharedFile, out awsCredentials);
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            List<XElement> keys = new List<XElement>();

            // This function must return a list of all the elements in the data store
            // using (var ssmClient = new AmazonSimpleSystemsManagementClient(awsCredentials, region))
            using (var ssmClient = new AmazonSimpleSystemsManagementClient())
            {
                try
                {
                    var response = ssmClient.GetParameterAsync(paramRequest).Result;
                    keys.Add(XElement.Parse(response.Parameter.Value));
                }
                catch (ParameterNotFoundException ex)
                {
                    /* FYI: 
                     * Normally, it's a bad practice to have empty exception caluse.
                     * However, we are expecting a specific exception and no action is required; becuase upon the first request, the AppKey will be generated.
                    */
                }
            }

            return keys;
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            ///* FYI: This function must write to the data store: this is optional if this
            // * service never provides the authentication, only uses it.
            // */
            //using (var ssmClient = new AmazonSimpleSystemsManagementClient(awsCredentials,region))
            //{
            //    var response = ssmClient.PutParameterAsync(new PutParameterRequest()
            //    {
            //        Type = ParameterType.String,
            //        Name = appKeyParamStoreName,
            //        Value = element.ToString()
            //    }).Result;
            //}

            throw new NotImplementedException();
        }

    }
}