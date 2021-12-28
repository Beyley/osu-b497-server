namespace osu_server;

public struct ClientStatus {
	public string          BeatmapChecksum;
	public int             BeatmapId;
	public bool            BeatmapUpdate;
	public Enums.Mods      CurrentMods;
	public Enums.PlayModes PlayMode;
	public Enums.Status    Status;
	public string          StatusText;

	public ClientStatus() {
		this.BeatmapChecksum = "";
		this.BeatmapId       = -1;
		this.BeatmapUpdate   = false;
		this.CurrentMods     = Enums.Mods.None;
		this.PlayMode        = Enums.PlayModes.OsuStandard;
		this.Status          = Enums.Status.Idle;
		this.StatusText      = "";
	}

	public void WriteToStream(BanchoWriter writer, bool beatmapUpdate) {
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
}
