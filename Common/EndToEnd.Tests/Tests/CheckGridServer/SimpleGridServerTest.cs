// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ArmoniK.EndToEndTests.Tests.CheckGridServer
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

  public class SimpleServiceContainer
  {
    public static double[] ComputeBasicArrayCube(double[] inputs)
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
  }
}