using System.Diagnostics.Contracts;
using System.Text;

namespace osu_server;

public class BanchoReader : BinaryReader {
	public BanchoReader(Stream input) : base(input, Encoding.UTF8) {}

	public override string ReadString() {
		byte type = this.ReadByte();
		return type == 11
			? base.ReadString()
			: null;
	}

	[Pure]
	public List<int> ReadIntList() {
		List<int> list = new();

		int count = this.ReadInt32();

		for (int i = 0; i < count; i++)
			list.Add(this.ReadInt32());

		return list;
	}

	[Pure]
	public List<string> ReadStringList() {
		List<string> list = new();

		int count = this.ReadInt32();

		for (int i = 0; i < count; i++)
			list.Add(this.ReadString());

		return list;
	}
}

public class BanchoWriter : BinaryWriter {
	public BanchoWriter(Stream input) : base(input, Encoding.UTF8) {}

	public override void Write(string value) {
		if (value.Length == 0) {
			this.Write(new byte[] { 0x00 });
			return;
		}

		this.Write((byte)11);
		base.Write(value);
	}

	public void Write(List<int> list) {
		this.Write(list.Count);
		foreach (int i in list)
			this.Write(i);
	}

	public void Write(List<string> list) {
		this.Write(list.Count);
		foreach (string i in list)
			this.Write(i);
	}
}
