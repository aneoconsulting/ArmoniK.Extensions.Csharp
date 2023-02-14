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

using ProtoBuf;

namespace ArmoniK.DevelopmentKit.Common;

/// <summary>
///   Payload for tasks
/// </summary>
[ProtoContract]
public class ArmonikPayload
{
  /// <summary>
  ///   Type of the request
  /// </summary>
  [ProtoMember(1)]
  public ArmonikRequestType ArmonikRequestType { get; set; }

  /// <summary>
  ///   Name of the method to call
  /// </summary>
  [ProtoMember(2)]
  public string MethodName { get; set; } = "";

  /// <summary>
  ///   Client data
  /// </summary>
  [ProtoMember(3)]
  public byte[] ClientPayload { get; set; } = Array.Empty<byte>();

  /// <summary>
  ///   Serialized
  /// </summary>
  [ProtoMember(4)]
  public bool SerializedArguments { get; set; }

  /// <summary>
  ///   Serialize the payload
  /// </summary>
  /// <returns>Serialized payload</returns>
  /// <exception cref="ArgumentNullException"></exception>
  public byte[] Serialize()
    => ProtoSerializer.SerializeMessageObject(this);

  /// <summary>
  ///   Deserialize a payload
  /// </summary>
  /// <param name="payload">Serialized payload</param>
  /// <returns>Actual payload</returns>
  public static ArmonikPayload? Deserialize(byte[]? payload)
  {
    if (payload == null || payload.Length == 0)
    {
      return null;
    }

    return ProtoSerializer.Deserialize<ArmonikPayload>(payload);
  }
}

/// <summary>
///   Request Type
/// </summary>
public enum ArmonikRequestType
{
  /// <summary>Execute</summary>
  Execute,

  /// <summary>Upload</summary>
  Upload,

  /// <summary>Register</summary>
  Register,

  /// <summary>ListResources</summary>
  ListResources,

  /// <summary>DeleteResources</summary>
  DeleteResources,

  /// <summary>GetServiceInvocation</summary>
  GetServiceInvocation,
}
