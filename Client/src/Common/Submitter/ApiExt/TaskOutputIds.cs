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

using ArmoniK.Api.gRPC.V1.Tasks;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
///   Pair containing the Id of a task and the list of its outputs.
/// </summary>
/// <param name="TaskId">Id of the task</param>
/// <param name="OutputIds">The list of the task outputs</param>
[PublicAPI]
public record TaskOutputIds(string                      TaskId,
                            IReadOnlyCollection<string> OutputIds)
{
  /// <summary>
  ///   Converts from the gRPC object.
  /// </summary>
  /// <param name="mapTaskResult">Object from ArmoniK's gRPC model.</param>
  public TaskOutputIds(GetResultIdsResponse.Types.MapTaskResult mapTaskResult)
    : this(mapTaskResult.TaskId,
           mapTaskResult.ResultIds)
  {
  }
}
