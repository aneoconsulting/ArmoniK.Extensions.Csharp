// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2024. All rights reserved.
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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Tasks;
using ArmoniK.Utils.Pool;

using Grpc.Net.Client;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

public static class TasksClientExt
{
  /// <summary>
  ///   Filter on task id
  /// </summary>
  /// <param name="taskId"> the task id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskIdFilter(string taskId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.TaskId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Value    = taskId,
                              Operator = FilterStringOperator.Equal,
                            },
           },
         },
       };

  /// <summary>
  ///   Filter tasks on their sessionId
  /// </summary>
  /// <param name="sessionId"> the session id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskSessionIdFilter(string sessionId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.SessionId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Value    = sessionId,
                              Operator = FilterStringOperator.Equal,
                            },
           },
         },
       };

  /// <summary>
  ///   Filter on task status ajd session id
  /// </summary>
  /// <param name="status"> the task status to filter on </param>
  /// <param name="sessionId"> the session id to filter on </param>
  /// <returns></returns>
  public static FiltersAnd TaskStatusFilter(TaskStatus status,
                                            string     sessionId)
    => new()
       {
         And =
         {
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.Status,
                                          },
                     },
             FilterStatus = new FilterStatus
                            {
                              Operator = FilterStatusOperator.Equal,
                              Value    = status,
                            },
           },
           new FilterField
           {
             Field = new TaskField
                     {
                       TaskSummaryField = new TaskSummaryField
                                          {
                                            Field = TaskSummaryEnumField.SessionId,
                                          },
                     },
             FilterString = new FilterString
                            {
                              Operator = FilterStringOperator.Equal,
                              Value    = sessionId,
                            },
           },
         },
       };

  /// <summary>
  ///   List tasks while handling page size
  /// </summary>
  /// <param name="pool"> the tasks client </param>
  /// <param name="filters"> filters to apply on the tasks </param>
  /// <param name="sort"> sorting order </param>
  /// <param name="pageSize"> page size </param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public static async IAsyncEnumerable<TaskSummary> ListTasksAsync(this ObjectPool<GrpcChannel>               pool,
                                                                   Filters                                    filters,
                                                                   ListTasksRequest.Types.Sort                sort,
                                                                   int                                        pageSize          = 50,
                                                                   [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var               page = 0;
    ListTasksResponse res;
    while ((res = await pool.WithTaskClient()
                            .WithDefaultRetries()
                            .ExecuteAsync(client => client.ListTasksAsync(new ListTasksRequest
                                                                          {
                                                                            Filters  = filters,
                                                                            Sort     = sort,
                                                                            PageSize = pageSize,
                                                                            Page     = page,
                                                                          },
                                                                          cancellationToken: cancellationToken),
                                          cancellationToken)
                            .ConfigureAwait(false)).Tasks.Any())
    {
      foreach (var taskSummary in res.Tasks)
      {
        yield return taskSummary;
      }

      page++;
    }
  }

  /// <summary>
  ///   List tasks while handling page size
  /// </summary>
  /// <param name="pool"> the tasks client </param>
  /// <param name="filters"> filters to apply on the tasks </param>
  /// <param name="sort"> sorting order </param>
  /// <param name="pageSize"> page size </param>
  /// <returns></returns>
  public static IEnumerable<TaskSummary> ListTasks(this ObjectPool<GrpcChannel> pool,
                                                   Filters                      filters,
                                                   ListTasksRequest.Types.Sort  sort,
                                                   int                          pageSize = 50)
    => ListTasksAsync(pool,
                      filters,
                      sort,
                      pageSize)
      .ToEnumerable();
}
