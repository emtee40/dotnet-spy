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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdTypeRefCOMD : DmdTypeRef {
		public override DmdTypeScope TypeScope { get; }
		public override string? MetadataNamespace { get; }
		public override string? MetadataName { get; }

		readonly DmdComMetadataReader reader;
		readonly int declTypeToken;

		public DmdTypeRefCOMD(DmdComMetadataReader reader, uint rid, IList<DmdCustomModifier>? customModifiers) : base(reader.Module, rid, customModifiers) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Dispatcher.VerifyAccess();
			uint token = new MDToken(Table.TypeRef, rid).Raw;
			DmdTypeUtilities.SplitFullName(MDAPI.GetTypeRefName(reader.MetaDataImport, token) ?? string.Empty, out var @namespace, out var name);
			MetadataNamespace = @namespace;
			MetadataName = name;

			var resScopeToken = new MDToken(MDAPI.GetTypeRefResolutionScope(reader.MetaDataImport, token));
			switch (resScopeToken.Table) {
			case Table.Module:
				TypeScope = new DmdTypeScope(reader.Module);
				break;

			case Table.TypeRef:
				TypeScope = DmdTypeScope.Invalid;
				declTypeToken = resScopeToken.ToInt32();
				break;

			case Table.ModuleRef:
				var moduleName = MDAPI.GetModuleRefName(reader.MetaDataImport, resScopeToken.Raw) ?? string.Empty;
				TypeScope = new DmdTypeScope(reader.GetName(), moduleName);
				break;

			case Table.AssemblyRef:
				TypeScope = new DmdTypeScope(reader.ReadAssemblyName_COMThread(resScopeToken.Rid));
				break;

			default:
				TypeScope = DmdTypeScope.Invalid;
				break;
			}
		}

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		protected override int GetDeclaringTypeRefToken() => declTypeToken;

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) {
			VerifyCustomModifiers(customModifiers);
			return AppDomain.Intern(COMThread(() => new DmdTypeRefCOMD(reader, Rid, customModifiers)));
		}

		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.Intern(COMThread(() => new DmdTypeRefCOMD(reader, Rid, null)));
	}
}
