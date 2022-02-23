// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
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
    public override async Task<Output> Process(ITaskHandler taskHandler)
    {
      var output = new Output();
      logger_.LogDebug("ExpectedResults {expectedResults}", taskHandler.ExpectedResults);
      try
      {
        var payload = TestPayload.Deserialize(taskHandler.Payload);
        if (payload != null)
        {
          switch (payload.Type)
          {
            case TestPayload.TaskType.Compute:
            {
              int input = BitConverter.ToInt32(payload.DataBytes);
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
              logger_.LogDebug("Sub Task created : {subtaskid}", taskId);
              output = new Output
              {
                Ok     = new Empty(),
                Status = TaskStatus.Completed,
              };
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
      }
      catch (Exception ex)
      {
        logger_.LogError(ex, "Error while computing task");

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

    public WorkerService(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
    }
  }
}