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
using System.Collections.ObjectModel;

namespace Etherna.CliHelper.Commands.Models
{
    /// <summary>
    /// An option found in the args, with the name and the args it was found with.
    /// </summary>
    /// <param name="option">The definition of the option</param>
    /// <param name="parsedName">The name the option was found with, short or long</param>
    /// <param name="parsedArgs">The args of the option</param>
    public class ParsedOption(
        CommandOption option,
        string parsedName,
        params string[] parsedArgs)
    {
        // Properties.
        /// <summary>
        /// The definition of the option.
        /// </summary>
        public CommandOption Option { get; } = option;
        /// <summary>
        /// The args of the option.
        /// </summary>
        public ReadOnlyCollection<string> ParsedArgs { get; } = Array.AsReadOnly(parsedArgs);
        /// <summary>
        /// The name the option was found with, short or long.
        /// </summary>
        public string ParsedName { get; } = parsedName;
    }
}