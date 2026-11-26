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
using System;
using System.Collections.Generic;

namespace Etherna.CliHelper.Commands
{
    /// <summary>
    /// Creates the registered commands, and navigates their tree.
    /// </summary>
    public interface ICommandManager
    {
        // Properties.
        /// <summary>
        /// The io service the commands read and write through.
        /// </summary>
        IIoService IoService { get; }
        
        // Methods.
        /// <summary>
        /// Create a new instance of a registered command.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <returns>The new command</returns>
        /// <exception cref="InvalidOperationException">The command type is not registered</exception>
        TCommand CreateCommand<TCommand>()
            where TCommand : CommandBase;

        /// <summary>
        /// Create a new instance of a registered command.
        /// </summary>
        /// <param name="commandType">The command type</param>
        /// <returns>The new command</returns>
        /// <exception cref="InvalidOperationException">The command type is not registered</exception>
        CommandBase CreateCommand(Type commandType);

        /// <summary>
        /// Get the path of a registered command: the types of its ancestors from the root, then its own.
        /// </summary>
        /// <param name="commandType">The command type</param>
        /// <returns>The command types of the path, from the root</returns>
        IEnumerable<Type> GetCommandPathTypes(Type commandType);

        /// <summary>
        /// Get the types of the sub commands of a registered command, in their registration order.
        /// </summary>
        /// <param name="commandType">The command type</param>
        /// <returns>The sub command types</returns>
        IEnumerable<Type> GetCommandSubTypes(Type commandType);

        /// <summary>
        /// Get the type of the parent command of a registered command.
        /// </summary>
        /// <param name="commandType">The command type</param>
        /// <returns>The parent command type, or null for a root command</returns>
        Type? TryGetParentCommand(Type commandType);
    }
}