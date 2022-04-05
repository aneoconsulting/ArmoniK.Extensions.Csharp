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
using System.Linq;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Extensions.Common.StreamWrapper.Tests.Common;
using ArmoniK.Extensions.Common.StreamWrapper.Worker;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.Extensions.Common.StreamWrapper.Tests.Server
{
  public class WorkerService : WorkerStreamWrapper
  {
    public WorkerService(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
    }

    public override async Task<Output> Process(ITaskHandler taskHandler)
    {
      var output = new Output();
      logger_.LogDebug("ExpectedResults {expectedResults}",
                       taskHandler.ExpectedResults);
      try
      {
        var payload = TestPayload.Deserialize(taskHandler.Payload);
        if (payload != null)
          switch (payload.Type)
          {
            case TestPayload.TaskType.Compute:
            {
              var input = BitConverter.ToInt32(payload.DataBytes);
              var result = new TestPayload
              {
                Type      = TestPayload.TaskType.Result,
                DataBytes = BitConverter.GetBytes(input * input),
              };
              await taskHandler.SendResult(payload.ResultKey,
                                           result.Serialize());
              output = new Output
              {
                Ok     = new Empty(),
                Status = TaskStatus.Completed,
              };
              break;
            }
            case TestPayload.TaskType.Result:
              break;
            case TestPayload.TaskType.Undefined:
              break;
            case TestPayload.TaskType.None:
              break;
            case TestPayload.TaskType.Error:
              throw new Exception("Expected exception in Task");
            case TestPayload.TaskType.Transfer:
            {
              var taskId = "transfer" + Guid.NewGuid();

              payload.Type = TestPayload.TaskType.Compute;
              var req = new TaskRequest
              {
                Id      = taskId,
                Payload = ByteString.CopyFrom(payload.Serialize()),
                ExpectedOutputKeys =
                {
                  payload.ResultKey,
                },
              };
              await taskHandler.CreateTasksAsync(new[] { req });
              logger_.LogDebug("Sub Task created : {subtaskid}",
                               taskId);
              output = new Output
              {
                Ok     = new Empty(),
                Status = TaskStatus.Completed,
              };
            }
              break;
            case TestPayload.TaskType.DatadepTransfer:
            {
              var         taskId = "DatadepTransfer-" + Guid.NewGuid();
              TaskRequest req;
              if (taskHandler.ExpectedResults.Count != 2)
                throw new ArgumentOutOfRangeException();

              var resId = taskHandler.ExpectedResults.First();
              var depId = taskHandler.ExpectedResults.Last();
              var input = BitConverter.ToInt32(payload.DataBytes);

              payload.Type = TestPayload.TaskType.DatadepCompute;

              req = new TaskRequest
              {
                Id      = taskId,
                Payload = ByteString.CopyFrom(payload.Serialize()),
                ExpectedOutputKeys =
                {
                  resId,
                },
                DataDependencies =
                {
                  depId,
                },
              };

              logger_.LogDebug("DataDepTransfer Input {input}", input);
              var result = new TestPayload
              {
                Type      = TestPayload.TaskType.Result,
                DataBytes = BitConverter.GetBytes(input * input),
              };
              await taskHandler.SendResult(depId,
                                           result.Serialize());

              await taskHandler.CreateTasksAsync(new[] { req });
              logger_.LogDebug("Sub Task created : {subtaskid}",
                               taskId);

              output = new Output
              {
                Ok     = new Empty(),
                Status = TaskStatus.Completed,
              };
            }
              break;
            case TestPayload.TaskType.DatadepCompute:
            {
              if (taskHandler.ExpectedResults.Count != 1)
                throw new ArgumentOutOfRangeException();
              if (taskHandler.DataDependencies.Count != 1)
                throw new ArgumentOutOfRangeException();

              var resId    = taskHandler.ExpectedResults.First();
              var input    = BitConverter.ToInt32(payload.DataBytes);
              var payload2 = TestPayload.Deserialize(taskHandler.DataDependencies.Values.First());

              if (payload2.Type != TestPayload.TaskType.Result)
                throw new Exception();

              var input2 = BitConverter.ToInt32(payload2.DataBytes);
              
              logger_.LogDebug("DataDepCompute Input1 {input}",
                               input);
              logger_.LogDebug("DataDepCompute Input2 {input}",
                               input2);

              var result = new TestPayload
              {
                Type      = TestPayload.TaskType.Result,
                DataBytes = BitConverter.GetBytes(input * input + input2),
              };
              await taskHandler.SendResult(resId,
                                           result.Serialize());

              output = new Output
              {
                Ok     = new Empty(),
                Status = TaskStatus.Completed,
              };
            }
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
      }
      catch (Exception ex)
      {
        logger_.LogError(ex,
                         "Error while computing task");

        output = new Output
        {
          Error = new Output.Types.Error
          {
            Details = ex.Message + ex.StackTrace,
          },
          Status = TaskStatus.Error,
        };
      }

      return output;
    }
  }
}