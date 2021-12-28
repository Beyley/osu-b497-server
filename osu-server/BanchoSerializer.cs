using System.Text;

namespace osu_server;

public class BanchoReader : BinaryReader {
	public BanchoReader(Stream input) : base(input, Encoding.UTF8) {}

	public override string ReadString() {
		byte type = this.ReadByte();
		return type == 11
			? Encoding.UTF8.GetString(Encoding.ASCII.GetBytes(base.ReadString()))
			: "";
	}
}

public class BanchoWriter : BinaryWriter {
	public BanchoWriter(Stream input) : base(input, Encoding.UTF8) {}

	private static byte[] WriteUleb128Numebr(int num) {
		List<byte> ret = new();

		if (num == 0)
			return new byte[] { 0x00 };

		int length = 0;

		while (num > 0) {
			ret.Add((byte)(num & 127));
			num >>= 7;
			if (num != 0)
				ret[length] |= 128;
			length += 1;
		}

		return ret.ToArray();
	}

	public override void Write(string value) {
		if (value.Length == 0) {
			this.Write(new byte[] { 0x00 });
			return;
		}

		this.Write((byte)11);
		this.Write(WriteUleb128Numebr(value.Length));
		this.Write(Encoding.UTF8.GetBytes(value));
	}
}
