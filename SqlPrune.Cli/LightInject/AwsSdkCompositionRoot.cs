using System;
using System.ComponentModel;
using System.IO;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using LightInject;
using Sugar.Command.Binder;

namespace Comsec.SqlPrune.LightInject
{
    public class AwsSdkCompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.Register<IAmazonS3>(f =>
            {
                IAmazonS3 client;

                RegionEndpoint region = null;

                var parameters = f.GetInstance<Parameters>();

                // Region override
                var regionName = parameters.AsString("region", null);
                if (!string.IsNullOrEmpty(regionName))
                {
                    region = RegionEndpoint.GetBySystemName(regionName);
                }

                // Profile
                var profileName = parameters.AsString("profile", null);
                if (string.IsNullOrEmpty(profileName))
                {
                    client = region == null ? new AmazonS3Client() : new AmazonS3Client(region);
                }
                else
                {
                    var profilesLocation = parameters.AsString("profiles-location", null);
                    if (string.IsNullOrEmpty(profilesLocation))
                    {
                        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                        profilesLocation = Path.Combine(userFolder, @".aws\credentials");
                    }

                    var chain = new CredentialProfileStoreChain(profilesLocation);

                    if (!chain.TryGetAWSCredentials(profileName, out var credentials))
                    {
                        throw new AmazonClientException("Unable to initialise AWS credentials from profile name");
                    }

                    client = region == null
                        ? new AmazonS3Client(credentials)
                        : new AmazonS3Client(credentials, region);
                }

                return client;
            });
        }
    }
}