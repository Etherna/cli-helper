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

using Etherna.CliHelper.Commands.Models;
using Etherna.CliHelper.Commands.Models.OptionRequirements;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Etherna.CliHelper.AotCompatibility
{
    // The command tree the probe runs:
    //   root
    //   ├── greet   (options, with requirements)
    //   └── tools
    //       └── echo   (constructor dependency resolved by the container)
    // None of these types is ever instantiated directly: the library creates them by reflection
    // over their constructors, the path that breaks first when trimming removes metadata.

    internal sealed class RootCommand : CommandBase
    {
        public override string Description => "AOT probe root command";
        public override bool IsRootCommand => true;
    }

    internal sealed class GreetCommand : CommandBase<GreetCommandOptions>
    {
        public override string Description => "Greet someone";

        protected override Task ExecuteAsync(string[] commandArgs)
        {
            for (var i = 0; i < Options.Times; i++)
                IoService.WriteLine($"Hello {Options.Name}!");
            return Task.CompletedTask;
        }
    }

    internal sealed class GreetCommandOptions : CommandOptionsBase
    {
        public string? Name { get; private set; }
        public int Times { get; private set; } = 1;

        public override IEnumerable<CommandOption> Definitions =>
        [
            new("-n", "--name", "Name to greet", args => Name = args[0], [typeof(string)]),
            new(null, "--times", "How many times to greet", args => Times = int.Parse(args[0], CultureInfo.InvariantCulture), [typeof(int)])
        ];

        public override IEnumerable<OptionRequirementBase> Requirements =>
        [
            new RequireOneOfOptionRequirement("--name"),
            new MinValueOptionRequirement("--times", 1)
        ];
    }

    internal sealed class ToolsCommand : CommandBase
    {
        public override string Description => "Group of tools";
    }

    internal sealed class EchoCommand(EchoFormatter formatter) : CommandBase
    {
        public override string Description => "Echo the arguments";
        public override bool PrintHelpWithNoArgs => false;

        protected override Task ExecuteAsync(string[] commandArgs)
        {
            IoService.WriteLine($"{CommandPathNames}: {formatter.Format(commandArgs)}");
            return Task.CompletedTask;
        }
    }

    internal sealed class EchoFormatter
    {
        public string Format(string[] args) => string.Join(' ', args);
    }
}
