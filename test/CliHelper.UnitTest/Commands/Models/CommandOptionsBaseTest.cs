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
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Etherna.CliHelper.Commands.Models
{
    public class CommandOptionsBaseTest
    {
        // Internal classes.
        private sealed class TestOptions : CommandOptionsBase
        {
            public bool Alpha { get; private set; }
            public bool Beta { get; private set; }
            public bool Gamma { get; private set; }
            public string? Name { get; private set; }
            public string[]? Range { get; private set; }

            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-a", "--alpha", "Alpha flag", _ => Alpha = true),
                new("-b", "--beta", "Beta flag", _ => Beta = true),
                new("-g", "--gamma", "Gamma flag", _ => Gamma = true),
                new("-n", "--name", "Name", args => Name = args[0], [typeof(string)]),
                new("-r", "--range", "Range", args => Range = args, [typeof(int), typeof(int)])
            ];
        }

        private sealed class RequiringOptions : CommandOptionsBase
        {
            public override IEnumerable<CommandOption> Definitions =>
            [
                new("-a", "--alpha", "Alpha flag", _ => { }),
                new("-c", "--count", "Count", _ => { }, [typeof(int)])
            ];

            public override IEnumerable<OptionRequirementBase> Requirements =>
            [
                new RequireOneOfOptionRequirement("-a"),
                new MinValueOptionRequirement("--count", 1)
            ];
        }

        // Fields.
        private readonly IIoService ioService = Mock.Of<IIoService>();

        // Tests.
        [Theory]
        [InlineData(new[] { "-ab" }, 1, true, true, false, null)]
        [InlineData(new[] { "-ba", "commandArg" }, 1, true, true, false, null)]
        [InlineData(new[] { "-gab", "commandArg" }, 1, true, true, true, null)]
        [InlineData(new[] { "-ab", "-n", "value", "commandArg" }, 3, true, true, false, "value")]
        [InlineData(new[] { "-n", "value", "-ga", "commandArg" }, 3, true, false, true, "value")]
        [InlineData(new[] { "-an", "value", "commandArg" }, 2, true, false, false, "value")] //ends the group, takes the following arg
        [InlineData(new[] { "-anvalue", "commandArg" }, 1, true, false, false, "value")]     //ends the group, takes the rest of it
        [InlineData(new[] { "-nvalue", "-b" }, 2, false, true, false, "value")]
        [InlineData(new[] { "-nab", "commandArg" }, 1, false, false, false, "ab")]           //the rest of the group is the arg, not options
        public void ParseArgsParsesComposedShortOptions(
            string[] args, int expectedParsedArgs, bool expectedAlpha, bool expectedBeta, bool expectedGamma, string? expectedName)
        {
            var options = new TestOptions();

            var parsedArgs = options.ParseArgs(args, ioService);

            Assert.Equal(expectedParsedArgs, parsedArgs);
            Assert.Equal(expectedAlpha, options.Alpha);
            Assert.Equal(expectedBeta, options.Beta);
            Assert.Equal(expectedGamma, options.Gamma);
            Assert.Equal(expectedName, options.Name);
        }

        [Fact]
        public void ParseArgsKeepsParsingSingleOptions()
        {
            var options = new TestOptions();

            var parsedArgs = options.ParseArgs(["--alpha", "-n", "value", "--beta", "commandArg"], ioService);

            Assert.Equal(4, parsedArgs);
            Assert.True(options.Alpha);
            Assert.True(options.Beta);
            Assert.False(options.Gamma);
            Assert.Equal("value", options.Name);
        }

        [Fact]
        public void ParseArgsTakesFollowingArgsStartingWithDashAsOptionArgs()
        {
            var options = new TestOptions();

            var parsedArgs = options.ParseArgs(["-n", "-b", "commandArg"], ioService);

            Assert.Equal(2, parsedArgs);
            Assert.Equal("-b", options.Name);
            Assert.False(options.Beta);
        }

        [Fact]
        public void ParseArgsReportsEveryViolatedRequirement()
        {
            var ioServiceMock = new Mock<IIoService>();
            var options = new RequiringOptions();

            var exception = Assert.Throws<ArgumentException>(() => options.ParseArgs(["-c", "0"], ioServiceMock.Object));

            Assert.Equal("Errors with command options", exception.Message);
            ioServiceMock.Verify(io => io.WriteError(
                "Invalid options:" + Environment.NewLine +
                "  --alpha is required." + Environment.NewLine +
                "  -c has min value 1." + Environment.NewLine), Times.Once);
        }

        [Fact]
        public void ParseArgsTakesAttachedArgAsFirstOfRequiredArgs()
        {
            var options = new TestOptions();

            var parsedArgs = options.ParseArgs(["-ar1", "2", "commandArg"], ioService);

            Assert.Equal(2, parsedArgs);
            Assert.True(options.Alpha);
            Assert.Equal(new[] { "1", "2" }, options.Range);
        }

        [Theory]
        [InlineData(new[] { "-an" }, "-n requires 1 args: string")]
        [InlineData(new[] { "-r1" }, "-r requires 2 args: int32 int32")]
        [InlineData(new[] { "-ax" }, "-x is not a valid option")]
        [InlineData(new[] { "-aa" }, "-a option is duplicate")]
        [InlineData(new[] { "--alpha", "-ba" }, "-a option is duplicate")]
        public void ParseArgsRejectsInvalidComposedShortOptions(string[] args, string expectedMessage)
        {
            var options = new TestOptions();

            var exception = Assert.Throws<ArgumentException>(() => options.ParseArgs(args, ioService));

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
