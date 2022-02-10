//TODO : remove pragma

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.Common
{
  public class ArmoniKPayload
  {
    public ArmonikRequestType ArmonikRequestType { get; set; }

    public byte[] ClientPayload { get; set; }


    public byte[] Serialize()
    {
      string jsonString = ProtoSerializer.SerializeMessageObject(this).ToString();
      return System.Text.Encoding.ASCII.GetBytes(StringToBase64(jsonString));
    }

    public static ArmoniKPayload Deserialize(byte[] payload)
    {
      if (payload == null || payload.Length == 0)
        return new ArmoniKPayload();

      var str = System.Text.Encoding.ASCII.GetString(payload);
      return ProtoSerializer.Deserialize<ArmoniKPayload>(Base64ToString(str));
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