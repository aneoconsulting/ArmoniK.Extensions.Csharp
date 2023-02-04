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

//TODO : remove pragma

using System;
using System.Text;

using ProtoBuf;

namespace ArmoniK.DevelopmentKit.Common;

[ProtoContract]
public class ArmonikPayload
{
  public ArmonikPayload()
  {
    MethodName          = "noMethod";
    ClientPayload       = null;
    SerializedArguments = false;
  }

  [ProtoMember(1)]
  public ArmonikRequestType ArmonikRequestType { get; set; }

  [ProtoMember(2)]
  public string? MethodName { get; set; }

  [ProtoMember(3)]
  public byte[]? ClientPayload { get; set; }

  [ProtoMember(4)]
  public bool SerializedArguments { get; set; }

  public byte[] Serialize()
  {
    if (ClientPayload is null)
    {
      throw new ArgumentNullException(nameof(ClientPayload));
    }

    var result = ProtoSerializer.SerializeMessageObject(this);

    return result;
  }

  public static ArmonikPayload? Deserialize(byte[]? payload)
  {
    if (payload == null || payload.Length == 0)
    {
      return new ArmonikPayload();
    }

    return ProtoSerializer.Deserialize<ArmonikPayload>(payload);
  }

  private static string StringToBase64(string serializedJson)
  {
    var serializedJsonBytes       = Encoding.UTF8.GetBytes(serializedJson);
    var serializedJsonBytesBase64 = Convert.ToBase64String(serializedJsonBytes);
    return serializedJsonBytesBase64;
  }

  private static string Base64ToString(string base64)
  {
    var c = Convert.FromBase64String(base64);
    return Encoding.ASCII.GetString(c);
  }
}

public enum ArmonikRequestType
{
  Execute,
  Upload,
  Register,
  ListResources,
  DeleteResources,
  GetServiceInvocation,
}
