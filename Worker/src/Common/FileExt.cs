// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2025. All rights reserved.
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

using System.IO;

namespace ArmoniK.DevelopmentKit.Worker.Common;

internal static class FileExt
{
  /// <summary>
  ///   Try moving file from source into destination.
  ///   If destination already exists, it will not be overwritten,
  ///   and source will be deleted.
  /// </summary>
  /// <param name="sourceFilename">The name of the file to move. Can include a relative or absolute path.</param>
  /// <param name="destinationFilename">The new path and name for the file.</param>
  /// <returns>Whether the file has been moved</returns>
  internal static bool MoveOrDelete(string sourceFilename,
                                    string destinationFilename)
  {
    try
    {
      File.Move(sourceFilename,
                destinationFilename);

      return true;
    }
    catch (IOException)
    {
      TryDelete(sourceFilename);

      if (!File.Exists(destinationFilename))
      {
        throw;
      }
    }

    return false;
  }

  /// <summary>
  ///   Try deleting a file.
  ///   Do not throw any error in case the file does not exist (eg: already deleted)
  /// </summary>
  /// <param name="path">Path of the file to delete</param>
  /// <returns>Whether the file has been deleted</returns>
  internal static bool TryDelete(string path)
  {
    try
    {
      File.Delete(path);

      return true;
    }
    catch (IOException)
    {
      if (File.Exists(path))
      {
        throw;
      }
    }

    return false;
  }

  /// <summary>
  ///   Try deleting a directory recursively.
  ///   Do not throw any error in case the directory does not exist (eg: already deleted)
  /// </summary>
  /// <param name="path">Path of the directory to delete</param>
  /// <returns>Whether the directory has been deleted</returns>
  internal static bool TryDeleteDirectory(string path)
  {
    try
    {
      Directory.Delete(path,
                       true);

      return true;
    }
    catch (IOException)
    {
      if (Directory.Exists(path))
      {
        throw;
      }
    }

    return false;
  }

  /// <summary>
  ///   Try creating a new directory.
  ///   Do not throw any error in case the directory already exists.
  /// </summary>
  /// <param name="path">Path of the directory to create</param>
  /// <returns>Whether the directory has been created</returns>
  internal static bool TryCreateDirectory(string path)
  {
    try
    {
      Directory.CreateDirectory(path);

      return true;
    }
    catch (IOException)
    {
      if (!Directory.Exists(path))
      {
        throw;
      }
    }

    return false;
  }

  /// <summary>
  ///   Try moving a directory and all its content from source into destination.
  ///   If destination files already exist, they will not be overwritten,
  ///   and source will be deleted.
  ///   If destination folders already exist, they will be merged with source,
  ///   and source will be deleted.
  /// </summary>
  /// <param name="sourceDirectory">The name of the directory to move. Can include a relative or absolute path.</param>
  /// <param name="destinationDirectory">The new path and name for the directory.</param>
  internal static void MoveOrDeleteDirectory(string sourceDirectory,
                                             string destinationDirectory)
  {
    TryCreateDirectory(destinationDirectory);

    try
    {
      foreach (var sourcePath in Directory.EnumerateFileSystemEntries(sourceDirectory))
      {
        var destinationPath = Path.Combine(destinationDirectory,
                                           Path.GetFileName(sourcePath));

        if (Directory.Exists(sourcePath))
        {
          MoveOrDeleteDirectory(sourcePath,
                                destinationPath);
        }
        else
        {
          MoveOrDelete(sourcePath,
                       destinationPath);
        }
      }
    }
    catch (IOException)
    {
      TryDeleteDirectory(sourceDirectory);

      if (Directory.Exists(sourceDirectory))
      {
        throw;
      }
    }
  }
}
