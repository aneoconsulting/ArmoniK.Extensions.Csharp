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

namespace ArmoniK.DevelopmentKit.Worker.DLLWorker.Services;
/// <summary>
/// 
/// </summary>
public class HealthCheckService : Health.HealthBase, IAsyncDisposable
{

    private readonly ILogger<HealthCheckService> _logger;
    private readonly ComputePlane _computePlaneOptions;
    private readonly GrpcChannelProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly WorkerStreamWrapper _workerWrapper;
    private readonly CancellationTokenSource cancellationTokenSource_;

    private bool _isDisposed;
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory, to create loggers.</param>
    /// <param name="computePlaneOptions">The compute plane options.</param>
    /// <param name="provider">The gRPC channel provider, to create channels with the Agent.</param>
    /// <param name="configuration">The configuration.</param>
    public HealthCheckService(ILoggerFactory loggerFactory, ComputePlane computePlaneOptions, GrpcChannelProvider provider, IConfiguration configuration) 
    {
        _logger = loggerFactory.CreateLogger<HealthCheckService>();
        _computePlaneOptions = computePlaneOptions ?? throw new ArgumentNullException(nameof(computePlaneOptions));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));       
        cancellationTokenSource_ = new CancellationTokenSource();

        try
        {
            _workerWrapper = new WorkerStreamWrapper(loggerFactory, computePlaneOptions, provider);
            _logger.LogInformation("HealthCheckService initialized with ComputePlane options: {ComputePlaneOptions}", computePlaneOptions);
            _logger.LogInformation("HealthCheckService initialized with GrpcChannelProvider: {GrpcChannelProvider}", provider);
            _logger.LogInformation("HealthCheckService initialized with configuration: {Configuration}", configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing HealthCheckService");
            throw;
        }
    }

    /// <inheritdoc/>
    public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
    {
        _logger.LogDebug("HealthCheckService.Check called with request: {Request}", request);

        if (_isDisposed)
        {
            _logger.LogWarning("HealthCheckService is disposed, returning NOT_SERVING status.");
            return Task.FromResult(new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.NotServing
            });
        }
        
        // Unable to perform health check if the worker wrapper is null
        if (_workerWrapper == null)
        {
            _logger.LogWarning("WorkerWrapper is null, returning NOT_SERVING status.");
            return Task.FromResult(new HealthCheckResponse
            {
                Status = HealthCheckResponse.Types.ServingStatus.NotServing
            });
        }

        // Available to health check
        _logger.LogDebug("HealthCheckService is healthy, returning SERVING status.");
        return Task.FromResult(new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.Serving
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            _logger.LogWarning("HealthCheckService is already disposed.");
            return;
        }
        _isDisposed = true;
        try
        {
            cancellationTokenSource_.Dispose();
            if(_workerWrapper != null)
            {
                await _workerWrapper.DisposeAsync();
                _logger.LogInformation("WorkerStreamWrapper disposed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing HealthCheckService");
        }
  }
}