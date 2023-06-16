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

using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

namespace ArmoniK.DevelopmentKit.Worker.Common;

public class WorkerHelpers
{
  public static IConfiguration GetDefaultConfiguration()
  {
    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();

    return builder.Build();
  }

  public static ILoggerFactory GetDefaultLoggerFactory(IConfiguration configuration = null)
  {
    configuration ??= GetDefaultConfiguration();

    var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(configuration)
                                                .WriteTo.Console(new CompactJsonFormatter())
                                                .Enrich.FromLogContext()
                                                .CreateLogger();
    return LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(loggerConfig));
  }
}
