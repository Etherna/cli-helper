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

namespace Etherna.CliHelper.Commands.Models.OptionRequirements
{
    /// <summary>
    /// A violation of an option requirement.
    /// </summary>
    /// <param name="message">The description of the violation, meant for the user</param>
    public class OptionRequirementError(string message)
    {
        // Properties.
        /// <summary>
        /// The description of the violation, meant for the user.
        /// </summary>
        public string Message { get; } = message;
    }
}