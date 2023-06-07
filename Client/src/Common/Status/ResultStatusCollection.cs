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

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   List of result status that will be collected during the request GetResultStatus
/// </summary>
public class ResultStatusCollection
{
  /// <summary>
  ///   List of completed task where the result is ready to be retrieved
  /// </summary>
  public IEnumerable<ResultStatusData> IdsReady { get; set; } = default;

  /// <summary>
  ///   List of task or task result in error
  /// </summary>
  public IEnumerable<ResultStatusData> IdsResultError { get; set; } = default;

  /// <summary>
  ///   List of Unknown TaskIds. There is a heavy error somewhere else in the execution when this list has element
  /// </summary>
  public IEnumerable<string> IdsError { get; set; } = default;

  /// <summary>
  ///   List of result not yet written in database
  /// </summary>
  public IEnumerable<ResultStatusData> IdsNotReady { get; set; }

  /// <summary>
  ///   The list of canceled task
  /// </summary>
  public IEnumerable<ResultStatusData> Canceled { get; set; }
}
