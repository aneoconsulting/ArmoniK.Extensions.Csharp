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
using System.Linq;
using System.Reflection;

using ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

using NUnit.Framework;

namespace ArmoniK.DevelopmentKit.Client.Common.Tests;

[TestFixture]
public class ArmoniKClientTests
{

  public static IEnumerable<TestCaseData> ArmoniKClientMethodswithTimeOut
  {
    get
    {
      var types = new[]
                  {
                    typeof(IArmoniKClient),
                    typeof(GrpcArmoniKClient),
                    typeof(RetryArmoniKClient),
                  };

      return types.SelectMany(type => type.GetMethods()
                                          .Select(methodInfo => new
                                                                {
                                                                  type,
                                                                  methodInfo,
                                                                }))
                  .Where(tuple => tuple.methodInfo.GetParameters()
                                       .Any(parameterInfo => parameterInfo.Name == "totalTimeoutMs"))
                  .Select(tuple => new TestCaseData(tuple.type.Name,
                                                    tuple.methodInfo.Name,
                                                    tuple.methodInfo.GetParameters()
                                                         .Single(parameterInfo => parameterInfo.Name == "totalTimeoutMs")));
    }
  }

  /// <summary>
  /// This test ensures that the parameter can be represented as a TimeSpan
  /// </summary>
  /// <param name="typeName"></param>
  /// <param name="methodName"></param>
  /// <param name="parameterInfo"></param>
  [TestCaseSource(nameof(ArmoniKClientMethodswithTimeOut))]
  public static void DefaultTotalTimeoutShouldBeConvertibleToTimeSpan(string typeName, string methodName, ParameterInfo parameterInfo)
  {
    Assert.That(parameterInfo.HasDefaultValue,
                Is.True);
    var value = (double)parameterInfo.DefaultValue!;
    Assert.That(value,
                Is.GreaterThan(0.0));
    // ReSharper disable once NotAccessedVariable
    TimeSpan span;
    Assert.DoesNotThrow(() => span = TimeSpan.FromMilliseconds(value));
  }
}
