using System.Runtime.Loader;
using System.Reflection;
using System;
using System.IO;

using ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public class AppsLoader
  {
    AssemblyLoadContext                  loadContext        = null;
    Assembly                             assembly           = null;
    Assembly                             assemblyGridWorker = null;
    private readonly ILogger<AppsLoader> logger_;
    string                               armonikDevelopmentkitSymphonyapi = "ArmoniK.DevelopmentKit.SymphonyApi";

    public IConfiguration Configuration { get; }

    public AppsLoader(IConfiguration configuration, string pathToAssemblies, string pathToZip)
    {
      logger_ = LoggerFactory.Create(builder =>
                                       builder.AddConfiguration(configuration)).CreateLogger<AppsLoader>();

      // Create a new context and mark it as 'collectible'.
      string tempLoadContextName = Guid.NewGuid().ToString();

      loadContext = new AssemblyLoadContext(tempLoadContextName,
                                            true);
      string localPathToAssembly;


      if (!ZipArchiver.ArchiveAlreadyExtracted(pathToZip))
        ZipArchiver.UnzipArchive(pathToZip);

      localPathToAssembly = ZipArchiver.GetLocalPathToAssembly(pathToZip);


      assembly = loadContext.LoadFromAssemblyPath(localPathToAssembly);

      if (assembly == null)
      {
        logger_.LogError($"Cannot load assembly from path [${localPathToAssembly}]");
        throw new WorkerApiException($"Cannot load assembly from path [${localPathToAssembly}]");
      }

      PathToAssembly = localPathToAssembly;


      string localPathToAssemblyGridWorker = $"{Path.GetDirectoryName(localPathToAssembly)}/{armonikDevelopmentkitSymphonyapi}.dll";


      assemblyGridWorker = loadContext.LoadFromAssemblyPath(localPathToAssemblyGridWorker);
      
      if (assemblyGridWorker == null)
      {
        logger_.LogError($"Cannot load assembly from path [${localPathToAssemblyGridWorker}]");
        throw new WorkerApiException($"Cannot load assembly from path [${localPathToAssemblyGridWorker}]");
      }


      PathToAssemblyGridWorker = localPathToAssembly;

      AppDomain currentDomain = AppDomain.CurrentDomain;
      currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

      Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
      {
        string folderPath = Path.GetDirectoryName(PathToAssembly);
        string assemblyPath = Path.Combine(folderPath,
                                           new AssemblyName(args.Name).Name + ".dll");
        if (!File.Exists(assemblyPath)) return null;
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

    public string PathToAssembly { get; set; }

    public string PathToAssemblyGridWorker { get; set; }

    public IGridWorker getGridWorkerInstance()
    {
      // Create an instance of a class from the assembly.
      try
      {
        Type classType = assemblyGridWorker.GetType($"{armonikDevelopmentkitSymphonyapi}.GridWorker");

        if (classType != null)
        {
          IGridWorker gridworker = (IGridWorker)Activator.CreateInstance(classType);

          return gridworker;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw new WorkerApiException(e);
      }

      throw new NullReferenceException(
        $"Cannot find ServiceContainer named : {armonikDevelopmentkitSymphonyapi}.GridWorker in dll [{PathToAssemblyGridWorker}]");
    }

    public T getServiceContainerInstance<T>(string appNamespace, string serviceContainerClassName)
    {
      // Create an instance of a class from the assembly.
      Type classType = assembly.GetType($"{appNamespace}.{serviceContainerClassName}");

      if (classType != null)
      {
        T serviceContainer = (T)Activator.CreateInstance(classType);

        return serviceContainer;
      }

      Dispose();
      throw new NullReferenceException(
        $"Cannot find ServiceContainer named : {appNamespace}.{serviceContainerClassName} in dll [{PathToAssembly}]");
    }

    public void Dispose()
    {
      assembly = null;
      if (loadContext != null)
        // Unload the context.
        loadContext.Unload();
    }

    ~AppsLoader()
    {
      Dispose();
    }
  }
}