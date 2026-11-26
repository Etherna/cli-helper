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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.CliHelper.Commands
{
    /// <summary>
    /// A command of the tree, with its parent and its sub commands. It is the configuration of its sub commands.
    /// </summary>
    /// <param name="commandType">The command type</param>
    /// <param name="parentCommandMap">The map of the parent command, or null for a root command</param>
    public sealed class CommandMap(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type commandType,
        CommandMap? parentCommandMap)
        : ICommandManagerConfiguration
    {
        // Fields.
        private readonly List<CommandMap> _subCommandMaps = [];

        // Properties.
        /// <summary>
        /// The command type.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type CommandType { get; } = commandType;
        /// <summary>
        /// The map of the parent command, or null for a root command.
        /// </summary>
        public CommandMap? ParentCommandMap { get; } = parentCommandMap;
        /// <summary>
        /// The maps of the sub commands, in their registration order.
        /// </summary>
        public IEnumerable<CommandMap> SubCommandMaps => _subCommandMaps;

        // Methods.
        /// <summary>
        /// Add a sub command of this command, with its sub commands.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="configSubCommands">Adds the sub commands of the command, on its own configuration</param>
        /// <returns>This command map, to add more sub commands of the same command</returns>
        public ICommandManagerConfiguration AddCommand<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand>(
            Action<ICommandManagerConfiguration>? configSubCommands = null)
            where TCommand : CommandBase
        {
            var subCommandMap = new CommandMap(typeof(TCommand), this);
            configSubCommands?.Invoke(subCommandMap);
            _subCommandMaps.Add(subCommandMap);

            return this;
        }
    }
}