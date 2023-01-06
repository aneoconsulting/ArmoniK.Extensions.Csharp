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

using System;
using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   List of result status that will be collected during the request GetResultStatus
/// </summary>
public class ResultStatusCollection
{
  /// <summary>
  ///   Default constructor for ResultStatusCollection
  /// </summary>
  /// <param name="idsReady">List of ids which result are ready</param>
  /// <param name="idsResultError">List of ids which result cannot be retrieved</param>
  /// <param name="idsError">List of ids which is in error from task</param>
  /// <param name="idsNotReady">List of ids which result are not yet ready</param>
  /// <param name="canceled">List of ids which result cannot be retrieved since the task was cancelled</param>
  public ResultStatusCollection(IEnumerable<ResultStatusData>? idsReady       = null,
                                IEnumerable<ResultStatusData>? idsResultError = null,
                                IEnumerable<string>?           idsError       = null,
                                IEnumerable<ResultStatusData>? idsNotReady    = null,
                                IEnumerable<ResultStatusData>? canceled       = null)
  {
    IdsReady       = idsReady       ?? Array.Empty<ResultStatusData>();
    IdsResultError = idsResultError ?? Array.Empty<ResultStatusData>();
    IdsError       = idsError       ?? Array.Empty<string>();
    IdsNotReady    = idsNotReady    ?? Array.Empty<ResultStatusData>();
    Canceled       = canceled;
  }

  /// <summary>
  ///   List of completed task where the result is ready to be retrieved
  /// </summary>
  public IEnumerable<ResultStatusData> IdsReady { get; init; }

  /// <summary>
  ///   List of task or task result in error
  /// </summary>
  public IEnumerable<ResultStatusData> IdsResultError { get; init; }

  /// <summary>
  ///   List of Unknown TaskIds. There is a heavy error somewhere else in the execution when this list has element
  /// </summary>
  public IEnumerable<string> IdsError { get; init; }

  /// <summary>
  ///   List of result not yet written in database
  /// </summary>
  public IEnumerable<ResultStatusData> IdsNotReady { get; init; }

  /// <summary>
  ///   The list of canceled task
  /// </summary>
  public IEnumerable<ResultStatusData>? Canceled { get; init; }
}
