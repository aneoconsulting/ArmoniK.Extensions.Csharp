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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;

/// <summary>
///   Provides a dataFlow block that batches inputs into arrays.
///   A batch is produced when the number of currently queued items becomes equal
///   to BatchSize, or when a Timeout period has elapsed after receiving the last item.
/// </summary>
public class BatchUntilInactiveBlock<T> : IPropagatorBlock<T, T[]>, IReceivableSourceBlock<T[]>
{
  private readonly ExecutionDataflowBlockOptions executionDataFlowBlockOptions_;
  private readonly BatchBlock<T>                 source_;
  private readonly TransformBlock<T[], T[]>      timeoutTransformBlock_;
  private readonly Timer                         timer_;

  /// <summary>
  ///   The buffer construct base on the number of request in the buffer
  ///   Be aware that buffer should be T sized for network B/W
  /// </summary>
  /// <param name="bufferRequestsSize"></param>
  /// <param name="timeout">Time out before the next submit call</param>
  /// <param name="executionDataFlowBlockOptions">
  ///   Parameters to control execution for each block in pipeline
  ///   Options to configure message.
  ///   https://learn.microsoft.com/fr-fr/dotnet/api/system.threading.tasks.dataflow.executiondataflowblockoptions?view=net-6.0
  /// </param>
  public BatchUntilInactiveBlock(int                               bufferRequestsSize,
                                 TimeSpan                          timeout,
                                 ExecutionDataflowBlockOptions? executionDataFlowBlockOptions = null)
  {
    executionDataFlowBlockOptions_ = executionDataFlowBlockOptions ?? new ExecutionDataflowBlockOptions
                                                                      {
                                                                        BoundedCapacity        = 1,
                                                                        MaxDegreeOfParallelism = 1,
                                                                        EnsureOrdered          = true,
                                                                      };

    source_ = new BatchBlock<T>(bufferRequestsSize,
                                new GroupingDataflowBlockOptions
                                {
                                  BoundedCapacity = bufferRequestsSize,
                                  EnsureOrdered   = true,
                                });

    timer_ = new Timer(_ =>
                       {
                         source_.TriggerBatch();
                       },
                       null,
                       timeout,
                       System.Threading.Timeout.InfiniteTimeSpan);

    timeoutTransformBlock_ = new TransformBlock<T[], T[]>(value =>
                                                          {
                                                            timer_.Change(timeout,
                                                                          System.Threading.Timeout.InfiniteTimeSpan);

                                                            return value;
                                                          },
                                                          executionDataFlowBlockOptions_);

    source_.LinkTo(timeoutTransformBlock_,
                   new DataflowLinkOptions
                   {
                     PropagateCompletion = true,
                   });

    Timeout = timeout;
  }

  /// <summary>
  ///   Simple Getter to return size of batch in the pipeline
  /// </summary>
  public int BatchSize
    => source_.BatchSize;

  /// <summary>
  ///   Return the TimeSpan timer set in the constructor
  /// </summary>
  private TimeSpan Timeout { get; }

  /// <inheritdoc />
  public Task Completion
    => source_.Completion;


  /// <inheritdoc />
  public void Complete()
    => source_.Complete();

  /// <inheritdoc />
  public void Fault(Exception exception)
    => ((IDataflowBlock)source_).Fault(exception);


  /// <inheritdoc />
  public IDisposable LinkTo(ITargetBlock<T[]>   target,
                            DataflowLinkOptions linkOptions)
    => timeoutTransformBlock_.LinkTo(target,
                                     linkOptions);


  /// <inheritdoc />
  public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader,
                                            T                     messageValue,
                                            ISourceBlock<T>       source,
                                            bool                  consumeToAccept)
  {
    var offerResult = ((ITargetBlock<T>)source_).OfferMessage(messageHeader,
                                                              messageValue,
                                                              source,
                                                              consumeToAccept);

    if (offerResult == DataflowMessageStatus.Accepted)
    {
      timer_.Change(Timeout,
                    System.Threading.Timeout.InfiniteTimeSpan);
    }

    return offerResult;
  }

  /// <inheritdoc />
  public T[] ConsumeMessage(DataflowMessageHeader messageHeader,
                            ITargetBlock<T[]>     target,
                            out bool              messageConsumed)
    => ((ISourceBlock<T[]>)source_).ConsumeMessage(messageHeader,
                                                   target,
                                                   out messageConsumed);

  /// <inheritdoc />
  public bool ReserveMessage(DataflowMessageHeader messageHeader,
                             ITargetBlock<T[]>     target)
    => ((ISourceBlock<T[]>)source_).ReserveMessage(messageHeader,
                                                   target);

  /// <inheritdoc />
  public void ReleaseReservation(DataflowMessageHeader messageHeader,
                                 ITargetBlock<T[]>     target)
    => ((ISourceBlock<T[]>)source_).ReleaseReservation(messageHeader,
                                                       target);

  /// <inheritdoc />
  public bool TryReceive(Predicate<T[]> filter,
                         out T[]        item)
    => source_.TryReceive(filter,
                          out item);

  /// <inheritdoc />
  public bool TryReceiveAll(out IList<T[]> items)
    => source_.TryReceiveAll(out items);

  /// <summary>
  ///   Create an ActionBlock with a delegated function to execute
  ///   at the end of pipeline
  /// </summary>
  /// <param name="action">the method to call</param>
  public void ExecuteAsync(Action<T[]> action)
  {
    var actBlock = new ActionBlock<T[]>(action,
                                        executionDataFlowBlockOptions_);

    timeoutTransformBlock_.LinkTo(actBlock,
                                  new DataflowLinkOptions
                                  {
                                    PropagateCompletion = true,
                                  });
  }

  /// <summary>
  ///   Trigger the batch even if it doesn't criteria to submit
  /// </summary>
  public void TriggerBatch()
    => source_.TriggerBatch();
}
