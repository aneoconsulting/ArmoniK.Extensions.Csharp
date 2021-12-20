// This file is part of ArmoniK project.
// 
// Copyright (c) ANEO. All rights reserved.
//   W. Kirschenmann <wkirschenmann@aneo.fr>

using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;


using Microsoft.Extensions.Configuration;

namespace ArmoniK.Adapters.WorkerApi
{
    public static class ServiceCollectionExt
    {
        [PublicAPI]
        public static IServiceCollection AddConfiguration
        (
            this IServiceCollection serviceCollection,
            IConfiguration configuration
        )
        {
            return serviceCollection;
        }
    }
}