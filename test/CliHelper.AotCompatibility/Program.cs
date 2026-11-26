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

using Etherna.CliHelper.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Etherna.CliHelper.AotCompatibility
{
    // Native AOT compatibility probe for CliHelper. The actual compatibility gate is the ILC publish
    // (see the .csproj and CI): publishing this app fails on any trim/AOT warning.
    // This entry point additionally proves, at runtime, that the AOT-sensitive paths work once compiled
    // ahead-of-time: the container building the io service and every command by reflection over their
    // constructors (dependencies included), and command names derived from the command type names.
    internal static class Program
    {
        // Methods.
        public static async Task<int> Main()
        {
            var failures =
                await CheckAsync("help", VerifyRootHelpAsync).ConfigureAwait(false) +
                await CheckAsync("options", VerifyOptionsAsync).ConfigureAwait(false) +
                await CheckAsync("options-help", VerifyOptionsHelpAsync).ConfigureAwait(false) +
                await CheckAsync("option-requirements", VerifyOptionRequirementsAsync).ConfigureAwait(false) +
                await CheckAsync("sub-commands", VerifySubCommandWithDependencyAsync).ConfigureAwait(false) +
                await CheckAsync("invalid-command", VerifyInvalidCommandAsync).ConfigureAwait(false);

            Console.WriteLine(failures == 0 ? "AOT smoke test PASSED" : $"AOT smoke test FAILED ({failures})");
            return failures;
        }

        // Checks.
        private static async Task<string> VerifyRootHelpAsync()
        {
            var (io, exception) = await RunAsync().ConfigureAwait(false);
            ThrowIfFailed(exception);

            Require(io.Output, "Usage:  root COMMAND");
            Require(io.Output, "  greet    Greet someone");
            Require(io.Output, "  tools    Group of tools");
            Require(io.Output, "Run 'root COMMAND -h'");

            return "printed root help, listing the sub commands";
        }

        private static async Task<string> VerifyOptionsAsync()
        {
            var (io, exception) = await RunAsync("greet", "--name", "aot", "--times", "2").ConfigureAwait(false);
            ThrowIfFailed(exception);

            var expected = $"Hello aot!{Environment.NewLine}Hello aot!{Environment.NewLine}";
            if (io.Output != expected)
                throw new InvalidOperationException($"Unexpected output \"{io.Output}\".");

            return "parsed options and ran the command";
        }

        private static async Task<string> VerifyOptionsHelpAsync()
        {
            var (io, exception) = await RunAsync("greet", "--help").ConfigureAwait(false);
            ThrowIfFailed(exception);

            Require(io.Output, "Usage:  root greet OPTIONS");
            Require(io.Output, "-n, --name string");
            Require(io.Output, "    --times int32");
            Require(io.Output, "--name is required.");
            Require(io.Output, "--times has min value 1.");

            return "printed options help, with arguments and requirements";
        }

        private static async Task<string> VerifyOptionRequirementsAsync()
        {
            var (io, exception) = await RunAsync("greet", "--times", "0").ConfigureAwait(false);
            if (exception is not ArgumentException { Message: "Errors with command options" })
                throw new InvalidOperationException("Invalid options were accepted.", exception);

            Require(io.Error, "--name is required.");
            Require(io.Error, "--times has min value 1.");

            return "rejected invalid options, reporting every requirement violated";
        }

        private static async Task<string> VerifySubCommandWithDependencyAsync()
        {
            var (io, exception) = await RunAsync("tools", "echo", "a", "b").ConfigureAwait(false);
            ThrowIfFailed(exception);

            var expected = $"root tools echo: a b{Environment.NewLine}";
            if (io.Output != expected)
                throw new InvalidOperationException($"Unexpected output \"{io.Output}\".");

            return "dispatched a nested command built with a constructor dependency";
        }

        private static async Task<string> VerifyInvalidCommandAsync()
        {
            var (_, exception) = await RunAsync("unknown").ConfigureAwait(false);
            if (exception is not ArgumentException { Message: "root: 'unknown' is not a valid command." })
                throw new InvalidOperationException("Unknown command was not rejected.", exception);

            return "rejected an unknown command";
        }

        // Helpers.
        private static async Task<int> CheckAsync(string name, Func<Task<string>> check)
        {
            try
            {
                var result = await check().ConfigureAwait(false);
                Console.WriteLine($"[ok] {name}: {result}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] {name}: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        private static void Require(string text, string expected)
        {
            if (!text.Contains(expected, StringComparison.Ordinal))
                throw new InvalidOperationException($"Missing \"{expected}\" in:{Environment.NewLine}{text}");
        }

        // Runs the root command on a fresh container, so every check starts with an empty io.
        private static async Task<(CapturingIoService Io, ArgumentException? Exception)> RunAsync(params string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<EchoFormatter>();
            services.AddCliHelper<CapturingIoService>()
                .AddCommand<RootCommand>(root => root
                    .AddCommand<GreetCommand>()
                    .AddCommand<ToolsCommand>(tools => tools
                        .AddCommand<EchoCommand>()));

            await using var provider = services.BuildServiceProvider();
            var commandManager = provider.GetRequiredService<ICommandManager>();
            var io = (CapturingIoService)commandManager.IoService;

            try
            {
                await commandManager.CreateCommand<RootCommand>().RunAsync(args).ConfigureAwait(false);
                return (io, null);
            }
            catch (ArgumentException ex)
            {
                return (io, ex);
            }
        }

        private static void ThrowIfFailed(ArgumentException? exception)
        {
            if (exception is not null)
                throw new InvalidOperationException($"Command failed: {exception.Message}", exception);
        }
    }
}
