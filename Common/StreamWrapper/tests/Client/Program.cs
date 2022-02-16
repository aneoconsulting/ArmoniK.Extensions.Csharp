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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Extensions.Common.StreamWrapper.Client;
using ArmoniK.Extensions.Common.StreamWrapper.Tests.Common;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;
using Serilog.Formatting.Compact;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.Extensions.Common.StreamWrapper.Tests.Client
{
  internal class Program
  {
    public static void Main(string[] args)
    {
      Run().GetAwaiter().GetResult();
    }

    public static async Task Run()
    {
      Console.WriteLine("Hello Test");

      var builder              = new ConfigurationBuilder().AddEnvironmentVariables();
      var configuration        = builder.Build();
      var configurationSection = configuration.GetSection(Options.Grpc.SettingSection);
      var endpoint             = configurationSection.GetValue<string>("Endpoint");

      Console.WriteLine($"endpoint : {endpoint}");
      var channel = GrpcChannel.ForAddress(endpoint);
      var client  = new Submitter.SubmitterClient(channel);

      string sessionId = System.Guid.NewGuid() + "my test session";
      string taskId    = System.Guid.NewGuid() + "my task";

      var taskOptions = new TaskOptions
      {
        MaxDuration = Duration.FromTimeSpan(TimeSpan.FromHours(1)),
        MaxRetries  = 2,
        Priority    = 1,
      };

      Console.WriteLine($"Creating Session");
      var session = client.CreateSession(new CreateSessionRequest
      {
        DefaultTaskOption = taskOptions,
        Id                = sessionId,
      });
      switch (session.ResultCase)
      {
        case CreateSessionReply.ResultOneofCase.Error:
          throw new Exception("Error while creating session : " + session.Error);
        case CreateSessionReply.ResultOneofCase.None:
          throw new Exception("Issue with Server !");
        case CreateSessionReply.ResultOneofCase.Ok:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      Console.WriteLine("Session Created");

      TestPayload payload = new TestPayload
      {
        Type      = TestPayload.TaskType.Compute,
        DataBytes = BitConverter.GetBytes(2),
      };

      var req = new TaskRequest
      {
        Id      = taskId,
        Payload = ByteString.CopyFrom(payload.Serialize()),
        ExpectedOutputKeys = { taskId },
      };

      Console.WriteLine("TaskRequest Created");

      var createTaskReply = await client.CreateTasksAsync(sessionId,
                                    taskOptions,
                                    new[] { req });

      switch (createTaskReply.DataCase)
      {
        case CreateTaskReply.DataOneofCase.NonSuccessfullIds:
          throw new Exception($"NonSuccessfullIds : {createTaskReply.NonSuccessfullIds}");
        case CreateTaskReply.DataOneofCase.None:
          throw new Exception("Issue with Server !");
        case CreateTaskReply.DataOneofCase.Successfull:
          Console.WriteLine("Task Created");
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      var request = new ResultRequest
      {
        Key     = taskId,
        Session = sessionId,
      };

      var waitForCompletion = client.WaitForCompletion(new WaitRequest
      {
        Filter = new TaskFilter
        {
          Session = new TaskFilter.Types.IdsRequest
          {
            Ids =
            {
              sessionId,
            },
          },
          //Included = new TaskFilter.Types.StatusesRequest
          //{
          //  Statuses =
          //  {
          //    TaskStatus.Completed,
          //  },
          //},
        },
        StopOnFirstTaskCancellation = true,
        StopOnFirstTaskError = true,
      });

      Console.WriteLine(waitForCompletion.ToString());

      var streamingCall = client.TryGetResultStream(request);

      try
      {
        await foreach (var reply in streamingCall.ResponseStream.ReadAllAsync())
        {
          switch (reply.TypeCase)
          {
            case ResultReply.TypeOneofCase.Result:
            {
              var resultPayload = TestPayload.Deserialize(reply.Result.Data.ToByteArray());

              Console.WriteLine($"Payload Type : {resultPayload.Type}");
              break;
            }
            case ResultReply.TypeOneofCase.None:
              throw new Exception("Issue with Server !");
            case ResultReply.TypeOneofCase.Error:
              throw new Exception($"Error in task {reply.Error.TaskId}");
            case ResultReply.TypeOneofCase.NotCompletedTask:
              throw new Exception($"Task {reply.NotCompletedTask} not completed");
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
      }
      catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
      {
        Console.WriteLine("Stream cancelled.");
      }
    }
  }
}