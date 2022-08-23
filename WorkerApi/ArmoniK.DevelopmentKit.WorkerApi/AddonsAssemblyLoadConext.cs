// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
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
using System.Reflection;
using System.Runtime.Loader;

namespace ArmoniK.DevelopmentKit.WorkerApi
{
  class AddonsAssemblyLoadContext : AssemblyLoadContext
  {
    private readonly AssemblyDependencyResolver _resolver;
    private readonly AssemblyDependencyResolver _rootResolver;

    public AddonsAssemblyLoadContext() : base(Guid.NewGuid().ToString(),
                                              isCollectible: true)
    {

    }

    public AddonsAssemblyLoadContext(string mainAssemblyToLoadPath) : base(Guid.NewGuid().ToString(),
                                                                           isCollectible: true)
    {
      _rootResolver = new AssemblyDependencyResolver(Assembly.GetExecutingAssembly().Location);

      _resolver     = new AssemblyDependencyResolver(mainAssemblyToLoadPath);

    }

    protected override Assembly Load(AssemblyName name)
    {
      if (_rootResolver.ResolveAssemblyToPath(name) != null) return null;

      var assemblyPath                       =  _resolver.ResolveAssemblyToPath(name);

      return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    /// <summary>Allows derived class to load an unmanaged library by name.</summary>
    /// <param name="unmanagedDllName">Name of the unmanaged library. Typically this is the filename without its path or extensions.</param>
    /// <returns>A handle to the loaded library, or <see cref="F:System.IntPtr.Zero" />.</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
      var assemblyPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
      return assemblyPath != null ? base.LoadUnmanagedDllFromPath(assemblyPath) : IntPtr.Zero;
    }
  }
}