// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023.All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// This is a simplified implementation of Either from LanguageExt:
// https://github.com/louthy/language-ext/blob/main/LanguageExt.Core/Monads/Alternative%20Value%20Monads/Either/Either/Either.cs

using System;

namespace ArmoniK.DevelopmentKit.Common.Utils;

/// <summary>
///   Represents a simple Either type to manage an object of type L or an exception of type R.
/// </summary>
/// <typeparam name="TL">The object in the Either.</typeparam>
/// <typeparam name="TException">The exception in the Either.</typeparam>
public class Either<TL, TException>
  where TException : Exception
{
  /// <summary>
  ///   The exception in the Either.
  /// </summary>
  private readonly TException? exception_;

  /// <summary>
  ///   The object in the Either.
  /// </summary>
  private readonly TL? obj_;

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
  public Either(TL obj)
  {
    obj_       = obj;
    exception_ = default;
    status_    = EitherStatus.Left;
  }

  /// <summary>
  ///   Constructs an Either with an exception.
  /// </summary>
  /// <param name="exception">The exception to be stored in the Either.</param>
  public Either(TException exception)
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
  public static explicit operator TException(Either<TL, TException> ma)
    => ma.exception_!;

  /// <summary>
  ///   Implicitly converts the Either to an object.
  /// </summary>
  /// <param name="ma">The Either to be converted.</param>
  /// <returns>The object stored in the Either.</returns>
  public static explicit operator TL(Either<TL, TException> ma)
    => ma.obj_ ?? throw ma.exception_!;

  /// <summary>
  ///   Implicitly converts an object to an Either.
  /// </summary>
  /// <param name="ma">The object to be converted.</param>
  /// <returns>An Either containing the object.</returns>
  public static implicit operator Either<TL, TException>(TL ma)
    => new(ma);

  /// <summary>
  ///   Implicitly converts an exception to an Either.
  /// </summary>
  /// <param name="ma">The exception to be converted.</param>
  /// <returns>An Either containing the exception.</returns>
  public static implicit operator Either<TL, TException>(TException ma)
    => new(ma);

  /// <summary>
  ///   Executes an action on the exception if the Either contains an exception.
  /// </summary>
  /// <param name="action">The action to be executed.</param>
  /// <returns>The object stored in the Either.</returns>
  public TL? IfRight(Action<TException> action)
  {
    if (status_ == EitherStatus.Right)
    {
      action(exception_!);
    }
    else
    {
      return obj_!;
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
