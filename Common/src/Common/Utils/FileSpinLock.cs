// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2023. All rights reserved.
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

using System;
using System.IO;
using System.Text;
using System.Threading;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Common.Utils;

/// <summary>
///   Spin lock on a file
/// </summary>
[PublicAPI]
public sealed class FileSpinLock : IDisposable
{
  private static readonly byte[] LockBytes = Encoding.ASCII.GetBytes("locked");

  [CanBeNull]
  private readonly FileStream fileStream_;

  /// <summary>
  ///   Creates a spinlock on the file. Sets <see cref="HasLock" /> to true if it successfully locked the file, false
  ///   otherwise
  /// </summary>
  /// <param name="lockFile">File to lock</param>
  /// <param name="deleteOnUnlock">Delete the lockfile when this object is disposed</param>
  /// <param name="timeoutMs">Maximum time to wait for the lock to be acquired</param>
  /// <param name="spinIntervalMs">Interval between lock tries</param>
  public FileSpinLock(string lockFile,
                      bool   deleteOnUnlock = true,
                      int    timeoutMs      = 30000,
                      int    spinIntervalMs = 250)
  {
    HasLock = false;
    var currentSpinTime = 0;
    spinIntervalMs = Math.Min(spinIntervalMs,
                              timeoutMs);
    do
    {
      try
      {
        fileStream_ ??= new FileStream(lockFile,
                                       FileMode.OpenOrCreate,
                                       FileAccess.ReadWrite,
                                       FileShare.None,
                                       1,
                                       FileOptions.WriteThrough | (deleteOnUnlock
                                                                     ? FileOptions.DeleteOnClose
                                                                     : FileOptions.None));
        if (fileStream_.Seek(0,
                             SeekOrigin.End) == 0)
        {
          fileStream_.Write(LockBytes,
                            0,
                            LockBytes.Length);
          fileStream_.Flush();
        }

        fileStream_.Lock(0,
                         LockBytes.Length);

        HasLock = true;
      }
      catch (IOException)
      {
        Thread.Sleep(spinIntervalMs);
        currentSpinTime += spinIntervalMs;
      }
      catch (UnauthorizedAccessException)
      {
        Thread.Sleep(spinIntervalMs);
        currentSpinTime += spinIntervalMs;
      }
    } while (!HasLock && currentSpinTime < timeoutMs + spinIntervalMs);
  }

  /// <summary>
  ///   True if the file is locked by the current class, false otherwise
  /// </summary>
  public bool HasLock { get; }

  /// <inheritdoc />
  public void Dispose()
  {
    if (HasLock)
    {
      fileStream_?.Unlock(0,
                          LockBytes.Length);
    }

    fileStream_?.Dispose();
  }
}
