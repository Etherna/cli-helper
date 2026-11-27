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


using System;

namespace Etherna.CliHelper.Commands.Models
{
    /// <summary>
    /// Declare an alias of a command: another name selecting it in the args, beside the name derived from its
    /// type. Repeat the attribute to declare more aliases. Help and errors keep naming the command by its name.
    /// </summary>
    /// <param name="alias">The alias: lowercase letters, digits and dashes, not starting with a dash</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CommandAliasAttribute(string alias) : Attribute
    {
        // Properties.
        /// <summary>
        /// The alias of the command.
        /// </summary>
        public string Alias { get; } = alias;
    }
}
