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
using System.Collections.Generic;
using System.Linq;

namespace Etherna.CliHelper.Models.Commands.OptionRequirements
{
    public class RequireOneOfOptionRequirement(params string[] optionsNames)
        : OptionRequirementBase(optionsNames)
    {
        public override string PrintHelpLine(CommandOptionsBase commandOptions) =>
            string.Join(", ", OptionsNames.Select(n => commandOptions.FindOptionByName(n).LongName)) +
            (OptionsNames.Count == 1 ? " is required." : " at least one is required.");

        public override IEnumerable<OptionRequirementError> ValidateOptions(
            CommandOptionsBase commandOptions,
            IEnumerable<ParsedOption> parsedOptions) =>
            OptionsNames.Any(optName => TryFindParsedOption(parsedOptions, optName, out _)) ?
                Array.Empty<OptionRequirementError>() :
                [new OptionRequirementError(PrintHelpLine(commandOptions))];
    }
}