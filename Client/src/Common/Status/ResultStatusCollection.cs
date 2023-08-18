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

using System.Collections.Generic;

using JetBrains.Annotations;

namespace ArmoniK.DevelopmentKit.Client.Common.Status;

/// <summary>
///   List of result status that will be collected during the request GetResultStatus
/// </summary>
/// <param name="IdsReady">List of completed task where the result is ready to be retrieved</param>
/// <param name="IdsResultError">List of task or task result in error</param>
/// <param name="IdsError">
///   List of Unknown TaskIds. There is a heavy error somewhere else in the execution when this list
///   has element
/// </param>
/// <param name="IdsNotReady">List of result not yet written in database</param>
/// <param name="Canceled">List of canceled task</param>
[PublicAPI]
public sealed record ResultStatusCollection(IReadOnlyList<ResultStatusData> IdsReady,
                                            IReadOnlyList<ResultStatusData> IdsResultError,
                                            IReadOnlyList<string>           IdsError,
                                            IReadOnlyList<ResultStatusData> IdsNotReady,
                                            IReadOnlyList<ResultStatusData> Canceled);
