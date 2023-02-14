// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using ArmoniK.DevelopmentKit.Common.Exceptions;

using JetBrains.Annotations;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.Client.Symphony;

[PublicAPI]
public class SessionStorage
{
  private readonly Dictionary<string, string> sessionFromTaskIds_;

  private readonly Dictionary<string, List<string>> taskIdsFromSession_;

  [PublicAPI]
  private SessionStorage()
  {
    taskIdsFromSession_ = new Dictionary<string, List<string>>();
    sessionFromTaskIds_ = new Dictionary<string, string>();
  }

  /// <summary>
  /// </summary>
  /// <param name="session"></param>
  /// <param name="taskId"></param>
  [PublicAPI]
  public void AttachTaskToSession(string session,
                                  string taskId)
  {
    taskIdsFromSession_[session] ??= new List<string>();

    taskIdsFromSession_[session]
      .Add(taskId);

    if (sessionFromTaskIds_.ContainsKey(taskId))
    {
      throw new WorkerApiException("TaskId {} already exist");
    }

    sessionFromTaskIds_[taskId] = session;
  }

  /// <summary>
  /// </summary>
  /// <param name="taskId"></param>
  /// <returns></returns>
  /// <exception cref="WorkerApiException"></exception>
  [PublicAPI]
  public string GetSessionFromTaskId(string taskId)
  {
    if (!sessionFromTaskIds_.ContainsKey(taskId) || sessionFromTaskIds_[taskId] == null)
    {
      throw new WorkerApiException($"Cannot find the taskId {taskId} in the storage");
    }

    return sessionFromTaskIds_[taskId];
  }

  /// <summary>
  /// </summary>
  /// <param name="session"></param>
  /// <returns></returns>
  /// <exception cref="WorkerApiException"></exception>
  [PublicAPI]
  public IEnumerable<string> GetTaskIdsFromSession(string session)
  {
    if (!taskIdsFromSession_.ContainsKey(session))
    {
      throw new WorkerApiException($"Cannot find Session {session} in the storage");
    }

    return taskIdsFromSession_[session];
  }
}
