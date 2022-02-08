// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2021. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace ArmoniK.DevelopmentKit.Common.Exceptions
{
  /// <summary>
  /// General Worker API Exception
  /// </summary>
  public class WorkerApiException : Exception
  {
    private readonly string message_ = "WorkerApi Exception during call function";

    /// <summary>
    /// The ctor of WorkerApiException
    /// </summary>
    public WorkerApiException()
    {
    }

    /// <summary>
    /// Th ctor to instantiate new thrown Exception with message
    /// </summary>
    /// <param name="message">The message that will be print in the exception</param>
    public WorkerApiException(string message) => message_ = message;

    /// <summary>
    /// The ctor to instantiate new thrown Exception with previous exception
    /// </summary>
    /// <param name="e">The previous exception</param>
    public WorkerApiException(Exception e) : base(e.Message,
                                                  e) => message_ = $"{message_} with InnerException {e.GetType()} message : {e.Message}";

    /// <summary>
    /// The ctor with new message and the previous thrown exception
    /// </summary>
    /// <param name="message">The new message that will override the one from the previous exception</param>
    /// <param name="e">The previous exception</param>
    public WorkerApiException(string message, ArgumentException e) : base(message,
                                                                          e)
      => message_ = message;

    /// <summary>
    /// Overriding the Message property
    /// </summary>
    public override string Message => message_;
  }
}