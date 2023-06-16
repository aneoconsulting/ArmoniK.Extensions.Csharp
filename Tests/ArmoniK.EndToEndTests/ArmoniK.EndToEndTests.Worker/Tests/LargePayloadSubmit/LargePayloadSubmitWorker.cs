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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Worker.Unified;

namespace ArmoniK.EndToEndTests.Worker.Tests.LargePayloadSubmit;

/// <summary>
///   The service to register in the worker
/// </summary>
public class LargePayloadSubmitWorker : BaseService<LargePayloadSubmitWorker>
{
  /// <summary>
  ///   Client method to compute data in the worker
  /// </summary>
  /// <param name="inputs">The first arguments from Client call</param>
  /// <param name="workloadTime">The second arguments from client call</param>
  /// <returns>The result to return</returns>
  public static double ComputeSum([NotNull] double[] inputs,
                                  int                workloadTime)
  {
    if (inputs == null)
    {
      throw new ArgumentNullException(nameof(inputs));
    }

    Thread.Sleep(workloadTime);

    return inputs.Sum();
  }
}
