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

using Etherna.CliHelper.Commands.Models.OptionRequirements;
using Etherna.CliHelper.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.CliHelper.Commands.Models
{
    /// <summary>
    /// Base class of the options of a command: their definitions, and the requirements they have to satisfy.
    /// </summary>
    public abstract class CommandOptionsBase
    {
        // Properties.
        /// <summary>
        /// Whether some of the options are required, with a <see cref="RequireOneOfOptionRequirement"/>.
        /// </summary>
        public bool AreRequired => Requirements.OfType<RequireOneOfOptionRequirement>().Any();
        /// <summary>
        /// The definitions of the options.
        /// </summary>
        public abstract IEnumerable<CommandOption> Definitions { get; }
        /// <summary>
        /// The requirements the options have to satisfy, verified after the args are parsed. None by default.
        /// </summary>
        public virtual IEnumerable<OptionRequirementBase> Requirements => [];
        
        // Methods.
        /// <summary>
        /// Find the definition of an option by its short or long name.
        /// </summary>
        /// <param name="name">The short or long name of the option</param>
        /// <returns>The option definition</returns>
        /// <exception cref="InvalidOperationException">No option has the name</exception>
        public CommandOption FindOptionByName(string name) =>
            Definitions.First(o => o.ShortName == name || o.LongName == name);
        
        /// <summary>
        /// Parse the options at the start of args, against these options alone.
        /// Short options compose POSIX style: "-ab" is "-a -b", and an option requiring args ends the group,
        /// taking the rest of it as its first arg ("-nvalue", "-anvalue"), or the following args when nothing
        /// is left ("-an value").
        /// </summary>
        /// <param name="args">Input args</param>
        /// <param name="ioService">Where to report the violated requirements</param>
        /// <returns>Found option args counter</returns>
        /// <exception cref="ArgumentException">The args are not valid options, or they violate a requirement.
        /// The message is meant for the user</exception>
        public int ParseArgs(
            string[] args,
            IIoService ioService)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(ioService);

            var optionsParser = new OptionsParser();
            optionsParser.AddOptions(this, GetType().Name);
            
            // Parse options.
            var parsedArgsCount = 0;
            while (parsedArgsCount < args.Length && args[parsedArgsCount].StartsWith('-'))
                parsedArgsCount = optionsParser.ParseOption(args, parsedArgsCount);
            
            // Verify option requirements.
            optionsParser.ValidateRequirements(ioService);

            return parsedArgsCount;
        }
    }
}