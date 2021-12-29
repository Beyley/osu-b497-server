namespace osu_server;

public class ClientStatus : Serializable {
	public string          BeatmapChecksum = "";
	public int             BeatmapId       = -1;
	public Enums.Mods      CurrentMods     = Enums.Mods.None;
	public Enums.PlayModes PlayMode        = Enums.PlayModes.OsuStandard;
	public Enums.Status    Status          = Enums.Status.Idle;
	public string          StatusText      = "";

	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);

		bool beatmapUpdate = true;
		
		writer.Write((byte)this.Status);
		writer.Write(beatmapUpdate);

		if (beatmapUpdate) {
			writer.Write(this.StatusText);
			writer.Write(this.BeatmapChecksum);
			writer.Write((ushort)this.CurrentMods);

			writer.Write((byte)this.PlayMode);
			writer.Write(this.BeatmapId);
		}

		writer.Flush();
	}
	public override void ReadFromStream(Stream s) {
		BanchoReader reader = new(s);

		this.Status = (Enums.Status)reader.ReadByte();
		bool beatmapUpdate = reader.ReadBoolean();

		if (!beatmapUpdate)
			return;
		
		this.StatusText      = reader.ReadString();
		this.BeatmapChecksum = reader.ReadString();
		this.CurrentMods     = (Enums.Mods)reader.ReadUInt16();
		this.PlayMode        = (Enums.PlayModes)reader.ReadByte();
		this.BeatmapId       = reader.ReadInt32();
	}
}
