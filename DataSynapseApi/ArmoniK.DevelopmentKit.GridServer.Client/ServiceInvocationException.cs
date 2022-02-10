using System;

using ArmoniK.DevelopmentKit.Common;

//TODO : remove pragma
#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  [MarkDownDoc]
  public class ServiceInvocationException : Exception
  {
    private readonly string message_ = "ServiceInvocationException during call function";

    public ServiceInvocationException()
    {
    }

    public ServiceInvocationException(string message) => message_ = message;

    public ServiceInvocationException(Exception e) : base(e.Message,
                                                          e) => message_ = $"{message_} with InnerException {e.GetType()} message : {e.Message}";

    public ServiceInvocationException(string message, ArgumentException e) : base(message,
                                                                                  e)
      => message_ = message;

    //Overriding the Message property
    public override string Message => message_;
  }
}