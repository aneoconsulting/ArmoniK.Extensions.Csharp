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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.SymphonyApi;

/// <summary>
///   Provides the context for the task that is bound to the given service invocation
/// </summary>
[MarkDownDoc]
public class TaskContext
{
  public byte[] Payload;
  public string TaskId { get; set; }

  public string SessionId { get; set; }

  public IEnumerable<string> DependenciesTaskIds { get; set; }

  public TaskOptions TaskOptions { get; set; }


  /// <summary>
  ///   The customer payload to deserialize by the customer
  /// </summary>
  /// <value></value>
  public byte[] TaskInput
  {
    get => Payload;

    set => Payload = value;
  }

  public IReadOnlyDictionary<string, byte[]> DataDependencies { get; set; }
}
