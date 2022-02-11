// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
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

    public static IEnumerable<ProcessReply.Types.CreateLargeTaskRequest> ToRequestStream(this TaskRequest taskRequest,
                                                                                         bool             isLast,
                                                                                         int              chunkMaxSize)
    {
      yield return new()
                   {
                     InitTask = new()
                                {
                                  DataDependencies =
                                  {
                                    taskRequest.DataDependencies,
                                  },
                                  ExpectedOutputKeys =
                                  {
                                    taskRequest.ExpectedOutputKeys,
                                  },
                                  Id       = taskRequest.Id,
                                  LastTask = isLast,
                                  PayloadChunk = new()
                                                 {
                                                   DataComplete = taskRequest.Payload.Length < chunkMaxSize,
                                                   Data = taskRequest.Payload.Length < chunkMaxSize
                                                            ? taskRequest.Payload
                                                            : ByteString.CopyFrom(taskRequest.Payload.Span[..chunkMaxSize]),
                                                 },
                                },
                   };

      if (taskRequest.Payload.Length < chunkMaxSize)
        yield break;

      var start = chunkMaxSize;

      while (start < taskRequest.Payload.Length)
      {
        var chunkSize = Math.Min(chunkMaxSize,
                                 taskRequest.Payload.Length - start);

        var nextStart = start + chunkSize;


        yield return new()
                     {
                       TaskPayload = new()
                                     {
                                       DataComplete = nextStart < taskRequest.Payload.Length,
                                       Data = ByteString.CopyFrom(taskRequest.Payload.Span.Slice(start,
                                                                                                 chunkSize)),
                                     },
                     };

        start = nextStart;
      }
    }
  }
}
