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

namespace Etherna.CliHelper.AotCompatibility
{
    internal sealed class GreetCommandOptions : CommandOptionsBase
    {
        // Properties.
        public override IEnumerable<CommandOption> Definitions =>
        [
            new("-n", "--name", "Name to greet", args => Name = args[0], [typeof(string)]),
            new(null, "--times", "How many times to greet", args => Times = int.Parse(args[0], CultureInfo.InvariantCulture), [typeof(int)])
        ];
        public string? Name { get; private set; }
        public override IEnumerable<OptionRequirementBase> Requirements =>
        [
            new RequireOneOfOptionRequirement("--name"),
            new MinValueOptionRequirement("--times", 1)
        ];
        public int Times { get; private set; } = 1;
    }
}
