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

namespace Etherna.CliHelper.Models.Commands.OptionRequirements
{
    public abstract class OptionRequirementBase(
        IReadOnlyCollection<string> optionsNames)
    {
        // Properties.
        public IReadOnlyCollection<string> OptionsNames { get; protected set; } = optionsNames;

        // Methods.
        public abstract string PrintHelpLine(
            CommandOptionsBase commandOptions);
        
        public abstract IEnumerable<OptionRequirementError> ValidateOptions(
            CommandOptionsBase commandOptions,
            IEnumerable<ParsedOption> parsedOptions);
        
        // Protected helpers.
        protected static bool TryFindParsedOption(
            IEnumerable<ParsedOption> parsedOptions,
            string optionName,
            out ParsedOption? foundParsedOption)
        {
            foundParsedOption = parsedOptions.SingleOrDefault(parsOpt =>
                parsOpt.Option.ShortName == optionName ||
                parsOpt.Option.LongName == optionName);
            return foundParsedOption != null;
        }
    }
}