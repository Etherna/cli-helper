// Copyright 2024-present Etherna SA
// This file is part of Cli Helper.
// 
// Cli Helper is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// Cli Helper is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with Cli Helper.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.CliHelper.Commands;
using Etherna.CliHelper.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.CliHelper
{
    public static class ServiceCollectionExtensions
    {
        public static ICommandManagerConfiguration AddCliHelper<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TIoService>(
            this IServiceCollection services)
            where TIoService : class, IIoService
        {
            var config = new CommandManagerConfiguration(services);
            
            // Add transient services.
            services.AddTransient<IIoService, TIoService>();
            
            // Add singleton services.
            services.AddSingleton<ICommandManager>(
                sp =>
                {
                    var ioService = sp.GetRequiredService<IIoService>();
                    return new CommandManager(config, ioService, sp);
                });
            
            // Return configuration.
            return config;
        }
    }
}