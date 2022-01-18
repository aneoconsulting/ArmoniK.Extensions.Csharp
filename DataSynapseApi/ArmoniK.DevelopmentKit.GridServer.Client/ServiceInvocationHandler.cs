//TODO : remove pragma

using ArmoniK.DevelopmentKit.WorkerApi.Common;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  /// The interface from which the handler must inherit to be considered as a handler of service
  /// in the method LocalExecute, Execute or Submit
  /// </summary>
  [MarkDownDoc]
  public interface IServiceInvocationHandler
  {
    /// <summary>
    /// The callBack method which has to be implemented to retrieve error or exception
    /// </summary>
    /// <param name="e">The exception sent to the client from the control plane</param>
    /// <param name="taskId">The task identifier which has invoke the error callBack</param>
    void HandleError(ServiceInvocationException e, string taskId);

    /// <summary>
    /// The callBack method which has to be implemented to retrieve response from the server
    /// </summary>
    /// <param name="response">The object receive from the server as result the method called by the client</param>
    /// <param name="taskId">The task identifier which has invoke the response callBack</param>
    void HandleResponse(object response, string taskId);
  }
}