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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ArmoniK.DevelopmentKit.Common.Exceptions;

using ProtoBuf;

#pragma warning disable CS1591


namespace ArmoniK.DevelopmentKit.Common;

public class ProtoSerializer
{
// *** you need some mechanism to map types to fields
  private static readonly IDictionary<int, Type> typeLookup = new List<Type>
                                                              {
                                                                typeof(ProtoNative<int>),
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
                                                              }.Select((t,
                                                                        idx) => new
                                                                                {
                                                                                  idx,
                                                                                  t,
                                                                                })
                                                               .ToDictionary(x => x.idx,
                                                                             x => x.t);

  public byte[] SerializeMessageObjectArray(object[] values)
  {
    using var ms = new MemoryStream();

    foreach (var obj in values)
    {
      WriteNext(ms,
                obj);
    }

    var data = ms.ToArray();

    GC.Collect();
    GC.WaitForPendingFinalizers();

    return data;
  }

  public static byte[] SerializeMessageObject(object value)
  {
    using var ms = new MemoryStream();

    WriteNext(ms,
              value);


    var data = ms.ToArray();
    return data;
  }

  public static object[] DeSerializeMessageObjectArray(byte[] data)
  {
    var result = new List<object>();

    using (var ms = new MemoryStream(data))
    {
      while (ReadNext(ms,
                      out var obj))
      {
        result.Add(obj);
      }
    }

    return result.Count == 0
             ? null
             : result.ToArray();
  }

  public static object DeSerializeMessageObject(byte[] data)
  {
    using var ms = new MemoryStream(data);

    ReadNext(ms,
             out var obj);
    return obj;
  }

  public static void RegisterClass(Type classType)
  {
    var max = typeLookup.Keys.Max();
    typeLookup[max + 1] = classType;
  }

  private static void WriteNext(Stream stream,
                                object obj)
  {
    obj ??= new Nullable();

    var type = obj.GetType();

    if (type.IsArray && typeLookup.All(pair => pair.Value.Name != type.Name))
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
    var field = typeLookup.Single(pair => pair.Value == type)
                          .Key;

    Serializer.NonGeneric.SerializeWithLengthPrefix(stream,
                                                    obj,
                                                    PrefixStyle.Base128,
                                                    field);
  }

  private static bool ReadNext(Stream     stream,
                               out object obj)
  {
    if (!Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream,
                                                              PrefixStyle.Base128,
                                                              field =>
                                                              {
                                                                return typeLookup[field];
                                                              },
                                                              out obj))
    {
      return false;
    }

    if (obj is Nullable)
    {
      obj = null;
    }

    if (obj is ProtoArray)
    {
      var finalObj = new List<object>();
      var arrInfo  = (ProtoArray)obj;
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
    }

    return true;
  }

  public static T Deserialize<T>(byte[] dataPayloadInBytes)
  {
    var obj = DeSerializeMessageObject(dataPayloadInBytes);

    return (T)obj;
  }

  [ProtoContract]
  public class Nullable
  {
  }

  [ProtoContract]
  public class ProtoArray
  {
    [ProtoMember(1)]
    public int NbElement;
  }

  [ProtoContract]
  public class ProtoNative<T>
  {
    [ProtoMember(1)]
    public T Element;
  }
}
