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
using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
///   Contains the definition of an ArmoniK task
/// </summary>
/// <param name="PayloadName">The name of the payload</param>
/// <param name="PayloadRawData">The task payload</param>
/// <param name="Inputs">The names of the other data required by the task. The task will not start without these data</param>
/// <param name="Outputs">The names of the data that should be produced by this task</param>
[PublicAPI]
// TODO: should be in ArmoniK.Api
public record TaskDefinition(string                PayloadName,
                             ByteString            PayloadRawData,
                             IReadOnlyList<string> Inputs,
                             IReadOnlyList<string> Outputs)
{
  /// <summary>
  /// </summary>
  /// <param name="PayloadName">The name of the payload</param>
  /// <param name="Payload">The task payload</param>
  /// <param name="Inputs">The names of the other data required by the task. The task will not start without these data</param>
  /// <param name="Outputs">The names of the data that should be produced by this task</param>
  public TaskDefinition(string                PayloadName,
                        ReadOnlyMemory<byte>  Payload,
                        IReadOnlyList<string> Inputs,
                        IReadOnlyList<string> Outputs)
    : this(PayloadName,
           UnsafeByteOperations.UnsafeWrap(Payload),
           Inputs,
           Outputs)
  {
  }

  internal TaskDefinition(TaskRequest taskRequest)
    : this(taskRequest.PayloadName,
           taskRequest.Payload,
           taskRequest.DataDependencies,
           taskRequest.ExpectedOutputKeys)
  {
  }

  /// <summary>
  ///   Provide read-only access to the payload content.
  /// </summary>
  public ReadOnlyMemory<byte> Payload
    => PayloadRawData.Memory;

  /// <summary>
  ///   Converts the current instance to a TaskRequest.
  /// </summary>
  /// <returns>
  ///   A TaskRequest instance with properties populated from the current instance.
  /// </returns>
  internal TaskRequest ToTaskRequest()
  {
    var output = new TaskRequest
                 {
                   Payload     = PayloadRawData,
                   PayloadName = PayloadName,
                 };
    output.DataDependencies.Add(Inputs);
    output.ExpectedOutputKeys.Add(Outputs);
    return output;
  }
}
