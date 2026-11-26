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

using System;

namespace Etherna.CliHelper.Services
{
    /// <summary>
    /// The <see cref="IIoService"/> on the system console. Errors are written to the standard output, in dark red.
    /// </summary>
    public class ConsoleIoService : IIoService
    {
        // Consts.
        private const ConsoleColor ErrorForegroundColor = ConsoleColor.DarkRed;
        
        // Methods.
        /// <inheritdoc />
        public ConsoleKeyInfo ReadKey() => Console.ReadKey();

        /// <inheritdoc />
        public string? ReadLine() => Console.ReadLine();

        /// <inheritdoc />
        public void Write(string? value) => Console.Write(value);
        
        /// <inheritdoc />
        public void WriteError(string value)
        {
            Console.ForegroundColor = ErrorForegroundColor;
            Console.Write(value);
            Console.ResetColor();
        }

        /// <inheritdoc />
        public void WriteErrorLine(string value)
        {
            Console.ForegroundColor = ErrorForegroundColor;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        /// <inheritdoc />
        public void WriteLine(string? value = null) => Console.WriteLine(value);
    }
}