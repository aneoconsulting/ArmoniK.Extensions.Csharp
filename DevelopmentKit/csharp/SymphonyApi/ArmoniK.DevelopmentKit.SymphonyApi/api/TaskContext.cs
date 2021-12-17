/* TaskContext.cs is part of the Armonik SDK solution.

   Copyright (c) 2021-2021 ANEO.
     D. DUBUC (https://github.com/ddubuc)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

*/

using System.Collections;
using System.Collections.Generic;

using ArmoniK.Core.gRPC.V1;

namespace ArmoniK.DevelopmentKit.SymphonyApi
{
    /// <summary>
    /// Provides the context for the task that is bound to the given service invocation
    /// </summary>
    public class TaskContext
    {
        public string TaskId { get; set; }
        public byte[] Payload;

        public string SessionId { get; set; }

        public IEnumerable<string> ParentIds { get; set; }

        public TaskOptions TaskOptions { get; set; }


        /// <summary>
        /// The customer payload to deserialize by the customer
        /// </summary>
        /// <value></value>
        public byte[] TaskInput
        {
            get { return Payload; }

            set { Payload = value; }
        }
    }
}