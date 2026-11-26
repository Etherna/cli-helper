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

using Etherna.CliHelper.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Etherna.CliHelper.Commands.Models
{
    /// <summary>
    /// Parse option args against the options of one or more owners, like the commands of a path.
    /// Short options compose POSIX style: "-ab" is "-a -b", and an option requiring args ends the group,
    /// taking the rest of it as its first arg ("-nvalue", "-anvalue"), or the following args when nothing
    /// is left ("-an value")
    /// </summary>
    internal sealed class OptionsParser
    {
        // Internal classes.
        private sealed class OptionsOwner(
            CommandOptionsBase options,
            string name)
        {
            public CommandOption[] Definitions { get; } = options.Definitions.ToArray();
            public string Name { get; } = name;
            public CommandOptionsBase Options { get; } = options;
            public List<ParsedOption> ParsedOptions { get; } = [];
        }

        // Fields.
        private readonly List<OptionsOwner> owners = [];

        // Methods.
        /// <summary>
        /// Add the options of an owner. They can't redefine a name already defined by another owner
        /// </summary>
        public void AddOptions(CommandOptionsBase options, string ownerName)
        {
            var newOwner = new OptionsOwner(options, ownerName);
            foreach (var optName in newOwner.Definitions.SelectMany(GetOptionNames))
            {
                var definingOwner = owners.FirstOrDefault(o => o.Definitions.Any(d => HasName(d, optName)));
                if (definingOwner is not null)
                    throw new InvalidOperationException(
                        $"Option {optName} of command '{ownerName}' is already defined by command '{definingOwner.Name}'");
            }
            
            owners.Add(newOwner);
        }

        public bool IsDefined(string optName) =>
            owners.Any(o => o.Definitions.Any(d => HasName(d, optName)));

        /// <summary>
        /// Parse the option arg at the index, with the following args it requires
        /// </summary>
        /// <returns>Index of the first arg not consumed</returns>
        public int ParseOption(string[] args, int index)
        {
            var optArg = args[index++];
            
            // Long option, or single short option.
            if (optArg.Length <= 2 || optArg[1] == '-')
            {
                var (owner, option) = FindUnparsedOption(optArg);
                return index + ExecOption(owner, option, optArg, null, args, index);
            }
            
            // Composed short options.
            for (var i = 1; i < optArg.Length; i++)
            {
                var optName = "-" + optArg[i];
                var (owner, option) = FindUnparsedOption(optName);
                if (!option.RequiredArgTypes.Any())
                {
                    ExecOption(owner, option, optName, null, args, index);
                    continue;
                }
                
                // An option requiring args ends the group.
                var attachedArg = i + 1 < optArg.Length ? optArg[(i + 1)..] : null;
                return index + ExecOption(owner, option, optName, attachedArg, args, index);
            }
            return index;
        }

        /// <summary>
        /// Verify the requirements of every owner on its parsed options, reporting all the violated ones
        /// </summary>
        public void ValidateRequirements(IIoService ioService)
        {
            var optionErrors = owners.SelectMany(o => o.Options.Requirements.SelectMany(
                r => r.ValidateOptions(o.Options, o.ParsedOptions))).ToArray();
            if (optionErrors.Length == 0)
                return;
            
            var errorStrBuilder = new StringBuilder();
            errorStrBuilder.AppendLine("Invalid options:");
            foreach (var error in optionErrors)
                errorStrBuilder.AppendLine("  " + error.Message);

            ioService.WriteError(errorStrBuilder.ToString());

            throw new ArgumentException("Errors with command options");
        }
        
        // Helpers.
        /// <summary>
        /// Run an option with its args, and save it as parsed
        /// </summary>
        /// <returns>Count of the following args consumed by the option</returns>
        private static int ExecOption(
            OptionsOwner owner,
            CommandOption option,
            string optName,
            string? attachedArg,
            string[] args,
            int followingArgsIndex)
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
            
            // Save on parsed options.
            owner.ParsedOptions.Add(new(option, optName, optArgs));
            
            return followingArgsCount;
        }

        private (OptionsOwner Owner, CommandOption Option) FindUnparsedOption(string optName)
        {
            foreach (var owner in owners)
            {
                // Find option by name.
                var option = owner.Definitions.FirstOrDefault(d => HasName(d, optName));
                if (option is null)
                    continue;
                
                // Verify duplicate options.
                if (owner.ParsedOptions.Any(p => HasName(p.Option, optName)))
                    throw new ArgumentException(optName + " option is duplicate");

                return (owner, option);
            }
            
            throw new ArgumentException(optName + " is not a valid option");
        }

        private static IEnumerable<string> GetOptionNames(CommandOption option) =>
            option.ShortName is null ? [option.LongName] : [option.ShortName, option.LongName];

        private static bool HasName(CommandOption option, string optName) =>
            option.ShortName == optName || option.LongName == optName;
    }
}