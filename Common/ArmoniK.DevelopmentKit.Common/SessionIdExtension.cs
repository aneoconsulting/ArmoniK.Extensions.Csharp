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

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Common
{
  [Obsolete]
  public static class SessionIdExtension
  {
    /// <summary>
    ///   Concatenante SessionId and SubSessionId into a string
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    public static string PackSessionId(this Session sessionId) => $"{sessionId.Id}#Obsolete";

    /// <summary>
    ///   Unpack SessionId and SubSessionId
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Session UnPackSessionId(this string id)
    {
      var split = id.Split('#');
      if (split.Length != 2)
        throw new ArgumentException("Id is not a valid SessionId",
                                    nameof(id));
      return new()
             {
               Id    = split[0],
             };
    }
  }
}
