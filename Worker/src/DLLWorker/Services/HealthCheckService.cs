// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2025. All rights reserved.
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

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Options;

using ArmoniK.Api.Worker.Worker;
using ArmoniK.DevelopmentKit.Worker.DLLWorker;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Worker.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Grpc.Core;
using Grpc.Health.V1;

using ProtoBuf.Meta;
using System.Threading.Tasks;
using System;
using System.Threading;
using Armonik.DevelopmentKit.Worker.DLLWorker.Services;

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker.Services;
/// <summary>
/// HealthCheck Class to associate a heathState to our worker
/// </summary>
public class HealthCheckService : Health.HealthBase, IAsyncDisposable, IHealthStatusController
{

    private readonly ILogger<HealthCheckService> logger_;
    private readonly ComputePlane computePlaneOptions_;
    private readonly GrpcChannelProvider provider_;
    private readonly IConfiguration configuration_;
    private readonly WorkerStreamWrapper workerWrapper_;
    private readonly CancellationTokenSource cancellationTokenSource_;

    private bool isDisposed_;
    private volatile bool isHealthy_ = true;
    private string statusReason_ = "Service is healthy.";
    private int failureCount_ = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory, to create loggers.</param>
    /// <param name="computePlaneOptions">The compute plane options.</param>
    /// <param name="provider">The gRPC channel provider, to create channels with the Agent.</param>
    /// <param name="configuration">The configuration.</param>
    public HealthCheckService(ILoggerFactory loggerFactory, ComputePlane computePlaneOptions, GrpcChannelProvider provider, IConfiguration configuration)
    {
        logger_ = loggerFactory.CreateLogger<HealthCheckService>();
        computePlaneOptions_ = computePlaneOptions ?? throw new ArgumentNullException(nameof(computePlaneOptions));
        provider_ = provider ?? throw new ArgumentNullException(nameof(provider));
        configuration_ = configuration ?? throw new ArgumentNullException(nameof(configuration));
        cancellationTokenSource_ = new CancellationTokenSource();

        try
        {
            workerWrapper_ = new WorkerStreamWrapper(loggerFactory, computePlaneOptions, provider);
            logger_.LogInformation("HealthCheckService initialized with ComputePlane options: {ComputePlaneOptions}", computePlaneOptions);
            logger_.LogInformation("HealthCheckService initialized with GrpcChannelProvider: {GrpcChannelProvider}", provider);
            logger_.LogInformation("HealthCheckService initialized with configuration: {Configuration}", configuration);
        }
        catch (Exception ex)
        {
            logger_.LogError(ex, "Error initializing HealthCheckService");
            throw;
        }
    }

    /// <inheritdoc/>
    public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
    {
        logger_.LogDebug("HealthCheckService.Check called with request: {Request}", request);

        if (isDisposed_)
        {
            logger_.LogWarning("HealthCheckService is disposed, returning NOT_SERVING status.");
            return Task.FromResult(new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.NotServing
            });
        }

        // Unable to perform health check if the worker wrapper is null
        if (workerWrapper_ == null)
        {
            logger_.LogWarning("WorkerWrapper is null, returning NOT_SERVING status.");
            return Task.FromResult(new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.NotServing
            });
        }

        // Check if the service is healthy
        if (!isHealthy_)
        {
            logger_.LogWarning("HealthCheckService marked as unhealthy, returning NOT_SERVING status.");
            return Task.FromResult(new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.NotServing
            });
        }

        // Available to health check
        logger_.LogDebug("HealthCheckService is healthy, returning SERVING status.");
        return Task.FromResult(new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.Serving
        });
    }

    /// <summary>
    /// Marks the service as healthy.
    /// </summary>
    public void MarkHealthy()
    {
        failureCount_ = 0; // Réinitialiser le compteur d'échecs
        isHealthy_ = true;
        statusReason_ = "Service is marked as healthy.";
        logger_.LogInformation("HealthCheckService marked as healthy.");
    }
    /// <summary>
    /// Marks the service as unhealthy
    /// </summary>
    public void MarkUnhealthy()
    {
        failureCount_++;
        if (failureCount_ >= 10)
        {
            isHealthy_ = false;
            statusReason_ = "Service is marked as unhealthy due to consecutive failures.";
            logger_.LogCritical("HealthCheckService marked as unhealthy.");
        }
        else if (failureCount_ >= 11)
        {
            // Si le worker réussit après avoir été marqué comme unhealthy, le rétablir
            MarkHealthy();
            failureCount_ = 0;
        }
    }

    /// <summary>
    /// Disposes the HealthCheckService asynchronously.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (isDisposed_)
        {
            logger_.LogWarning("HealthCheckService is already disposed.");
            return;
        }
        isDisposed_ = true;
        try
        {
            cancellationTokenSource_.Dispose();
            if (workerWrapper_ != null)
            {
                await workerWrapper_.DisposeAsync();
                logger_.LogInformation("WorkerStreamWrapper disposed successfully.");
            }
        }
        catch (Exception ex)
        {
            logger_.LogError(ex, "Error disposing HealthCheckService");
        }
    }

    /// <summary>
    /// Gets the current health status
    /// </summary>
     bool IHealthStatusController.IsHealthy()
    {
        return isHealthy_;
    }   
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetStatusInfo(){
        return $"Healthy: {isHealthy_}, Disposed: {isDisposed_}, Reason: {statusReason_}, Failure Count: {failureCount_}";
    }

}