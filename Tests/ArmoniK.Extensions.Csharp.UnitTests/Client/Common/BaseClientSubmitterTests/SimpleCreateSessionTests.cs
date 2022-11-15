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

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Submitter;
using ArmoniK.DevelopmentKit.Client.Common.Factory;
using ArmoniK.DevelopmentKit.Client.Common.Submitter;
using ArmoniK.Extensions.Csharp.UnitTests.Helpers;

using Grpc.Core;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace ArmoniK.Extensions.Csharp.UnitTests.Client.Common.BaseClientSubmitterTests;

public class SimpleCreateSessionTests
{
  private readonly LoggerFactory                    loggerFactory_;
  private          Mock<IClientFactory>?            clientFactory_;
  private          ChannelPool?                     mockChannelPool_;
  private          Mock<Submitter.SubmitterClient>? submitterClientGrpcMock_;

  public SimpleCreateSessionTests()
    => (_, loggerFactory_) = NunitConfigurationHelpers.GetConfigurationAndLoggerFactory();

  [SetUp]
  public void InitTest()
  {
    mockChannelPool_ = new ChannelPool(() => new ChannelMock("Channel-" + Guid.NewGuid()),
                                       loggerFactory_);

    submitterClientGrpcMock_ = new Mock<Submitter.SubmitterClient>();

    clientFactory_ = new Mock<IClientFactory>();

    clientFactory_.Setup(client => client.NewSubmitterClient(It.Is<ChannelBase>(channel => channel != null)))
                  .Returns(submitterClientGrpcMock_.Object)
                  .Verifiable();
  }

  [Test]
  public void SimpleCreateSession()
  {
    var expectedSessionId = Guid.NewGuid()
                                .ToString();


    submitterClientGrpcMock_!.Setup(b => b.CreateSession(It.Is<CreateSessionRequest>(r => r.DefaultTaskOption == null || r.PartitionIds == null),
                                                         null,
                                                         null,
                                                         CancellationToken.None))
                             .Throws(new ArgumentException("One or argument is null"));

    submitterClientGrpcMock_.Setup(b => b.CreateSession(It.Is<CreateSessionRequest>(r => r.DefaultTaskOption != null && r.PartitionIds != null),
                                                        null,
                                                        null,
                                                        CancellationToken.None))
                            .Returns(new CreateSessionReply
                                     {
                                       SessionId = expectedSessionId,
                                     })
                            .Verifiable();


    var baseSubmitter = new BaseClientSubmitter<FakeSessionService>(mockChannelPool_,
                                                                    clientFactory_!.Object,
                                                                    loggerFactory_)
                        {
                          TaskOptions = new TaskOptions
                                        {
                                          ApplicationName      = "TestApps",
                                          ApplicationNamespace = "TestNameSpace",
                                          ApplicationService   = "TestServiceName",
                                          ApplicationVersion   = "1.0.0-7337",
                                        },
                        };


    var session = baseSubmitter.CreateSession(new[]
                                              {
                                                "default",
                                              });


    Assert.That(expectedSessionId,
                Is.EqualTo(session.Id));

    submitterClientGrpcMock_.Reset();
  }

  [Test]
  public void CreateSessionThrowRpcException()
  {
    submitterClientGrpcMock_!.Setup(b => b.CreateSession(It.IsAny<CreateSessionRequest>(),
                                                         null,
                                                         null,
                                                         CancellationToken.None))
                             .Throws(new RpcException(new Status(StatusCode.Aborted,
                                                                 "Fail to connect Fake server")))
                             .Verifiable();

    var baseSubmitter = new BaseClientSubmitter<FakeSessionService>(mockChannelPool_,
                                                                    clientFactory_!.Object,
                                                                    loggerFactory_)
                        {
                          TaskOptions = new TaskOptions
                                        {
                                          ApplicationName      = "TestApps",
                                          ApplicationNamespace = "TestNameSpace",
                                          ApplicationService   = "TestServiceName",
                                          ApplicationVersion   = "1.0.0-7337",
                                        },
                        };


    Assert.Throws<RpcException>(() => baseSubmitter.CreateSession(new[]
                                                                  {
                                                                    "default",
                                                                  }));


    submitterClientGrpcMock_.Reset();
  }

  [Test]
  public void SimpleCreateSessionExceptionWithTaskOptions()
  {
    var expectedSessionId = Guid.NewGuid()
                                .ToString();

    submitterClientGrpcMock_!.Setup(b => b.CreateSession(It.Is<CreateSessionRequest>(r => r.DefaultTaskOption != null && r.PartitionIds != null),
                                                         null,
                                                         null,
                                                         CancellationToken.None))
                             .Returns(new CreateSessionReply
                                      {
                                        SessionId = expectedSessionId,
                                      });

    var baseSubmitter = new BaseClientSubmitter<FakeSessionService>(mockChannelPool_,
                                                                    clientFactory_!.Object,
                                                                    loggerFactory_)
                        {
                          TaskOptions = new TaskOptions
                                        {
                                          ApplicationName      = "TestApps",
                                          ApplicationNamespace = "TestNameSpace",
                                          ApplicationService   = "TestServiceName",
                                          ApplicationVersion   = "1.0.0-7337",
                                        },
                        };


    baseSubmitter.TaskOptions = null;


    Assert.Throws<NullReferenceException>(() => baseSubmitter.CreateSession(new[]
                                                                            {
                                                                              "default",
                                                                            }));

    submitterClientGrpcMock_.Verify();
  }

  [Test]
  public void CreateSessionExceptionWithPartitionIds()
  {
    submitterClientGrpcMock_!
      .Setup(b => b.CreateSession(It.Is<CreateSessionRequest>(r => r.DefaultTaskOption == null || r.PartitionIds == null || r.PartitionIds.Count == 0),
                                  null,
                                  null,
                                  CancellationToken.None))
      .Throws<NullReferenceException>();

    var baseSubmitter = new BaseClientSubmitter<FakeSessionService>(mockChannelPool_,
                                                                    clientFactory_!.Object,
                                                                    loggerFactory_)
                        {
                          TaskOptions = new TaskOptions
                                        {
                                          ApplicationName      = "TestApps",
                                          ApplicationNamespace = "TestNameSpace",
                                          ApplicationService   = "TestServiceName",
                                          ApplicationVersion   = "1.0.0-7337",
                                        },
                        };


    Assert.Throws<NullReferenceException>(() => baseSubmitter.CreateSession(new string[]
                                                                            {
                                                                            }));

    Assert.Throws<ArgumentNullException>(() => baseSubmitter.CreateSession(null));
  }
}
