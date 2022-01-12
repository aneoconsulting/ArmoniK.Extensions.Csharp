using System.Text.Json;
//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer
{
  public class DataSynapsePayload
  {
    public DataSynapseRequestType DataSynapseRequestType { get; set; }

    public byte[] ClientPayload { get; set; }


    public byte[] Serialize()
    {
      string jsonString = JsonSerializer.Serialize(this);
      return System.Text.Encoding.ASCII.GetBytes(StringToBase64(jsonString));
    }

    public static DataSynapsePayload Deserialize(byte[] payload)
    {
      if (payload == null || payload.Length == 0)
        return new DataSynapsePayload();

      var str = System.Text.Encoding.ASCII.GetString(payload);
      return JsonSerializer.Deserialize<DataSynapsePayload>(Base64ToString(str));
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

  public enum DataSynapseRequestType
  {
    Execute,
    Upload,
    Register,
    ListResources,
    DeleteResources,
    GetServiceInvocation
  }
}