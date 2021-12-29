namespace osu_server; 

public enum ReplayAction {
	Standard,
	NewSong,
	Skip,
	Completion,
	Fail
}

public class ReplayFrameBundle : Serializable {
	public List<ReplayFrame> ReplayFrames;
	public ScoreFrame        ScoreFrame;
	public ReplayAction      Action;
	
	public override void WriteToStream(Stream s) {
		BinaryWriter sw = new(s);

		sw.Write((ushort)this.ReplayFrames.Count);
		foreach(ReplayFrame f in this.ReplayFrames)
			f.WriteToStream(s);

		sw.Write((byte)this.Action);

		this.ScoreFrame.WriteToStream(s);
	}
	public override void ReadFromStream(Stream s) {
		this.ReplayFrames = new List<ReplayFrame>();

		BanchoReader reader = new(s);
		
		int frameAmount = reader.ReadUInt16();
		for (int i = 0; i < frameAmount; i++)
			this.ReplayFrames.Add(new ReplayFrame(s));

		this.Action = (ReplayAction)reader.ReadByte();

		try
		{
			this.ScoreFrame = new ScoreFrame(s);
		}
		catch (Exception) {
			// ignored
		}
	}
}
