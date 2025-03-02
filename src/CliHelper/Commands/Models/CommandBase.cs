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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.CliHelper.Commands.Models
{
    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
    public abstract class CommandBase
    {
        // Fields.
        private ICommandManager _commandManager = null!;

        // Properties.
        public required ICommandManager CommandManager
        {
            get => _commandManager;
            set => _commandManager = value;
        }
        public string CommandPathNames => string.Join(' ',
            CommandPathTypes.Select(cType => CommandManager.CreateCommand(cType).Name));
        public IEnumerable<Type> CommandPathTypes => CommandManager.GetCommandPathTypes(GetType());
        public virtual string CommandArgsHelpString => HasSubCommands ? "COMMAND" : "";
        public string CommandPathUsageHelpString
        {
            get
            {
                var strBuilder = new StringBuilder();
                foreach (var commandType in CommandPathTypes)
                {
                    var command = CommandManager.CreateCommand(commandType);
                    strBuilder.Append(command.Name);
                    if (command.HasOptions)
                    {
                        strBuilder.Append(command.HasRequiredOptions ?
                            $" {command.Name.ToUpperInvariant()}_OPTIONS" :    
                            $" [{command.Name.ToUpperInvariant()}_OPTIONS]");
                    }

                    strBuilder.Append(' ');
                }
                strBuilder.Append(CommandArgsHelpString);
                return strBuilder.ToString();
            }
        }
        public abstract string Description { get; }
        public virtual bool HasOptions => false;
        public virtual bool HasRequiredOptions => false;
        public bool HasSubCommands => SubCommandTypes.Any();
        public virtual bool IsRootCommand => false;
        public string Name => GetCommandNameFromType(GetType());
        public virtual bool PrintHelpWithNoArgs => true;
        public IEnumerable<Type> SubCommandTypes => CommandManager.GetCommandSubTypes(GetType());
        
        // Protected properties.
        protected IIoService IoService => CommandManager.IoService;

        // Public methods.
        public async Task RunAsync(string[] args)
        {
            // Parse arguments.
            var printHelp = EvaluatePrintHelp(args);
            var optionArgsCount = printHelp ? 0 : ParseOptionArgs(args);
            
            // Print help or run command.
            if (printHelp)
                PrintHelp();
            else
                await ExecuteAsync(args[optionArgsCount..]).ConfigureAwait(false);
        }
        
        // Protected methods.
        protected virtual void AppendOptionsHelp(StringBuilder strBuilder) { }
        
        /// <summary>
        /// Parse command options
        /// </summary>
        /// <param name="args">Input args</param>
        /// <returns>Found option args counter</returns>
        protected virtual int ParseOptionArgs(string[] args) => 0;
        
        protected virtual async Task ExecuteAsync(string[] commandArgs)
        {
            ArgumentNullException.ThrowIfNull(commandArgs, nameof(commandArgs));
            await ExecuteSubCommandAsync(commandArgs).ConfigureAwait(false);
        }

        protected async Task ExecuteSubCommandAsync(string[] commandArgs)
        {
            ArgumentNullException.ThrowIfNull(commandArgs, nameof(commandArgs));

            if (commandArgs.Length == 0)
                throw new ArgumentException("A command name is required");
            
            var subCommandName = commandArgs[0];
            var subCommandArgs = commandArgs[1..];

            var selectedCommandType = SubCommandTypes.FirstOrDefault(
                t => GetCommandNameFromType(t) == subCommandName);
            
            if (selectedCommandType is null)
                throw new ArgumentException($"{CommandPathNames}: '{subCommandName}' is not a valid command.");

            var selectedCommand = CommandManager.CreateCommand(selectedCommandType);
            await selectedCommand.RunAsync(subCommandArgs).ConfigureAwait(false);
        }
        
        // Protected helpers.
        protected static string GetCommandNameFromType(Type commandType)
        {
            ArgumentNullException.ThrowIfNull(commandType, nameof(commandType));
            
            if (!typeof(CommandBase).IsAssignableFrom(commandType))
                throw new ArgumentException($"{commandType.Name} is not a command type");

            return commandType.Name.Replace("Command", "", StringComparison.InvariantCulture).ToLowerInvariant();
        }
        
        // Helpers.
        private bool EvaluatePrintHelp(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args, nameof(args));
            
            switch (args.Length)
            {
                case 0 when PrintHelpWithNoArgs:
                    return true;
                case 1:
                    switch (args[0])
                    {
                        case "-h":
                        case "--help":
                            return true;
                    }
                    break;
            }
            return false;
        }

        [SuppressMessage("Performance", "CA1851:Possible multiple enumerations of \'IEnumerable\' collection")]
        private void PrintHelp()
        {
            var strBuilder = new StringBuilder();
            
            // Add name and description.
            strBuilder.AppendLine(CommandPathNames);
            strBuilder.AppendLine(Description);
            strBuilder.AppendLine();

            // Add usage.
            strBuilder.AppendLine($"Usage:  {CommandPathUsageHelpString}");
            strBuilder.AppendLine();
        
            // Add sub commands.
            if (SubCommandTypes.Any())
            {
                var allSubCommands = SubCommandTypes.Select(CommandManager.CreateCommand);
                
                strBuilder.AppendLine("Commands:");
                var descriptionShift = allSubCommands.Select(c => c.Name.Length).Max() + 4;
                foreach (var command in allSubCommands)
                {
                    strBuilder.Append("  ");
                    strBuilder.Append(command.Name);
                    for (int i = 0; i < descriptionShift - command.Name.Length; i++)
                        strBuilder.Append(' ');
                    strBuilder.AppendLine(command.Description);
                }
                strBuilder.AppendLine();
            }
        
            // Add options.
            AppendOptionsHelp(strBuilder);
        
            // Add print help.
            strBuilder.AppendLine($"Run '{CommandPathNames} -h' or '{CommandPathNames} --help' to print help.");
            if (IsRootCommand)
                strBuilder.AppendLine($"Run '{CommandPathNames} COMMAND -h' or '{CommandPathNames} COMMAND --help' for more information on a command.");
            strBuilder.AppendLine();
        
            // Print it.
            var helpOutput = strBuilder.ToString();
            IoService.Write(helpOutput);
        }
    }
    
    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
    public abstract class CommandBase<TOptions> : CommandBase
        where TOptions : CommandOptionsBase, new()
    {
        // Properties.
        public override bool HasOptions => true;
        public override bool HasRequiredOptions => Options.AreRequired;
        public TOptions Options { get; } = new();
        
        // Methods.
        protected override int ParseOptionArgs(string[] args) => Options.ParseArgs(args, IoService);

        protected override void AppendOptionsHelp(StringBuilder strBuilder)
        {
            ArgumentNullException.ThrowIfNull(strBuilder, nameof(strBuilder));

            if (!Options.Definitions.Any()) return;
            
            // Option descriptions.
            strBuilder.AppendLine("Options:");
            var descriptionShift = Options.Definitions.Select(opt =>
            {
                var len = opt.LongName.Length;
                foreach (var reqArgType in opt.RequiredArgTypes)
                    len += reqArgType.Name.Length + 1;
                return len;
            }).Max() + 4;
            foreach (var option in Options.Definitions)
            {
                strBuilder.Append("  ");
                strBuilder.Append(option.ShortName is null ? "    " : $"{option.ShortName}, ");
                strBuilder.Append(option.LongName);
                var strLen = option.LongName.Length;
                foreach (var reqArgType in option.RequiredArgTypes)
                {
                    strBuilder.Append($" {reqArgType.Name.ToLower(CultureInfo.InvariantCulture)}");
                    strLen += reqArgType.Name.Length + 1;
                }
                for (int i = 0; i < descriptionShift - strLen; i++)
                    strBuilder.Append(' ');
                strBuilder.AppendLine(option.Description);
            }
            strBuilder.AppendLine();
                
            // Requirements.
            if (Options.Requirements.Any())
            {
                strBuilder.AppendLine("Option requirements:");
                foreach (var requirement in Options.Requirements)
                    strBuilder.AppendLine("  " + requirement.PrintHelpLine(Options));
                strBuilder.AppendLine();
            }
        }
    }
}