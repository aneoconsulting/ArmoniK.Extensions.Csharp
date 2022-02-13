//TODO : remove pragma

using ProtoBuf;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.Common
{
  [ProtoContract]
  public class ArmonikPayload
  {
    [ProtoMember(1)]
    public ArmonikRequestType ArmonikRequestType { get; set; }

    [ProtoMember(2)]
    public string MethodName { get; set; }

    [ProtoMember(3)]
    public byte[] ClientPayload { get; set; }

    [ProtoMember(4)] public bool SerializedArguments { get; set; }




    public byte[] Serialize()
    {
      return ProtoSerializer.SerializeMessageObject(this);
    }

    public static ArmonikPayload Deserialize(byte[] payload)
    {
      if (payload == null || payload.Length == 0)
        return new ArmonikPayload();

      return ProtoSerializer.Deserialize<ArmonikPayload>(payload);
    }

    private static string StringToBase64(string serializedJson)
    {
      var serializedJsonBytes       = System.Text.Encoding.UTF8.GetBytes(serializedJson);
      var serializedJsonBytesBase64 = System.Convert.ToBase64String(serializedJsonBytes);
      return serializedJsonBytesBase64;
    }

    private static string Base64ToString(string base64)
    {
      var c = System.Convert.FromBase64String(base64);
      return System.Text.Encoding.ASCII.GetString(c);
    }
  }

  public enum ArmonikRequestType
  {
    Execute,
    Upload,
    Register,
    ListResources,
    DeleteResources,
    GetServiceInvocation
  }
}