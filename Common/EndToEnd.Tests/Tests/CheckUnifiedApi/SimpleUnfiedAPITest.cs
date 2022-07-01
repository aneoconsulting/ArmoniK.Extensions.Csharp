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

using ArmoniK.DevelopmentKit.Client.Exceptions;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.GridServer.Client;

using Microsoft.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Tests.CheckUnifiedApi
{
  public static class SelectExtensions
  {
    public static IEnumerable<double> ConvertToArray(this IEnumerable<byte> arr)
    {
      var bytes = arr as byte[] ?? arr.ToArray();

      var values = new double[bytes.Count() / sizeof(double)];

      var i = 0;
      for (; i < values.Length; i++)
        values[i] = BitConverter.ToDouble(bytes.ToArray(),
                                          i * 8);
      return values;
    }
  }

  public class SimpleService : BaseService<SimpleService>
  {
    private Random rd = new Random();
    public double[] ComputeBasicArrayCube(double[] inputs)
    {
      return inputs.Select(x => x * x * x).ToArray();
    }

    public static double ComputeReduceCube(double[] inputs)
    {
      return inputs.Select(x => x * x * x).Sum();
    }

    public static double ComputeReduceCube(byte[] inputs)
    {
      var doubles = inputs.ConvertToArray();

      return doubles.Select(x => x * x * x).Sum();
    }

    public static double[] ComputeMadd(byte[] inputs1, byte[] inputs2, double k)
    {
      var doubles1 = inputs1.ConvertToArray().ToArray();
      var doubles2 = inputs2.ConvertToArray().ToArray();


      return doubles1.Select((x, idx) => k * x * doubles2[idx]).ToArray();
    }

    public double[] NonStaticComputeMadd(byte[] inputs1, byte[] inputs2, double k)
    {
      var doubles1 = inputs1.ConvertToArray().ToArray();
      var doubles2 = inputs2.ConvertToArray().ToArray();


      return doubles1.Select((x, idx) => k * x * doubles2[idx]).ToArray();
    }

    public double[] RandomTaskError(double percentageOfFailure = 0.25)
    {
      var randNum = rd.NextDouble();
      if (randNum < (percentageOfFailure / 100))
        throw new GridServerException("An expected failure in this random call");

      return new[]
      {
        0.0, 1.0, 2.0,
      };
    }
  }
}