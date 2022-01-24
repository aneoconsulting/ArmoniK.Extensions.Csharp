// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.SymphonyApi.Client;
using ArmoniK.DevelopmentKit.WorkerApi.Common;
using ArmoniK.EndToEndTests.Common;

using Google.Protobuf.WellKnownTypes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

using Type = System.Type;

namespace ArmoniK.EndToEndTests
{
  public class Program
  {
    private static IConfiguration   Configuration { get; set; }
    private static ILogger<Program> Logger { get; set; }

    private static void Main(string[] args)
    {
      Console.WriteLine("Hello Armonik End to End Tests !");


      var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                              .AddJsonFile("appsettings.json",
                                                           true,
                                                           true)
                                              .AddEnvironmentVariables();

      Configuration = builder.Build();

      var factory = new LoggerFactory(new[]
      {
        new SerilogLoggerProvider(new LoggerConfiguration()
                                  .ReadFrom
                                  .Configuration(Configuration)
                                  .CreateLogger())
      });

      Logger = factory.CreateLogger<Program>();

      var client = new ArmonikSymphonyClient(Configuration);

      Logger.LogInformation("Configure taskOptions");
      var taskOptions = InitializeTaskOptions();

      //var sessionId = client.CreateSession(taskOptions);

      //Logger.LogInformation($"New session created : {sessionId}");

      IEnumerable<TestContext> clientsContainers = RetrieveClientTests();

      foreach (var clientContainer in clientsContainers)
      {
        foreach (var methodTest in clientContainer.MethodTests)
        {
          methodTest.Invoke(clientContainer.ClientClassInstance,null);
        }
      }
    }

    private static IEnumerable<TestContext> RetrieveClientTests()
    {
      //Get first test where ServiceContainer Exists

      var serviceContainerTypes = new[] { Assembly.GetExecutingAssembly() }
                                  .SelectMany(x =>
                                  {
                                    try
                                    {
                                      return x.GetTypes();
                                    }
                                    catch (ReflectionTypeLoadException ex)
                                    {
                                      return ex.Types.Where(t => t != null);
                                    }
                                    catch
                                    {
                                      return Type.EmptyTypes;
                                    }
                                  })
                                  .Where(x => x != null)
                                  .Where(x => x.IsPublic &&
                                              !typeof(Delegate).IsAssignableFrom(x) &&
                                              !x.GetCustomAttributes<ObsoleteAttribute>().Any() &&
                                              !x.GetCustomAttributes<DisabledAttribute>().Any())
                                  .ToArray();

      Logger.LogInformation($"List of tests : \n\t{string.Join("\n\t", serviceContainerTypes.Select(x => $"{x.Namespace}.{x.Name}"))}");

      return serviceContainerTypes.Select(x => new Tuple<Type, MethodInfo[]>(x,
                                                                             GetMethods(x)))
                                  .Where(x => x.Item2 != null).Select(x => new TestContext()
                                  {
                                    ClassCLient         = x.Item1,
                                    ClientClassInstance = Activator.CreateInstance(x.Item1),
                                    NameSpaceTest       = x.Item1.Namespace ?? string.Empty,
                                    MethodTests = x.Item2,
                                  });
    }


    public static MethodInfo[] GetMethods(Type type)
    {
      return type.GetMethods(BindingFlags.Public |
                             BindingFlags.Instance |
                             BindingFlags.NonPublic |
                             BindingFlags.InvokeMethod)
                 .Where(x => !x.IsSpecialName && 
                                      !x.GetCustomAttributes<ObsoleteAttribute>().Any() &&
                                      x.GetCustomAttributes<EntryPointAttribute>().Any() &&
                                      !x.IsPrivate)
                 .ToArray();
    }

    /// <summary>
    ///   Initialize Setting for task i.e :
    ///   Duration :
    ///   The max duration of a task
    ///   Priority :
    ///   Work in Progress. Setting priority of task
    ///   AppName  :
    ///   The name of the Application dll (Without Extension)
    ///   VersionName :
    ///   The version of the package to unzip and execute
    ///   Namespace :
    ///   The namespace where the service can find
    ///   the ServiceContainer object develop by the customer
    /// </summary>
    /// <returns></returns>
    private static TaskOptions InitializeTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new Duration
        {
          Seconds = 300,
        },
        MaxRetries = 5,
        Priority   = 1,
        IdTag      = "ArmonikTag",
      };
      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.Samples.EndToEndTests");

      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.0.0");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.Samples.EndToEndTests");

      return taskOptions;
    }

    /// <summary>
    ///   Simple function to wait and get the result from subTasking and result delegation
    ///   to a subTask
    /// </summary>
    /// <param name="client">The client API to connect to the Control plane Service</param>
    /// <param name="taskId">The task which is waiting for</param>
    /// <returns></returns>
    private static byte[] WaitForSubTaskResult(ArmonikSymphonyClient client, string taskId)
    {
      client.WaitSubtasksCompletion(taskId);
      var taskResult = client.GetResult(taskId);
      var result     = ClientPayload.Deserialize(taskResult);

      if (!string.IsNullOrEmpty(result.SubTaskId))
      {
        client.WaitSubtasksCompletion(result.SubTaskId);
        taskResult = client.GetResult(result.SubTaskId);
      }

      return taskResult;
    }
  }
}