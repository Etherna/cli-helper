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

namespace Etherna.CliHelper.Models.Commands
{
    public class ParsedOption(
        CommandOption option,
        string parsedName,
        params string[] parsedArgs)
    {
        // Properties.
        public CommandOption Option { get; } = option;
        public ReadOnlyCollection<string> ParsedArgs { get; } = Array.AsReadOnly(parsedArgs);
        public string ParsedName { get; } = parsedName;
    }
}