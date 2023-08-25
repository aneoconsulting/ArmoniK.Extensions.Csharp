// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023.All rights reserved.
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
using System.Linq;

namespace ArmoniK.DevelopmentKit.Common.Extensions;

/// <summary>
///   Convert IEnumerable byte to IEnumerable double
/// </summary>
public static class EnumerableExt
{
  /// <summary>
  ///   Convert IEnumerable byte to IEnumerable double
  /// </summary>
  /// <param name="arr"></param>
  /// <returns></returns>
  public static IEnumerable<double> ConvertToArray(this IEnumerable<byte> arr)
  {
    var bytes = arr as byte[] ?? arr.ToArray();

    var values = new double[bytes.Count() / sizeof(double)];

    for (var i = 0; i < values.Length; i++)
    {
      values[i] = BitConverter.ToDouble(bytes,
                                        i * 8);
    }

    return values;
  }
}
