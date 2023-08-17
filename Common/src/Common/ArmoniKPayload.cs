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

using ProtoBuf;

namespace ArmoniK.DevelopmentKit.Common;

/// <summary>
///   A class used to define the computation required from the worker
/// </summary>
/// <param name="MethodName">The name of the method to execute</param>
/// <param name="ClientPayload">The arguments for the method</param>
/// <param name="SerializedArguments">Defines whether the payload should be transmitted as is to the worker method.</param>
[ProtoContract(SkipConstructor = true)]
public record ArmonikPayload([property: ProtoMember(1)]
                             string MethodName,
                             [property: ProtoMember(2)]
                             byte[] ClientPayload,
                             [property: ProtoMember(3)]
                             bool SerializedArguments)
{
  public byte[] Serialize()
    => ProtoSerializer.Serialize(this);

  public static ArmonikPayload Deserialize(byte[] payload)
    => ProtoSerializer.Deserialize<ArmonikPayload>(payload)!;
}
