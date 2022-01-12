using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public enum EngineType
  {
    /// <summary>
    /// 
    /// </summary>
    Symphony = 0,


    /// <summary>
    /// 
    /// </summary>
    DataSynapse = 1,
  }

  public static class EngineTypeHelper
  {
    public static EngineType ToEnum(string enumName)
      => enumName switch
         {
           "Symphony"    => EngineType.Symphony,
           "DataSynapse" => EngineType.DataSynapse,
           _             => throw new KeyNotFoundException($"enumName, possible choice are [{string.Join(", ", typeof(EngineType).GetEnumNames())}]")
         };
  }

  public class EngineTypes
  {
    private readonly Dictionary<EngineType, string> engineTypes_ = new()
    {
      { EngineType.Symphony, "ArmoniK.DevelopmentKit.SymphonyApi" },

      { EngineType.DataSynapse, "ArmoniK.DevelopmentKit.GridServer" },
    };

    /// <summary>
    /// Get the EngineType Assembly name for AppLoader
    /// </summary>
    /// <param name="key"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public string this[EngineType key]
    {
      get
      {
        if (engineTypes_.ContainsKey(key)) return engineTypes_[key];

        throw new KeyNotFoundException($"There is no engine type [{key}]");
      }
    }
  }
}