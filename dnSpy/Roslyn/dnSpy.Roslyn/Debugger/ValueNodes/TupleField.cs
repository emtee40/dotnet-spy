/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	readonly struct TupleField {
		/// <summary>
		/// Item1, Item2, etc
		/// </summary>
		public readonly string DefaultName;

		/// <summary>
		/// User defined name, if any.
		/// </summary>
		public readonly string? CustomName;

		/// <summary>
		/// All fields that must be accessed in order to get the value shown in the UI, eg. Rest.Rest.Item3
		/// </summary>
		public readonly DmdFieldInfo[] Fields;

		public TupleField(string defaultName, string? customName, DmdFieldInfo[] fields) {
			DefaultName = defaultName;
			CustomName = customName;
			Fields = fields;
		}
	}
}
