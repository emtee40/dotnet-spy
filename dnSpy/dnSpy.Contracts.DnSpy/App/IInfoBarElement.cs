/*
    Copyright (C) 2023 ElektroKill

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace dnSpy.Contracts.App {
	/// <summary>
	/// A single element in the info bar
	/// </summary>
	public interface IInfoBarElement {
		/// <summary>
		/// Message being displayed as part of this info bar element
		/// </summary>
		string Message { get; }

		/// <summary>
		/// Icon being displayed as part of this info bar element
		/// </summary>
		InfoBarIcon Icon { get; }

		/// <summary>
		/// Closes the info bar element
		/// </summary>
		void Close();
	}
}
