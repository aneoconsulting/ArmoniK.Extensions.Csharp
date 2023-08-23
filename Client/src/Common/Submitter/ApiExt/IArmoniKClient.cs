// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using JetBrains.Annotations;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

/// <summary>
///   Provides access to the ArmoniK Submitter Service
/// </summary>
[PublicAPI]
// TODO: should be in ArmoniK.Api
// TODO: Should be split in different services interfaces
public interface IArmoniKClient
{

  /// <summary>
  ///   Creates a new ArmoniK session.
  /// </summary>
  /// <param name="taskOptions">Default task options for the session</param>
  /// <param name="partitions">List of all partition that will be used during the session</param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<string> CreateSessionAsync(TaskOptions                 taskOptions,
                                         IReadOnlyCollection<string> partitions,
                                         int                         maxRetries        = 1,
                                         double                      totalTimeoutMs    = 1e10,
                                         CancellationToken           cancellationToken = default);


  /// <summary>
  ///   Submits new tasks to ArmoniK
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="definitions">Definition of the tasks</param>
  /// <param name="taskOptions">Task options for these tasks</param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<IEnumerable<TaskInfo>> SubmitTasksAsync(string                      sessionId,
                                                      IEnumerable<TaskDefinition> definitions,
                                                      TaskOptions                 taskOptions,
                                                      int                         maxRetries        = 1,
                                                      double                      totalTimeoutMs    = 1e10,
                                                      CancellationToken           cancellationToken = default);


  /// <summary>
  ///   Gets the resultIds from the tasks
  /// </summary>
  /// <param name="taskIds">Id of the tasks</param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<IEnumerable<TaskOutputIds>> GetResultIdsAsync(IReadOnlyCollection<string> taskIds,
                                                            int                         maxRetries        = 1,
                                                            double                      totalTimeoutMs    = 1e10,
                                                            CancellationToken           cancellationToken = default);
  /// <summary>
  ///   Downloads a result
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="resultId">Id of the result</param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<byte[]> DownloadResultAsync(string            sessionId,
                                          string            resultId,
                                          int               maxRetries        = 1,
                                          double            totalTimeoutMs    = 1e10,
                                          CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets the status of a task
  /// </summary>
  /// <param name="taskIds">Id of the tasks</param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<IEnumerable<TaskIdStatus>> GetTaskStatusAsync(IReadOnlyCollection<string> taskIds,
                                                            int                         maxRetries        = 1,
                                                            double                      totalTimeoutMs    = 1e10,
                                                            CancellationToken           cancellationToken = default);

  /// <summary>
  ///   Gets the status of a result
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="resultIds"></param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<IEnumerable<ResultIdStatus>> GetResultStatusAsync(string                      sessionId,
                                                                IReadOnlyCollection<string> resultIds,
                                                                int                         maxRetries        = 1,
                                                                double                      totalTimeoutMs    = 1e10,
                                                                CancellationToken           cancellationToken = default);

  /// <summary>
  ///   Tries to get the error result of a task
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="taskId">Id of the task</param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  /// <returns><c>null</c> if no error is available, the error message otherwise</returns>
  public Task<string?> TryGetTaskErrorAsync(string            sessionId,
                                            string            taskId,
                                            int               maxRetries        = 1,
                                            double            totalTimeoutMs    = 1e10,
                                            CancellationToken cancellationToken = default);

  /// <summary>
  ///   Waits for tasks to be completed
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="taskIds">Id of the tasks</param>
  /// <param name="stopOnFirstTaskCancellation"></param>
  /// <param name="stopOnFirstTaskError"></param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<ImmutableDictionary<TaskStatus, int>> WaitForCompletionAsync(string                      sessionId,
                                                                           IReadOnlyCollection<string> taskIds,
                                                                           bool                        stopOnFirstTaskCancellation,
                                                                           bool                        stopOnFirstTaskError,
                                                                           int                         maxRetries        = 1,
                                                                           double                      totalTimeoutMs    = 1e10,
                                                                           CancellationToken           cancellationToken = default);

  /// <summary>
  ///   Waits for some results to be available
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="resultId"></param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task WaitForAvailability(string            sessionId,
                                  string            resultId,
                                  int               maxRetries        = 1,
                                  double            totalTimeoutMs    = 1e10,
                                  CancellationToken cancellationToken = default);


  /// <summary>
  ///   Create the metadata corresponding to tasks
  /// </summary>
  /// <param name="sessionId">Id of the session</param>
  /// <param name="resultNames"></param>
  /// <param name="maxRetries">Number of times the call must be retried. Default=1</param>
  /// <param name="totalTimeoutMs">Define a timeout for the global call (including all retries)</param>
  /// <param name="cancellationToken"></param>
  public Task<IEnumerable<KeyValuePair<string, string>>> CreateResultMetaDataAsync(string                      sessionId,
                                                                                   IReadOnlyCollection<string> resultNames,
                                                                                   int                         maxRetries        = 1,
                                                                                   double                      totalTimeoutMs    = 1e10,
                                                                                   CancellationToken           cancellationToken = default);
}
