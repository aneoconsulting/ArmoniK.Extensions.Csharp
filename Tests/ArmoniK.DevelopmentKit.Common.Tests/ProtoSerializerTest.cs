// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
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
//

using System.Collections;

using NUnit.Framework;

namespace ArmoniK.DevelopmentKit.Common.Tests
{
  public class ProtoSerializerTest
  {


    [Test]
    [TestCase(1)]
    [TestCase(1U)]
    [TestCase(1L)]
    [TestCase(1UL)]
    [TestCase(1.0)]
    [TestCase(1.0f)]
    [TestCase((short)1)]
    [TestCase((byte)1)]
    [TestCase("test")]
    public void SerializeAndDeserialize<T>(T? message)
    {
      var serialized = ProtoSerializer.Serialize(message);
      var result     = ProtoSerializer.Deserialize<T>(serialized);
      Assert.That(result, Is.EqualTo(message));
    }

    [Test]
    [TestCase(1)]
    [TestCase(1U)]
    [TestCase(1L)]
    [TestCase(1UL)]
    [TestCase(1.0)]
    [TestCase(1.0f)]
    [TestCase((short)1)]
    [TestCase((byte)1)]
    [TestCase("test")]
    public void SerializeAndDeserializeArrayTypes<T>(T? message)
    {
      var serialized = ProtoSerializer.Serialize(new []{message});
      var result     = ProtoSerializer.Deserialize<T[]>(serialized);
      Assert.That(result, Is.Not.Null);
      Assert.That(result!.Length, Is.EqualTo(1));
      Assert.That(result![0], Is.EqualTo(message));
    }

    [Test]
    public void SerializeAndDeserializeArray()
    {
      var message = Enumerable.Range(0,
                                     5).ToArray() as Array;
      var serialized = ProtoSerializer.Serialize(message);
      var result     = ProtoSerializer.Deserialize<Array>(serialized);
      Assert.That(result,
                  Is.Not.Null);

      var array = result.Cast<int>()
                        .ToArray();

      Assert.That(array.Count,
                  Is.EqualTo(5));
      Assert.Multiple(() =>
                      {
                        Assert.That(array[0],
                                    Is.EqualTo(0));
                        Assert.That(array[1],
                                    Is.EqualTo(1));
                        Assert.That(array[2],
                                    Is.EqualTo(2));
                        Assert.That(array[3],
                                    Is.EqualTo(3));
                        Assert.That(array[4],
                                    Is.EqualTo(4));
                      });
    }


    [Test]
    public void SerializeAndDeserializeArmonikPayload()
    {
      var message = new ArmonikPayload()
                    {
                      MethodName = "methodName",
                      ClientPayload = new []{
                                              (byte)0x01,
                                              (byte)0x02,
                                            },
                      SerializedArguments = false,
                    };
      var serialized = ProtoSerializer.Serialize(message);
      var result     = ProtoSerializer.Deserialize<ArmonikPayload>(serialized);
      Assert.That(result,
                  Is.Not.Null);

      Assert.Multiple(() =>
                      {
                        Assert.That(result!.ClientPayload,
                                    Is.Not.Null);
                        Assert.That(result!.ClientPayload[0],
                                    Is.EqualTo((byte)0x01));
                        Assert.That(result!.ClientPayload[1],
                                    Is.EqualTo((byte)0x02));
                        Assert.That(result!.MethodName,
                                    Is.EqualTo("methodName"));
                        Assert.That(result!.SerializedArguments,
                                    Is.EqualTo(false));
                      });
    }


    [Test]
    public void SerializeAndDeserializeArrayofArray()
    {
      string[]?[] message = Enumerable.Range(0,
                                             3)
                                      .Select(i => Enumerable.Range(0,
                                                                    2)
                                                             .Select(i1 => $"{i},{i1}")
                                                             .ToArray())
                                      .Append(null)
                                      .ToArray();
      var serialized = ProtoSerializer.Serialize(message);
      var result     = ProtoSerializer.Deserialize<object?[]>(serialized);
      Assert.That(result,
                  Is.Not.Null);

      Assert.Multiple(() =>
      {
        Assert.That(result![0],
                    Is.Not.Null);
        Assert.That(result![0],
                    Is.TypeOf<string[]>());
        Assert.That(result![1],
                                    Is.Not.Null);
        Assert.That(result![1],
                    Is.TypeOf<string[]>());
        Assert.That(result![2],
                                    Is.Not.Null);
        Assert.That(result![2],
                    Is.TypeOf<string[]>());
        Assert.That(result![3],
                                    Is.Null);
                        Assert.That((result![0] as string[])![0],
                                    Is.EqualTo("0,0"));
                        Assert.That((result![0] as string[])![1],
                                    Is.EqualTo("0,1"));
                        Assert.That((result![1] as string[])![0],
                                    Is.EqualTo("1,0"));
                        Assert.That((result![1] as string[])![1],
                                    Is.EqualTo("1,1"));
                        Assert.That((result![2] as string[])![0],
                                    Is.EqualTo("2,0"));
                        Assert.That((result![2] as string[])![1],
                                    Is.EqualTo("2,1"));
                      });
    }
  }
}
