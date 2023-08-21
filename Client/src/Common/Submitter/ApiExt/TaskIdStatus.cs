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

using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Status;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
/// </summary>
/// <param name="TaskId"></param>
/// <param name="TaskStatus"></param>
[PublicAPI]
public record TaskIdStatus(string                TaskId,
                           ArmonikTaskStatusCode TaskStatus)
{
  /// <summary>
  /// </summary>
  /// <param name="idStatus"></param>
  public TaskIdStatus(GetTaskStatusReply.Types.IdStatus idStatus)
    : this(idStatus.TaskId,
           idStatus.Status.ToArmonikStatusCode())
  {
  }
}
