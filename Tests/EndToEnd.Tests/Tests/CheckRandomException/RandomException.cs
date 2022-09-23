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
using System.Collections.Generic;
using System.Linq;

using ArmoniK.DevelopmentKit.GridServer;

namespace ArmoniK.EndToEndTests.Tests.CheckRandomException;

public static class SelectExtensions
{
  public static IEnumerable<double> ConvertToArray(this IEnumerable<byte> arr)
  {
    var bytes = arr as byte[] ?? arr.ToArray();

    var values = new double[bytes.Count() / sizeof(double)];

    var i = 0;
    for (; i < values.Length; i++)
    {
      values[i] = BitConverter.ToDouble(bytes.ToArray(),
                                        i * 8);
    }

    return values;
  }
}

public class SimpleServiceContainer
{
  private readonly Random rd = new();

  public SimpleServiceContainer()
  {
    var rand_num = rd.NextDouble();
  }

  public double[] ComputeBasicArrayCube(double[] inputs,
                                        double   percentageOfFailure = 0.15)
  {
    var randNum = rd.NextDouble();
    if (randNum < percentageOfFailure)
    {
      throw new GridServerException("An expected failure in this random call");
    }

    return inputs.Select(x => x * x * x)
                 .ToArray();
  }
}
