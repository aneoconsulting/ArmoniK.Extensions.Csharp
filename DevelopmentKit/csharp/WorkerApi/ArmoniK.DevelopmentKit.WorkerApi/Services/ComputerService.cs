// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;

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
    private          AppsLoader               appsLoader_;
    private          IGridWorker              gridWorker_;
    private readonly ILogger<ComputerService> logger_;


    public IConfiguration Configuration { get; }
    private string SessionId { get; set; }

    public ComputerService(IConfiguration           configuration,
                           ILogger<ComputerService> logger)
    {
      Configuration = configuration;
      logger_       = logger;
    }


    /// <inheritdoc />
    public override Task<ComputeReply> Execute(ComputeRequest request, ServerCallContext context)
    {
      try
      {
        logger_.LogInformation($"Receive new task Session        {request.Session}#{request.Subsession} -> task {request.TaskId}");
        logger_.LogInformation($"Previous Session#SubSession was {SessionId ?? "NOT SET"}");

        if (string.IsNullOrEmpty(SessionId) || !SessionId.Equals($"{request.Session}#{request.Subsession}"))
        {
          var assemblyPath = String.Format("/tmp/packages/{0}/{1}/{0}.dll",
                                           request.TaskOptions[AppsOptions.GridAppNameKey],
                                           request.TaskOptions[AppsOptions.GridAppVersionKey]
          );
          SessionId = $"{request.Session}#{request.Subsession}";
          var pathToZipFile =
            $"{Configuration["target_data_path"]}/{request.TaskOptions[AppsOptions.GridAppNameKey]}-v{request.TaskOptions[AppsOptions.GridAppVersionKey]}.zip";



          gridWorker_?.SessionFinalize();

          if (appsLoader_ != null &&
              appsLoader_.RequestNewAssembly(request.TaskOptions[AppsOptions.EngineTypeNameKey],
                                             pathToZipFile))
          {
            appsLoader_.Dispose();
            appsLoader_ = null;
          }

          appsLoader_ ??= new AppsLoader(Configuration,
                                         request.TaskOptions[AppsOptions.EngineTypeNameKey],
                                         pathToZipFile);


          gridWorker_ = appsLoader_.GetGridWorkerInstance();
          gridWorker_.Configure(Configuration,
                                request.TaskOptions,
                                appsLoader_);

          gridWorker_.InitializeSessionWorker(SessionId);
        }

        logger_.LogInformation($"Executing task {request.TaskId}");

        var result = gridWorker_.Execute(SessionId,
                                         request);


        return Task.FromResult(new ComputeReply { Result = ByteString.CopyFrom(result) });
      }
      catch (WorkerApiException we)
      {
        logger_.LogError(ExtractException(we));
        throw new RpcException(new Status(StatusCode.Aborted,
                                          ExtractException(we)));
      }
      catch (Exception e)
      {
        logger_.LogError(ExtractException(e));
        throw new RpcException(new Status(StatusCode.Aborted,
                                          ExtractException(e)));
      }
    }

    private static string ExtractException(Exception e)
    {
      var message = $"Error Message : {e.Message}" +
             $"\n\tStackTrace : {string.Join("\n\t", e.StackTrace)}";
      int       level   = 1;
      Exception current = e;
      while (current.InnerException != null)
      {
        message = message + $"\nInnerException Msg : {current.Message}\n\t{string.Join("\n\t", current.InnerException.StackTrace)}";
        level++;

        if (level > 5)
          break;

        current = current.InnerException;
      }

      return message;
    }
  }
}