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
    /// The input and output of the commands. Commands read and write through it in place of the console,
    /// so their io can be redirected.
    /// </summary>
    public interface IIoService
    {
        // Methods.
        /// <summary>
        /// Read the next key pressed by the user.
        /// </summary>
        /// <returns>The pressed key</returns>
        ConsoleKeyInfo ReadKey();

        /// <summary>
        /// Read the next line of input.
        /// </summary>
        /// <returns>The line read, or null when no more input is available</returns>
        string? ReadLine();

        /// <summary>
        /// Write a value to the output.
        /// </summary>
        /// <param name="value">The value to write</param>
        void Write(string? value);

        /// <summary>
        /// Write an error to the output, distinguished from the regular output.
        /// </summary>
        /// <param name="value">The error to write</param>
        void WriteError(string value);

        /// <summary>
        /// Write an error to the output, distinguished from the regular output, followed by a line terminator.
        /// </summary>
        /// <param name="value">The error to write</param>
        void WriteErrorLine(string value);

        /// <summary>
        /// Write a value to the output, followed by a line terminator.
        /// </summary>
        /// <param name="value">The value to write, or null to write the line terminator alone</param>
        void WriteLine(string? value = null);
    }
}