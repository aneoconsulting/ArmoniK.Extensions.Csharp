// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
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

using ArmoniK.Utils;

using Grpc.Core;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Add some <c>ChannelBase</c> related features to <c>ObjectPool</c>
/// </summary>
public static class ChannelPoolExt
{
  /// <summary>
  ///   Call f with an acquired channel and automatically manage the guard lifecycle
  /// </summary>
  /// <param name="f">Function to be called</param>
  /// <typeparam name="T">Type of the return type of f</typeparam>
  /// <returns>Value returned by f</returns>
  public static T WithChannel<T>(this ObjectPool<ChannelBase> pool,
                                 Func<ChannelBase, T>         f)
  {
    using var guard = pool.Get();
    return f(guard.Value);
  }
}
