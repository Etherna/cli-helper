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
using System.Diagnostics.CodeAnalysis;

namespace Etherna.CliHelper.Commands
{
    /// <summary>
    /// Configures a level of the command tree. Every command added is registered on the service collection,
    /// with its sub commands.
    /// </summary>
    public interface ICommandManagerConfiguration
    {
        // Methods.
        /// <summary>
        /// Add a command at this level of the tree, with its sub commands.
        /// </summary>
        /// <typeparam name="TCommand">The command type</typeparam>
        /// <param name="configSubCommands">Adds the sub commands of the command, on its own configuration</param>
        /// <returns>This configuration, to add more commands at the same level</returns>
        ICommandManagerConfiguration AddCommand<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommand>(
            Action<ICommandManagerConfiguration>? configSubCommands = null)
            where TCommand : CommandBase;
    }
}