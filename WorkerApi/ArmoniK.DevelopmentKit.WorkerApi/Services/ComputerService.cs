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

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.WorkerApi.Services
{
    public class ComputerService : Core.gRPC.V1.ComputerService.ComputerServiceBase
    {
        private ILogger<ComputerService> Logger { get; set; }

        public ServiceRequestContext ServiceRequestContext { get; private set; }

        public IConfiguration Configuration { get; }

        public ComputerService(IConfiguration configuration,
            ILogger<ComputerService> logger,
            ServiceRequestContext serviceRequestContext)
        {
            Configuration = configuration;
            Logger = logger;
            ServiceRequestContext = serviceRequestContext;
           
        }


        /// <inheritdoc />
        public override Task<ComputeReply> Execute(ComputeRequest request, ServerCallContext context)
        {
            try
            {
                Logger.LogInformation(
                    $"Receive new task Session        {request.Session}#{request.Subsession} -> task {request.TaskId}");
                Logger.LogInformation(
                    $"Previous Session#SubSession was {ServiceRequestContext.SessionId?.Session ?? "NOT SET"}");
                SessionId sessionIdCaller = new SessionId()
                {
                    Session = request.Session,
                    SubSession = request.Subsession
                };
                if (ServiceRequestContext.IsNewSessionId(sessionIdCaller))
                {
                    ServiceRequestContext.SessionId = sessionIdCaller;

                    var pathToZipFile =
                        $"{Configuration["target_data_path"]}/{request.TaskOptions[AppsOptions.GridAppNameKey]}-v{request.TaskOptions[AppsOptions.GridAppVersionKey]}.zip";


                    ServiceRequestContext.GridWorker?.SessionFinalize();

                    var engineTypeName = request.TaskOptions.ContainsKey(AppsOptions.EngineTypeNameKey)
                        ? request.TaskOptions[AppsOptions.EngineTypeNameKey]
                        : EngineType.Symphony.ToString();

                    if (ServiceRequestContext.AppsLoader != null &&
                        ServiceRequestContext.AppsLoader.RequestNewAssembly(
                            engineTypeName,
                            pathToZipFile))
                    {
                        ServiceRequestContext.AppsLoader.Dispose();
                        ServiceRequestContext.AppsLoader = null;
                    }
                 

                    ServiceRequestContext.AppsLoader ??= new AppsLoader(Configuration, ServiceRequestContext.LoggerFactory,
                        engineTypeName,
                        pathToZipFile);

                    ServiceRequestContext.GridWorker =
                        ServiceRequestContext.AppsLoader.GetGridWorkerInstance(Configuration, ServiceRequestContext.LoggerFactory);

                    ServiceRequestContext.GridWorker.Configure(Configuration,
                        request.TaskOptions,
                        ServiceRequestContext.AppsLoader);

                    ServiceRequestContext.GridWorker.InitializeSessionWorker(ServiceRequestContext.SessionId
                        .PackSessionId());
                }

                ServiceRequestContext.SessionId = sessionIdCaller;

                Logger.LogInformation($"Executing task {request.TaskId}");

                var result = ServiceRequestContext.GridWorker.Execute(ServiceRequestContext.SessionId.PackSessionId(),
                    request);


                return Task.FromResult(new ComputeReply
                {
                    Result = ByteString.CopyFrom(result),
                });
            }
            catch (WorkerApiException we)
            {
                Logger.LogError(ExtractException(we));
                throw new RpcException(new Status(StatusCode.Aborted,
                    ExtractException(we)));
            }
            catch (Exception e)
            {
                Logger.LogError(ExtractException(e));
                throw new RpcException(new Status(StatusCode.Aborted,
                    ExtractException(e)));
            }
        }

        private static string ExtractException(Exception e)
        {
            int level = 1;
            Exception current = e;
            List<Exception> exList = new();
            exList.Add(e);

            while (current.InnerException != null)
            {
                current = current.InnerException;
                exList.Add(current);
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
                message +=
                    $"\nFrom Exception : {exception.GetType()} message : {exception.Message}\n\t{string.Join("\n\t", exception.StackTrace)}";
            }

            return message;
        }
    }
}