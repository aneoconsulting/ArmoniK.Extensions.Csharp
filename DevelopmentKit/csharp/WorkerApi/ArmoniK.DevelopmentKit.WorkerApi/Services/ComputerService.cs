using System;
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC;
using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

using Google.Protobuf;

using Grpc.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.WorkerApi.Services
{
  public class ComputerService : Core.gRPC.V1.ComputerService.ComputerServiceBase
  {
    private          string                   sessionId;
    private          AppsLoader               appsLoader;
    private          IGridWorker              gridWorker;
    private readonly ILogger<ComputerService> logger_;

    public ComputerService(IConfiguration           configuration,
                           ILogger<ComputerService> logger)
    {
      Configuration = configuration;
      logger_       = logger;
    }

    public IConfiguration Configuration { get; }


    /// <inheritdoc />
    public override Task<ComputeReply> Execute(ComputeRequest request, ServerCallContext context)
    {
      try
      {
        if (string.IsNullOrEmpty(sessionId) || !sessionId.Equals(request.Session))
        {
          string assembly_path = String.Format("/tmp/packages/{0}/{1}/{0}.dll",
                                               request.TaskOptions[AppsOptions.GridAppNameKey],
                                               request.TaskOptions[AppsOptions.GridAppVersionKey]
          );
          string pathToZipFile = String.Format("{0}/{1}-v{2}.zip",
                                               Configuration.GetSection("Volumes")["target_app_path"],
                                               request.TaskOptions[AppsOptions.GridAppNameKey],
                                               request.TaskOptions[AppsOptions.GridAppVersionKey]
          );
          sessionId = $"{request.Session}#{request.Subsession}";

          if (gridWorker != null && appsLoader != null)
          {
            gridWorker.SessionFinilize();
            appsLoader.Dispose();
          }

          appsLoader = new AppsLoader(Configuration,
                                      assembly_path,
                                      pathToZipFile);
          //request.TaskOptions["GridWorkerNamespace"]
          gridWorker = appsLoader.getGridWorkerInstance();
          gridWorker.Configure(Configuration,
                               request.TaskOptions,
                               appsLoader);

          gridWorker.InitializeSessionWorker(sessionId);
        }

        var result = gridWorker.Execute(sessionId, request);


        return Task.FromResult(new ComputeReply { Result = ByteString.CopyFrom(result) });
      }
      catch (WorkerApiException we)
      {
        logger_.LogError(we.Message);
        throw; // TODO manage error code to return for gRPC
      }
      catch (Exception e)
      {
        logger_.LogError(e.Message);
        throw; // TODO manage error code to return for gRPC
      }
    }
  }
}