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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Common.Utils;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   The class to map submitId to taskId when submit is done asynchronously
/// </summary>
public class RequestTaskMap
{
  private const    int                                                   WaitTime    = 100;
  private readonly ConcurrentDictionary<Guid, Either<string, Exception>> dictionary_ = new();

  /// <summary>
  ///   Push the submitId and taskId in the concurrentDictionary
  /// </summary>
  /// <param name="submitId">The submit Id push during the submission</param>
  /// <param name="taskId">the taskId was given by the control Plane</param>
  public void PutResponse(Guid   submitId,
                          string taskId)
    => dictionary_[submitId] = taskId;

  /// <summary>
  ///   Get the correct taskId based on the submitId
  /// </summary>
  /// <param name="submitId">The submit Id push during the submission</param>
  /// <returns>the async taskId</returns>
  public async Task<string> GetResponseAsync(Guid submitId)
  {
    while (!dictionary_.ContainsKey(submitId))
    {
      await Task.Delay(WaitTime);
    }

    return dictionary_[submitId]
      .IfRight(e =>
               {
                 throw e;
               });
  }


  /// <summary>
  ///   Notice user that there was at least one error during the submission of buffer
  /// </summary>
  /// <param name="submitIds"></param>
  /// <param name="exception">exception occurring the submission</param>
  public void BufferFailures(IEnumerable<Guid> submitIds,
                             Exception         exception)
  {
    foreach (var submitId in submitIds)
    {
      dictionary_[submitId] = exception;
    }
  }
}
