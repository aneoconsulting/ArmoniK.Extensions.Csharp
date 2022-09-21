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
using System.Threading;

using ArmoniK.DevelopmentKit.Worker.Grid;

namespace ArmoniK.EndToEndTests.Tests.HeavyMatrixCompute;

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

public class HeavyMatrixCompute : BaseService<HeavyMatrixCompute>
{
  public static double[] ComputeBasicArrayCube(double[] inputs)
    => inputs.Select(x => x * x * x)
             .ToArray();

  public static double ComputeReduceCube(double[] inputs)
  {
    Thread.Sleep(8000); // 12 seconds of compute since we submit 6 task/s
    return inputs.Select(x => x * x * x)
                .Sum();
  }
}
