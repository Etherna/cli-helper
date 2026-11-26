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

namespace Etherna.CliHelper.Commands.Models.OptionRequirements
{
    /// <summary>
    /// Verifies another requirement when an option is present.
    /// </summary>
    /// <param name="optionsName">The name of the option, short or long</param>
    /// <param name="thenRequirement">The requirement verified when the option is present</param>
    public class IfPresentThenOptionRequirement(
        string optionsName,
        OptionRequirementBase thenRequirement)
        : OptionRequirementBase([optionsName])
    {
        // Methods.
        /// <inheritdoc />
        public override string PrintHelpLine(CommandOptionsBase commandOptions)
        {
            ArgumentNullException.ThrowIfNull(commandOptions);
            
            return ComposeSentence(
                commandOptions.FindOptionByName(OptionsNames.First()).LongName,
                thenRequirement.PrintHelpLine(commandOptions));
        }

        /// <inheritdoc />
        public override IEnumerable<OptionRequirementError> ValidateOptions(CommandOptionsBase commandOptions, IEnumerable<ParsedOption> parsedOptions)
        {
            var optName = OptionsNames.First();
            
            if (!TryFindParsedOption(parsedOptions, optName, out _))
                return [];

            var thenErrors = thenRequirement.ValidateOptions(commandOptions, parsedOptions);

            return thenErrors.Select(thenError =>
                new OptionRequirementError(ComposeSentence(optName, thenError.Message)));
        }

        // Helpers.
        private static string ComposeSentence(string optName, string thenMessageLine) =>
            $"If {optName} is present then {thenMessageLine}";
    }
}