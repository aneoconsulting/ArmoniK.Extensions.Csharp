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

namespace ArmoniK.DevelopmentKit.Client.Common.Submitter;

/// <summary>
///   Provides a dataflow block that batches inputs into arrays.
///   A batch is produced when the number of currently queued items becomes equal
///   to BatchSize, or when a Timeout period has elapsed after receiving the last item.
/// </summary>
public class BatchUntilInactiveBlock<T> : IPropagatorBlock<T, T[]>, IReceivableSourceBlock<T[]>
{
  private readonly BatchBlock<T>            _source;
  private readonly TransformBlock<T[], T[]> _timeoutTransformBlock;
  private readonly Timer                    _timer;

  public BatchUntilInactiveBlock(int                          batchSize,
                                 TimeSpan                     timeout,
                                 GroupingDataflowBlockOptions dataFlowBlockOptions)
  {
    _source = new BatchBlock<T>(batchSize,
                                dataFlowBlockOptions);
    _timer = new Timer(_ =>
                       {
                         _source.TriggerBatch();
                       },
                       null,
                       timeout,
                       System.Threading.Timeout.InfiniteTimeSpan);

    _timeoutTransformBlock = new TransformBlock<T[], T[]>(value =>
                                                          {
                                                            _timer.Change(timeout,
                                                                          System.Threading.Timeout.InfiniteTimeSpan);

                                                            return value;
                                                          });

    _source.LinkTo(_timeoutTransformBlock);

    Timeout = timeout;
  }

  public BatchUntilInactiveBlock(int      batchSize,
                                 TimeSpan timeout)
    : this(batchSize,
           timeout,
           new GroupingDataflowBlockOptions()
           {
           })
  {
  }

  public int BatchSize
    => _source.BatchSize;

  public TimeSpan Timeout { get; }

  public int OutputCount
    => _source.OutputCount;

  public long TotalBufferMemoryBytes { get; private set; }

  public Task Completion
    => _source.Completion;

  public void Complete()
    => _source.Complete();

  public void Fault(Exception exception)
    => ((IDataflowBlock)_source).Fault(exception);

  public IDisposable LinkTo(ITargetBlock<T[]>   target,
                            DataflowLinkOptions linkOptions)
    => _timeoutTransformBlock.LinkTo(target,
                                     linkOptions);

  public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader,
                                            T                     messageValue,
                                            ISourceBlock<T>       source,
                                            bool                  consumeToAccept)
  {
    var offerResult = ((ITargetBlock<T>)_source).OfferMessage(messageHeader,
                                                              messageValue,
                                                              source,
                                                              consumeToAccept);

    if (offerResult == DataflowMessageStatus.Accepted)
    {
      _timer.Change(Timeout,
                    System.Threading.Timeout.InfiniteTimeSpan);
    }

    return offerResult;
  }

  public T[]? ConsumeMessage(DataflowMessageHeader messageHeader,
                             ITargetBlock<T[]?>    target,
                             out bool              messageConsumed)
    => ((ISourceBlock<T[]?>)_source).ConsumeMessage(messageHeader,
                                                    target,
                                                    out messageConsumed);

  public bool ReserveMessage(DataflowMessageHeader messageHeader,
                             ITargetBlock<T[]>     target)
    => ((ISourceBlock<T[]>)_source).ReserveMessage(messageHeader,
                                                   target);

  public void ReleaseReservation(DataflowMessageHeader messageHeader,
                                 ITargetBlock<T[]>     target)
    => ((ISourceBlock<T[]>)_source).ReleaseReservation(messageHeader,
                                                       target);

  public bool TryReceive(Predicate<T[]>? filter,
                         out T[]         item)
    => _source.TryReceive(filter,
                          out item);

  public bool TryReceiveAll(out IList<T[]> items)
    => _source.TryReceiveAll(out items);

  public void TriggerBatch()
    => _source.TriggerBatch();

  public void ExecuteAsync(Action<IEnumerable<T>> action)
    => _timeoutTransformBlock.LinkTo(new ActionBlock<T[]>(action),
                                     new DataflowLinkOptions
                                     {
                                       PropagateCompletion = true,
                                       
                                     });
}
