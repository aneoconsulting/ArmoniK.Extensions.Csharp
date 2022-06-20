// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using Grpc.Core;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace ArmoniK.Extensions.Common.StreamWrapper.Worker
{
  [PublicAPI]
  public class WorkerStreamWrapper : Api.gRPC.V1.Worker.WorkerBase
  {
    public           ILogger<WorkerStreamWrapper> logger_;
    private readonly ILoggerFactory               loggerFactory_;

    public WorkerStreamWrapper(ILoggerFactory loggerFactory)
    {
      logger_ = loggerFactory.CreateLogger<WorkerStreamWrapper>();
      loggerFactory_ = loggerFactory;
    }

    /// <inheritdoc />
    public sealed override async Task Process(IAsyncStreamReader<ProcessRequest> requestStream,
                                              IServerStreamWriter<ProcessReply>  responseStream,
                                              ServerCallContext                  context)
    {
      var taskHandler = await TaskHandler.Create(requestStream,
                                                 responseStream,
                                                 new()
                                                 {
                                                   DataChunkMaxSize = 50 * 1024,
                                                 },
                                                 context.CancellationToken,
                                                 loggerFactory_.CreateLogger<TaskHandler>());

      logger_.LogDebug("Execute Process");
      var output = await Process(taskHandler);

      await responseStream.WriteAsync(new ()
                                      {
                                        Output = output,
                                      });
      if (await requestStream.MoveNext(context.CancellationToken))
        throw new InvalidOperationException("The request stream is expected to be finished.");
    }

    public virtual Task<Output> Process(ITaskHandler taskHandler)
      => throw new RpcException(new(StatusCode.Unimplemented,
                                    ""));

    public override Task<HealthCheckReply> HealthCheck(Empty             request,
                                                       ServerCallContext context)
      => Task.FromResult(new HealthCheckReply
      {
        Status = HealthCheckReply.Types.ServingStatus.Serving,
      });
  }
}
