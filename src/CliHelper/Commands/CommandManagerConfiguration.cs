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
using System.Diagnostics.CodeAnalysis;

namespace Etherna.CliHelper.Commands
{
    /// <summary>
    /// The configuration of the root commands, registering every command of the tree on the service collection.
    /// </summary>
    /// <param name="services">The service collection to register the commands on</param>
    public class CommandManagerConfiguration(IServiceCollection services)
        : ICommandManagerConfiguration
    {
        // Fields.
        private readonly Dictionary<Type, CommandMap> _allCommandMaps = [];
        
        // Properties.
        /// <summary>
        /// The maps of every command of the tree, root and sub commands, by command type.
        /// </summary>
        public IReadOnlyDictionary<Type, CommandMap> AllCommandMaps => _allCommandMaps;
        
        // Methods.
        /// <summary>
        /// Add a root command, with its sub commands.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="configSubCommands">Adds the sub commands of the command, on its own configuration</param>
        /// <returns>This configuration, to add more root commands</returns>
        /// <exception cref="InvalidOperationException">An alias of the command is not valid</exception>
        public ICommandManagerConfiguration AddCommand<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand>(
            Action<ICommandManagerConfiguration>? configSubCommands = null)
            where TCommand : CommandBase
        {
            var commandMap = new CommandMap(typeof(TCommand), null);
            commandMap.VerifyNames([]);
            configSubCommands?.Invoke(commandMap);
            
            RegisterRecursivelyCommandMaps(commandMap);

            return this;
        }

        // Helpers.
        private void RegisterRecursivelyCommandMaps(CommandMap commandMap)
        {
            services.AddTransient(
                commandMap.CommandType,
                sp =>
                {
                    var command = (CommandBase)ActivatorUtilities.CreateInstance(sp, commandMap.CommandType);
                    command.CommandManager = sp.GetRequiredService<ICommandManager>();
                    
                    return command;
                });
            
            _allCommandMaps.Add(commandMap.CommandType, commandMap);
            foreach (var subCommandMap in commandMap.SubCommandMaps)
                RegisterRecursivelyCommandMaps(subCommandMap);
        }
    }
}