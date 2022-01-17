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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.Samples.EndToEndTests.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.Samples.EndToEndTests.Tests
{
  public class Client_1 : ClientSideBaseTest<Client_1>
  {
    [EntryPoint]
    public void EntryPoint(string[] args)
    {
     

      var client = new ArmonikSymphonyClient(Configuration);

      Log.LogInformation("Configure taskOptions");
      var taskOptions = InitializeTaskOptions();

      var sessionId = client.CreateSession(taskOptions);

      Log.LogInformation($"New session created : {sessionId}");

      Log.LogInformation("Running End to End test to compute Square value with SubTasking");
      ClientStartup1(client);
    }

    /// <summary>
    ///   Initialize Setting for task i.e :
    ///   Duration :
    ///   The max duration of a task
    ///   Priority :
    ///   Work in Progress. Setting priority of task
    ///   AppName  :
    ///   The name of the Application dll (Without Extension)
    ///   VersionName :
    ///   The version of the package to unzip and execute
    ///   Namespace :
    ///   The namespace where the service can find
    ///   the ServiceContainer object develop by the customer
    /// </summary>
    /// <returns></returns>
    public override TaskOptions InitializeTaskOptions()
    {
      TaskOptions                                     = base.InitializeTaskOptions();

      TaskOptions.Options[AppsOptions.GridAppNameKey] = "ArmoniK.Samples.EndToEndTests.Tests";

      TaskOptions.Options[AppsOptions.GridAppVersionKey] = "1.0.0";

      TaskOptions.Options[AppsOptions.GridAppNamespaceKey] = "ArmoniK.Samples.EndToEndTests";

      return TaskOptions;
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="client">The client API to connect to the Control plane Service</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private static byte[] WaitForSubTaskResult(ArmonikSymphonyClient client, string taskId)
    {
      client.WaitSubtasksCompletion(taskId);
      var taskResult = client.GetResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      if (!string.IsNullOrEmpty(result.SubTaskId))
      {
        client.WaitSubtasksCompletion(result.SubTaskId);
        taskResult = client.GetResult(result.SubTaskId);
      }

      return taskResult;
    }

    /// <summary>
    ///   The first test developed to validate dependencies subTasking
    /// </summary>
    /// <param name="client"></param>
    public void ClientStartup1(ArmonikSymphonyClient client)
    {
      var numbers = new List<int>
      {
        1,
        2,
        3,
      };
      var clientPaylaod = new ClientPayload
      {
        IsRootTask = true,
        Numbers    = numbers,
        Type       = ClientPayload.TaskType.ComputeSquare,
      };
      var taskId = client.SubmitTask(clientPaylaod.Serialize());

      var taskResult = WaitForSubTaskResult(client,
                                            taskId);
      var result = ClientPayload.Deserialize(taskResult);

      Log.LogInformation($"output result : {result.Result}");
    }
  }
}