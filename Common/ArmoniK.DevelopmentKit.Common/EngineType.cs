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

using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Common
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

    /// <summary>
    /// 
    /// </summary>
    Armonik = 2,
  }

  public static class EngineTypeHelper
  {
    public static EngineType ToEnum(string enumName)
      => enumName switch
         {
           "Symphony"    => EngineType.Symphony,
           "DataSynapse" => EngineType.DataSynapse,
           "Armonik" => EngineType.Armonik,
           _             => throw new KeyNotFoundException($"enumName, possible choice are [{string.Join(", ", typeof(EngineType).GetEnumNames())}]")
         };
  }

  public class EngineTypes
  {
    private readonly Dictionary<EngineType, string> engineTypes_ = new()
    {
      { EngineType.Symphony, "ArmoniK.DevelopmentKit.SymphonyApi" },

      { EngineType.DataSynapse, "ArmoniK.DevelopmentKit.GridServer" },

      { EngineType.Armonik, "ArmoniK.DevelopmentKit.Worker" },
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