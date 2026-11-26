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

using System.Collections.Generic;
using System.Linq;

namespace Etherna.CliHelper.Commands.Models.OptionRequirements
{
    /// <summary>
    /// Requires at most one of the options.
    /// </summary>
    /// <param name="optionsNames">The names of the mutually exclusive options, short or long</param>
    public class ExclusiveOptionRequirement(params string[] optionsNames)
        : OptionRequirementBase(optionsNames)
    {
        // Methods.
        /// <inheritdoc />
        public override string PrintHelpLine(CommandOptionsBase commandOptions) =>
            ComposeSentence(OptionsNames.Select(n => commandOptions.FindOptionByName(n).LongName));

        /// <inheritdoc />
        public override IEnumerable<OptionRequirementError> ValidateOptions(
            CommandOptionsBase commandOptions,
            IEnumerable<ParsedOption> parsedOptions)
        {
            if (OptionsNames.Count(optName => TryFindParsedOption(parsedOptions, optName, out _)) >= 2)
            {
                var invalidParsedNames = parsedOptions.Where(parsedOpt =>
                        OptionsNames.Contains(parsedOpt.Option.ShortName) ||
                        OptionsNames.Contains(parsedOpt.Option.LongName))
                    .Select(foundOpt => foundOpt.ParsedName);

                return [new OptionRequirementError(ComposeSentence(invalidParsedNames))];
            }

            return [];
        }
        
        // Helpers.
        private static string ComposeSentence(IEnumerable<string> optNames) =>
            $"{string.Join(", ", optNames)} are mutual exclusive.";
    }
}