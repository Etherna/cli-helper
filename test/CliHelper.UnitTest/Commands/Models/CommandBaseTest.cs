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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Etherna.CliHelper.Commands.Models
{
    public class CommandBaseTest
    {
        // Internal classes.
        // The command tree:
        //   root      -k/--api-key string, -i/--ignore-update, --retries int (min 1)
        //   ├── resource
        //   │   ├── fund    -p/--pin, -t/--traffic (one required), RESOURCE_ID
        //   │   └── list    -a/--all, -h/--hidden
        //   ├── echo        runs with no args
        //   ├── index       -i/--index-filename string, conflicting with the root short name
        //   └── tag         -x/--api-key, conflicting with the root long name
        private sealed class ExecutionRecorder
        {
            public string[]? EchoArgs { get; set; }
            public string[]? FundArgs { get; set; }
            public bool FundPin { get; set; }
            public bool FundTraffic { get; set; }
            public bool ListAll { get; set; }
            public bool ListExecuted { get; set; }
            public bool ListHidden { get; set; }
            public string? RootApiKey { get; set; }
            public string[]? RootArgs { get; set; }
            public bool RootExecuted { get; set; }
            public bool RootIgnoreUpdate { get; set; }
        }

        private sealed class CapturingIoService : IIoService
        {
            private readonly StringBuilder error = new();
            private readonly StringBuilder output = new();

            public string Error => error.ToString();
            public string Output => output.ToString();

            public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
            public string? ReadLine() => null;
            public void Write(string? value) => output.Append(value);
            public void WriteError(string value) => error.Append(value);
            public void WriteErrorLine(string value) => error.AppendLine(value);
            public void WriteLine(string? value = null) => output.AppendLine(value);
        }

        private sealed class RootCommand(ExecutionRecorder recorder) : CommandBase<RootCommandOptions>
        {
            public override string Description => "Root command";
            public override bool IsRootCommand => true;

            // Reads its options before dispatching, like a root checking for updates.
            protected override async Task ExecuteAsync(string[] commandArgs)
            {
                recorder.RootArgs = commandArgs;
                recorder.RootExecuted = true;
                recorder.RootApiKey = Options.ApiKey;
                recorder.RootIgnoreUpdate = Options.IgnoreUpdate;
                await ExecuteSubCommandAsync(commandArgs);
            }
        }

        private sealed class RootCommandOptions : CommandOptionsBase
        {
            public string? ApiKey { get; private set; }
            public bool IgnoreUpdate { get; private set; }
            public int Retries { get; private set; } = 1;

            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-k", "--api-key", "Api key", args => ApiKey = args[0], [typeof(string)]),
                new("-i", "--ignore-update", "Ignore updates", _ => IgnoreUpdate = true),
                new(null, "--retries", "Retries", args => Retries = int.Parse(args[0], CultureInfo.InvariantCulture), [typeof(int)])
            ];

            public override IEnumerable<OptionRequirementBase> Requirements =>
            [
                new MinValueOptionRequirement("--retries", 1)
            ];
        }

        private sealed class ResourceCommand : CommandBase
        {
            public override string Description => "Manage resources";
        }

        private sealed class FundCommand(ExecutionRecorder recorder) : CommandBase<FundCommandOptions>
        {
            public override string CommandArgsHelpString => "RESOURCE_ID";
            public override string Description => "Fund a resource";

            protected override Task ExecuteAsync(string[] commandArgs)
            {
                recorder.FundArgs = commandArgs;
                recorder.FundPin = Options.Pin;
                recorder.FundTraffic = Options.Traffic;
                return Task.CompletedTask;
            }
        }

        private sealed class FundCommandOptions : CommandOptionsBase
        {
            public bool Pin { get; private set; }
            public bool Traffic { get; private set; }

            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-p", "--pin", "Fund pinning", _ => Pin = true),
                new("-t", "--traffic", "Fund traffic", _ => Traffic = true)
            ];

            public override IEnumerable<OptionRequirementBase> Requirements =>
            [
                new RequireOneOfOptionRequirement("-p", "-t")
            ];
        }

        private sealed class ListCommand(ExecutionRecorder recorder) : CommandBase<ListCommandOptions>
        {
            public override string Description => "List resources";

            protected override Task ExecuteAsync(string[] commandArgs)
            {
                recorder.ListExecuted = true;
                recorder.ListAll = Options.All;
                recorder.ListHidden = Options.Hidden;
                return Task.CompletedTask;
            }
        }

        private sealed class ListCommandOptions : CommandOptionsBase
        {
            public bool All { get; private set; }
            public bool Hidden { get; private set; }

            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-a", "--all", "List all", _ => All = true),
                new("-h", "--hidden", "List hidden", _ => Hidden = true)
            ];
        }

        private sealed class EchoCommand(ExecutionRecorder recorder) : CommandBase
        {
            public override string Description => "Echo args";
            public override bool PrintHelpWithNoArgs => false;

            protected override Task ExecuteAsync(string[] commandArgs)
            {
                recorder.EchoArgs = commandArgs;
                return Task.CompletedTask;
            }
        }

        private sealed class IndexCommand : CommandBase<IndexCommandOptions>
        {
            public override string Description => "Index things";

            protected override Task ExecuteAsync(string[] commandArgs) => Task.CompletedTask;
        }

        private sealed class IndexCommandOptions : CommandOptionsBase
        {
            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-i", "--index-filename", "Index file name", _ => { }, [typeof(string)])
            ];
        }

        private sealed class TagCommand : CommandBase<TagCommandOptions>
        {
            public override string Description => "Tag things";

            protected override Task ExecuteAsync(string[] commandArgs) => Task.CompletedTask;
        }

        private sealed class TagCommandOptions : CommandOptionsBase
        {
            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-x", "--api-key", "Tag api key", _ => { }, [typeof(string)])
            ];
        }

        private sealed record RunResult(
            ExecutionRecorder Recorder,
            CapturingIoService Io,
            Exception? Exception);

        // Consts.
        private static readonly string FundHelp = JoinLines(
            "root resource fund",
            "Fund a resource",
            "",
            "Usage:  root resource fund OPTIONS RESOURCE_ID",
            "",
            "Options:",
            "  -p, --pin        Fund pinning",
            "  -t, --traffic    Fund traffic",
            "",
            "Options of root:",
            "  -k, --api-key string    Api key",
            "  -i, --ignore-update     Ignore updates",
            "      --retries int32     Retries",
            "",
            "Option requirements:",
            "  --pin, --traffic at least one is required.",
            "  --retries has min value 1.",
            "",
            "Run 'root resource fund -h' or 'root resource fund --help' to print help.",
            "");

        private static readonly string ResourceHelp = JoinLines(
            "root resource",
            "Manage resources",
            "",
            "Usage:  root resource [OPTIONS] COMMAND",
            "",
            "Commands:",
            "  fund    Fund a resource",
            "  list    List resources",
            "",
            "Options of root:",
            "  -k, --api-key string    Api key",
            "  -i, --ignore-update     Ignore updates",
            "      --retries int32     Retries",
            "",
            "Option requirements:",
            "  --retries has min value 1.",
            "",
            "Run 'root resource -h' or 'root resource --help' to print help.",
            "");

        private static readonly string RootHelp = JoinLines(
            "root",
            "Root command",
            "",
            "Usage:  root [OPTIONS] COMMAND",
            "",
            "Commands:",
            "  resource    Manage resources",
            "  echo        Echo args",
            "  index       Index things",
            "  tag         Tag things",
            "",
            "Options:",
            "  -k, --api-key string    Api key",
            "  -i, --ignore-update     Ignore updates",
            "      --retries int32     Retries",
            "",
            "Option requirements:",
            "  --retries has min value 1.",
            "",
            "Run 'root -h' or 'root --help' to print help.",
            "Run 'root COMMAND -h' or 'root COMMAND --help' for more information on a command.",
            "");

        // Data.
        public static TheoryData<string[], string> HelpArgs => new()
        {
            { [], RootHelp },                                     //nothing follows the root
            { ["-h"], RootHelp },
            { ["--help"], RootHelp },
            { ["-k", "KEY", "-h"], RootHelp },                    //after options
            { ["resource"], ResourceHelp },                       //nothing follows an intermediate command
            { ["-i", "resource"], ResourceHelp },
            { ["resource", "-h"], ResourceHelp },
            { ["resource", "fund"], FundHelp },                   //nothing follows the leaf
            { ["resource", "fund", "--help"], FundHelp },
            { ["resource", "fund", "-p", "-h"], FundHelp },       //after options
            { ["resource", "-h", "fund", "-p"], ResourceHelp }    //help of the command reached so far
        };

        public static TheoryData<string[]> PathOptionsArgs => new()
        {
            new[] { "-k", "KEY", "-i", "resource", "fund", "-p", "RES_ID" }, //each one right after its command
            new[] { "resource", "fund", "-p", "-k", "KEY", "-i", "RES_ID" }, //all after the last command
            new[] { "resource", "-i", "fund", "-k", "KEY", "-p", "RES_ID" }, //mixed
            new[] { "resource", "fund", "-pi", "-kKEY", "RES_ID" },          //composed across commands
            new[] { "resource", "fund", "--api-key", "KEY", "--ignore-update", "--pin", "RES_ID" } //long names
        };

        public static TheoryData<string[], string> RequirementErrorArgs => new()
        {
            { ["-i", "resource", "fund", "RES_ID"], JoinLines(
                "Invalid options:",
                "  --pin, --traffic at least one is required.") },             //leaf requirement
            { ["--retries", "0", "resource", "fund", "-p", "RES_ID"], JoinLines(
                "Invalid options:",
                "  --retries has min value 1.") },                             //ancestor requirement
            { ["resource", "fund", "--retries", "0", "RES_ID"], JoinLines(
                "Invalid options:",
                "  --retries has min value 1.",
                "  --pin, --traffic at least one is required.") }              //both, root first
        };

        // Tests.
        [Theory]
        [MemberData(nameof(PathOptionsArgs))]
        public async Task RunAsyncParsesOptionsOfThePathAnywhereAfterTheirCommand(string[] args)
        {
            var result = await RunAsync(args);

            Assert.Null(result.Exception);
            Assert.True(result.Recorder.RootExecuted);
            Assert.Equal("KEY", result.Recorder.RootApiKey);
            Assert.True(result.Recorder.RootIgnoreUpdate);
            Assert.True(result.Recorder.FundPin);
            Assert.False(result.Recorder.FundTraffic);
            Assert.Equal(new[] { "RES_ID" }, result.Recorder.FundArgs);
            Assert.Equal(new[] { "RES_ID" }, result.Recorder.RootArgs); //every command of the path gets the command args
            Assert.Equal("", result.Io.Output);
        }

        [Fact]
        public async Task RunAsyncConsumesOptionArgsBeforeResolvingCommands()
        {
            var result = await RunAsync("-k", "resource", "resource", "fund", "-p", "RES_ID");

            Assert.Null(result.Exception);
            Assert.Equal("resource", result.Recorder.RootApiKey);
            Assert.True(result.Recorder.FundPin);
            Assert.Equal(new[] { "RES_ID" }, result.Recorder.FundArgs);
        }

        [Fact]
        public async Task RunAsyncKeepsOptionsAfterCommandArgsAsArgs()
        {
            var result = await RunAsync("resource", "fund", "-p", "RES_ID", "-k", "KEY");

            Assert.Null(result.Exception);
            Assert.Null(result.Recorder.RootApiKey);
            Assert.Equal(new[] { "RES_ID", "-k", "KEY" }, result.Recorder.FundArgs);
        }

        [Fact]
        public async Task RunAsyncExecutesALeafGivenOnlyOptions()
        {
            var result = await RunAsync("resource", "list", "-a");

            Assert.Null(result.Exception);
            Assert.True(result.Recorder.ListExecuted);
            Assert.True(result.Recorder.ListAll);
            Assert.Equal("", result.Io.Output);
        }

        [Theory]
        [InlineData(new[] { "echo" }, new string[0])]
        [InlineData(new[] { "-i", "echo", "a", "-b" }, new[] { "a", "-b" })]
        public async Task RunAsyncExecutesALeafNotPrintingHelpWithNoArgs(string[] args, string[] expectedArgs)
        {
            var result = await RunAsync(args);

            Assert.Null(result.Exception);
            Assert.Equal(expectedArgs, result.Recorder.EchoArgs);
            Assert.Equal("", result.Io.Output);
        }

        [Fact]
        public async Task RunAsyncParsesHelpNamesDefinedAsOptions()
        {
            var result = await RunAsync("resource", "list", "-h");

            Assert.Null(result.Exception);
            Assert.True(result.Recorder.ListExecuted);
            Assert.True(result.Recorder.ListHidden);
            Assert.Equal("", result.Io.Output);
        }

        [Fact]
        public async Task RunAsyncResolvesThePathFromTheCommandItRunsOn()
        {
            var result = await RunAsync<ResourceCommand>("fund", "-p", "RES_ID");

            Assert.Null(result.Exception);
            Assert.False(result.Recorder.RootExecuted);
            Assert.True(result.Recorder.FundPin);
            Assert.Equal(new[] { "RES_ID" }, result.Recorder.FundArgs);
        }

        [Fact]
        public async Task RunAsyncDoesNotParseOptionsOfCommandsOutOfThePath()
        {
            var result = await RunAsync<ResourceCommand>("fund", "-k", "KEY", "-p", "RES_ID");

            Assert.IsType<ArgumentException>(result.Exception);
            Assert.Equal("-k is not a valid option", result.Exception.Message);
        }

        [Theory]
        [MemberData(nameof(RequirementErrorArgs))]
        public async Task RunAsyncVerifiesRequirementsOfThePathBeforeExecutingAnyCommand(string[] args, string expectedError)
        {
            var result = await RunAsync(args);

            Assert.IsType<ArgumentException>(result.Exception);
            Assert.Equal("Errors with command options", result.Exception.Message);
            Assert.Equal(expectedError, result.Io.Error);
            Assert.False(result.Recorder.RootExecuted);
        }

        [Theory]
        [InlineData(new[] { "-p", "resource", "fund", "RES_ID" }, "-p is not a valid option")] //before its command
        [InlineData(new[] { "resource", "fund", "-x", "RES_ID" }, "-x is not a valid option")]
        [InlineData(new[] { "resource", "fund", "-p", "-k" }, "-k requires 1 args: string")]
        [InlineData(new[] { "-i", "resource", "fund", "-i", "-p", "RES_ID" }, "-i option is duplicate")]
        [InlineData(new[] { "-i", "resource", "fund", "--ignore-update", "-p", "RES_ID" }, "--ignore-update option is duplicate")]
        public async Task RunAsyncRejectsInvalidOptionsBeforeExecutingAnyCommand(string[] args, string expectedMessage)
        {
            var result = await RunAsync(args);

            Assert.IsType<ArgumentException>(result.Exception);
            Assert.Equal(expectedMessage, result.Exception.Message);
            Assert.False(result.Recorder.RootExecuted);
        }

        // A command with sub commands could handle the args itself, so an invalid sub command is found by
        // the dispatch, after the commands before it have been executed.
        [Theory]
        [InlineData(new[] { "-k", "KEY" }, "A command name is required")]
        [InlineData(new[] { "unknown" }, "root: 'unknown' is not a valid command.")]
        [InlineData(new[] { "resource", "unknown" }, "root resource: 'unknown' is not a valid command.")]
        public async Task RunAsyncRejectsInvalidSubCommandsOnDispatch(string[] args, string expectedMessage)
        {
            var result = await RunAsync(args);

            Assert.IsType<ArgumentException>(result.Exception);
            Assert.Equal(expectedMessage, result.Exception.Message);
        }

        [Theory]
        [InlineData(new[] { "index", "-i", "file" }, "Option -i of command 'root index' is already defined by command 'root'")]
        [InlineData(new[] { "tag" }, "Option --api-key of command 'root tag' is already defined by command 'root'")]
        public async Task RunAsyncRejectsConflictingOptionsOnThePath(string[] args, string expectedMessage)
        {
            var result = await RunAsync(args);

            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.Equal(expectedMessage, result.Exception.Message);
            Assert.False(result.Recorder.RootExecuted);
        }

        [Theory]
        [MemberData(nameof(HelpArgs))]
        public async Task RunAsyncPrintsHelpOfTheCommandReached(string[] args, string expectedHelp)
        {
            var result = await RunAsync(args);

            Assert.Null(result.Exception);
            Assert.False(result.Recorder.RootExecuted);
            Assert.Equal(expectedHelp, result.Io.Output);
        }

        [Fact]
        public async Task RunAsyncPrintsHelpOfALeafDefiningHelpNamesAsOptions()
        {
            var result = await RunAsync("resource", "list", "--help");

            Assert.Null(result.Exception);
            Assert.False(result.Recorder.ListExecuted);
            Assert.StartsWith(JoinLines("root resource list", "List resources"), result.Io.Output, StringComparison.Ordinal);
        }

        // Helpers.
        private static string JoinLines(params string[] lines) =>
            string.Join(Environment.NewLine, lines) + Environment.NewLine;

        private static Task<RunResult> RunAsync(params string[] args) =>
            RunAsync<RootCommand>(args);

        private static async Task<RunResult> RunAsync<TCommand>(params string[] args)
            where TCommand : CommandBase
        {
            var services = new ServiceCollection();
            services.AddSingleton<ExecutionRecorder>();
            services.AddCliHelper<CapturingIoService>()
                .AddCommand<RootCommand>(root => root
                    .AddCommand<ResourceCommand>(resource => resource
                        .AddCommand<FundCommand>()
                        .AddCommand<ListCommand>())
                    .AddCommand<EchoCommand>()
                    .AddCommand<IndexCommand>()
                    .AddCommand<TagCommand>());

            await using var provider = services.BuildServiceProvider();
            var commandManager = provider.GetRequiredService<ICommandManager>();
            var recorder = provider.GetRequiredService<ExecutionRecorder>();
            var io = (CapturingIoService)commandManager.IoService;

            try
            {
                await commandManager.CreateCommand<TCommand>().RunAsync(args);
                return new RunResult(recorder, io, null);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return new RunResult(recorder, io, ex);
            }
        }
    }
}
