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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
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
    private readonly ILogger<ComputerService> logger_;

    public ServiceRequestContext ServiceRequestContext { get; private set; }

    public IConfiguration Configuration { get; }

    public ComputerService(IConfiguration           configuration,
                           ILogger<ComputerService> logger,
                           ServiceRequestContext    serviceRequestContext)
    {
      Configuration         = configuration;
      logger_               = logger;
      ServiceRequestContext = serviceRequestContext;
    }


    /// <inheritdoc />
    public override Task<ComputeReply> Execute(ComputeRequest request, ServerCallContext context)
    {
      try
      {
        logger_.LogInformation($"Receive new task Session        {request.Session}#{request.Subsession} -> task {request.TaskId}");
        logger_.LogInformation($"Previous Session#SubSession was {ServiceRequestContext.SessionId?.Session ?? "NOT SET"}");
        SessionId sessionIdCaller = new SessionId()
        {
          Session    = request.Session,
          SubSession = request.Subsession
        };
        if (ServiceRequestContext.IsNewSessionId(sessionIdCaller))
        {
          var assemblyPath = String.Format("/tmp/packages/{0}/{1}/{0}.dll",
                                           request.TaskOptions[AppsOptions.GridAppNameKey],
                                           request.TaskOptions[AppsOptions.GridAppVersionKey]);
          ServiceRequestContext.SessionId = sessionIdCaller;

          var pathToZipFile =
            $"{Configuration["target_data_path"]}/{request.TaskOptions[AppsOptions.GridAppNameKey]}-v{request.TaskOptions[AppsOptions.GridAppVersionKey]}.zip";


          ServiceRequestContext.GridWorker?.SessionFinalize();

          if (ServiceRequestContext.AppsLoader != null &&
              ServiceRequestContext.AppsLoader.RequestNewAssembly(request.TaskOptions[AppsOptions.EngineTypeNameKey],
                                                                  pathToZipFile))
          {
            ServiceRequestContext.AppsLoader.Dispose();
            ServiceRequestContext.AppsLoader = null;
          }

          ServiceRequestContext.AppsLoader ??= new AppsLoader(Configuration,
                                                              request.TaskOptions[AppsOptions.EngineTypeNameKey],
                                                              pathToZipFile);


          ServiceRequestContext.GridWorker = ServiceRequestContext.AppsLoader.GetGridWorkerInstance(Configuration);
          ServiceRequestContext.GridWorker.Configure(Configuration,
                                                     request.TaskOptions,
                                                     ServiceRequestContext.AppsLoader);

          ServiceRequestContext.GridWorker.InitializeSessionWorker(ServiceRequestContext.SessionId.PackSessionId());
        }

        ServiceRequestContext.SessionId = sessionIdCaller;

        logger_.LogInformation($"Executing task {request.TaskId}");

        var result = ServiceRequestContext.GridWorker.Execute(ServiceRequestContext.SessionId.PackSessionId(),
                                                              request);


        return Task.FromResult(new ComputeReply
        {
          Result = ByteString.CopyFrom(result),
        });
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
      int             level   = 1;
      Exception       current = e;
      List<Exception> exList  = new();
      exList.Add(e);

      while (current.InnerException != null)
      {
        current = current.InnerException;
        exList.Add(current);
        //message += $"\nInnerException : {current.GetType()} message : {current.Message}\n\t{string.Join("\n\t", current.StackTrace)}";
        level++;

        if (level > 30)
          break;
      }

      exList.Reverse();
      var message = $"Root Exception cause : {exList[0].GetType()} | message : {exList[0].Message}" +
                    $"\n\tReversed StackTrace : \n\t{string.Join("\n\t", exList[0].StackTrace)}";

      exList.RemoveAt(0);


      foreach (var exception in exList)
      {
        message += $"\nFrom Exception : {exception.GetType()} message : {exception.Message}\n\t{string.Join("\n\t", exception.StackTrace)}";
      }

      return message;
    }
  }
}