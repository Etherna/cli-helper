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

using Etherna.CliHelper.Commands.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Etherna.CliHelper.Commands
{
    public class CommandManagerConfiguration(IServiceCollection services)
        : ICommandManagerConfiguration
    {
        // Fields.
        private readonly Dictionary<Type, CommandMap> _allCommandMaps = new();
        
        // Properties.
        public IReadOnlyDictionary<Type, CommandMap> AllCommandMaps => _allCommandMaps;
        
        // Methods.
        public ICommandManagerConfiguration AddCommand<TCommand>(
            Action<ICommandManagerConfiguration>? configSubCommands = null)
            where TCommand : CommandBase
        {
            var commandMap = new CommandMap(typeof(TCommand), null);
            configSubCommands?.Invoke(commandMap);
            
            RegisterRecursivelyCommandMaps(commandMap);

            return this;
        }

        // Helpers.
        private void RegisterRecursivelyCommandMaps(CommandMap commandMap)
        {
            services.AddTransient(commandMap.CommandType);
            
            _allCommandMaps.Add(commandMap.CommandType, commandMap);
            foreach (var subCommandMap in commandMap.SubCommandMaps)
                RegisterRecursivelyCommandMaps(subCommandMap);
        }
    }
}