namespace ArmoniK.DevelopmentKit.GridServer.Client
{
    public interface IServiceInvocationHandler
    {
      void HandleError(ServiceInvocationException e, string taskId);

      void HandleResponse(object response, string taskId);
    }
}
