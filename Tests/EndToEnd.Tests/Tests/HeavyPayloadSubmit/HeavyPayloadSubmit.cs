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

using System.Linq;
using System.Threading;

using ArmoniK.DevelopmentKit.Worker.Grid;

using JetBrains.Annotations;

namespace ArmoniK.EndToEndTests.Tests.HeavyPayloadSubmit;

/// <summary>
///   The service executed in the worker
/// </summary>
[PublicAPI]
public class HeavyPayloadSubmit : BaseService<HeavyPayloadSubmit>
{
  /// <summary>
  ///   The function called by the submitter
  /// </summary>
  /// <param name="inputs"></param>
  /// <param name="workLoadInMs">Simulate workload time in milliseconds </param>
  /// <returns>Return the sum of vector</returns>
  public static double ComputeReduceCube(double[] inputs,
                                         int      workLoadInMs)
  {
    Thread.Sleep(workLoadInMs);
    return inputs.Select(x => x * x * x)
                 .Sum();
  }
}
