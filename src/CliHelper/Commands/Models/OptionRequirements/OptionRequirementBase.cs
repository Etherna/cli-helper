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
    /// Base class of a requirement on the options of a command, verified after the args are parsed, and
    /// described in the help.
    /// </summary>
    /// <param name="optionsNames">The names of the options the requirement is about, short or long</param>
    public abstract class OptionRequirementBase(
        IReadOnlyCollection<string> optionsNames)
    {
        // Properties.
        /// <summary>
        /// The names of the options the requirement is about, short or long.
        /// </summary>
        public IReadOnlyCollection<string> OptionsNames { get; protected set; } = optionsNames;

        // Methods.
        /// <summary>
        /// Compose the line describing the requirement in the help.
        /// </summary>
        /// <param name="commandOptions">The options the requirement belongs to</param>
        /// <returns>The help line</returns>
        public abstract string PrintHelpLine(
            CommandOptionsBase commandOptions);
        
        /// <summary>
        /// Verify the requirement on the options found in the args.
        /// </summary>
        /// <param name="commandOptions">The options the requirement belongs to</param>
        /// <param name="parsedOptions">The options found in the args</param>
        /// <returns>The violations of the requirement, none when it is satisfied</returns>
        public abstract IEnumerable<OptionRequirementError> ValidateOptions(
            CommandOptionsBase commandOptions,
            IEnumerable<ParsedOption> parsedOptions);
        
        // Protected helpers.
        /// <summary>
        /// Find an option found in the args by its short or long name.
        /// </summary>
        /// <param name="parsedOptions">The options found in the args</param>
        /// <param name="optionName">The short or long name of the option</param>
        /// <param name="foundParsedOption">The parsed option, or null when it is not found</param>
        /// <returns>Whether the option is found</returns>
        protected static bool TryFindParsedOption(
            IEnumerable<ParsedOption> parsedOptions,
            string optionName,
            out ParsedOption? foundParsedOption)
        {
            foundParsedOption = parsedOptions.SingleOrDefault(parsOpt =>
                parsOpt.Option.ShortName == optionName ||
                parsOpt.Option.LongName == optionName);
            return foundParsedOption is not null;
        }
    }
}