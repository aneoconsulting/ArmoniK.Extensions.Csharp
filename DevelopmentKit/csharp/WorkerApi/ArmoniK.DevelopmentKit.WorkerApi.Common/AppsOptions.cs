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

namespace ArmoniK.DevelopmentKit.WorkerApi.Common
{
  public static class AppsOptions
  {
      public static string EngineTypeNameKey { get; } = "EngineType";
      public static string GridAppNameKey { get; } = "GridAppName";
      public static string GridAppVersionKey { get; } = "GridAppVersion";
      public static string GridAppNamespaceKey { get; } = "GridAppNamespace";
      public static string GridVolumesKey { get; } = "gridVolumes";
      public static string GridAppVolumesKey { get; } = "target_app_path";
      public static string GridServiceNameKey { get; set; } = "GridServiceName";
  }
}
