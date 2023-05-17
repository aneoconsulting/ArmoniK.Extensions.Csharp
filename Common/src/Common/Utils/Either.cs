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
///   Implement simple either to manage Object L or Exception R
/// </summary>
/// <typeparam name="L">The object in Either</typeparam>
/// <typeparam name="R">The Exception if it exists one in the object</typeparam>
public class Either<L, R>
{
  private readonly R exception_;
  private readonly L obj_;

  private readonly EitherStatus status_;

  public Either()
  {
    status_ = EitherStatus.None;
    obj_    = default;
  }

  /// <summary>
  ///   Default constructor to build either with Left value
  /// </summary>
  /// <param name="obj">the object to set in Either</param>
  public Either(L obj)
  {
    obj_       = obj;
    exception_ = default;
    status_    = EitherStatus.Left;
  }

  /// <summary>
  ///   Default constructor to build either with Left value
  /// </summary>
  /// <param name="exception">The exception object to set in Either object</param>
  public Either(R exception)
  {
    exception_ = exception;
    obj_       = default;
    status_    = EitherStatus.Right;
  }

  /// <summary>
  ///   The operator to build R if the Right object is set
  /// </summary>
  /// <param name="ma"></param>
  /// <returns>return the object R</returns>
  public static explicit operator R(Either<L, R> ma)
    => ma.exception_;

  /// <summary>
  ///   The operator to build L if the Left object is set
  /// </summary>
  /// <param name="ma"></param>
  /// <returns>the object L</returns>
  public static explicit operator L(Either<L, R> ma)
    => ma.obj_;

  /// <summary>
  ///   The operator to build Either if the Left object is set
  /// </summary>
  /// <param name="ma"></param>
  /// <returns>the object L</returns>
  public static implicit operator Either<L, R>(L ma)
    => new(ma);

  /// <summary>
  ///   The operator to build Either if the Right object is set
  /// </summary>
  /// <param name="ma"></param>
  /// <returns>the object L</returns>
  public static implicit operator Either<L, R>(R ma)
    => new(ma);


  /// <summary>
  ///   If right apply action otherwise return the Left object
  /// </summary>
  /// <param name="action">action to apply</param>
  /// <returns>return left otherwise</returns>
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

  private enum EitherStatus
  {
    None  = 0,
    Left  = 1,
    Right = 2,
  }
}
