// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using ArmoniK.Api.gRPC.V1;

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   Class for storing relation between result id, task id and result status
/// </summary>
public class ResultStatusData
{
  /// <summary>
  ///   Constructor for the class
  /// </summary>
  /// <param name="resultId">The id of the result</param>
  /// <param name="taskId">The id of the task producing the result</param>
  /// <param name="status">The status of the result</param>
  public ResultStatusData(string       resultId,
                          string       taskId,
                          ResultStatus status)
  {
    ResultId = resultId;
    TaskId   = taskId;
    Status   = status;
  }

  /// <summary>
  ///   The id of the result
  /// </summary>
  public string ResultId { get; }

  /// <summary>
  ///   The id of the task producing the result
  /// </summary>
  public string TaskId { get; }

  /// <summary>
  ///   The status of the result
  /// </summary>
  public ResultStatus Status { get; }
}

