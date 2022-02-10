// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

using ArmoniK.Core.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Common
{
  public static class TaskIdExt
  {
    /// <summary>
    ///   Concatenate TaskId and SubSessionId into a string
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public static string PackTaskId(this TaskId taskId) => $"{taskId.SubSession}#{taskId.Task}";

    /// <summary>
    ///   Unpack TaskId and SubTaskId
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static TaskId UnPackTaskId(this string id)
    {
      var split = id.Split('#');
      if (split.Length != 2)
        throw new ArgumentException("Id is not a valid TaskId",
                                    nameof(id));
      return new()
             {
               SubSession = split[0],
               Task       = split[1],
             };
    }

    /// <summary>
    ///   Unpack TaskId and SubTaskId
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static bool CanUnPackTaskId(this string id)
    {
      var split = id.Split('#');
      if (split.Length != 2)
        return false;

      return true;
    }
  }
}
