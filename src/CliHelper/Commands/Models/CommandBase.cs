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
    /// <summary>
    /// Base class of the commands. The name of a command is its type name without "Command", lowercased:
    /// <c>ResourceCommand</c> is <c>resource</c>. Commands are registered with
    /// <see cref="ICommandManagerConfiguration"/> and created by <see cref="ICommandManager"/>.
    /// </summary>
    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
    public abstract class CommandBase
    {
        // Fields.
        private ICommandManager _commandManager = null!;
        private CommandBase? resolvedSubCommand;

        // Properties.
        /// <summary>
        /// The args of the command in its usage help, after the options. "COMMAND" when it has sub commands,
        /// empty otherwise.
        /// </summary>
        public virtual string CommandArgsHelpString => HasSubCommands ? "COMMAND" : "";
        /// <summary>
        /// The manager that created the command, through which it navigates the command tree.
        /// </summary>
        public required ICommandManager CommandManager
        {
            get => _commandManager;
            set => _commandManager = value;
        }
        /// <summary>
        /// The names of the commands of the path, from the root to this command, space separated.
        /// </summary>
        public string CommandPathNames => string.Join(' ', CommandPathTypes.Select(GetCommandNameFromType));
        /// <summary>
        /// The types of the commands of the path, from the root to this command.
        /// </summary>
        public IEnumerable<Type> CommandPathTypes => CommandManager.GetCommandPathTypes(GetType());
        /// <summary>
        /// The usage of the command in its help: the path, then "OPTIONS" when a command of the path has options
        /// ("[OPTIONS]" when none of them is required), then <see cref="CommandArgsHelpString"/>.
        /// </summary>
        public string CommandPathUsageHelpString
        {
            get
            {
                var pathCommands = CommandPathTypes.Select(CommandManager.CreateCommand).ToArray();
                List<string> usageParts = [CommandPathNames];
                if (pathCommands.Any(c => c.HasOptions))
                    usageParts.Add(pathCommands.Any(c => c.HasRequiredOptions) ? "OPTIONS" : "[OPTIONS]");
                if (CommandArgsHelpString.Length != 0)
                    usageParts.Add(CommandArgsHelpString);
                return string.Join(' ', usageParts);
            }
        }
        /// <summary>
        /// The description of the command, in the help.
        /// </summary>
        public abstract string Description { get; }
        /// <summary>
        /// Whether the command defines options.
        /// </summary>
        public virtual bool HasOptions => false;
        /// <summary>
        /// Whether the command requires some of its options, with a
        /// <see cref="OptionRequirements.RequireOneOfOptionRequirement"/>.
        /// </summary>
        public virtual bool HasRequiredOptions => false;
        /// <summary>
        /// Whether the command has sub commands.
        /// </summary>
        public bool HasSubCommands => SubCommandTypes.Any();
        /// <summary>
        /// Whether the command is the root of the application: its help also explains how to get the help of
        /// a sub command. False by default.
        /// </summary>
        public virtual bool IsRootCommand => false;
        /// <summary>
        /// The name of the command: its type name without "Command", lowercased.
        /// </summary>
        public string Name => GetCommandNameFromType(GetType());
        /// <summary>
        /// Whether the command prints its help when nothing follows it in the args, instead of being executed.
        /// True by default.
        /// </summary>
        public virtual bool PrintHelpWithNoArgs => true;
        /// <summary>
        /// The types of the sub commands, in their registration order.
        /// </summary>
        public IEnumerable<Type> SubCommandTypes => CommandManager.GetCommandSubTypes(GetType());
        
        // Protected properties.
        /// <summary>
        /// The io service the command reads and writes through.
        /// </summary>
        protected IIoService IoService => CommandManager.IoService;
        
        // Internal properties.
        internal virtual CommandOptionsBase? CommandOptions => null;

        // Methods.
        /// <summary>
        /// Run the command. Args hold the sub commands to run, followed by the command args. The options of every
        /// command of the path can be anywhere after the command defining them, and before the command args.
        /// The options of the whole path are parsed and verified before any command is executed.
        /// </summary>
        /// <param name="args">Input args</param>
        /// <exception cref="ArgumentException">The args are not valid, like an unknown command or option, or they
        /// violate an option requirement. The message is meant for the user</exception>
        /// <exception cref="InvalidOperationException">A command of the path defines an option name already
        /// defined by one of its ancestors</exception>
        public async Task RunAsync(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            
            // Resolve the commands path, and parse the options of all its commands.
            var optionsParser = new OptionsParser();
            AddCommandOptions(optionsParser, this);
            var leafCommand = this;
            var leafArgsIndex = 0;
            var argsIndex = 0;
            while (argsIndex < args.Length)
            {
                var arg = args[argsIndex];
                
                // Option, or help request.
                if (arg.StartsWith('-'))
                {
                    if (arg is "-h" or "--help" && !optionsParser.IsDefined(arg))
                    {
                        leafCommand.PrintHelp();
                        return;
                    }
                    
                    argsIndex = optionsParser.ParseOption(args, argsIndex);
                    continue;
                }
                
                // Sub command, or begin of command args.
                var subCommandType = leafCommand.SubCommandTypes.FirstOrDefault(t => GetCommandNameFromType(t) == arg);
                if (subCommandType is null)
                    break;

                var subCommand = CommandManager.CreateCommand(subCommandType);
                AddCommandOptions(optionsParser, subCommand);
                leafCommand.resolvedSubCommand = subCommand;
                leafCommand = subCommand;
                leafArgsIndex = ++argsIndex;
            }
            
            // Print help when nothing follows the leaf command.
            if (leafArgsIndex == args.Length && leafCommand.PrintHelpWithNoArgs)
            {
                leafCommand.PrintHelp();
                return;
            }
            
            // Verify option requirements of the whole path, and execute it.
            optionsParser.ValidateRequirements(IoService);
            await ExecuteAsync(args[argsIndex..]).ConfigureAwait(false);
        }
        
        // Protected methods.
        /// <summary>
        /// Append the help of the options defined by the command. The options of its ancestors and the option
        /// requirements follow it in the help.
        /// </summary>
        /// <param name="strBuilder">The builder of the help</param>
        protected virtual void AppendOptionsHelp(StringBuilder strBuilder)
        {
            ArgumentNullException.ThrowIfNull(strBuilder);
            
            if (CommandOptions is not null)
                AppendOptionDefinitionsHelp(strBuilder, "Options:", CommandOptions);
        }
        
        /// <summary>
        /// Execute the command, after its path has been resolved and the options parsed. By default it executes
        /// the resolved sub command: an override calls <see cref="ExecuteSubCommandAsync"/> to continue the path.
        /// </summary>
        /// <param name="commandArgs">The args following the commands path, and its options</param>
        protected virtual async Task ExecuteAsync(string[] commandArgs)
        {
            ArgumentNullException.ThrowIfNull(commandArgs);
            await ExecuteSubCommandAsync(commandArgs).ConfigureAwait(false);
        }

        /// <summary>
        /// Execute the sub command resolved from the args.
        /// </summary>
        /// <param name="commandArgs">The args following the commands path, and its options</param>
        /// <exception cref="ArgumentException">No sub command has been resolved: the command args are empty,
        /// or begin with an unknown command</exception>
        protected async Task ExecuteSubCommandAsync(string[] commandArgs)
        {
            ArgumentNullException.ThrowIfNull(commandArgs);

            if (resolvedSubCommand is null)
                throw new ArgumentException(commandArgs.Length == 0 ?
                    "A command name is required" :
                    $"{CommandPathNames}: '{commandArgs[0]}' is not a valid command.");

            await resolvedSubCommand.ExecuteAsync(commandArgs).ConfigureAwait(false);
        }
        
        // Protected helpers.
        /// <summary>
        /// Get the name of a command from its type: the type name without "Command", lowercased.
        /// </summary>
        /// <param name="commandType">The command type</param>
        /// <returns>The command name</returns>
        /// <exception cref="ArgumentException">The type is not a command type</exception>
        protected static string GetCommandNameFromType(Type commandType)
        {
            ArgumentNullException.ThrowIfNull(commandType);
            
            if (!typeof(CommandBase).IsAssignableFrom(commandType))
                throw new ArgumentException($"{commandType.Name} is not a command type");

            return commandType.Name.Replace("Command", "", StringComparison.InvariantCulture).ToLowerInvariant();
        }
        
        // Helpers.
        private static void AddCommandOptions(OptionsParser optionsParser, CommandBase command)
        {
            if (command.CommandOptions is not null)
                optionsParser.AddOptions(command.CommandOptions, command.CommandPathNames);
        }
        
        private static void AppendOptionDefinitionsHelp(
            StringBuilder strBuilder,
            string header,
            CommandOptionsBase options)
        {
            var definitions = options.Definitions.ToArray();
            if (definitions.Length == 0) return;
            
            strBuilder.AppendLine(header);
            var descriptionShift = definitions.Select(opt =>
            {
                var len = opt.LongName.Length;
                foreach (var reqArgType in opt.RequiredArgTypes)
                    len += reqArgType.Name.Length + 1;
                return len;
            }).Max() + 4;
            foreach (var option in definitions)
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
        
            // Add options, own first, then inherited from the nearest ancestor.
            AppendOptionsHelp(strBuilder);
            var ancestorCommands = CommandPathTypes.Reverse().Skip(1).Select(CommandManager.CreateCommand).ToArray();
            foreach (var ancestorCommand in ancestorCommands)
                if (ancestorCommand.CommandOptions is not null)
                    AppendOptionDefinitionsHelp(
                        strBuilder,
                        $"Options of {ancestorCommand.CommandPathNames}:",
                        ancestorCommand.CommandOptions);
            
            // Add option requirements.
            var requirementHelpLines = ancestorCommands.Prepend(this)
                .Select(c => c.CommandOptions)
                .OfType<CommandOptionsBase>()
                .SelectMany(options => options.Requirements.Select(r => r.PrintHelpLine(options)))
                .ToArray();
            if (requirementHelpLines.Length != 0)
            {
                strBuilder.AppendLine("Option requirements:");
                foreach (var requirementHelpLine in requirementHelpLines)
                    strBuilder.AppendLine("  " + requirementHelpLine);
                strBuilder.AppendLine();
            }
        
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
    
    /// <summary>
    /// Base class of the commands with options.
    /// </summary>
    /// <typeparam name="TOptions">The options of the command</typeparam>
    public abstract class CommandBase<TOptions> : CommandBase
        where TOptions : CommandOptionsBase, new()
    {
        // Properties.
        /// <inheritdoc />
        public override bool HasOptions => true;
        /// <inheritdoc />
        public override bool HasRequiredOptions => Options.AreRequired;
        /// <summary>
        /// The options of the command, set from the args before it is executed.
        /// </summary>
        public TOptions Options { get; } = new();
        
        // Internal properties.
        internal override CommandOptionsBase CommandOptions => Options;
    }
}