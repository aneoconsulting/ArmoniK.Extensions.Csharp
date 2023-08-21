// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
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

using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1.Submitter;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
///   Contains all the information about a submitted task.
/// </summary>
/// <param name="TaskId">The Id of the task</param>
/// <param name="Inputs">The names of the other data required by the task. The task will not start without these data</param>
/// <param name="Outputs">The names of the data that should be produced by this task</param>
/// <param name="PayloadId">The id of the payload</param>
[PublicAPI]
// TODO: should be in ArmoniK.Api
public record TaskInfo(string                TaskId,
                       IReadOnlyList<string> Inputs,
                       IReadOnlyList<string> Outputs,
                       string                PayloadId)
{
  internal TaskInfo(CreateTaskReply.Types.TaskInfo taskInfo)
    : this(taskInfo.TaskId,
           taskInfo.DataDependencies,
           taskInfo.ExpectedOutputKeys,
           taskInfo.PayloadId)
  {
  }
}
