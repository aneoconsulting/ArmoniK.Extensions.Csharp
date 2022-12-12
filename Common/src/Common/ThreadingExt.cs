// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2022. All rights reserved.
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
using System.Threading;
using System.Threading.Tasks;

namespace ArmoniK.DevelopmentKit.Common;

/// <summary>
///   Threading extensions
/// </summary>
public static class ThreadingExt
{
  /// <summary>
  ///   Lock the semaphore and return a lock guard that will unlock the semaphore when disposed
  /// </summary>
  public static IDisposable LockGuard(this SemaphoreSlim sem)
  {
    sem.Wait();
    return Disposable.Create(() => sem.Release());
  }

  /// <summary>
  ///   Lock the semaphore and return a lock guard that will unlock the semaphore when disposed
  /// </summary>
  public static async Task<IDisposable> LockGuardAsync(this SemaphoreSlim sem)
  {
    await sem.WaitAsync();
    return Disposable.Create(() => sem.Release());
  }

  /// <summary>
  ///   Acquire the semaphore before calling the function,
  ///   and release it after.
  /// </summary>
  public static T LockedExecute<T>(this SemaphoreSlim sem,
                                   Func<T>            func)
  {
    using var guard = sem.LockGuard();
    return func();
  }
}
