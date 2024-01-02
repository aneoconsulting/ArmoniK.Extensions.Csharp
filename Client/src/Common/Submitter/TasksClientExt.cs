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

using System.Collections.Generic;
using System.Linq;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Tasks;

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
  /// <param name="tasksClient"> the tasks client </param>
  /// <param name="filters"> filters to apply on the tasks </param>
  /// <param name="sort"> sorting order </param>
  /// <param name="pageSize"> page size </param>
  /// <returns></returns>
  public static IEnumerable<TaskSummary> ListTasks(this Tasks.TasksClient      tasksClient,
                                                   Filters                     filters,
                                                   ListTasksRequest.Types.Sort sort,
                                                   int                         pageSize = 50)
  {
    var               page = 0;
    ListTasksResponse res;
    while ((res = tasksClient.ListTasks(new ListTasksRequest
                                        {
                                          Filters  = filters,
                                          Sort     = sort,
                                          PageSize = pageSize,
                                          Page     = page,
                                        })).Tasks.Any())
    {
      foreach (var taskSummary in res.Tasks)
      {
        yield return taskSummary;
      }

      page++;
    }
  }
}
