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

namespace ArmoniK.DevelopmentKit.Worker.Unified;

/// <summary>
///   implementation of this interface in <see cref="TaskWorkerService" /> or your own implementation allows to have the
///   <see cref="TaskContext" /> configured automatically by the <see cref="GridWorker" />
/// </summary>
public interface ITaskContextConfiguration
{
  /// <summary>
  ///   Allow the initialization of <see cref="TaskContext" />
  /// </summary>
  TaskContext TaskContext { get; set; }
}

public interface ICheckHealth
{
  bool CheckHealth()
    => true;
}
