// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ArmoniK.Api.gRPC.V1.Agent;
using ArmoniK.Api.Worker.Worker;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Worker.Common;

/// <summary>
///   Enumerable to create result Ids
/// </summary>
[PublicAPI]
public class ResultIdEnumerable : IEnumerable<string>, IEnumerator<string>
{
  private readonly bool         limit_;
  private readonly List<string> resultIds_;
  private readonly ITaskHandler taskHandler_;
  private          int          index_;

  /// <summary>
  ///   Creates a enumerable of result ids
  /// </summary>
  /// <param name="taskHandler">TaskHandler to create the results</param>
  /// <param name="count">Initial number of results to generate</param>
  /// <param name="limit">
  ///   Set to true to limit the number of result ids generated to the initial count, otherwise generates a
  ///   new result id for each call beyond the initial count
  /// </param>
  public ResultIdEnumerable(ITaskHandler taskHandler,
                            int          count = 0,
                            bool         limit = false)
  {
    taskHandler_ = taskHandler;
    resultIds_ = count > 0
                   ? taskHandler_.CreateResultsMetaDataAsync(Enumerable.Range(0,
                                                                              count)
                                                                       .Select(_ => new CreateResultsMetaDataRequest.Types.ResultCreate
                                                                                    {
                                                                                      Name = Guid.NewGuid()
                                                                                                 .ToString(),
                                                                                    }))
                                 .Result.Results.Select(r => r.ResultId)
                                 .ToList()
                   : new List<string>();
    limit_ = limit;
    index_ = -1;
  }

  /// <inheritdoc cref="IEnumerator{T}" />
  public IEnumerator<string> GetEnumerator()
    => this;

  IEnumerator IEnumerable.GetEnumerator()
    => this;

  /// <inheritdoc cref="IEnumerator{T}" />
  public bool MoveNext()
  {
    index_++;
    if (index_ < resultIds_.Count)
    {
      return true;
    }

    if (limit_)
    {
      return false;
    }

    resultIds_.Add(taskHandler_.CreateResultsMetaDataAsync(new[]
                                                           {
                                                             new CreateResultsMetaDataRequest.Types.ResultCreate
                                                             {
                                                               Name = Guid.NewGuid()
                                                                          .ToString(),
                                                             },
                                                           })
                               .Result.Results.Single()
                               .ResultId);

    return true;
  }

  /// <inheritdoc cref="IEnumerator{T}" />
  public void Reset()
    => index_ = -1;

  /// <inheritdoc cref="IEnumerator{T}" />
  [CanBeNull]
  public string Current
    => index_ >= 0 || index_ < resultIds_.Count
         ? resultIds_[index_]
         : null;

  object IEnumerator.Current
    => Current;

  /// <inheritdoc cref="IEnumerator{T}" />
  public void Dispose()
  {
    resultIds_.Clear();
    GC.SuppressFinalize(this);
  }
}
