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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using ArmoniK.DevelopmentKit.Common.Exceptions;

using ProtoBuf;

namespace ArmoniK.DevelopmentKit.Common;

// TODO: should it be marked for [PublicApi] ?
public static class ProtoSerializer
{
// *** you need some mechanism to map types to fields
  private static readonly List<Type> TypeLookup = new()
                                                  {
                                                    typeof(int),
                                                    typeof(uint),
                                                    typeof(long),
                                                    typeof(ulong),
                                                    typeof(double),
                                                    typeof(float),
                                                    typeof(short),
                                                    typeof(byte),
                                                    typeof(string),
                                                    typeof(int[]),
                                                    typeof(uint[]),
                                                    typeof(long[]),
                                                    typeof(ulong[]),
                                                    typeof(double[]),
                                                    typeof(float[]),
                                                    typeof(short[]),
                                                    typeof(byte[]),
                                                    typeof(string[]),
                                                    typeof(Nullable),
                                                    typeof(ProtoArray),
                                                    typeof(ArmonikPayload),
                                                  };

  public static byte[] Serialize(object? value)
  {
    using var ms = new MemoryStream();

    WriteNext(ms,
              value);


    var data = ms.ToArray();
    return data;
  }

  // TODO: is it [PublicApi]?
  // ReSharper disable once UnusedMember.Global
  public static void RegisterClass(Type type)
  {
    if (TypeLookup.Contains(type))
    {
      throw new ArgumentException("Type already registered",
                                  nameof(type));
    }

    TypeLookup.Add(type);
  }

  private static void WriteNext(Stream  stream,
                                object? obj)
  {
    obj ??= new Nullable();

    var type = obj.GetType();

    if (type.IsArray && TypeLookup.All(t => t.Name != type.Name))
    {
      WriteNext(stream,
                new ProtoArray
                {
                  NbElement = ((Array)obj).Length,
                });

      foreach (var subObj in (Array)obj)
      {
        WriteNext(stream,
                  subObj);
      }
    }
    else
    {
      SerializeSingle(stream,
                      obj,
                      type);
    }
  }

  private static void SerializeSingle(Stream stream,
                                      object obj,
                                      Type   type)
  {
    // "+1" to have 1-based indexing instead of 0-based indexing. Required by protocol buffer.
    var field = TypeLookup.IndexOf(type) + 1;
    Serializer.NonGeneric.SerializeWithLengthPrefix(stream,
                                                    obj,
                                                    PrefixStyle.Base128,
                                                    field);
  }

  private static bool ReadNext(Stream      stream,
                               out object? obj)
  {
    if (!Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream,
                                                              PrefixStyle.Base128,
                                                              // "-1" to have 1-based indexing instead of 0-based indexing. Required by protocol buffer.
                                                              field => TypeLookup[field - 1],
                                                              out obj))
    {
      return false;
    }

    switch (obj)
    {
      case Nullable:
        obj = null;
        break;

      case ProtoArray arrInfo:
      {
        var finalObj = new List<object?>();
        if (arrInfo.NbElement < 0)
        {
          throw new WorkerApiException($"ProtoArray failure number of element [{arrInfo.NbElement}] < 0 ");
        }

        for (var i = 0; i < arrInfo.NbElement; i++)
        {
          if (!ReadNext(stream,
                        out var subObj))
          {
            throw new WorkerApiException($"Fail to iterate over ProtoArray with Element {arrInfo.NbElement} at index [{i}]");
          }

          finalObj.Add(subObj);
        }

        obj = finalObj.ToArray();
        break;
      }
    }

    return true;
  }

  public static T? Deserialize<T>(byte[] dataPayloadInBytes)
  {
    using var ms = new MemoryStream(dataPayloadInBytes);
    if (!ReadNext(ms,
                  out var obj))
    {
      throw new SerializationException("Error while deserializing object.");
    }

    return (T?)obj;
  }

  [ProtoContract]
  public class Nullable
  {
  }

  [ProtoContract]
  private class ProtoArray
  {
    [ProtoMember(1)]
    public int NbElement;
  }
}
