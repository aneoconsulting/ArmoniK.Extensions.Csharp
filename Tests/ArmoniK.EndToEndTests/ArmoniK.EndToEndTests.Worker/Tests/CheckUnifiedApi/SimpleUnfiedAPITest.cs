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

using System;
using System.Linq;

using ArmoniK.DevelopmentKit.Common.Extensions;
using ArmoniK.DevelopmentKit.Worker.Unified;
using ArmoniK.DevelopmentKit.Worker.Unified.Exceptions;

namespace ArmoniK.EndToEndTests.Worker.Tests.CheckUnifiedApi;

public class CheckUnifiedApiWorker : TaskSubmitterWorkerService
{
  private readonly Random rd = new();

  public double[] ComputeBasicArrayCube(double[] inputs)
    => inputs.Select(x => x * x * x)
             .ToArray();

  public static double ComputeReduceCube(double[] inputs)
    => inputs.Select(x => x * x * x)
             .Sum();

  public static double ComputeReduceCube(byte[] inputs)
  {
    var doubles = inputs.ConvertToArray();

    return doubles.Select(x => x * x * x)
                  .Sum();
  }

  public string GetTaskOptionsFromWorker()
    => TaskOptions.ApplicationName;

  public static double[] ComputeMadd(byte[] inputs1,
                                     byte[] inputs2,
                                     double k)
  {
    var doubles1 = inputs1.ConvertToArray()
                          .ToArray();
    var doubles2 = inputs2.ConvertToArray()
                          .ToArray();


    return doubles1.Select((x,
                            idx) => k * x * doubles2[idx])
                   .ToArray();
  }

  public double[] NonStaticComputeMadd(byte[] inputs1,
                                       byte[] inputs2,
                                       double k)
  {
    var doubles1 = inputs1.ConvertToArray()
                          .ToArray();
    var doubles2 = inputs2.ConvertToArray()
                          .ToArray();


    return doubles1.Select((x,
                            idx) => k * x * doubles2[idx])
                   .ToArray();
  }

  public double[] RandomTaskError(double percentageOfFailure = 25)
  {
    var randNum = rd.NextDouble();
    if (randNum < percentageOfFailure / 100)
    {
      throw new GridServerException("An expected failure in this random call");
    }

    return new[]
           {
             0.0,
             1.0,
             2.0,
           };
  }
}
