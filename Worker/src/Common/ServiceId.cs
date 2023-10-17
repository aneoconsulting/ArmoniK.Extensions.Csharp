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

using ArmoniK.DevelopmentKit.Common;

namespace ArmoniK.DevelopmentKit.Worker.Common;

/// <summary>
///   Identifier for a Package
/// </summary>
public readonly struct PackageId : IEquatable<PackageId>
{
  /// <summary>
  ///   Creates a PackageId
  /// </summary>
  /// <param name="applicationName">Application name</param>
  /// <param name="applicationVersion">Application version</param>
  public PackageId(string applicationName,
                   string applicationVersion)
  {
    ApplicationName    = applicationName;
    ApplicationVersion = applicationVersion;
  }

  /// <summary>
  ///   Application name
  /// </summary>
  public string ApplicationName { get; }

  /// <summary>
  ///   Application version
  /// </summary>
  public string ApplicationVersion { get; }

  /// <inheritdoc />
  public bool Equals(PackageId other)
    => ApplicationName == other.ApplicationName && ApplicationVersion == other.ApplicationVersion;

  /// <summary>
  ///   Indicates whether the objects are equal.
  /// </summary>
  /// <param name="a">Object a</param>
  /// <param name="b">Object b</param>
  /// <returns>true if the objects are equal, false otherwise</returns>
  public static bool operator ==(PackageId a,
                                 PackageId b)
    => a.Equals(b);

  /// <summary>
  ///   Indicates whether the objects are not equal.
  /// </summary>
  /// <param name="a">Object a</param>
  /// <param name="b">Object b</param>
  /// <returns>false if the objects are equal, true otherwise</returns>
  public static bool operator !=(PackageId a,
                                 PackageId b)
    => !(a == b);

  /// <inheritdoc />
  public override bool Equals(object obj)
    => obj is PackageId id && Equals(id);

  /// <inheritdoc />
  public override int GetHashCode()
    => HashCode.Combine(ApplicationName,
                        ApplicationVersion);

  /// <summary>
  ///   Name of this package's main assembly file
  /// </summary>
  public string MainAssemblyFileName
    => $"{ApplicationName}.dll";

  /// <summary>
  ///   Subpath of this package
  /// </summary>
  public string PackageSubpath
    => Path.Combine(ApplicationName,
                    ApplicationVersion);

  /// <summary>
  ///   Name of the zip file for that package
  /// </summary>
  public string ZipFileName
    => $"{ApplicationName}-v{ApplicationVersion}.zip";


  /// <inheritdoc />
  public override string ToString()
    => $"{ApplicationName}-v{ApplicationVersion}";
}

/// <summary>
///   Identifier for a Service
/// </summary>
public readonly struct ServiceId : IEquatable<ServiceId>
{
  /// <summary>
  ///   Creates a ServiceId
  /// </summary>
  /// <param name="packageId">PackageId</param>
  /// <param name="applicationNamespace">Namespace</param>
  /// <param name="engineType">Engine type</param>
  public ServiceId(PackageId  packageId,
                   string     applicationNamespace,
                   EngineType engineType)
  {
    PackageId            = packageId;
    ApplicationNamespace = applicationNamespace;
    EngineType           = engineType;
  }

  /// <summary>
  ///   PackageId of the service
  /// </summary>
  public PackageId PackageId { get; }

  /// <summary>
  ///   Namespace of the service
  /// </summary>
  public string ApplicationNamespace { get; }

  /// <summary>
  ///   Type of engine of the service
  /// </summary>
  public EngineType EngineType { get; }


  /// <inheritdoc />
  public bool Equals(ServiceId other)
    => EngineType == other.EngineType && ApplicationNamespace == other.ApplicationNamespace && PackageId.Equals(other.PackageId);

  /// <summary>
  ///   Indicates whether the objects are equal.
  /// </summary>
  /// <param name="a">Object a</param>
  /// <param name="b">Object b</param>
  /// <returns>true if the objects are equal, false otherwise</returns>
  public static bool operator ==(ServiceId a,
                                 ServiceId b)
    => a.Equals(b);

  /// <summary>
  ///   Indicates whether the objects are not equal.
  /// </summary>
  /// <param name="a">Object a</param>
  /// <param name="b">Object b</param>
  /// <returns>false if the objects are equal, true otherwise</returns>
  public static bool operator !=(ServiceId a,
                                 ServiceId b)
    => !(a == b);

  /// <inheritdoc />
  public override bool Equals(object obj)
    => obj is ServiceId && Equals(obj);

  /// <inheritdoc />
  public override int GetHashCode()
    => HashCode.Combine(PackageId,
                        ApplicationNamespace,
                        (int)EngineType);

  /// <inheritdoc />
  public override string ToString()
    => $"{PackageId}#{EngineType}#{ApplicationNamespace}";
}
