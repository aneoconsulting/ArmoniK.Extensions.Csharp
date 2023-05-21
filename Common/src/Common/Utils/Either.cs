// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-$CURRENT_YEAR$. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace ArmoniK.DevelopmentKit.Common.Utils;

/// <summary>
///   Represents a simple Either type to manage an object of type L or an exception of type R.
/// </summary>
/// <typeparam name="L">The object in the Either.</typeparam>
/// <typeparam name="R">The exception in the Either.</typeparam>
public class Either<L, R>
{
  /// <summary>
  ///   The exception in the Either.
  /// </summary>
  private readonly R exception_;

  /// <summary>
  ///   The object in the Either.
  /// </summary>
  private readonly L obj_;

  /// <summary>
  ///   The status of the Either.
  /// </summary>
  private readonly EitherStatus status_;

  /// <summary>
  ///   Constructs an Either with no object or exception.
  /// </summary>
  public Either()
  {
    status_ = EitherStatus.None;
    obj_    = default;
  }

  /// <summary>
  ///   Constructs an Either with an object.
  /// </summary>
  /// <param name="obj">The object to be stored in the Either.</param>
  public Either(L obj)
  {
    obj_       = obj;
    exception_ = default;
    status_    = EitherStatus.Left;
  }

  /// <summary>
  ///   Constructs an Either with an exception.
  /// </summary>
  /// <param name="exception">The exception to be stored in the Either.</param>
  public Either(R exception)
  {
    exception_ = exception;
    obj_       = default;
    status_    = EitherStatus.Right;
  }

  /// <summary>
  ///   Implicitly converts the Either to an exception.
  /// </summary>
  /// <param name="ma">The Either to be converted.</param>
  /// <returns>The exception stored in the Either.</returns>
  public static explicit operator R(Either<L, R> ma)
    => ma.exception_;

  /// <summary>
  ///   Implicitly converts the Either to an object.
  /// </summary>
  /// <param name="ma">The Either to be converted.</param>
  /// <returns>The object stored in the Either.</returns>
  public static explicit operator L(Either<L, R> ma)
    => ma.obj_;

  /// <summary>
  ///   Implicitly converts an object to an Either.
  /// </summary>
  /// <param name="ma">The object to be converted.</param>
  /// <returns>An Either containing the object.</returns>
  public static implicit operator Either<L, R>(L ma)
    => new(ma);

  /// <summary>
  ///   Implicitly converts an exception to an Either.
  /// </summary>
  /// <param name="ma">The exception to be converted.</param>
  /// <returns>An Either containing the exception.</returns>
  public static implicit operator Either<L, R>(R ma)
    => new(ma);

  /// <summary>
  ///   Executes an action on the exception if the Either contains an exception.
  /// </summary>
  /// <param name="action">The action to be executed.</param>
  /// <returns>The object stored in the Either.</returns>
  public L IfRight(Action<R> action)
  {
    if (status_ == EitherStatus.Right)
    {
      action(exception_);
    }
    else
    {
      return obj_;
    }

    return default;
  }

  /// <summary>
  ///   An enum to represent the status of the Either.
  /// </summary>
  private enum EitherStatus
  {
    None  = 0,
    Left  = 1,
    Right = 2,
  }
}
