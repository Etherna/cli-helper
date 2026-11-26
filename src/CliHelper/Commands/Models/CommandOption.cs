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
using System.Text.RegularExpressions;

namespace Etherna.CliHelper.Commands.Models
{
    /// <summary>
    /// The definition of an option of a command: its names, and the args it requires.
    /// </summary>
    public class CommandOption
    {
        // Fields.
        private static readonly Regex longNameRegex = new("^--[A-Za-z0-9-]+$");
        private static readonly Regex shortNameRegex = new("^-[A-Za-z0-9]$");

        // Constructor.
        /// <summary>
        /// Define an option.
        /// </summary>
        /// <param name="shortName">The short name, a dash and a letter or digit like <c>-n</c>, or null for
        /// none</param>
        /// <param name="longName">The long name, two dashes and letters, digits or dashes like <c>--name</c></param>
        /// <param name="description">The description of the option, in the help</param>
        /// <param name="onFound">Invoked with the option args, when the option is found in the args</param>
        /// <param name="requiredArgTypes">The types of the args the option requires, one per arg, in the help.
        /// None by default</param>
        /// <exception cref="ArgumentException">A name is not valid</exception>
        public CommandOption(
            string? shortName,
            string longName,
            string description,
            Action<string[]> onFound,
            IEnumerable<Type>? requiredArgTypes = null)
        {
            if (shortName is not null && !shortNameRegex.IsMatch(shortName))
                throw new ArgumentException("Invalid short option name");

            if (!longNameRegex.IsMatch(longName))
                throw new ArgumentException("Invalid long option name");

            ShortName = shortName;
            LongName = longName;
            RequiredArgTypes = requiredArgTypes ?? [];
            Description = description;
            OnFound = onFound;
        }

        // Properties.
        /// <summary>
        /// The description of the option, in the help.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// The long name of the option, like <c>--name</c>.
        /// </summary>
        public string LongName { get; }
        /// <summary>
        /// Invoked with the option args, when the option is found in the args.
        /// </summary>
        public Action<string[]> OnFound { get; }
        /// <summary>
        /// The types of the args the option requires, one per arg, in the help.
        /// </summary>
        public IEnumerable<Type> RequiredArgTypes { get; }
        /// <summary>
        /// The short name of the option, like <c>-n</c>, or null for none.
        /// </summary>
        public string? ShortName { get; }
    }
}