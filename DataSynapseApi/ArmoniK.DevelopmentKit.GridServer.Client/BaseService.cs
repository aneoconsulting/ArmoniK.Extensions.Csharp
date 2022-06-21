using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Formatting.Compact;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// The base of service to manage Logger, logger factory, and warmup of service and session
  /// </summary>
  public class BaseService<T>
  {
    public BaseService()
    {

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerFactory"></param>
    public BaseService(ILoggerFactory loggerFactory)
    {
      LoggerFactory = loggerFactory;

      Logger = loggerFactory.CreateLogger<T>();

    }

    public static ILogger<T> Logger { get; set; }

    /// <summary>
    ///   Get or Set SubSessionId object stored during the call of SubmitTask, SubmitSubTask,
    ///   SubmitSubTaskWithDependencies or WaitForCompletion, WaitForSubTaskCompletion or GetResults
    /// </summary>
    public Session SessionId { get; set; }

    /// <summary>
    /// Property to retrieve the sessionService previously created
    /// </summary>
    //internal SessionPollingService SessionService { get; set; }

    //internal ITaskHandler TaskHandler { get; set; }

    internal IDictionary<string, string> ClientOptions { get; set; } = new Dictionary<string, string>();

    /// <summary>
    ///   Get or set the taskId (ONLY INTERNAL USED)
    /// </summary>
    public TaskId TaskId { get; set; }

    /// <summary>
    ///   Get or Set Configuration
    /// </summary>
    public IConfiguration Configuration { get; set; }

    /// <summary>
    /// The logger factory to create new Logger in sub class caller
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    ///   The middleware triggers the invocation of this handler just after a Service Instance is started.
    ///   The application developer must put any service initialization into this handler.
    ///   Default implementation does nothing.
    /// </summary>
    /// <param name="serviceContext">
    ///   Holds all information on the state of the service at the start of the execution.
    /// </param>
    public virtual void OnCreateService(ServiceContext serviceContext)
    {
    }

    public class ServiceContext
    {
    }

    public class SessionContext
    {
    }

    /// <summary>
    ///   This handler is executed once after the callback OnCreateService and before the OnInvoke
    /// </summary>
    /// <param name="sessionContext">
    ///   Holds all information on the state of the session at the start of the execution.
    /// </param>
    public void OnSessionEnter(SessionContext sessionContext)
    {
    }



    /// <summary>
    ///   The middleware triggers the invocation of this handler to unbind the Service Instance from its owning Session.
    ///   This handler should do any cleanup for any resources that were used in the onSessionEnter() method.
    /// </summary>
    /// <param name="sessionContext">
    ///   Holds all information on the state of the session at the start of the execution such as session ID.
    /// </param>
    public void OnSessionLeave(SessionContext sessionContext)
    {
    }


    /// <summary>
    ///   The middleware triggers the invocation of this handler just before a Service Instance is destroyed.
    ///   This handler should do any cleanup for any resources that were used in the onCreateService() method.
    /// </summary>
    /// <param name="serviceContext">
    ///   Holds all information on the state of the service at the start of the execution.
    /// </param>
    public void OnDestroyService(ServiceContext serviceContext) {}
  }

  public class GridWorkerExt
  {
    public static IConfiguration GetDefaultConfiguration()
    {
      var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json",
                                 true,
                                 false)
                    .AddEnvironmentVariables();

      return builder.Build();
    }
    
  }
}
