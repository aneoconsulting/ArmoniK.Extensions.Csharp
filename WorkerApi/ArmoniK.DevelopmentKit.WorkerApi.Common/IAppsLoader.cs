using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public interface IAppsLoader : IDisposable
  {
    T GetServiceContainerInstance<T>(string gridAppNamespace, string gridServiceName);
  }
}
