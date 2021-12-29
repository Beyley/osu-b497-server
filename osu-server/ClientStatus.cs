namespace osu_server;

public class ClientStatus {
	public string          BeatmapChecksum = "";
	public int             BeatmapId       = -1;
	public Enums.Mods      CurrentMods     = Enums.Mods.None;
	public Enums.PlayModes PlayMode        = Enums.PlayModes.OsuStandard;
	public Enums.Status    Status          = Enums.Status.Idle;
	public string          StatusText      = "";

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
