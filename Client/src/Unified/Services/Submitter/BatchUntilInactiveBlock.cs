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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ArmoniK.DevelopmentKit.Client.Unified.Services.Submitter;

/// <summary>
///   Provides a dataFlow block that batches inputs into arrays.
///   A batch is produced when the number of currently queued items becomes equal
///   to BatchSize, or when a Timeout period has elapsed after receiving the last item.
/// </summary>
public class BatchUntilInactiveBlock<T> : IPropagatorBlock<T, T[]>, IReceivableSourceBlock<T[]>
{
  private readonly BatchBlock<T>            source_;
  private readonly TransformBlock<T[], T[]> timeoutTransformBlock_;
  private readonly Timer                    timer_;

  /// <summary>
  ///   The buffer construct base on the number of request in the buffer
  ///   Be aware that buffer should be T sized for network B/W
  /// </summary>
  /// <param name="bufferRequestsSize"></param>
  /// <param name="timeout">Time out before the next submit call</param>
  /// <param name="dataFlowBlockOptions">
  ///   Options to configure message.
  ///   (https://learn.microsoft.com/fr-fr/dotnet/api/system.threading.tasks.dataflow.groupingdataflowblockoptions?view=net-6.0)
  /// </param>
  public BatchUntilInactiveBlock(int                          bufferRequestsSize,
                                 TimeSpan                     timeout,
                                 GroupingDataflowBlockOptions dataFlowBlockOptions)
  {
    source_ = new BatchBlock<T>(bufferRequestsSize,
                                dataFlowBlockOptions);
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
                                                          });

    source_.LinkTo(timeoutTransformBlock_);

    Timeout = timeout;
  }

  /// <summary>
  ///   The buffer construct base on the number of request in the buffer
  ///   Be aware that buffer should be T sized for network B/W
  /// </summary>
  /// <param name="bufferRequestsSize">The capacity buffer to retain request before sending</param>
  /// <param name="timeout">Time out before the next submit call</param>
  public BatchUntilInactiveBlock(int      bufferRequestsSize,
                                 TimeSpan timeout)
    : this(bufferRequestsSize,
           timeout,
           new GroupingDataflowBlockOptions())
  {
  }

  /// <summary>
  ///   Simple Getter to return size of batch in the pipeline
  /// </summary>
  public int BatchSize
    => source_.BatchSize;

  /// <summary>
  ///   Return the TimeSpan timer set in the constructor
  /// </summary>
  public TimeSpan Timeout { get; }

  /// <summary>
  ///   Task to check the completion
  /// </summary>
  public Task Completion
    => source_.Completion;

  /// <summary>
  ///   Signal that the pipeline should be closed
  /// </summary>
  public void Complete()
    => source_.Complete();

  /// <summary>
  ///   Return the exception produce in a Transform block or ActionBlock
  /// </summary>
  /// <param name="exception"></param>
  public void Fault(Exception exception)
    => ((IDataflowBlock)source_).Fault(exception);

  /// <summary>
  ///   Link block TransformBLock or ActionBlock
  /// </summary>
  /// <param name="target">the Block to link to</param>
  /// <param name="linkOptions">
  ///   Specific option to configure max message and propagation completion
  ///   https://learn.microsoft.com/fr-fr/dotnet/api/system.threading.tasks.dataflow.dataflowlinkoptions?view=net-6.0
  /// </param>
  /// <returns></returns>
  public IDisposable LinkTo(ITargetBlock<T[]>   target,
                            DataflowLinkOptions linkOptions)
    => timeoutTransformBlock_.LinkTo(target,
                                     linkOptions);

  /// <summary>
  ///   To signal to the propagator object that a message is coming.
  ///   Check if the pipeline is opened and the message is accepted
  /// </summary>
  /// <param name="messageHeader"></param>
  /// <param name="messageValue"></param>
  /// <param name="source"></param>
  /// <param name="consumeToAccept"></param>
  /// <returns></returns>
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

  /// <summary>
  ///   Called by a linked ITargetBlock TInput  to accept and consume a
  ///   DataFlowMessageHeader previously offered by this ISourceBlockTOutput .
  /// </summary>
  /// <param name="messageHeader"></param>
  /// <param name="target"></param>
  /// <param name="messageConsumed"></param>
  /// <returns></returns>
  public T[]? ConsumeMessage(DataflowMessageHeader messageHeader,
                             ITargetBlock<T[]?>    target,
                             out bool              messageConsumed)
    => ((ISourceBlock<T[]?>)source_).ConsumeMessage(messageHeader,
                                                    target,
                                                    out messageConsumed);

  /// <summary>
  ///   Called by a linked ITargetBlock TInput  to reserve a previously offered
  ///   DataFlowMessageHeader by this ISourceBlockTOutput .
  /// </summary>
  /// <param name="messageHeader"></param>
  /// <param name="target"></param>
  /// <returns></returns>
  public bool ReserveMessage(DataflowMessageHeader messageHeader,
                             ITargetBlock<T[]>     target)
    => ((ISourceBlock<T[]>)source_).ReserveMessage(messageHeader,
                                                   target);

  /// <summary>
  ///   Called by a linked ITargetBlock  TInput  to release a previously reserved DataflowMessageHeader by this
  ///   ISourceBlock;.
  /// </summary>
  /// <param name="messageHeader"></param>
  /// <param name="target"></param>
  public void ReleaseReservation(DataflowMessageHeader messageHeader,
                                 ITargetBlock<T[]>     target)
    => ((ISourceBlock<T[]>)source_).ReleaseReservation(messageHeader,
                                                       target);

  /// <summary>
  ///   Attempts to synchronously receive an available output item from the IReceivableSourceBlock TOutput
  /// </summary>
  /// <param name="filter"></param>
  /// <param name="item"></param>
  /// <returns></returns>
  public bool TryReceive(Predicate<T[]>? filter,
                         out T[]         item)
    => source_.TryReceive(filter,
                          out item);

  /// <summary>
  ///   Attempts to synchronously receive all available items from the IReceivableSourceBlock TOutput
  /// </summary>
  /// <param name="items"></param>
  /// <returns></returns>
  public bool TryReceiveAll(out IList<T[]> items)
    => source_.TryReceiveAll(out items);

  /// <summary>
  ///   Trigger the batch even if it doesn't criteria to submit
  /// </summary>
  public void TriggerBatch()
    => source_.TriggerBatch();

  /// <summary>
  ///   Create an ActionBlock with a delegated function to execute
  ///   at the end of pipeline
  /// </summary>
  /// <param name="action">the method to call</param>
  public void ExecuteAsync(Action<IEnumerable<T>> action)
    => timeoutTransformBlock_.LinkTo(new ActionBlock<T[]>(action),
                                     new DataflowLinkOptions
                                     {
                                       PropagateCompletion = true,
                                     });
}
