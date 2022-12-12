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

using System.Collections.Generic;

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   List of result status that will be collected during the request GetResultStatus
/// </summary>
public class ResultStatusCollection
{
  /// <summary>
  ///   List of completed task where the result is ready to be retrieved
  /// </summary>
  public IEnumerable<ResultStatusData> IdsReady { get; set; } = default;

  /// <summary>
  ///   List of task or task result in error
  /// </summary>
  public IEnumerable<ResultStatusData> IdsResultError { get; set; } = default;

  /// <summary>
  ///   List of Unknown TaskIds. There is a heavy error somewhere else in the execution when this list has element
  /// </summary>
  public IEnumerable<string> IdsError { get; set; } = default;

  /// <summary>
  ///   List of result not yet written in database
  /// </summary>
  public IEnumerable<ResultStatusData> IdsNotReady { get; set; }

  /// <summary>
  ///   The list of canceled task
  /// </summary>
  public IEnumerable<ResultStatusData> Canceled { get; set; }
}

