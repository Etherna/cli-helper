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
using Etherna.CliHelper.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.CliHelper.Commands
{
    public class CommandManager(
        CommandManagerConfiguration config,
        IIoService ioService,
        IServiceProvider serviceProvider)
    {
        // Properties.
        public IIoService DefaultIoService { get; } = ioService;

        // Methods.
        public TCommand CreateCommand<TCommand>()
            where TCommand : CommandBase =>
            (TCommand)CreateCommand(typeof(TCommand));

        public CommandBase CreateCommand(Type commandType)
        {
            ArgumentNullException.ThrowIfNull(commandType, nameof(commandType));
            
            if (!config.AllCommandMaps.ContainsKey(commandType))
                throw new InvalidOperationException($"Unregistered command type {commandType.Name}");
            
            return (CommandBase)serviceProvider.GetRequiredService(commandType);
        }
        
        public IEnumerable<Type> GetCommandSubTypes(Type commandType) =>
            config.AllCommandMaps[commandType].SubCommandMaps.Select(m => m.CommandType);

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
            config.AllCommandMaps[commandType].ParentCommandMap?.CommandType;
    }
}