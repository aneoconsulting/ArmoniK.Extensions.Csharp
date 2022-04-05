// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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

using ArmoniK.DevelopmentKit.WorkerApi.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;

namespace ArmoniK.DevelopmentKit.WorkerApi
{
  public class Startup
  {
    public Startup(IWebHostEnvironment env)
    {
      var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json",
                                 true,
                                 true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json",
                                 true)
                    .AddEnvironmentVariables();

      Configuration = builder.Build();

      Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(Configuration)
                                            .WriteTo.Console(new CompactJsonFormatter())
                                            .Enrich.FromLogContext()
                                            .CreateLogger();
      var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger));
      LoggerFactory = loggerFactory;
    }

    public ILoggerFactory LoggerFactory { get; set; }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      //services.AddConfiguration(Configuration);
      services.AddGrpc();
      services.AddLogging();
      services.AddSingleton<ServiceRequestContext>(new ServiceRequestContext(LoggerFactory));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
        app.UseDeveloperExceptionPage();

      app.UseSerilogRequestLogging();

      app.UseRouting();

      app.UseEndpoints(endpoints => endpoints.MapGrpcService<ComputerService>());
    }
  }
}