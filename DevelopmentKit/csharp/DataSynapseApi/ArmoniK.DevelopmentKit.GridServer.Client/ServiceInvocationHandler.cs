//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
    public interface IServiceInvocationHandler
    {
      void HandleError(ServiceInvocationException e, string taskId);

      void HandleResponse(object response, string taskId);
    }
}
