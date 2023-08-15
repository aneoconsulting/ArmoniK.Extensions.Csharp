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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using ArmoniK.DevelopmentKit.Common.Exceptions;

using JetBrains.Annotations;

using ProtoBuf;

namespace ArmoniK.DevelopmentKit.Common;

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
                                                    typeof(IEnumerable),
                                                    typeof(IDictionary),
                                                    typeof(Array),
                                                    typeof(ArmonikPayload),
                                                  };

  public static byte[] SerializeMessageObjectArray(object?[] values)
  {
    using var ms = new MemoryStream();
    foreach (var obj in values)
    {
      WriteNext(ms,
                obj);
    }

    var data = ms.ToArray();
    return data;
  }

  public static byte[] SerializeMessageObject(object? value)
  {
    using var ms = new MemoryStream();

    WriteNext(ms,
              value);


    var data = ms.ToArray();
    return data;
  }

  public static object?[] DeSerializeMessageObjectArray(byte[] data)
  {
    var result = new List<object?>();

    using var ms = new MemoryStream(data);
    while (ReadNext(ms,
                    out var obj))
    {
      result.Add(obj);
    }

    return result.ToArray();
  }

  private static object? DeSerializeMessageObject(byte[] data)
  {
    using var ms = new MemoryStream(data);

    if (!ReadNext(ms,
                  out var obj))
    {
      throw new SerializationException("Error while deserializing object.");
    }
    return obj;
  }

  [UsedImplicitly]
  public static void RegisterClass(Type type)
  {
    if (TypeLookup.Contains(type))
    {
      throw new ArgumentException("Type already registered",
                                  nameof(type));
    }
    TypeLookup.Add(type);
  }

  private static void WriteNext(Stream stream,
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
    var field = TypeLookup.IndexOf(type);

    Serializer.NonGeneric.SerializeWithLengthPrefix(stream,
                                                    obj,
                                                    PrefixStyle.Base128,
                                                    field);
  }

  private static bool ReadNext(Stream     stream,
                               out object? obj)
  {
    if (!Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream,
                                                              PrefixStyle.Base128,
                                                              field => TypeLookup[field],
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
    var obj = DeSerializeMessageObject(dataPayloadInBytes);

    return (T?)obj;
  }

  [ProtoContract]
  private class Nullable
  {
  }

  [ProtoContract]
  private class ProtoArray
  {
    [ProtoMember(1)]
    public int NbElement;
  }
}
