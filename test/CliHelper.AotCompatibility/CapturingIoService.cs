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
using System.Text;

namespace Etherna.CliHelper.AotCompatibility
{
    // Records what the commands write, so the probe can check it instead of printing it.
    // It is registered through AddCliHelper<TIoService>, so the container builds it by reflection.
    internal sealed class CapturingIoService : IIoService
    {
        // Fields.
        private readonly StringBuilder error = new();
        private readonly StringBuilder output = new();

        // Properties.
        public string Error => error.ToString();
        public string Output => output.ToString();

        // Methods.
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
        public string? ReadLine() => null;
        public void Write(string? value) => output.Append(value);
        public void WriteError(string value) => error.Append(value);
        public void WriteErrorLine(string value) => error.AppendLine(value);
        public void WriteLine(string? value = null) => output.AppendLine(value);
    }
}
