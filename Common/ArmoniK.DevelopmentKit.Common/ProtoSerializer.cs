using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ArmoniK.DevelopmentKit.Common.Exceptions;

using ProtoBuf;

#pragma warning disable CS1591


namespace ArmoniK.DevelopmentKit.Common
{
  public class ProtoSerializer
  {
    public byte[] SerializeMessageObjectArray(object[] values)
    {
      MemoryStream ms = new MemoryStream();
      foreach (var obj in values)
      {
        WriteNext(ms,
                  obj);
      }

      var data = ms.ToArray();
      return data;
    }

    public static byte[] SerializeMessageObject(object value)
    {
      MemoryStream ms = new MemoryStream();

      WriteNext(ms,
                value);


      var data = ms.ToArray();
      return data;
    }

    public object[] DeSerializeMessageObjectArray(byte[] data)
    {
      var result = new List<object>();

      using (MemoryStream ms = new MemoryStream(data))
      {
        object obj;

        while (ReadNext(ms,
                        out obj))
        {
          result.Add(obj);
        }
      }

      return result.Count == 0 ? null : result.ToArray();
    }

    public static object DeSerializeMessageObject(byte[] data)
    {
      var result = new List<object>();

      using (MemoryStream ms = new MemoryStream(data))
      {
        object obj;

        while (ReadNext(ms,
                        out obj))
        {
          result.Add(obj);
        }
      }

      return result.Count == 0 ? null : result.ToArray();
    }

    [ProtoContract]
    public class Nullable
    {
    }

    [ProtoContract]
    public class ProtoArray
    {
      [ProtoMember(1)] public int NbElement;
    }

    // *** you need some mechanism to map types to fields
    private static IDictionary<int, Type> typeLookup = new Dictionary<int, Type>
    {
      { 1, typeof(int) },
      { 2, typeof(uint) },
      { 3, typeof(long) },
      { 4, typeof(ulong) },
      { 5, typeof(double) },
      { 6, typeof(float) },
      { 7, typeof(short) },
      { 8, typeof(byte[]) },
      { 9, typeof(byte) },
      { 10, typeof(string) },
      { 11, typeof(Nullable) },
      { 12, typeof(ProtoArray) },
      { 13, typeof(IEnumerable) },
      { 14, typeof(IDictionary) },
      { 15, typeof(Array) },
    };

    public static void RegisterClass(Type classType)
    {
      int max = typeLookup.Keys.Max();
      typeLookup[max + 1] = classType;
    }

    public static void WriteNext(Stream stream, object obj)
    {
      if (obj == null) obj = new Nullable();

      Type type = obj.GetType();

      if (type.IsArray)
      {
        WriteNext(stream,
                  new ProtoArray()
                  {
                    NbElement = ((Array)obj).Length
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

    private static void SerializeSingle(Stream stream, object obj, Type type)
    {
      int field = typeLookup.Single(pair => pair.Value == type).Key;
      Serializer.NonGeneric.SerializeWithLengthPrefix(stream,
                                                      obj,
                                                      PrefixStyle.Base128,
                                                      field);
    }

    public static bool ReadNext(Stream stream, out object obj)
    {
      if (Serializer.NonGeneric.TryDeserializeWithLengthPrefix(stream,
                                                               PrefixStyle.Base128,
                                                               field => typeLookup[field],
                                                               out obj))
      {
        if (obj is Nullable) obj = null;

        if (obj is ProtoArray)
        {
          var        finalObj = new List<object>();
          ProtoArray arrInfo  = (ProtoArray)obj;
          if (arrInfo.NbElement < 0) throw new WorkerApiException($"ProtoArray failure number of element [{arrInfo.NbElement}] < 0 ");

          for (int i = 0; i < arrInfo.NbElement; i++)
          {
            if (!ReadNext(stream,
                          out var subObj)) throw new WorkerApiException($"Fail to iterate over ProtoArray with Element {arrInfo.NbElement} at index [{i}]");

            finalObj.Add(subObj);
          }

          obj = finalObj.ToArray();
        }

        return true;
      }

      return false;
    }

    public static T Deserialize<T>(string dataBase64String)
    {
      Object obj = DeSerializeMessageObject(Encoding.ASCII.GetBytes(dataBase64String));

      return (T)obj;
    }
  }
}