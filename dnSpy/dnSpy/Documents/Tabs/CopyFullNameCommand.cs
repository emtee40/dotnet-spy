﻿/*
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

using System;
using System.Runtime.InteropServices;
using System.Windows;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.Tabs {
	static class CopyFullNameCommand {
		static void ExecuteInternal(IFullName? token) {
			if (token is not null) {
				try {
					Clipboard.SetText(token.FullName);
				}
				catch (ExternalException) { }
			}
		}

		[ExportMenuItem(Header = "res:CopyFullNameCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_TOKENS, Order = 60)]
		sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) => GetReference(context) is not null;
			public override void Execute(IMenuItemContext context) => ExecuteInternal(GetReference(context));
			static IFullName? GetReference(IMenuItemContext context) => GetReference(context, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);

			internal static IFullName? GetReference(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				var @ref = context.Find<TextReference?>();
				var realRef = @ref?.Reference;
				if (realRef is IFullName fullName)
					return fullName;
				return null;
			}
		}

		[ExportMenuItem(Header = "res:CopyFullNameCommand", Group = MenuConstants.GROUP_CTX_SEARCH_TOKENS, Order = 10)]
		sealed class SearchCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) => GetReference(context) is not null;
			public override void Execute(IMenuItemContext context) => ExecuteInternal(GetReference(context));
			static IFullName? GetReference(IMenuItemContext context) => CodeCommand.GetReference(context, MenuConstants.GUIDOBJ_SEARCH_GUID);
		}

		[ExportMenuItem(Header = "res:CopyFullNameCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_TOKENS, Order = 50)]
		sealed class DocumentsCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) => GetReference(context) is not null;
			public override void Execute(IMenuItemContext context) => ExecuteInternal(GetReference(context));
			static IFullName? GetReference(IMenuItemContext context) => GetReference(context, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID);

			internal static IFullName? GetReference(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				var nodes = context.Find<TreeNodeData[]?>();
				if (nodes is null || nodes.Length == 0)
					return null;
				var node = nodes[0] as IMDTokenNode;
				return node?.Reference as IFullName;
			}
		}

		[ExportMenuItem(Header = "res:CopyFullNameCommand", Group = MenuConstants.GROUP_CTX_ANALYZER_TOKENS, Order = 10)]
		sealed class AnalyzerCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) => GetReference(context) is not null;
			public override void Execute(IMenuItemContext context) => ExecuteInternal(GetReference(context));
			static IFullName? GetReference(IMenuItemContext context) => DocumentsCommand.GetReference(context, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
		}
	}
}
