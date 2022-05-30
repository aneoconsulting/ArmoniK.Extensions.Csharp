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
using System.Collections.Generic;

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf;

using JetBrains.Annotations;

namespace ArmoniK.Extensions.Common.StreamWrapper.Worker
{
  [PublicAPI]
  public static class TaskRequestExtensions
  {
    public static IEnumerable<ProcessReply.Types.CreateLargeTaskRequest> ToRequestStream(this IEnumerable<TaskRequest> taskRequests,
                                                                                         TaskOptions                   taskOptions,
                                                                                         int                           chunkMaxSize)
    {
      yield return new()
      {
        InitRequest = new()
        {
          TaskOptions = taskOptions,
        },
      };

      using var taskRequestEnumerator = taskRequests.GetEnumerator();

      if (!taskRequestEnumerator.MoveNext())
      {
        yield break;
      }

      var currentRequest = taskRequestEnumerator.Current;

      while (taskRequestEnumerator.MoveNext())
      {

        foreach (var createLargeTaskRequest in currentRequest.ToRequestStream(false,
                                                                              chunkMaxSize))
        {
          yield return createLargeTaskRequest;
        }


        currentRequest = taskRequestEnumerator.Current;
      }

      foreach (var createLargeTaskRequest in currentRequest.ToRequestStream(true,
                                                                            chunkMaxSize))
      {
        yield return createLargeTaskRequest;
      }
    }

    private static IEnumerable<ProcessReply.Types.CreateLargeTaskRequest> ToRequestStream(this TaskRequest taskRequest,
                                                                                         bool             isLast,
                                                                                         int              chunkMaxSize)
    {
      yield return new()
      {
        InitTask = new()
        {
          Header = new()
          {
            DataDependencies =
            {
              taskRequest.DataDependencies,
            },
            ExpectedOutputKeys =
            {
              taskRequest.ExpectedOutputKeys,
            },
            Id = taskRequest.Id,
          },
        },
      };

      var start = 0;

      while (start < taskRequest.Payload.Length)
      {
        var chunkSize = Math.Min(chunkMaxSize,
                                 taskRequest.Payload.Length - start);

        yield return new()
        {
          TaskPayload = new()
          {
            Data = UnsafeByteOperations.UnsafeWrap(taskRequest.Payload.Memory.Slice(start,
                                                                                    chunkSize)),
          },
        };

        start += chunkSize;
      }

      yield return new()
      {
        TaskPayload = new()
        {
          DataComplete = true,
        },
      };

      if (isLast)
      {
        yield return new()
        {
          InitTask = new()
          {
            LastTask = true,
          },
        };

      }

    }
  }
}
