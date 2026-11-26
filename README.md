# Cli Helper

[![Etherna.CliHelper on NuGet](https://img.shields.io/nuget/v/Etherna.CliHelper?label=Etherna.CliHelper)](https://www.nuget.org/packages/Etherna.CliHelper/)
[![Target frameworks](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](#supported-frameworks)
[![License: LGPL-3.0](https://img.shields.io/badge/license-LGPL--3.0-blue)](https://github.com/Etherna/cli-helper/blob/main/COPYING.LESSER)

**Cli Helper** is a .NET library to build command line applications as a tree of commands. Every command
is a class, registered with its sub commands in the dependency injection container: the library resolves
the path of commands the args select, parses the options of every command of the path, verifies their
requirements, and generates the help and the usage from the commands themselves.

Cli Helper is a **library**, not an application. It is the command line layer of the Etherna command line
tools, like the [Etherna Gateway CLI](https://github.com/Etherna/etherna-gateway-cli).

## Contents

- [Features](#features)
- [Packages](#packages)
- [Installation](#installation)
- [Usage examples](#usage-examples)
  - [Define the commands](#define-the-commands)
  - [Options and requirements](#options-and-requirements)
  - [Register the command tree and run it](#register-the-command-tree-and-run-it)
  - [Read root options before building the container](#read-root-options-before-building-the-container)
- [Supported frameworks](#supported-frameworks)
- [Building and testing](#building-and-testing)
- [Project layout](#project-layout)
- [Package repositories](#package-repositories)
- [Contributing](#contributing)
- [Issue reports](#issue-reports)
- [Questions? Problems?](#questions-problems)
- [License](#license)

## Features

- **Commands registered in the container** — a command is a class deriving from `CommandBase`, named after
  its type without `Command`, lowercased (`GreetCommand` is `greet`). The tree is declared once with
  `AddCliHelper`, and the container builds every command with its constructor dependencies.
- **Nested sub commands** — commands group sub commands at any depth (`tool clock now`): a command with
  sub commands dispatches to the one the args select.
- **POSIX style options** — short and long names (`-n`, `--name`), composed short options (`-sn Ada` is
  `-s -n Ada`), and the options of every command of the path accepted anywhere after the command defining
  them, before the command args.
- **Declarative option requirements** — required options, mutually exclusive and forbidden options,
  numeric bounds, and requirements applied only when another option is present. Every violated
  requirement is reported at once, before any command runs.
- **Generated help** — `-h` and `--help` print the description, the usage, the sub commands, the options
  (own and of the ancestors) and the option requirements of a command, with no hand-written help text.
- **Console I/O behind `IIoService`** — commands read and write through an abstraction, implemented on
  the console by `ConsoleIoService`, so a test can capture what they print.
- **Native AOT compatible** — the library is trimming and Native AOT safe, verified on every pull request
  by publishing a native probe application and running it.

## Packages

One NuGet package is published:

| Package | Use it for | Depends on |
| --- | --- | --- |
| [**Etherna.CliHelper**](https://www.nuget.org/packages/Etherna.CliHelper/) | Command line applications built as a tree of commands, with options, requirements and generated help. | `Microsoft.Extensions.DependencyInjection.Abstractions` |

## Installation

```bash
dotnet add package Etherna.CliHelper

# The library registers on the abstractions: the application brings the container.
dotnet add package Microsoft.Extensions.DependencyInjection
```

## Usage examples

The examples build `tool`, a small application with this command tree:

```
tool              -p/--profile string
├── clock
│   └── now
└── greet         -n/--name string (required), -s/--shout, --times int32 (1 to 10)
```

### Define the commands

A command overrides `Description`, shown in the help. A command with sub commands runs nothing by itself:
the default `ExecuteAsync` dispatches to the sub command the args select. A leaf command overrides
`ExecuteAsync`, and writes through `IoService`. By default a command followed by nothing in the args
prints its help: a command that runs with no args sets `PrintHelpWithNoArgs` to false. `IsRootCommand`
adds to the help of the root how to get the help of a sub command.

```csharp
using Etherna.CliHelper.Commands.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// tool: the root command. It runs nothing itself, it dispatches to its sub commands.
public sealed class ToolCommand : CommandBase<ToolCommandOptions>
{
    public override string Description => "A sample command line tool";
    public override bool IsRootCommand => true;
}

public sealed class ToolCommandOptions : CommandOptionsBase
{
    public override IEnumerable<CommandOption> Definitions =>
    [
        new("-p", "--profile", "Settings profile to use", args => Profile = args[0], [typeof(string)])
    ];
    public string Profile { get; private set; } = "default";
}

// tool clock: a group of commands.
public sealed class ClockCommand : CommandBase
{
    public override string Description => "Tell the time";
}

// tool clock now: built by the container, with its constructor dependencies.
public sealed class NowCommand(TimeProvider timeProvider) : CommandBase
{
    public override string Description => "Print the current UTC time";
    public override bool PrintHelpWithNoArgs => false;

    protected override Task ExecuteAsync(string[] commandArgs)
    {
        IoService.WriteLine($"{timeProvider.GetUtcNow():u}");
        return Task.CompletedTask;
    }
}
```

A command overriding `ExecuteAsync` on a path that goes on, like a root checking something before its
sub commands run, calls `ExecuteSubCommandAsync(commandArgs)` to continue it.

### Options and requirements

The options of a command are a `CommandOptionsBase`, given to the command as the type argument of
`CommandBase<TOptions>`. Every `CommandOption` has an optional short name, a long name, a description,
the action invoked with its args when it is found, and the types of the args it requires, shown in the
help. The args reach the action as strings: an action throws `ArgumentException` for a value it can't
convert, which reaches the user like the errors of the library.

The requirements are verified after the options of the whole path are parsed, and before any command
runs: `RequireOneOfOptionRequirement`, `ExclusiveOptionRequirement`, `ForbiddenOptionRequirement`,
`MinValueOptionRequirement`, `MaxValueOptionRequirement`, `RangeOptionRequirement`, and
`IfPresentThenOptionRequirement`, applying another requirement only when an option is present.

```csharp
using Etherna.CliHelper.Commands.Models;
using Etherna.CliHelper.Commands.Models.OptionRequirements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

public sealed class GreetCommandOptions : CommandOptionsBase
{
    public override IEnumerable<CommandOption> Definitions =>
    [
        new("-n", "--name", "Who to greet", args => Name = args[0], [typeof(string)]),
        new("-s", "--shout", "Greet in uppercase", _ => Shout = true),
        new(null, "--times", "How many times to greet", args => Times = ParseCount(args[0]), [typeof(int)])
    ];
    public string? Name { get; private set; }
    public override IEnumerable<OptionRequirementBase> Requirements =>
    [
        new RequireOneOfOptionRequirement("--name"),
        new RangeOptionRequirement("--times", 1, 10)
    ];
    public bool Shout { get; private set; }
    public int Times { get; private set; } = 1;

    private static int ParseCount(string arg) =>
        int.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
            ? count
            : throw new ArgumentException($"--times takes a whole number, not '{arg}'");
}

public sealed class GreetCommand : CommandBase<GreetCommandOptions>
{
    public override string Description => "Greet someone";

    protected override Task ExecuteAsync(string[] commandArgs)
    {
        var greeting = $"Hello {Options.Name}!";
        for (var i = 0; i < Options.Times; i++)
            IoService.WriteLine(Options.Shout ? greeting.ToUpperInvariant() : greeting);
        return Task.CompletedTask;
    }
}
```

The help of the command is generated from these definitions, with the options of its ancestors:

```text
$ tool greet --help
tool greet
Greet someone

Usage:  tool greet OPTIONS

Options:
  -n, --name string    Who to greet
  -s, --shout          Greet in uppercase
      --times int32    How many times to greet

Options of tool:
  -p, --profile string    Settings profile to use

Option requirements:
  --name is required.
  --times has value in range [1, 10].

Run 'tool greet -h' or 'tool greet --help' to print help.
```

### Register the command tree and run it

`AddCliHelper` registers the command manager with the io service, and returns the configuration of the
root commands: `AddCommand` adds a command, with a delegate adding its sub commands, and chains to add
more commands at the same level. Then the root command is resolved from the container and run on the
args.

```csharp
using Etherna.CliHelper;
using Etherna.CliHelper.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

var services = new ServiceCollection();
services.AddSingleton(TimeProvider.System);
services.AddCliHelper<ConsoleIoService>()
    .AddCommand<ToolCommand>(tool => tool
        .AddCommand<ClockCommand>(clock => clock
            .AddCommand<NowCommand>())
        .AddCommand<GreetCommand>());

await using var serviceProvider = services.BuildServiceProvider();
var ioService = serviceProvider.GetRequiredService<IIoService>();

try
{
    await serviceProvider.GetRequiredService<ToolCommand>().RunAsync(args);
    return 0;
}
catch (ArgumentException e)
{
    ioService.WriteErrorLine(e.Message);
    return 1;
}
```

A mistake of the user throws `ArgumentException`, with a message meant for the user: an unknown command
or option, a duplicated option, missing option args, a violated requirement. The violated requirements,
all of them, are written through `IIoService.WriteError` before it. A mistake of the application, like
two commands of a path defining the same option name, throws `InvalidOperationException` instead.

```text
$ tool
tool
A sample command line tool

Usage:  tool [OPTIONS] COMMAND

Commands:
  clock    Tell the time
  greet    Greet someone

Options:
  -p, --profile string    Settings profile to use

Run 'tool -h' or 'tool --help' to print help.
Run 'tool COMMAND -h' or 'tool COMMAND --help' for more information on a command.

$ tool greet -sn Ada
HELLO ADA!
$ tool clock now -p work
2026-11-26 17:16:42Z
$ tool greet --times 0
Invalid options:
  --name is required.
  --times has value in range [1, 10].
Errors with command options
$ tool clock tomorrow
tool clock: 'tomorrow' is not a valid command.
```

### Read root options before building the container

When a service registration depends on a root option, `CommandOptionsBase.ParseArgs` reads it before the
container exists. It parses only the options at the start of the args, against those options alone, and
verifies their requirements: a root option needed here goes right after the program name
(`tool -p work clock now`), while `RunAsync` accepts it anywhere after the root. Anything else at the
start, like a help request, throws `ArgumentException`: ignore it there, since `RunAsync` parses the
args again and reports the real mistakes.

```csharp
using Etherna.CliHelper;
using Etherna.CliHelper.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

// Read the profile before building the container, which needs it for a registration.
var rootOptions = new ToolCommandOptions();
try
{
    rootOptions.ParseArgs(args, new ConsoleIoService());
}
catch (ArgumentException)
{
    // Something else than a root option at the start, like a help request:
    // RunAsync parses the args again, and reports the real mistakes.
}

var services = new ServiceCollection();
services.AddSingleton(new ToolSettings(rootOptions.Profile));
services.AddSingleton(TimeProvider.System);
services.AddCliHelper<ConsoleIoService>()
    .AddCommand<ToolCommand>(tool => tool
        .AddCommand<ClockCommand>(clock => clock
            .AddCommand<NowCommand>())
        .AddCommand<GreetCommand>());

await using var serviceProvider = services.BuildServiceProvider();
var ioService = serviceProvider.GetRequiredService<IIoService>();

try
{
    await serviceProvider.GetRequiredService<ToolCommand>().RunAsync(args);
    return 0;
}
catch (ArgumentException e)
{
    ioService.WriteErrorLine(e.Message);
    return 1;
}

public sealed record ToolSettings(string Profile);
```

The commands then receive the settings through their constructors, like `NowCommand` receives the
`TimeProvider`.

## Supported frameworks

The library multi-targets **.NET 8, 9 and 10**. Install it into any project on a compatible framework.

## Building and testing

Cli Helper builds with the standard .NET SDK:

```bash
dotnet restore CliHelper.sln
dotnet build   CliHelper.sln -c Release   # compiles every target framework
dotnet test    CliHelper.sln -c Release   # runs the xUnit test project

# Native AOT gate: publish the probe, then run its native binary
dotnet publish test/CliHelper.AotCompatibility/CliHelper.AotCompatibility.csproj -c Release -r linux-x64
./test/CliHelper.AotCompatibility/bin/Release/net10.0/linux-x64/publish/CliHelper.AotCompatibility
```

`TreatWarningsAsErrors=true` and `AnalysisMode=AllEnabledByDefault` are enabled on the library, so
warnings break the build on every target framework. The AOT publish fails on any trim or AOT warning.

Coding conventions and architecture notes live in [AGENTS.md](https://github.com/Etherna/cli-helper/blob/main/AGENTS.md).

## Project layout

```
src/
  CliHelper                    the library: commands, options, requirements, io service
test/
  CliHelper.UnitTest           xUnit + Moq unit tests
  CliHelper.AotCompatibility   Native AOT probe, published and run by CI
```

## Package repositories

You can get the latest public releases from the [NuGet.org feed](https://www.nuget.org/profiles/etherna).

If you'd like to work with the latest internal releases, you can use our
[Etherna NuGet feed](https://nuget.etherna.io/v3/index.json) (NuGet V3).

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](https://github.com/Etherna/cli-helper/blob/main/CONTRIBUTING.md)
and our [Code of Conduct](https://github.com/Etherna/cli-helper/blob/main/CODE_OF_CONDUCT.md) before opening a
pull request.

## Issue reports

If you've discovered a bug, or have an idea for a new feature, please report it to our issue manager based on Jira https://etherna.atlassian.net/projects/CLIH.

Detailed reports with stack traces, actual and expected behaviours are welcome.

For a security problem, don't open a public issue: follow the [security policy](https://github.com/Etherna/cli-helper/security/policy) to report it privately.

## Questions? Problems?

For questions or problems please write an email to [info@etherna.io](mailto:info@etherna.io).

## License

![LGPL Logo](https://raw.githubusercontent.com/Etherna/cli-helper/main/doc/lgplv3-with-text-154x68.png)

We use the GNU Lesser General Public License v3 or later (SPDX `LGPL-3.0-or-later`) for this project:
[COPYING.LESSER](https://github.com/Etherna/cli-helper/blob/main/COPYING.LESSER) adds the lesser terms to the GNU GPL v3 of [COPYING](https://github.com/Etherna/cli-helper/blob/main/COPYING).
If you require a custom license, you can contact us at [license@etherna.io](mailto:license@etherna.io).
