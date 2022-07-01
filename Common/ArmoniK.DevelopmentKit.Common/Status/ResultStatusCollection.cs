using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;

using Google.Protobuf.Collections;

namespace ArmoniK.DevelopmentKit.Common.Status
{
  public class ResultStatusCollection
  {
    /// <summary>
    /// List of completed task where the result is ready to be retrieved
    /// </summary>
    public IEnumerable<Tuple<string, ResultStatus>> IdsReady { get; set; } = default;
    
    /// <summary>
    /// List of task or task result in error
    /// </summary>
    public IEnumerable<Tuple<string, ResultStatus>> IdsResultError { get; set; } = default;
    
    /// <summary>
    /// List of Unknown TaskIds. There is a heavy error somewhere else in the execution when this list has element
    /// </summary>
    public IEnumerable<string> IdsError { get; set; } = default;
    
    /// <summary>
    /// List of result not yet written in database
    /// </summary>
    public IEnumerable<Tuple<string, ResultStatus>> IdsNotReady { get; set; }

    /// <summary>
    /// The list of canceled task
    /// </summary>
    public IEnumerable<Tuple<string, ResultStatus>> Canceled { get; set; }
  }
}
