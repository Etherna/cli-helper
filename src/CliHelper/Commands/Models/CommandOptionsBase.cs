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
using System.Globalization;
using System.Linq;
using System.Text;

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
        /// Parse command options. Short options compose POSIX style: "-ab" is "-a -b", and an option
        /// requiring args ends the group, taking the rest of it as its first arg ("-nvalue", "-anvalue"),
        /// or the following args when nothing is left ("-an value")
        /// </summary>
        /// <param name="args">Input args</param>
        /// <returns>Found option args counter</returns>
        public virtual int ParseArgs(
            string[] args,
            IIoService ioService)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(ioService);

            // Parse options.
            var parsedArgsCount = 0;
            var foundOptions = new List<ParsedOption>();
            while (parsedArgsCount < args.Length && args[parsedArgsCount].StartsWith('-'))
            {
                var optArg = args[parsedArgsCount++];
                
                // Long option, or single short option.
                if (optArg.Length <= 2 || optArg[1] == '-')
                {
                    var option = FindUnparsedOption(optArg, foundOptions);
                    parsedArgsCount += ExecOption(option, optArg, null, args, parsedArgsCount, foundOptions);
                    continue;
                }
                
                // Composed short options.
                for (var i = 1; i < optArg.Length; i++)
                {
                    var optName = "-" + optArg[i];
                    var option = FindUnparsedOption(optName, foundOptions);
                    if (!option.RequiredArgTypes.Any())
                    {
                        ExecOption(option, optName, null, args, parsedArgsCount, foundOptions);
                        continue;
                    }
                    
                    // An option requiring args ends the group.
                    var attachedArg = i + 1 < optArg.Length ? optArg[(i + 1)..] : null;
                    parsedArgsCount += ExecOption(option, optName, attachedArg, args, parsedArgsCount, foundOptions);
                    break;
                }
            }
            
            // Verify option requirements.
            var optionErrors = Requirements.SelectMany(r => r.ValidateOptions(this, foundOptions)).ToArray();
            if (optionErrors.Length != 0)
            {
                var errorStrBuilder = new StringBuilder();
                errorStrBuilder.AppendLine("Invalid options:");
                foreach (var error in optionErrors)
                    errorStrBuilder.AppendLine("  " + error.Message);

                ioService.WriteError(errorStrBuilder.ToString());

                throw new ArgumentException("Errors with command options");
            }

            return parsedArgsCount;
        }
        
        // Helpers.
        /// <summary>
        /// Run an option with its args, and save it as parsed
        /// </summary>
        /// <returns>Count of the following args consumed by the option</returns>
        private static int ExecOption(
            CommandOption option,
            string optName,
            string? attachedArg,
            string[] args,
            int followingArgsIndex,
            List<ParsedOption> parsedOptions)
        {
            // Check required option args. An attached arg is the first one.
            var requiredArgsCount = option.RequiredArgTypes.Count();
            var followingArgsCount = attachedArg is null ? requiredArgsCount : requiredArgsCount - 1;
            if (args.Length - followingArgsIndex < followingArgsCount)
                throw new ArgumentException($"{optName} requires {requiredArgsCount} args: {string.Join(" ", option.RequiredArgTypes.Select(t => t.Name.ToLower(CultureInfo.InvariantCulture)))}");
            
            // Exec option code.
            var followingArgs = args[followingArgsIndex..(followingArgsIndex + followingArgsCount)];
            string[] optArgs = attachedArg is null ? followingArgs : [attachedArg, .. followingArgs];
            option.OnFound(optArgs);
            
            // Save on found options.
            parsedOptions.Add(new(option, optName, optArgs));
            
            return followingArgsCount;
        }
        
        private CommandOption FindUnparsedOption(string optName, List<ParsedOption> parsedOptions)
        {
            // Find option by name.
            var option = Definitions.FirstOrDefault(opt => opt.ShortName == optName || opt.LongName == optName) ??
                throw new ArgumentException(optName + " is not a valid option");
            
            // Verify duplicate options.
            if (parsedOptions.Any(opt => opt.Option.ShortName == optName || opt.Option.LongName == optName))
                throw new ArgumentException(optName + " option is duplicate");

            return option;
        }
    }
}