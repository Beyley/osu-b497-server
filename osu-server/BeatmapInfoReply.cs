namespace osu_server;

public class BeatmapInfoReply : Serializable {
	public List<BeatmapInfo> BeatmapInfoList = new();

	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);

		writer.Write(this.BeatmapInfoList.Count);
		writer.Flush();

		foreach (BeatmapInfo info in this.BeatmapInfoList)
			info.WriteToStream(s);
	}

	public override void ReadFromStream(Stream s) {
		throw new NotImplementedException();
	}
}
