using System.Collections.Generic;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using LightInject;

namespace Comsec.SqlPrune.LightInject
{
    public class SqlPruneCoreCompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.Register<IAmazonS3>(f =>
            {
                var region = f.TryGetInstance<RegionEndpoint>();

                var credentials = f.TryGetInstance<AWSCredentials>();

                return credentials != null
                    ? region != null
                        ? new AmazonS3Client(credentials, region)
                        : new AmazonS3Client(credentials)
                    : new AmazonS3Client();
            });

            serviceRegistry.Register<IFileProvider, LocalFileSystemProvider>("local");
            serviceRegistry.Register<IFileProvider, S3Provider>("s3");

            serviceRegistry.Register<IEnumerable<IFileProvider>>(f => new[]
                                                                      {
                                                                          f.GetInstance<IFileProvider>("local"),
                                                                          f.GetInstance<IFileProvider>("s3")
                                                                      });
            
            serviceRegistry.Register<IPruneService, PruneService>();
        }
    }
}