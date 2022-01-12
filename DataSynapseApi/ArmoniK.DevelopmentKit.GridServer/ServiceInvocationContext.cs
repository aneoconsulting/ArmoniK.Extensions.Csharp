using ArmoniK.Core.gRPC.V1;

//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer
{
    /// <summary>
    /// The ServiceInvocationContext class provides an interface for interacting with
    /// an invocation, such as getting the session and task IDs, while it is running on an
    /// Engine.This is an alternative to using, for example, the system properties when
    /// running a Java Service.Using this class enables immediate updating of invocation
    /// information.In contrast, setting the INVOCATION_INFO system property only
    /// updates at the end of the invocation.
    /// The ServiceInvocationContext object can be reused; the method calls always
    /// apply to the currently executing Service Session and invocation.Make all method
    /// calls by a service, update, or init method; if not, the method call might throw
    /// an IllegalStateException or return invalid data.Note that you cannot call this
    /// method from a different thread; it will fail if it is not called from the main thread.
    /// </summary>
    public class ServiceInvocationContext
    {
      public SessionId SessionId { get; set; }

      public bool IsEquals(string session)
        => SessionId != null && session != null && SessionId.Session.Equals(session);
    }
}
