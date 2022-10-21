// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Client.Unified.Exceptions;
using ArmoniK.DevelopmentKit.Client.Unified.Factory;
using ArmoniK.DevelopmentKit.Client.Unified.Services;
using ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client.Tests.CheckSubtaskingTreeUnifiedApi;

public class SubtaskingTreeUnifiedApiClient : ClientBaseTest<SubtaskingTreeUnifiedApiClient>, IServiceInvocationHandler
{
  private readonly Dictionary<string, int> expectedIntegerResults_ = new();

  public SubtaskingTreeUnifiedApiClient(IConfiguration configuration,
                                        ILoggerFactory loggerFactory)
    : base(configuration,
           loggerFactory)
  {
  }

  /// <summary>
  ///   The callBack method which has to be implemented to retrieve error or exception
  /// </summary>
  /// <param name="e">The exception sent to the client from the control plane</param>
  /// <param name="taskId">The task identifier which has invoked the error callBack</param>
  public void HandleError(ServiceInvocationException e,
                          string                     taskId)
  {
    Log.LogError($"Error from {taskId} : " + e.Message);
    throw new ApplicationException($"Error from {taskId}",
                                   e);
  }

  /// <summary>
  ///   The callBack method which has to be implemented to retrieve response from the server
  /// </summary>
  /// <param name="response">The object received from the server as result the method called by the client</param>
  /// <param name="taskId">The task identifier which has invoked the response callBack</param>
  public void HandleResponse(object response,
                             string taskId)
  {
    switch (response)
    {
      case null:
        Log.LogInformation("Task finished but nothing returned in Result");
        break;
      case double value:
        Log.LogInformation($"Task finished with result {value}");
        break;
      case double[] doubles:
        Log.LogInformation("Result is " + string.Join(", ",
                                                      doubles));
        break;
      case byte[] values:
        var result = ClientPayload.Deserialize(values);
        Log.LogInformation($"Result is {result.Result} expected is : {expectedIntegerResults_[taskId]}");
        Assert.AreEqual(expectedIntegerResults_[taskId],
                        result.Result);
        break;
    }
  }


  [EntryPoint]
  public override void EntryPoint()
  {
    Log.LogInformation("Configure taskOptions");
    var taskOptions = InitializeTaskOptions();
    OverrideTaskOptions(taskOptions);

    var props = new Properties(taskOptions,
                               Configuration.GetSection("Grpc")["EndPoint"],
                               5001);

    using var cs = ServiceFactory.CreateService(props,
                                                LoggerFactory);

    Log.LogInformation("Running End to End test to compute Sum of numbers with subtasking");
    SumNumbersWithSubtasking(cs);
  }

  private static void OverrideTaskOptions(TaskOptions taskOptions)
  {
    taskOptions.EngineType          = EngineType.Unified.ToString();
    taskOptions.ApplicationService  = "SubtaskingTreeUnifiedApiWorker";
    taskOptions.MaxDuration.Seconds = 1800;
  }
  

  private static object[] ParamsHelper(params object[] elements)
    => elements;


  private void SumNumbersWithSubtasking(Service sessionService,
                                        int     maxNumberToSum    = 16,
                                        int     subtaskSplitCount = 2)
  {
    Log.LogInformation($"Launching Sum of numbers 1 to {maxNumberToSum}");
    var numbers = Enumerable.Range(1,
                                   maxNumberToSum)
                            .ToList();
    var payload = new ClientPayload
                  {
                    IsRootTask = true,
                    Numbers    = numbers,
                    NbSubTasks = subtaskSplitCount,
                    Type       = ClientPayload.TaskType.None,
                  };

    var sw = Stopwatch.StartNew();


    var taskId = sessionService.Submit("ComputeSubTaskingTreeSum",
                                       ParamsHelper(payload.Serialize()),
                                       this);
    expectedIntegerResults_[taskId] = numbers.Sum();
  }
}
