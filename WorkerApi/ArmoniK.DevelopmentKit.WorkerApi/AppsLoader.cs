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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.WorkerApi
{
  public class AppsLoader : IAppsLoader
  {
    private Assembly assembly_;

    private          AssemblyLoadContext loadContext_;
    private readonly Assembly            assemblyGridWorker_;
    private          ILogger<AppsLoader> logger_;
    private readonly EngineType          engineType_;

    private string ArmoniKDevelopmentKitServerApi { get; set; }

    public AssemblyLoadContext UserAssemblyLoadContext => loadContext_;

    public AppsLoader(IConfiguration configuration,
                      ILoggerFactory loggerFactory,
                      string         engineTypeAssemblyName,
                      IFileAdaptater fileAdaptater,
                      string         fileName)
    {
      engineType_ = EngineTypeHelper.ToEnum(engineTypeAssemblyName);

      FileAdaptater = fileAdaptater;

      ArmoniKDevelopmentKitServerApi = new EngineTypes()[engineType_];

      logger_ = loggerFactory.CreateLogger<AppsLoader>();

      if (!ZipArchiver.ArchiveAlreadyExtracted(fileAdaptater,
                                               fileName))
        ZipArchiver.UnzipArchive(fileAdaptater,
                                 fileName);


      var localPathToAssembly = ZipArchiver.GetLocalPathToAssembly(Path.Combine(fileAdaptater.DestinationDirPath,
                                                                                fileName));

      loadContext_ = new AddonsAssemblyLoadContext(localPathToAssembly);

      assembly_ = loadContext_.LoadFromAssemblyPath(localPathToAssembly);

      if (assembly_ == null)
      {
        logger_.LogError($"Cannot load assembly from path [${localPathToAssembly}]");
        throw new WorkerApiException($"Cannot load assembly from path [${localPathToAssembly}]");
      }

      PathToAssembly = localPathToAssembly;

      var localPathToAssemblyGridWorker = $"{Path.GetDirectoryName(localPathToAssembly)}/{ArmoniKDevelopmentKitServerApi}.dll";

      assemblyGridWorker_ = loadContext_.LoadFromAssemblyPath(localPathToAssemblyGridWorker);

      if (assemblyGridWorker_ == null)
      {
        logger_.LogError($"Cannot load assembly from path [${localPathToAssemblyGridWorker}]");
        throw new WorkerApiException($"Cannot load assembly from path [${localPathToAssemblyGridWorker}]");
      }

      logger_.LogInformation($"GridWorker assembly from path [${localPathToAssemblyGridWorker}]");

      PathToAssemblyGridWorker = localPathToAssemblyGridWorker;

      var currentDomain = AppDomain.CurrentDomain;
      currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

      Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
      {
        var folderPath = Path.GetDirectoryName(PathToAssembly);
        var assemblyPath = Path.Combine(folderPath ?? "",
                                        new AssemblyName(args.Name).Name + ".dll");

        Assembly assembly;

        try
        {
          assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception)
        {
          folderPath = "/app";
          assemblyPath = Path.Combine(folderPath,
                                      new AssemblyName(args.Name).Name + ".dll");

          if (!File.Exists(assemblyPath)) return null;

          assembly = Assembly.LoadFrom(assemblyPath);
        }

        return assembly;
      }
    }

    public IConfiguration Configuration { get; }

    public IFileAdaptater FileAdaptater { get; set; }

    public string PathToAssembly { get; set; }

    public string PathToAssemblyGridWorker { get; set; }

    public IGridWorker GetGridWorkerInstance(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
      // Create an instance of a class from the assembly.
      try
      {
        using (UserAssemblyLoadContext.EnterContextualReflection())
        {
          var classType = assemblyGridWorker_.GetType($"{ArmoniKDevelopmentKitServerApi}.GridWorker");

          if (classType != null)
          {
            var args = new object[] { (IConfiguration)configuration, (ILoggerFactory)loggerFactory };


            var gridWorker = (IGridWorker)Activator.CreateInstance(classType,
                                                                   args);

            return gridWorker;
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw new WorkerApiException(e);
      }

      throw new NullReferenceException($"Cannot find ServiceContainer named : {ArmoniKDevelopmentKitServerApi}.GridWorker in dll [{PathToAssemblyGridWorker}]");
    }

    public T GetServiceContainerInstance<T>(string appNamespace, string serviceContainerClassName)
    {
      using (UserAssemblyLoadContext.EnterContextualReflection())
      {

        // Create an instance of a class from the assembly.
        var classType = assembly_.GetType($"{appNamespace}.{serviceContainerClassName}");

        if (classType != null)
        {
          var serviceContainer = (T)Activator.CreateInstance(classType);

          return serviceContainer;
        }
      }

      Dispose();
      throw new NullReferenceException($"Cannot find ServiceContainer named : {appNamespace}.{serviceContainerClassName} in dll [{PathToAssembly}]");
    }

    public void Dispose()
    {
      assembly_ = null;
      if (loadContext_ != null)
        // Unload the context.
        loadContext_.Unload();
      loadContext_ = null;
    }

    ~AppsLoader()
    {
      Dispose();
    }

    public bool RequestNewAssembly(string engineType, string pathToZipFile)
    {
      if (pathToZipFile == null)
        throw new ArgumentNullException(nameof(pathToZipFile),
                                        "pathToZipFile is a null argument");

      return engineType == null ||
             engineType_ != EngineTypeHelper.ToEnum(engineType) ||
             FileAdaptater == null ||
             !pathToZipFile.Equals(FileAdaptater);
    }
  }
}