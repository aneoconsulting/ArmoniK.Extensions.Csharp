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


using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ArmoniK.Api.Client.Options;
using ArmoniK.Api.Client.Submitter;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Results;
using ArmoniK.Utils;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class Program
{
  public static async Task Main(string[] cArgs)
  {
    // Parse arguments
    var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                  .AddJsonFile("appsettings.json",
                                                               true,
                                                               false)
                                                  .AddEnvironmentVariables()
                                                  .AddCommandLine(cArgs)
                                                  .Build();


    // Configure logger
    var loggerFactory = LoggerFactory.Create(logging =>
                                             {
                                               logging.AddConsole();
                                               logging.SetMinimumLevel(LogLevel.Trace);
                                               logging.AddConfiguration(configuration);
                                             });
    var logger = loggerFactory.CreateLogger("Program");

    // Bind args
    Args args = new();
    args.Grpc = configuration.GetSection(nameof(args.Grpc))
                             .Get<GrpcClient>()!;
    args.Grpc.AllowUnsafeConnection = true;
    args.Concurrency = int.Parse(configuration.GetSection(nameof(args.Concurrency))
                                              .Value ?? "1");
    args.Requests = int.Parse(configuration.GetSection(nameof(args.Requests))
                                           .Value ?? "100000");
    args.MaxErrors = int.Parse(configuration.GetSection(nameof(args.MaxErrors))
                                            .Value ?? "100");

    logger.LogInformation("Starting {n} requests with {concurrency} workers, handler: {httpMessageHandler}",
                          args.Requests,
                          args.Concurrency,
                          args.Grpc.HttpMessageHandler);

    var cts = new CancellationTokenSource();
    try
    {
      // Create Channel Pool
      var channelPool = new ObjectPool<GrpcChannel>(ct => new ValueTask<GrpcChannel>(GrpcChannelFactory.CreateChannel(args.Grpc,
                                                                                                                      loggerFactory.CreateLogger("GrpcChannel"))),


#if NET5_0_OR_GREATER
                                                    async (channel,
                                                           _) =>
                                                    {
                                                      switch (channel.State)
                                                      {
                                                        case ConnectivityState.TransientFailure:
                                                          await channel.ShutdownAsync()
                                                                       .ConfigureAwait(false);
                                                          return false;
                                                        case ConnectivityState.Shutdown:
                                                          return false;
                                                        case ConnectivityState.Idle:
                                                        case ConnectivityState.Connecting:
                                                        case ConnectivityState.Ready:
                                                        default:
                                                          return true;
                                                      }
                                                    }
#else
                                                  (_,
                                                   _) => new ValueTask<bool>(true)
#endif
                                                   );

      // Perform the request in a loop
      var nbErrors = 0;

      await Enumerable.Range(0,
                             args.Requests)
                      .ParallelForEach(new ParallelTaskOptions(args.Concurrency,
                                                               cts.Token),
                                       async i =>
                                       {
                                         try
                                         {
                                           await using var channel = await channelPool.GetAsync(cts.Token)
                                                                                      .ConfigureAwait(false);
                                           var client = new Results.ResultsClient(channel);

                                           await client.GetServiceConfigurationAsync(new Empty(),
                                                                                     cancellationToken: cts.Token)
                                                       .ConfigureAwait(false);
                                         }
                                         catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
                                         {
                                         }
                                         catch (Exception e)
                                         {
                                           var n = Interlocked.Increment(ref nbErrors);
                                           logger.LogError(e,
                                                           "Request #{i} failed ({error}/{maxError})",
                                                           i,
                                                           n,
                                                           args.MaxErrors);

                                           if (n >= args.MaxErrors)
                                           {
                                             cts.Cancel();
                                           }
                                         }
                                       })
                      .ConfigureAwait(false);

      logger.LogInformation("Finished {n} requests with {concurrency} workers",
                            args.Requests,
                            args.Concurrency);
    }
    catch (OperationCanceledException e) when (e.CancellationToken == cts.Token)
    {
    }
    finally
    {
      Console.Out.Flush();
      Console.Error.Flush();
    }
  }

  public class Args
  {
    public int        Concurrency = 1;
    public GrpcClient Grpc        = new();
    public int        MaxErrors   = 100;
    public int        Requests    = 100000;
  }
}
