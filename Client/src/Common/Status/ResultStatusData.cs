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

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   Class for storing relation between result id, task id and result status
/// </summary>
public class ResultStatusData
{
  /// <summary>
  ///   Constructor for the class
  /// </summary>
  /// <param name="resultId">The id of the result</param>
  /// <param name="taskId">The id of the task producing the result</param>
  /// <param name="status">The status of the result</param>
  public ResultStatusData(string       resultId,
                          string       taskId,
                          ResultStatus status)
  {
    ResultId = resultId;
    TaskId   = taskId;
    Status   = status;
  }

  /// <summary>
  ///   The id of the result
  /// </summary>
  public string ResultId { get; }

  /// <summary>
  ///   The id of the task producing the result
  /// </summary>
  public string TaskId { get; }

  /// <summary>
  ///   The status of the result
  /// </summary>
  public ResultStatus Status { get; }
}
