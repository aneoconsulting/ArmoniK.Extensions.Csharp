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

using ProtoBuf;

namespace ArmoniK.EndToEndTests.Common;

[ProtoContract]
public class TaskResult
{
  [ProtoMember(1)]
  public string ResultString { get; set; } = "scalar";

  [ProtoMember(2)]
  public int Result { get; set; }

  [ProtoMember(3)]
  public int Priority { get; set; } = 1;

  [ProtoMember(4)]
  public string CompletedAt { get; set; } = DateTime.UtcNow.ToUniversalTime()
                                                    .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

  public byte[] Serialize()
  {
    using var stream = new MemoryStream();

    Serializer.NonGeneric.SerializeWithLengthPrefix(stream,
                                                    this,
                                                    PrefixStyle.Base128,
                                                    1);

    ResultString = Result.ToString();

    return stream.ToArray();
  }

  public static TaskResult Deserialize(byte[] payload)
  {
    if (payload == null || payload.Length == 0)
    {
      throw new Exception("payload is null or empty");
    }

    using var stream = new MemoryStream(payload);

    if (!Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream,
                                                              PrefixStyle.Base128,
                                                              _ => typeof(TaskResult),
                                                              out var obj))
    {
      throw new Exception("Fail to deserialize");
    }

    return (TaskResult)obj;
  }
}
