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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using ArmoniK.EndToEndTests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.EndToEndTests.Client;

public class Program
{
  private static IConfiguration   Configuration { get; set; }
  private static ILogger<Program> Logger        { get; set; }
  private static ILoggerFactory   LoggerFactory { get; set; }

  private static void Main(string[] args)
  {
    Console.WriteLine("Hello Armonik End to End Tests !");


    var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile("appsettings.json",
                                                         true,
                                                         false)
                                            .AddEnvironmentVariables();


    Configuration = builder.Build();

    LoggerFactory = new LoggerFactory(new[]
                                      {
                                        new SerilogLoggerProvider(new LoggerConfiguration().ReadFrom.Configuration(Configuration)
                                                                                           .CreateLogger()),
                                      },
                                      new LoggerFilterOptions().AddFilter("Grpc",
                                                                          LogLevel.Trace));


    Logger = LoggerFactory.CreateLogger<Program>();

    Logger.LogInformation($"EntryPoint : {Configuration.GetSection("Grpc")["EndPoint"]}");
    Logger.LogInformation($"CaCert     : {Configuration.GetSection("Grpc")["CaCert"]}");
    Logger.LogInformation($"ClientCert : {Configuration.GetSection("Grpc")["ClientCert"]}");
    Logger.LogInformation($"ClientKey  : {Configuration.GetSection("Grpc")["ClientKey"]}");

    var clientsContainers = args is
                            {
                              Length: > 0,
                            }
                              ? RetrieveSpecificTests(args)
                              : RetrieveClientTests();

    foreach (var clientContainer in clientsContainers)
    {
      if (clientContainer.MethodTests == null)
      {
        continue;
      }

      foreach (var methodTest in clientContainer.MethodTests)
      {
        Logger.LogInformation($"\n\n-------- [TEST] : {clientContainer.ClassClient} : {methodTest.Name}");
        methodTest.Invoke(clientContainer.ClientClassInstance,
                          null);
      }
    }
  }

  private static IEnumerable<TestContext> RetrieveClientTests()
  {
    var serviceContainerTypes = new[]
                                {
                                  Assembly.GetExecutingAssembly(),
                                }.SelectMany(x =>
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
                                 .Where(x => x.IsPublic && !x.IsGenericType && !typeof(Delegate).IsAssignableFrom(x) && !x.GetCustomAttributes<ObsoleteAttribute>()
                                                                                                                          .Any() && !x
                                                                                                                                     .GetCustomAttributes<
                                                                                                                                       DisabledAttribute>()
                                                                                                                                     .Any())
                                 .ToArray();

    var results = serviceContainerTypes.Select(x => new Tuple<Type, MethodInfo[]>(x,
                                                                                  GetMethods(x)))
                                       .Where(x => x.Item2 != null && x.Item2.Length > 0 && x.Item2.Any(m => m.GetCustomAttributes<EntryPointAttribute>()
                                                                                                              .Any()))
                                       .Select(x => new TestContext
                                                    {
                                                      ClassClient = x.Item1,
                                                      ClientClassInstance = Activator.CreateInstance(x.Item1,
                                                                                                     Configuration,
                                                                                                     LoggerFactory),
                                                      NameSpaceTest = x.Item1.Namespace ?? string.Empty,
                                                      MethodTests   = x.Item2,
                                                    });

    var retrieveClientTests = results.ToList();

    Logger.LogInformation($"List of tests : \n\t{string.Join("\n\t", retrieveClientTests.Select(x => $"{x.NameSpaceTest}.{x.ClassClient}"))}");

    return retrieveClientTests;
  }

  private static IEnumerable<TestContext> RetrieveSpecificTests(string[] listTests)
  {
    //Get first test where ServiceContainer Exists

    var serviceContainerTypes = new[]
                                {
                                  Assembly.GetExecutingAssembly(),
                                }.SelectMany(x =>
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
                                 .Where(x => x.IsPublic && !x.IsGenericType && !typeof(Delegate).IsAssignableFrom(x) && !x.GetCustomAttributes<ObsoleteAttribute>()
                                                                                                                          .Any())
                                 .ToArray();

    var results = serviceContainerTypes.Select(x => new Tuple<Type, MethodInfo[]>(x,
                                                                                  GetMethods(x)))
                                       .Where(x => x.Item2 is
                                                   {
                                                     Length: > 0,
                                                   } && x.Item2.Any(m => m.GetCustomAttributes<EntryPointAttribute>()
                                                                          .Any()))
                                       .Select(x => new TestContext
                                                    {
                                                      ClassClient = x.Item1,
                                                      ClientClassInstance = Activator.CreateInstance(x.Item1,
                                                                                                     Configuration,
                                                                                                     LoggerFactory),
                                                      NameSpaceTest = x.Item1.Namespace ?? string.Empty,
                                                      MethodTests   = x.Item2,
                                                    })
                                       .Where(x =>
                                              {
                                                Logger.LogInformation("test detected {test}",
                                                                      x.ClassClient?.Name);
                                                return listTests.Any(t => x.ClassClient != null && string.Equals(x.ClassClient.Name,
                                                                                                                 t,
                                                                                                                 StringComparison.CurrentCultureIgnoreCase));
                                              });

    var retrieveClientTests = results.ToList();

    Logger.LogInformation($"List of specifics tests : \n\t{string.Join("\n\t", retrieveClientTests.Select(x => $"{x.NameSpaceTest}.{x.ClassClient}"))}");

    return retrieveClientTests;
  }


  /// <summary>
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static MethodInfo[] GetMethods(Type type)
    => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
           .Where(x => !x.IsSpecialName                                                          && !x.GetCustomAttributes<ObsoleteAttribute>()
                                                                    .Any()                       && x.GetCustomAttributes<EntryPointAttribute>()
                                                                                          .Any() && !x.IsPrivate)
           .ToArray();
}
