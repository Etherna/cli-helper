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
    public abstract class CommandOptionsBase
    {
        // Properties.
        public bool AreRequired => Requirements.OfType<RequireOneOfOptionRequirement>().Any();
        public abstract IEnumerable<CommandOption> Definitions { get; }
        public virtual IEnumerable<OptionRequirementBase> Requirements => [];
        
        // Methods.
        public CommandOption FindOptionByName(string name) =>
            Definitions.First(o => o.ShortName == name || o.LongName == name);
        
        /// <summary>
        /// Parse the options at the start of args, against these options alone.
        /// Short options compose POSIX style: "-ab" is "-a -b", and an option requiring args ends the group,
        /// taking the rest of it as its first arg ("-nvalue", "-anvalue"), or the following args when nothing
        /// is left ("-an value")
        /// </summary>
        /// <param name="args">Input args</param>
        /// <param name="ioService">Where to report the violated requirements</param>
        /// <returns>Found option args counter</returns>
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