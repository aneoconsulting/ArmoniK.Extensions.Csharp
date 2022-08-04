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

using System.Text;
using System.Text.Json;

namespace ArmoniK.Extensions.Common.StreamWrapper.Tests.Common
{
  public class TestPayload
  {
    public enum TaskType
    {
      Result,
      Undefined,
      None,
      Compute,
      Error,
      Transfer,
      DatadepTransfer,
      DatadepCompute,
    }

    public byte[]? DataBytes { get; set; }

    public TaskType Type { get; set; }

    public string? ResultKey { get; set; }

    public byte[] Serialize()
    {
      var jsonString = JsonSerializer.Serialize(this);
      return Encoding.ASCII.GetBytes(StringToBase64(jsonString));
    }

    public static TestPayload? Deserialize(byte[] payload)
    {
      if (payload.Length == 0)
      {
        return new TestPayload
        {
          Type = TaskType.Undefined,
        };
      }

      var str = Encoding.ASCII.GetString(payload);
      return JsonSerializer.Deserialize<TestPayload>(Base64ToString(str));
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
}