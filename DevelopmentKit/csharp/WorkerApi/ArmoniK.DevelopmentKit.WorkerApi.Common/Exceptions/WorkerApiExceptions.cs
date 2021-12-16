using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.WorkerApi.Common.Exceptions
{
    public class WorkerApiException : Exception
    {
      public WorkerApiException()
      {

      }

      public WorkerApiException(string message)
      {
        _message = message;
      }

      private string _message = "WorkerApi Exception during call function";

      public WorkerApiException(Exception e) : base(e.Message, e)
      {
        _message = $"{_message} \ninnerEception message : {e.Message}";
      }

      public WorkerApiException(string message, ArgumentException e) : base(message,
                                                                            e)
      {
        _message = message;
      }

        //Overriding the Message property
        public override string Message
      {
        get { return _message; }
      }
    }
}
