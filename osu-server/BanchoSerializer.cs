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
}
