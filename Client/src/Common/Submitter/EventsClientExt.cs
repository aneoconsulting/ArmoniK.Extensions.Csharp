// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Events;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.DevelopmentKit.Client.Common.Exceptions;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

internal static class EventsClientExt
{
  private static FiltersAnd ResultsFilter(string resultId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new ResultField
                     {
                       ResultRawField = new ResultRawField
                                        {
                                          Field = ResultRawEnumField.ResultId,
                                        },
                     },
             FilterString = new FilterString
                            {
                              Operator = FilterStringOperator.Equal,
                              Value    = resultId,
                            },
           },
         },
       };

  private static async Task WaitForResultsAsync(this Events.EventsClient client,
                                                string                   sessionId,
                                                ICollection<string>      resultIds,
                                                CancellationToken        cancellationToken)
  {
    var resultsNotFound = new HashSet<string>(resultIds);
    while (resultsNotFound.Any())
    {
      using var streamingCall = client.GetEvents(new EventSubscriptionRequest
                                                 {
                                                   SessionId = sessionId,
                                                   ReturnedEvents =
                                                   {
                                                     EventsEnum.ResultStatusUpdate,
                                                     EventsEnum.NewResult,
                                                   },
                                                   ResultsFilters = new Filters
                                                                    {
                                                                      Or =
                                                                      {
                                                                        resultsNotFound.Select(ResultsFilter),
                                                                      },
                                                                    },
                                                 });
      try
      {
        while (await streamingCall.ResponseStream.MoveNext(cancellationToken))
        {
          var resp = streamingCall.ResponseStream.Current;
          if (resp.UpdateCase == EventSubscriptionResponse.UpdateOneofCase.ResultStatusUpdate && resultsNotFound.Contains(resp.ResultStatusUpdate.ResultId))
          {
            if (resp.ResultStatusUpdate.Status == ResultStatus.Completed)
            {
              resultsNotFound.Remove(resp.ResultStatusUpdate.ResultId);
              if (!resultsNotFound.Any())
              {
                break;
              }
            }

            if (resp.ResultStatusUpdate.Status == ResultStatus.Aborted)
            {
              throw new ResultAbortedException($"Result {resp.ResultStatusUpdate.ResultId} has been aborted");
            }
          }

          if (resp.UpdateCase == EventSubscriptionResponse.UpdateOneofCase.NewResult && resultsNotFound.Contains(resp.NewResult.ResultId))
          {
            if (resp.NewResult.Status == ResultStatus.Completed)
            {
              resultsNotFound.Remove(resp.NewResult.ResultId);
              if (!resultsNotFound.Any())
              {
                break;
              }
            }

            if (resp.NewResult.Status == ResultStatus.Aborted)
            {
              throw new ResultAbortedException($"Result {resp.NewResult.ResultId} has been aborted");
            }
          }
        }
      }
      catch (OperationCanceledException e)
      {
      }
    }
  }
}
