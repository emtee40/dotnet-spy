/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;

namespace dnSpy.BamlDecompiler.Baml {
	enum BamlRecordType : byte {
		ClrEvent = 0x13,
		Comment = 0x17,
		AssemblyInfo = 0x1c,
		AttributeInfo = 0x1f,
		ConstructorParametersStart = 0x2a,
		ConstructorParametersEnd = 0x2b,
		ConstructorParameterType = 0x2c,
		ConnectionId = 0x2d,
		ContentProperty = 0x2e,
		DefAttribute = 0x19,
		DefAttributeKeyString = 0x26,
		DefAttributeKeyType = 0x27,
		DeferableContentStart = 0x25,
		DefTag = 0x18,
		DocumentEnd = 0x2,
		DocumentStart = 0x1,
		ElementEnd = 0x4,
		ElementStart = 0x3,
		EndAttributes = 0x1a,
		KeyElementEnd = 0x29,
		KeyElementStart = 0x28,
		LastRecordType = 0x39,
		LineNumberAndPosition = 0x35,
		LinePosition = 0x36,
		LiteralContent = 0xf,
		NamedElementStart = 0x2f,
		OptimizedStaticResource = 0x37,
		PIMapping = 0x1b,
		PresentationOptionsAttribute = 0x34,
		ProcessingInstruction = 0x16,
		Property = 0x5,
		PropertyArrayEnd = 0xa,
		PropertyArrayStart = 0x9,
		PropertyComplexEnd = 0x8,
		PropertyComplexStart = 0x7,
		PropertyCustom = 0x6,
		PropertyDictionaryEnd = 0xe,
		PropertyDictionaryStart = 0xd,
		PropertyListEnd = 0xc,
		PropertyListStart = 0xb,
		PropertyStringReference = 0x21,
		PropertyTypeReference = 0x22,
		PropertyWithConverter = 0x24,
		PropertyWithExtension = 0x23,
		PropertyWithStaticResourceId = 0x38,
		RoutedEvent = 0x12,
		StaticResourceEnd = 0x31,
		StaticResourceId = 0x32,
		StaticResourceStart = 0x30,
		StringInfo = 0x20,
		Text = 0x10,
		TextWithConverter = 0x11,
		TextWithId = 0x33,
		TypeInfo = 0x1d,
		TypeSerializerInfo = 0x1e,
		XmlAttribute = 0x15,
		XmlnsProperty = 0x14
	}

	abstract class BamlRecord {
		public abstract BamlRecordType Type { get; }
		public long Position { get; internal set; }
		public abstract void Read(BamlBinaryReader reader);
		public abstract void Write(BamlBinaryWriter writer);
	}

	abstract class SizedBamlRecord : BamlRecord {
		public override void Read(BamlBinaryReader reader) {
			long pos = reader.BaseStream.Position;
			int size = reader.ReadEncodedInt();

			ReadData(reader, size - (int)(reader.BaseStream.Position - pos));
			Debug.Assert(reader.BaseStream.Position - pos == size);
		}

		static int SizeofEncodedInt(int val) {
			if ((val & ~0x7F) == 0)
				return 1;
			if ((val & ~0x3FFF) == 0)
				return 2;
			if ((val & ~0x1FFFFF) == 0)
				return 3;
			if ((val & ~0xFFFFFFF) == 0)
				return 4;
			return 5;
		}

		public override void Write(BamlBinaryWriter writer) {
			long pos = writer.BaseStream.Position;
			WriteData(writer);
			var size = (int)(writer.BaseStream.Position - pos);
			size = SizeofEncodedInt(SizeofEncodedInt(size) + size) + size;
			writer.BaseStream.Position = pos;
			writer.WriteEncodedInt(size);
			WriteData(writer);
		}

		protected abstract void ReadData(BamlBinaryReader reader, int size);
		protected abstract void WriteData(BamlBinaryWriter writer);
	}

	interface IBamlDeferRecord {
		long Position { get; }
		BamlRecord Record { get; set; }
		void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve);
		void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr);
	}

	sealed class XmlnsPropertyRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.XmlnsProperty;

		public string Prefix { get; set; }
		public string XmlNamespace { get; set; }
		public ushort[] AssemblyIds { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Prefix = reader.ReadString();
			XmlNamespace = reader.ReadString();
			AssemblyIds = new ushort[reader.ReadUInt16()];
			for (int i = 0; i < AssemblyIds.Length; i++)
				AssemblyIds[i] = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Prefix);
			writer.Write(XmlNamespace);
			writer.Write((ushort)AssemblyIds.Length);
			foreach (ushort i in AssemblyIds)
				writer.Write(i);
		}
	}

	sealed class PresentationOptionsAttributeRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.PresentationOptionsAttribute;

		public string Value { get; set; }
		public ushort NameId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
			NameId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(NameId);
		}
	}

	sealed class PIMappingRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.PIMapping;

		public string XmlNamespace { get; set; }
		public string ClrNamespace { get; set; }
		public ushort AssemblyId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			XmlNamespace = reader.ReadString();
			ClrNamespace = reader.ReadString();
			AssemblyId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(XmlNamespace);
			writer.Write(ClrNamespace);
			writer.Write(AssemblyId);
		}
	}

	sealed class AssemblyInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.AssemblyInfo;

		public ushort AssemblyId { get; set; }
		public string AssemblyFullName { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AssemblyId = reader.ReadUInt16();
			AssemblyFullName = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AssemblyId);
			writer.Write(AssemblyFullName);
		}
	}

	class PropertyRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.Property;

		public ushort AttributeId { get; set; }
		public string Value { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AttributeId = reader.ReadUInt16();
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(Value);
		}
	}

	sealed class PropertyWithConverterRecord : PropertyRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyWithConverter;

		public ushort ConverterTypeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			base.ReadData(reader, size);
			ConverterTypeId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			base.WriteData(writer);
			writer.Write(ConverterTypeId);
		}
	}

	sealed class PropertyCustomRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyCustom;

		public ushort AttributeId { get; set; }
		public ushort RawSerializerTypeId { get; set; }
		public byte[] Data { get; set; }

		public short SerializerTypeId {
			get => (short)(RawSerializerTypeId & 0xFFF);
			set => RawSerializerTypeId = (ushort)(RawSerializerTypeId & 0xF000 | (ushort)value & 0xFFF);
		}

		public bool IsValueTypeId {
			get => (RawSerializerTypeId & 0x4000) == 0x4000;
			set => ModifyRawSerializerTypeId(value, 0x4000);
		}

		protected override void ReadData(BamlBinaryReader reader, int size) {
			long pos = reader.BaseStream.Position;
			AttributeId = reader.ReadUInt16();
			RawSerializerTypeId = reader.ReadUInt16();
			Data = reader.ReadBytes(size - (int)(reader.BaseStream.Position - pos));
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(RawSerializerTypeId);
			writer.Write(Data);
		}

		void ModifyRawSerializerTypeId(bool set, ushort flags) {
			if (set)
				RawSerializerTypeId |= flags;
			else
				RawSerializerTypeId &= (ushort)~flags;
		}
	}

	sealed class DefAttributeRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.DefAttribute;

		public string Value { get; set; }
		public ushort NameId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
			NameId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(NameId);
		}
	}

	sealed class DefAttributeKeyStringRecord : SizedBamlRecord, IBamlDeferRecord {
		internal uint pos = 0xffffffff;

		public override BamlRecordType Type => BamlRecordType.DefAttributeKeyString;

		public ushort ValueId { get; set; }
		public bool Shared { get; set; }
		public bool SharedSet { get; set; }

		public BamlRecord Record { get; set; }

		public void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			Record = resolve(doc[index].Position + pos);
		}

		public void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			wtr.BaseStream.Seek(pos, SeekOrigin.Begin);
			wtr.Write((uint)(Record.Position - doc[index].Position));
		}

		protected override void ReadData(BamlBinaryReader reader, int size) {
			ValueId = reader.ReadUInt16();
			pos = reader.ReadUInt32();
			Shared = reader.ReadBoolean();
			SharedSet = reader.ReadBoolean();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(ValueId);
			pos = (uint)writer.BaseStream.Position;
			writer.Write((uint)0);
			writer.Write(Shared);
			writer.Write(SharedSet);
		}

		static void NavigateTree(BamlDocument doc, BamlRecordType start, BamlRecordType end, ref int index) {
			index++;
			while (true) //Assume there always is a end
			{
				if (doc[index].Type == start)
					NavigateTree(doc, start, end, ref index);
				else if (doc[index].Type == end)
					return;
				index++;
			}
		}
	}

	class TypeInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.TypeInfo;

		public ushort TypeId { get; set; }
		public ushort AssemblyId { get; set; }
		public string TypeFullName { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			TypeId = reader.ReadUInt16();
			AssemblyId = reader.ReadUInt16();
			TypeFullName = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(TypeId);
			writer.Write(AssemblyId);
			writer.Write(TypeFullName);
		}
	}

	sealed class TypeSerializerInfoRecord : TypeInfoRecord {
		public override BamlRecordType Type => BamlRecordType.TypeSerializerInfo;

		public ushort SerializerTypeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			base.ReadData(reader, size);
			SerializerTypeId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			base.WriteData(writer);
			writer.Write(SerializerTypeId);
		}
	}

	enum BamlAttributeUsage : byte {
		Default,
		XmlLang,
		XmlSpace,
		RuntimeName,
	}

	sealed class AttributeInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.AttributeInfo;

		public ushort AttributeId { get; set; }
		public ushort OwnerTypeId { get; set; }
		public BamlAttributeUsage AttributeUsage { get; set; }
		public string Name { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AttributeId = reader.ReadUInt16();
			OwnerTypeId = reader.ReadUInt16();
			AttributeUsage = (BamlAttributeUsage)reader.ReadByte();
			Name = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(OwnerTypeId);
			writer.Write((byte)AttributeUsage);
			writer.Write(Name);
		}
	}

	sealed class StringInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.StringInfo;

		public ushort StringId { get; set; }
		public string Value { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			StringId = reader.ReadUInt16();
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(StringId);
			writer.Write(Value);
		}
	}

	class TextRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.Text;

		public string Value { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) => Value = reader.ReadString();

		protected override void WriteData(BamlBinaryWriter writer) => writer.Write(Value);
	}

	sealed class TextWithConverterRecord : TextRecord {
		public override BamlRecordType Type => BamlRecordType.TextWithConverter;

		public ushort ConverterTypeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			base.ReadData(reader, size);
			ConverterTypeId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			base.WriteData(writer);
			writer.Write(ConverterTypeId);
		}
	}

	sealed class TextWithIdRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.TextWithId;

		public ushort ValueId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) => ValueId = reader.ReadUInt16();

		protected override void WriteData(BamlBinaryWriter writer) => writer.Write(ValueId);
	}

	sealed class LiteralContentRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.LiteralContent;

		public string Value { get; set; }
		public uint Reserved0 { get; set; }
		public uint Reserved1 { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
			Reserved0 = reader.ReadUInt32();
			Reserved1 = reader.ReadUInt32();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(Reserved0);
			writer.Write(Reserved1);
		}
	}

	sealed class RoutedEventRecord : SizedBamlRecord {
		public override BamlRecordType Type => BamlRecordType.RoutedEvent;

		public string Value { get; set; }
		public ushort AttributeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AttributeId = reader.ReadUInt16();
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(Value);
		}
	}

	sealed class DocumentStartRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.DocumentStart;

		public bool LoadAsync { get; set; }
		public uint MaxAsyncRecords { get; set; }
		public bool DebugBaml { get; set; }

		public override void Read(BamlBinaryReader reader) {
			LoadAsync = reader.ReadBoolean();
			MaxAsyncRecords = reader.ReadUInt32();
			DebugBaml = reader.ReadBoolean();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(LoadAsync);
			writer.Write(MaxAsyncRecords);
			writer.Write(DebugBaml);
		}
	}

	sealed class DocumentEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.DocumentEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	[Flags]
	enum ElementStartRecordFlags : byte {
		CreateUsingTypeConverter = 0x1,
		IsInjected = 0x2,
	}

	class ElementStartRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ElementStart;

		public ushort TypeId { get; set; }
		public ElementStartRecordFlags Flags { get; set; }

		public bool CreateUsingTypeConverter {
			get => (Flags & ElementStartRecordFlags.CreateUsingTypeConverter) != 0;
			set => ModifyFlags(value, ElementStartRecordFlags.CreateUsingTypeConverter);
		}

		public bool IsInjected {
			get => (Flags & ElementStartRecordFlags.IsInjected) != 0;
			set => ModifyFlags(value, ElementStartRecordFlags.IsInjected);
		}

		public override void Read(BamlBinaryReader reader) {
			TypeId = reader.ReadUInt16();
			Flags = (ElementStartRecordFlags)reader.ReadByte();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(TypeId);
			writer.Write((byte)Flags);
		}

		void ModifyFlags(bool set, ElementStartRecordFlags flag) {
			if (set)
				Flags |= flag;
			else
				Flags &= ~flag;
		}
	}

	sealed class ElementEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ElementEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class KeyElementStartRecord : DefAttributeKeyTypeRecord {
		public override BamlRecordType Type => BamlRecordType.KeyElementStart;
	}

	sealed class KeyElementEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.KeyElementEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class ConnectionIdRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ConnectionId;

		public uint ConnectionId { get; set; }

		public override void Read(BamlBinaryReader reader) => ConnectionId = reader.ReadUInt32();

		public override void Write(BamlBinaryWriter writer) => writer.Write(ConnectionId);
	}

	sealed class PropertyWithExtensionRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyWithExtension;

		public ushort AttributeId { get; set; }
		public ushort Flags { get; set; }
		public ushort ValueId { get; set; }

		public short ExtensionTypeId {
			get => (short)(Flags & 0xFFF);
			set => Flags = (ushort)(Flags & 0xF000 | (ushort)value & 0xFFF);
		}

		public bool IsValueStaticExtension {
			get => (Flags & 0x2000) == 0x2000;
			set => ModifyFlags(value, 0x2000);
		}

		public bool IsValueTypeExtension {
			get => (Flags & 0x4000) == 0x4000;
			set => ModifyFlags(value, 0x4000);
		}

		public override void Read(BamlBinaryReader reader) {
			AttributeId = reader.ReadUInt16();
			Flags = reader.ReadUInt16();
			ValueId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(Flags);
			writer.Write(ValueId);
		}

		void ModifyFlags(bool set, ushort flags) {
			if (set)
				Flags |= flags;
			else
				Flags &= (ushort)~flags;
		}
	}

	sealed class PropertyTypeReferenceRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyTypeReference;

		public ushort TypeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			base.Read(reader);
			TypeId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			base.Write(writer);
			writer.Write(TypeId);
		}
	}

	sealed class PropertyStringReferenceRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyStringReference;

		public ushort StringId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			base.Read(reader);
			StringId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			base.Write(writer);
			writer.Write(StringId);
		}
	}

	sealed class PropertyWithStaticResourceIdRecord : StaticResourceIdRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyWithStaticResourceId;

		public ushort AttributeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			AttributeId = reader.ReadUInt16();
			base.Read(reader);
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			base.Write(writer);
		}
	}

	sealed class ContentPropertyRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ContentProperty;

		public ushort AttributeId { get; set; }

		public override void Read(BamlBinaryReader reader) => AttributeId = reader.ReadUInt16();

		public override void Write(BamlBinaryWriter writer) => writer.Write(AttributeId);
	}

	class DefAttributeKeyTypeRecord : ElementStartRecord, IBamlDeferRecord {
		internal uint pos = 0xffffffff;

		public override BamlRecordType Type => BamlRecordType.DefAttributeKeyType;

		public bool Shared { get; set; }
		public bool SharedSet { get; set; }

		public BamlRecord Record { get; set; }

		public void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			Record = resolve(doc[index].Position + pos);
		}

		public void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			wtr.BaseStream.Seek(pos, SeekOrigin.Begin);
			wtr.Write((uint)(Record.Position - doc[index].Position));
		}

		public override void Read(BamlBinaryReader reader) {
			base.Read(reader);
			pos = reader.ReadUInt32();
			Shared = reader.ReadBoolean();
			SharedSet = reader.ReadBoolean();
		}

		public override void Write(BamlBinaryWriter writer) {
			base.Write(writer);
			pos = (uint)writer.BaseStream.Position;
			writer.Write((uint)0);
			writer.Write(Shared);
			writer.Write(SharedSet);
		}

		static void NavigateTree(BamlDocument doc, BamlRecordType start, BamlRecordType end, ref int index) {
			index++;
			while (true) {
				if (doc[index].Type == start)
					NavigateTree(doc, start, end, ref index);
				else if (doc[index].Type == end)
					return;
				index++;
			}
		}
	}

	sealed class PropertyListStartRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyListStart;
	}

	sealed class PropertyListEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyListEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class PropertyDictionaryStartRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyDictionaryStart;
	}

	sealed class PropertyDictionaryEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyDictionaryEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class PropertyArrayStartRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyArrayStart;
	}

	sealed class PropertyArrayEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyArrayEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	class PropertyComplexStartRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyComplexStart;

		public ushort AttributeId { get; set; }

		public override void Read(BamlBinaryReader reader) => AttributeId = reader.ReadUInt16();

		public override void Write(BamlBinaryWriter writer) => writer.Write(AttributeId);
	}

	sealed class PropertyComplexEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.PropertyComplexEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class ConstructorParametersStartRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ConstructorParametersStart;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class ConstructorParametersEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ConstructorParametersEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	sealed class ConstructorParameterTypeRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.ConstructorParameterType;

		public ushort TypeId { get; set; }

		public override void Read(BamlBinaryReader reader) => TypeId = reader.ReadUInt16();

		public override void Write(BamlBinaryWriter writer) => writer.Write(TypeId);
	}

	sealed class DeferableContentStartRecord : BamlRecord, IBamlDeferRecord {
		long pos;
		internal uint size = 0xffffffff;

		public override BamlRecordType Type => BamlRecordType.DeferableContentStart;

		public BamlRecord Record { get; set; }

		public void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve) => Record = resolve(pos + size);

		public void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr) {
			wtr.BaseStream.Seek(pos, SeekOrigin.Begin);
			wtr.Write((uint)(Record.Position - (pos + 4)));
		}

		public override void Read(BamlBinaryReader reader) {
			size = reader.ReadUInt32();
			pos = reader.BaseStream.Position;
		}

		public override void Write(BamlBinaryWriter writer) {
			pos = writer.BaseStream.Position;
			writer.Write((uint)0);
		}
	}

	sealed class StaticResourceStartRecord : ElementStartRecord {
		public override BamlRecordType Type => BamlRecordType.StaticResourceStart;
	}

	sealed class StaticResourceEndRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.StaticResourceEnd;

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	class StaticResourceIdRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.StaticResourceId;

		public ushort StaticResourceId { get; set; }

		public override void Read(BamlBinaryReader reader) => StaticResourceId = reader.ReadUInt16();

		public override void Write(BamlBinaryWriter writer) => writer.Write(StaticResourceId);
	}

	sealed class OptimizedStaticResourceRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.OptimizedStaticResource;

		public byte Flags { get; set; }
		public ushort ValueId { get; set; }

		public bool IsValueTypeExtension {
			get => (Flags & 0x1) != 0;
			set => ModifyFlags(value, 0x1);
		}

		public bool IsValueStaticExtension {
			get => (Flags & 0x2) != 0;
			set => ModifyFlags(value, 0x2);
		}

		public override void Read(BamlBinaryReader reader) {
			Flags = reader.ReadByte();
			ValueId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(Flags);
			writer.Write(ValueId);
		}

		void ModifyFlags(bool set, byte flags) {
			if (set)
				Flags |= flags;
			else
				Flags &= (byte)~flags;
		}
	}

	sealed class LineNumberAndPositionRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.LineNumberAndPosition;

		public uint LineNumber { get; set; }
		public uint LinePosition { get; set; }

		public override void Read(BamlBinaryReader reader) {
			LineNumber = reader.ReadUInt32();
			LinePosition = reader.ReadUInt32();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(LineNumber);
			writer.Write(LinePosition);
		}
	}

	sealed class LinePositionRecord : BamlRecord {
		public override BamlRecordType Type => BamlRecordType.LinePosition;

		public uint LinePosition { get; set; }

		public override void Read(BamlBinaryReader reader) => LinePosition = reader.ReadUInt32();

		public override void Write(BamlBinaryWriter writer) => writer.Write(LinePosition);
	}

	sealed class NamedElementStartRecord : ElementStartRecord {
		public override BamlRecordType Type => BamlRecordType.NamedElementStart;

		public string RuntimeName { get; set; }

		public override void Read(BamlBinaryReader reader) {
			TypeId = reader.ReadUInt16();
			RuntimeName = reader.ReadString();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(TypeId);
			if (RuntimeName is not null) {
				writer.Write(RuntimeName);
			}
		}
	}
}
