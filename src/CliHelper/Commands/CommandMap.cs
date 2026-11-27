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
using System.Linq;
using System.Text.RegularExpressions;

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
        private static readonly Regex aliasRegex = new("^[a-z0-9][a-z0-9-]*$");
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
        /// <exception cref="InvalidOperationException">An alias of the command is not valid, or its name or an alias
        /// is already used by another sub command of this command</exception>
        public ICommandManagerConfiguration AddCommand<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand>(
            Action<ICommandManagerConfiguration>? configSubCommands = null)
            where TCommand : CommandBase
        {
            var subCommandMap = new CommandMap(typeof(TCommand), this);
            subCommandMap.VerifyNames(_subCommandMaps);
            configSubCommands?.Invoke(subCommandMap);
            _subCommandMaps.Add(subCommandMap);

            return this;
        }

        // Helpers.
        /// <summary>
        /// Verify the aliases of the command, and that its name and aliases select it alone among its siblings.
        /// </summary>
        /// <param name="siblingMaps">The maps of the commands at the same level of the tree</param>
        /// <exception cref="InvalidOperationException">An alias is not valid, or a name is used twice</exception>
        internal void VerifyNames(IEnumerable<CommandMap> siblingMaps)
        {
            var invalidAlias = CommandBase.GetCommandAliasesFromType(CommandType)
                .FirstOrDefault(a => !aliasRegex.IsMatch(a));
            if (invalidAlias is not null)
                throw new InvalidOperationException(
                    $"Alias '{invalidAlias}' of command '{GetPathNames()}' is not valid: use lowercase letters, digits and dashes, not starting with a dash");

            var names = GetNames();
            var repeatedName = names.GroupBy(n => n).FirstOrDefault(g => g.Count() > 1)?.Key;
            if (repeatedName is not null)
                throw new InvalidOperationException(
                    $"Command '{GetPathNames()}' declares '{repeatedName}' more than once, as its name or an alias");

            foreach (var siblingMap in siblingMaps)
            {
                var sharedName = names.Intersect(siblingMap.GetNames()).FirstOrDefault();
                if (sharedName is not null)
                    throw new InvalidOperationException(
                        $"Name or alias '{sharedName}' of command '{GetPathNames()}' is already used by command '{siblingMap.GetPathNames()}'");
            }
        }

        private string[] GetNames() =>
            [CommandBase.GetCommandNameFromType(CommandType), .. CommandBase.GetCommandAliasesFromType(CommandType)];

        private string GetPathNames()
        {
            var name = CommandBase.GetCommandNameFromType(CommandType);
            return ParentCommandMap is null ? name : $"{ParentCommandMap.GetPathNames()} {name}";
        }
    }
}