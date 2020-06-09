using System.Collections.Generic;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using LightInject;

namespace Comsec.SqlPrune.LightInject
{
    public class SqlPruneCoreCompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {
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