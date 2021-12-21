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
    private          string                   sessionId_;
    private          AppsLoader               appsLoader_;
    private          IGridWorker              gridWorker_;
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
        logger_.LogInformation($"Receive new task Session        {request.Session}#{request.Subsession} -> task {request.TaskId}");
        logger_.LogInformation($"Previous Session#SubSession was {sessionId_ ?? "NOT SET"}");

        if (string.IsNullOrEmpty(sessionId_) || !sessionId_.Equals($"{request.Session}#{request.Subsession}"))
        {
          var assemblyPath = String.Format("/tmp/packages/{0}/{1}/{0}.dll",
                                           request.TaskOptions[AppsOptions.GridAppNameKey],
                                           request.TaskOptions[AppsOptions.GridAppVersionKey]
                                          );
          var pathToZipFile = String.Format("{0}/{1}-v{2}.zip",
                                            Configuration["target_data_path"],
                                            request.TaskOptions[AppsOptions.GridAppNameKey],
                                            request.TaskOptions[AppsOptions.GridAppVersionKey]
                                           );
          sessionId_ = $"{request.Session}#{request.Subsession}";

          if (gridWorker_ != null && appsLoader_ != null)
          {
            gridWorker_.SessionFinalize();
            appsLoader_.Dispose();
          }

          appsLoader_ = new(Configuration,
                            assemblyPath,
                            pathToZipFile);
          //request.TaskOptions["GridWorkerNamespace"]
          gridWorker_ = appsLoader_.GetGridWorkerInstance();
          gridWorker_.Configure(Configuration,
                               request.TaskOptions,
                               appsLoader_);

          gridWorker_.InitializeSessionWorker(sessionId_);
        }

        logger_.LogInformation($"Executing task {request.TaskId}");
        
        var result = gridWorker_.Execute(sessionId_, request);


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