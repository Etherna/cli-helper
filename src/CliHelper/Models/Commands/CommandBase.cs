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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.CliHelper.Models.Commands
{
    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
    public abstract class CommandBase
    {
        // Fields.
        private readonly Assembly assembly;
        private readonly IServiceProvider serviceProvider;
        private ImmutableArray<Type>? _availableSubCommandTypes;
        private ImmutableArray<Type>? _commandPathTypes;

        // Constructor.
        protected CommandBase(
            Assembly assembly,
            IIoService ioService,
            IServiceProvider serviceProvider)
        {
            IoService = ioService;
            this.assembly = assembly;
            this.serviceProvider = serviceProvider;
        }

        // Properties.
        public ImmutableArray<Type> AvailableSubCommandTypes
        {
            get
            {
                if (_availableSubCommandTypes is null)
                {
                    var subCommandsNamespace = GetType().Namespace + "." + GetType().Name.Replace("Command", "", StringComparison.InvariantCulture);
                    _availableSubCommandTypes = assembly.GetTypes()
                        .Where(t => t is {IsClass:true, IsAbstract: false} &&
                                    t.Namespace == subCommandsNamespace &&
                                    typeof(CommandBase).IsAssignableFrom(t))
                        .OrderBy(t => t.Name)
                        .ToImmutableArray();
                }
                return _availableSubCommandTypes.Value;
            }
        }
        public string CommandPathNames => string.Join(' ',
            CommandPathTypes.Select(cType => ((CommandBase)serviceProvider.GetRequiredService(cType)).Name));
        public ImmutableArray<Type> CommandPathTypes
        {
            get
            {
                if (_commandPathTypes is null)
                {
                    var currentCommandNamespace = GetType().Namespace;
                    if (currentCommandNamespace is null)
                        throw new InvalidOperationException();

                    _commandPathTypes = GetParentCommandTypesFromNamespace(currentCommandNamespace)
                        .Append(GetType()).ToImmutableArray();
                }
                return _commandPathTypes.Value;
            }
        }
        public virtual string CommandArgsHelpString => HasSubCommands ? "COMMAND" : "";
        public string CommandPathUsageHelpString
        {
            get
            {
                var strBuilder = new StringBuilder();
                foreach (var commandType in CommandPathTypes)
                {
                    var command = (CommandBase)serviceProvider.GetRequiredService(commandType);
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
        public bool HasSubCommands => AvailableSubCommandTypes.Any();
        public virtual bool IsRootCommand => false;
        public string Name => GetCommandNameFromType(GetType());
        public virtual bool PrintHelpWithNoArgs => true;
        
        // Protected properties.
        protected IIoService IoService { get; }
        
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

            var selectedCommandType = AvailableSubCommandTypes.FirstOrDefault(
                t => GetCommandNameFromType(t) == subCommandName);
            
            if (selectedCommandType is null)
                throw new ArgumentException($"{CommandPathNames}: '{subCommandName}' is not a valid command.");

            var selectedCommand = (CommandBase)serviceProvider.GetRequiredService(selectedCommandType);
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
        
        // Private helpers.
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

        private static IEnumerable<Type> GetParentCommandTypesFromNamespace(string currentNamespace)
        {
            var lastSeparatorIndex = currentNamespace.LastIndexOf('.');
            var parentNamespace = currentNamespace[..lastSeparatorIndex];
            var parentCommandName = currentNamespace[(lastSeparatorIndex + 1)..] + "Command";
            var parentCommandType = typeof(CommandBase).GetTypeInfo().Assembly.GetTypes()
                .FirstOrDefault(t => t is { IsClass: true, IsAbstract: false } &&
                                     t.FullName == parentNamespace + '.' + parentCommandName &&
                                     typeof(CommandBase).IsAssignableFrom(t));
            
            if (parentCommandType is null)
                return Array.Empty<Type>();
            return GetParentCommandTypesFromNamespace(parentNamespace).Append(parentCommandType);
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
            var availableSubCommandTypes = AvailableSubCommandTypes;
            if (availableSubCommandTypes.Any())
            {
                var allSubCommands = availableSubCommandTypes.Select(t => (CommandBase)serviceProvider.GetRequiredService(t));
                
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
        where TOptions: CommandOptionsBase, new()
    {
        // Constructor.
        protected CommandBase(
            Assembly assembly,
            IIoService ioService,
            IServiceProvider serviceProvider)
            : base(assembly, ioService, serviceProvider)
        { }
        
        // Properties.
        public override bool HasOptions => true;
        public override bool HasRequiredOptions => Options.AreRequired;
        public TOptions Options { get; } = new TOptions();
        
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