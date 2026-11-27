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
using Etherna.CliHelper.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Etherna.CliHelper.Commands
{
    public class CommandMapTest
    {
        // Internal classes.
        [CommandAlias("-r")]
        private sealed class BadRootCommand : CommandBase
        {
            public override string Description => "Root with an invalid alias";
        }

        private sealed class BatchCommand : CommandBase
        {
            public override string Description => "Batches";
        }

        [CommandAlias("-x")]
        private sealed class DashCommand : CommandBase
        {
            public override string Description => "Alias starting with a dash";
        }

        [CommandAlias("ls")]
        private sealed class DirCommand : CommandBase
        {
            public override string Description => "Alias of list";
        }

        [CommandAlias("")]
        private sealed class EmptyCommand : CommandBase
        {
            public override string Description => "Empty alias";
        }

        [CommandAlias("ls")]
        private sealed class ListCommand : CommandBase
        {
            public override string Description => "List";
        }

        private sealed class LsCommand : CommandBase
        {
            public override string Description => "Name of an alias of list";
        }

        private static class OtherCommands
        {
            public sealed class ListCommand : CommandBase
            {
                public override string Description => "Another list";
            }

            public sealed class LsCommand : CommandBase
            {
                public override string Description => "Another ls";
            }
        }

        private sealed class PinCommand : CommandBase
        {
            public override string Description => "Pins";
        }

        private sealed class RootCommand : CommandBase
        {
            public override string Description => "Root";
        }

        [CommandAlias("self")]
        private sealed class SelfCommand : CommandBase
        {
            public override string Description => "Alias equal to its name";
        }

        [CommandAlias("list")]
        private sealed class ShowCommand : CommandBase
        {
            public override string Description => "Alias equal to the name of list";
        }

        [CommandAlias("a b")]
        private sealed class SpaceCommand : CommandBase
        {
            public override string Description => "Alias with a space";
        }

        [CommandAlias("tw")]
        [CommandAlias("tw")]
        private sealed class TwiceCommand : CommandBase
        {
            public override string Description => "Alias declared twice";
        }

        [CommandAlias("Up")]
        private sealed class UpperCommand : CommandBase
        {
            public override string Description => "Uppercase alias";
        }

        // Tests.
        // Names and aliases are verified among the sub commands of the same command only.
        [Fact]
        public void AddCommandAcceptsANameUsedUnderAnotherCommand()
        {
            // Setup.
            var services = new ServiceCollection();

            // Action.
            var exception = Record.Exception(() => services.AddCliHelper<ConsoleIoService>()
                .AddCommand<RootCommand>(root => root
                    .AddCommand<ListCommand>()                       //root list, alias ls
                    .AddCommand<BatchCommand>(batch => batch
                        .AddCommand<LsCommand>())                    //root batch ls
                    .AddCommand<PinCommand>(pin => pin
                        .AddCommand<OtherCommands.LsCommand>()       //root pin ls
                        .AddCommand<OtherCommands.ListCommand>()))); //root pin list

            // Assert.
            Assert.Null(exception);
        }

        [Fact]
        public void AddCommandRejectsAnInvalidAliasOfARootCommand()
        {
            // Setup.
            var configuration = new ServiceCollection().AddCliHelper<ConsoleIoService>();

            // Action and assert.
            var exception = Assert.Throws<InvalidOperationException>(() => configuration.AddCommand<BadRootCommand>());
            Assert.Equal(
                "Alias '-r' of command 'badroot' is not valid: use lowercase letters, digits and dashes, not starting with a dash",
                exception.Message);
        }

        [Theory]
        [InlineData("dash", "-x")]
        [InlineData("empty", "")]
        [InlineData("space", "a b")]
        [InlineData("upper", "Up")]
        public void AddCommandRejectsInvalidAliases(string command, string expectedAlias)
        {
            // Setup.
            var configuration = new ServiceCollection().AddCliHelper<ConsoleIoService>();
            Action<ICommandManagerConfiguration> addSubCommands = command switch
            {
                "dash" => root => root.AddCommand<DashCommand>(),
                "empty" => root => root.AddCommand<EmptyCommand>(),
                "space" => root => root.AddCommand<SpaceCommand>(),
                "upper" => root => root.AddCommand<UpperCommand>(),
                _ => throw new ArgumentOutOfRangeException(nameof(command))
            };

            // Action and assert.
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.AddCommand<RootCommand>(addSubCommands));
            Assert.Equal(
                $"Alias '{expectedAlias}' of command 'root {command}' is not valid: use lowercase letters, digits and dashes, not starting with a dash",
                exception.Message);
        }

        // Every name and alias selects one command among its siblings.
        [Theory]
        [InlineData("self", "Command 'root self' declares 'self' more than once, as its name or an alias")]
        [InlineData("twice", "Command 'root twice' declares 'tw' more than once, as its name or an alias")]
        [InlineData("show", "Name or alias 'list' of command 'root show' is already used by command 'root list'")]
        [InlineData("ls", "Name or alias 'ls' of command 'root ls' is already used by command 'root list'")]
        [InlineData("dir", "Name or alias 'ls' of command 'root dir' is already used by command 'root list'")]
        [InlineData("other-list", "Name or alias 'list' of command 'root list' is already used by command 'root list'")]
        public void AddCommandRejectsNamesSelectingTwoCommands(string command, string expectedMessage)
        {
            // Setup.
            var configuration = new ServiceCollection().AddCliHelper<ConsoleIoService>();
            Action<ICommandManagerConfiguration> addSubCommands = command switch
            {
                "dir" => root => root.AddCommand<ListCommand>().AddCommand<DirCommand>(),
                "ls" => root => root.AddCommand<ListCommand>().AddCommand<LsCommand>(),
                "other-list" => root => root.AddCommand<ListCommand>().AddCommand<OtherCommands.ListCommand>(),
                "self" => root => root.AddCommand<SelfCommand>(),
                "show" => root => root.AddCommand<ListCommand>().AddCommand<ShowCommand>(),
                "twice" => root => root.AddCommand<TwiceCommand>(),
                _ => throw new ArgumentOutOfRangeException(nameof(command))
            };

            // Action and assert.
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.AddCommand<RootCommand>(addSubCommands));
            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
