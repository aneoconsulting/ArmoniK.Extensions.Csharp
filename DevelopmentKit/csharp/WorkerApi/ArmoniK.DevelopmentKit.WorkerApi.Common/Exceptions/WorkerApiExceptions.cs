using System;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions
{
    public class WorkerApiException : Exception
    {
      public WorkerApiException()
      {

      }

      public WorkerApiException(string message)
      {
        message_ = message;
      }

      private readonly string message_ = "WorkerApi Exception during call function";

      public WorkerApiException(Exception e) : base(e.Message, e)
      {
        message_ = $"{message_} \ninnerEception message : {e.Message}";
      }

      public WorkerApiException(string message, ArgumentException e) : base(message,
                                                                            e)
      {
        message_ = message;
      }

        //Overriding the Message property
        public override string Message
      {
        get { return message_; }
      }
    }
}
