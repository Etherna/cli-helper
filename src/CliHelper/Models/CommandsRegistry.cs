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

using Etherna.CliHelper.Models.Commands;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.CliHelper.Models
{
    public class CommandsRegistry : ICommandsMapper
    {
        // Fields.
        private readonly Dictionary<Type, CommandMap> allCommandMaps = new();
        
        // Methods.
        public IEnumerable<Type> GetCommandSubTypes(Type commandType) =>
            allCommandMaps[commandType].SubCommandMaps.Select(m => m.CommandType);

        public IEnumerable<Type> GetCommandPathTypes(Type commandType)
        {
            ArgumentNullException.ThrowIfNull(commandType, nameof(commandType));
            
            var commandPathTypes = new List<Type>();
            var currentCommandType = commandType;
            while (currentCommandType != null)
            {
                commandPathTypes.Insert(0, currentCommandType);
                currentCommandType = TryGetParentCommand(currentCommandType);
            }
            return commandPathTypes;
        }

        public Type? TryGetParentCommand(Type commandType) =>
            allCommandMaps[commandType].ParentCommandMap?.CommandType;

        public ICommandsMapper AddCommand<TCommand>(
            Action<ICommandsMapper>? configSubCommands = null)
            where TCommand : CommandBase
        {
            var commandMap = new CommandMap(typeof(TCommand), null);
            configSubCommands?.Invoke(commandMap);
            
            AddRecursivelyCommandMaps(commandMap);

            return this;
        }

        // Helpers.
        private void AddRecursivelyCommandMaps(CommandMap commandMap)
        {
            allCommandMaps.Add(commandMap.CommandType, commandMap);
            foreach (var subCommandMap in commandMap.SubCommandMaps)
                AddRecursivelyCommandMaps(subCommandMap);
        }
    }
}