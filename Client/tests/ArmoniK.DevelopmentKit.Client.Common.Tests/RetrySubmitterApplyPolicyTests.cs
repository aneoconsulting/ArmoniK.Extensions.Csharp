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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.DevelopmentKit.Client.Common.Submitter.ApiExt;

using Grpc.Core;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

namespace ArmoniK.DevelopmentKit.Client.Common.Tests;

/// <summary>
///   Tests the RetryArmoniKClient class
/// </summary>
[TestFixture]
public class RetrySubmitterApplyPolicyTests
{
  /// <summary>
  ///   MaxRetries should be strictly positive
  /// </summary>
  [Test]
  public void NegativeMaxRetriesTest()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => retrySubmitterClient.ApplyRetryPolicy(_ => Task.CompletedTask,
                                                                                                -1,
                                                                                                TimeSpan.FromMinutes(1),
                                                                                                CancellationToken.None));
  }

  /// <summary>
  ///   MaxRetries should be strictly positive
  /// </summary>
  [Test]
  public void ZeroMaxRetriesTest()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => retrySubmitterClient.ApplyRetryPolicy(_ => Task.CompletedTask,
                                                                                                0,
                                                                                                TimeSpan.FromMinutes(1),
                                                                                                CancellationToken.None));
  }

  /// <summary>
  ///   Timeout should be strictly positive
  /// </summary>
  [Test]
  public void NegativeTimeoutTest()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => retrySubmitterClient.ApplyRetryPolicy(_ => Task.CompletedTask,
                                                                                                1,
                                                                                                TimeSpan.FromMinutes(-1),
                                                                                                CancellationToken.None));
  }

  /// <summary>
  ///   Timeout should be strictly positive
  /// </summary>
  [Test]
  public void ZeroTimeoutTest()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => retrySubmitterClient.ApplyRetryPolicy(_ => Task.CompletedTask,
                                                                                                1,
                                                                                                TimeSpan.FromMinutes(0),
                                                                                                CancellationToken.None));
  }

  /// <summary>
  ///   Happy flow scenario
  /// </summary>
  [Test]
  public async Task ShouldSendResultIfNoError()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();

    var result = await retrySubmitterClient.ApplyRetryPolicy(_ => Task.FromResult(source),
                                                             1,
                                                             TimeSpan.FromSeconds(1),
                                                             CancellationToken.None);

    Assert.That(result,
                Is.Not.Null);
    Assert.That(result,
                Is.SameAs(source));
  }

  /// <summary>
  ///   First call throws an RpcException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public async Task ShouldSendResultIfOneRpcException()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();
    var calls  = 0;

    var result = await retrySubmitterClient.ApplyRetryPolicy(_ =>
                                                             {
                                                               calls++;
                                                               if (calls == 1)
                                                               {
                                                                 throw new RpcException(Grpc.Core.Status.DefaultCancelled);
                                                               }

                                                               return Task.FromResult(source);
                                                             },
                                                             5,
                                                             TimeSpan.FromSeconds(1),
                                                             CancellationToken.None);
    Assert.Multiple(() =>
                    {
                      Assert.That(result,
                                  Is.Not.Null);
                      Assert.That(result,
                                  Is.SameAs(source));
                      Assert.That(calls,
                                  Is.EqualTo(2));
                    });
  }

  /// <summary>
  ///   First call throws an RpcException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public async Task ShouldSendResultIfOneInnerRpcException()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();
    var calls  = 0;

    var result = await retrySubmitterClient.ApplyRetryPolicy(_ =>
                                                             {
                                                               calls++;
                                                               if (calls == 1)
                                                               {
                                                                 throw new Exception("",
                                                                                     new RpcException(Grpc.Core.Status.DefaultCancelled));
                                                               }

                                                               return Task.FromResult(source);
                                                             },
                                                             5,
                                                             TimeSpan.FromSeconds(1),
                                                             CancellationToken.None);
    Assert.Multiple(() =>
                    {
                      Assert.That(result,
                                  Is.Not.Null);
                      Assert.That(result,
                                  Is.SameAs(source));
                      Assert.That(calls,
                                  Is.EqualTo(2));
                    });
  }

  /// <summary>
  ///   First call throws an IOException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public void ShouldThrowIfOneIOException()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();
    var calls  = 0;

    Assert.ThrowsAsync<ArmoniKException>(() => retrySubmitterClient.ApplyRetryPolicy(_ =>
                                                                                     {
                                                                                       calls++;
                                                                                       if (calls == 1)
                                                                                       {
                                                                                         throw new IOException();
                                                                                       }

                                                                                       return Task.FromResult(source);
                                                                                     },
                                                                                     5,
                                                                                     TimeSpan.FromSeconds(1),
                                                                                     CancellationToken.None));

    Assert.Multiple(() =>
                    {
                      Assert.That(calls,
                                  Is.EqualTo(1));
                    });
  }

  /// <summary>
  ///   First call throws an IOException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public async Task ShouldSendResultIfOneIOException()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();
    var calls  = 0;

    var result = await retrySubmitterClient.ApplyRetryPolicy(_ =>
                                                             {
                                                               calls++;
                                                               if (calls == 1)
                                                               {
                                                                 throw new IOException();
                                                               }

                                                               return Task.FromResult(source);
                                                             },
                                                             5,
                                                             TimeSpan.FromSeconds(1),
                                                             CancellationToken.None,
                                                             null,
                                                             exception => exception is IOException);
    Assert.Multiple(() =>
                    {
                      Assert.That(result,
                                  Is.Not.Null);
                      Assert.That(result,
                                  Is.SameAs(source));
                      Assert.That(calls,
                                  Is.EqualTo(2));
                    });
  }

  /// <summary>
  ///   First call throws an IOException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public void ShouldThrowIfOneInnerIOException()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();
    var calls  = 0;

    Assert.ThrowsAsync<ArmoniKException>(() => retrySubmitterClient.ApplyRetryPolicy(_ =>
                                                                                     {
                                                                                       calls++;
                                                                                       if (calls == 1)
                                                                                       {
                                                                                         throw new Exception("",
                                                                                                             new IOException());
                                                                                       }

                                                                                       return Task.FromResult(source);
                                                                                     },
                                                                                     5,
                                                                                     TimeSpan.FromSeconds(1),
                                                                                     CancellationToken.None));

    Assert.Multiple(() =>
                    {
                      Assert.That(calls,
                                  Is.EqualTo(1));
                    });
  }

  /// <summary>
  ///   First call throws an IOException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public async Task ShouldSendResultIfOneInnerIOException()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var source = new object();
    var calls  = 0;

    var result = await retrySubmitterClient.ApplyRetryPolicy(_ =>
                                                             {
                                                               calls++;
                                                               if (calls == 1)
                                                               {
                                                                 throw new Exception("",
                                                                                     new IOException());
                                                               }

                                                               return Task.FromResult(source);
                                                             },
                                                             5,
                                                             TimeSpan.FromSeconds(1),
                                                             CancellationToken.None,
                                                             null,
                                                             exception => exception is IOException);
    Assert.Multiple(() =>
                    {
                      Assert.That(result,
                                  Is.Not.Null);
                      Assert.That(result,
                                  Is.SameAs(source));
                      Assert.That(calls,
                                  Is.EqualTo(2));
                    });
  }

  /// <summary>
  ///   First call throws an IOException but is handled by the policy and the proper result is returned.
  /// </summary>
  [Test]
  public void ShouldThrowAfterSeveralMaxRetries()
  {
    var logger          = NullLogger<RetryArmoniKClient>.Instance;
    var submitterClient = new Mock<IArmoniKClient>().Object;

    var retrySubmitterClient = new RetryArmoniKClient(logger,
                                                      submitterClient);

    var calls = 0;

    Assert.ThrowsAsync<ArmoniKException>(() => retrySubmitterClient.ApplyRetryPolicy<Task<object>>(_ =>
                                                                                                   {
                                                                                                     calls++;
                                                                                                     throw new Exception("",
                                                                                                                         new RpcException(Grpc.Core.Status
                                                                                                                                              .DefaultSuccess));
                                                                                                   },
                                                                                                   5,
                                                                                                   TimeSpan.FromMilliseconds(10),
                                                                                                   CancellationToken.None));
    Assert.That(calls,
                Is.EqualTo(6)); // 1 first attempt + 5 retries
  }
}
