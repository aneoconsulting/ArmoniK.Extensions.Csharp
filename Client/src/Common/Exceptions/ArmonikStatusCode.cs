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

namespace ArmoniK.DevelopmentKit.Client.Common.Exceptions;

/// <summary>
///   List of status for task and result in Armonik
/// </summary>
public enum ArmonikStatusCode
{
  /// <summary>
  ///   The task is completed but result could not be ready
  /// </summary>
  TaskCompleted,

  /// <summary>
  ///   The task has failed and no result can be expected
  /// </summary>
  TaskFailed,

  /// <summary>
  ///   The task has been canceled by operator or user himself
  /// </summary>
  TaskCancelled,

  /// <summary>
  ///   the task has reached the max duration of execution
  /// </summary>
  TaskTimeout,

  /// <summary>
  ///   The result is ready to be retrieved
  /// </summary>
  ResultReady,

  /// <summary>
  ///   The result is not yet ready and the task is still in processing
  /// </summary>
  ResultNotReady,

  /// <summary>
  ///   The result is in error and the task could finished without no result
  /// </summary>
  ResultError,

  /// <summary>
  ///   Unknown status of task or result
  /// </summary>
  Unknown,
}
