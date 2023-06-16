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

using System.Text;

using ArmoniK.DevelopmentKit.Common;

using NUnit.Framework;

[TestFixture]
public static class ArmoniKPayloadSerialization
{
  [Test]
  public static void ShouldSerialize()
  {
    var payload = new ArmonikPayload
                  {
                    MethodName          = "Test",
                    ClientPayload       = Encoding.ASCII.GetBytes("Payload"),
                    SerializedArguments = true,
                  };
    var serialize = payload.Serialize();

    var deserialize = ProtoSerializer.Deserialize<ArmonikPayload>(serialize);
    Assert.Multiple(() =>
                    {
                      Assert.That(deserialize.MethodName,
                                  Is.EqualTo(payload.MethodName));
                      Assert.That(deserialize.ClientPayload,
                                  Is.EqualTo(payload.ClientPayload));
                      Assert.That(deserialize.SerializedArguments,
                                  Is.EqualTo(payload.SerializedArguments));
                    });
  }
}
