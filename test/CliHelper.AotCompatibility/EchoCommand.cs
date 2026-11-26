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
using System.Threading.Tasks;

namespace Etherna.CliHelper.AotCompatibility
{
    internal sealed class EchoCommand(EchoFormatter formatter) : CommandBase
    {
        // Properties.
        public override string Description => "Echo the arguments";
        public override bool PrintHelpWithNoArgs => false;

        // Protected methods.
        protected override Task ExecuteAsync(string[] commandArgs)
        {
            IoService.WriteLine($"{CommandPathNames}: {formatter.Format(commandArgs)}");
            return Task.CompletedTask;
        }
    }
}
