// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
