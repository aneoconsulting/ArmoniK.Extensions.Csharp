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

using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Common;

/// <summary>
///   Engine type to set in TaskOption to select API to use during the execution on worker
/// </summary>
public enum EngineType
{
  /// <summary>
  /// </summary>
  Symphony = 0,

  /// <summary>
  /// </summary>
  Unified = 1,
}

/// <summary>
///   Convert helper from Enum to class and reverse
/// </summary>
public static class EngineTypeHelper
{
  /// <summary>
  ///   The method to convert string enum name to obejct enum
  /// </summary>
  /// <param name="enumName"></param>
  /// <returns></returns>
  /// <exception cref="KeyNotFoundException"></exception>
  public static EngineType ToEnum(string enumName)
    => enumName switch
       {
         "Symphony" => EngineType.Symphony,
         "Unified"  => EngineType.Unified,
         _          => throw new KeyNotFoundException($"enumName, possible choice are [{string.Join(", ", typeof(EngineType).GetEnumNames())}]"),
       };
}

/// <summary>
///   Map table to target API
/// </summary>
public class EngineTypes
{
  private readonly Dictionary<EngineType, string> engineTypes_ = new()
                                                                 {
                                                                   {
                                                                     EngineType.Symphony, "ArmoniK.DevelopmentKit.Worker.Symphony"
                                                                   },

                                                                   {
                                                                     EngineType.Unified, "ArmoniK.DevelopmentKit.Worker.Unified"
                                                                   },
                                                                 };

  /// <summary>
  ///   Get the EngineType Assembly name for AppLoader
  /// </summary>
  /// <param name="key">The Engine type</param>
  /// <exception cref="KeyNotFoundException">Exception the key doesn't exist</exception>
  public string this[EngineType key]
  {
    get
    {
      if (engineTypes_.TryGetValue(key,
                                   out var item))
      {
        return item;
      }

      throw new KeyNotFoundException($"There is no engine type [{key}]");
    }
  }
}
