namespace osu_server; 

public class BeatmapInfo : Serializable {
	public int            Id;
	public int            BeatmapId;
	public int            BeatmapSetId;
	public int            ThreadId;
	public int            Ranked;
	public Enums.Rankings PlayerRank;
	public string         Checksum;
	
	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);
		
		writer.Write((short)this.Id);
		writer.Write(this.BeatmapId);
		writer.Write(this.BeatmapSetId);
		writer.Write(this.ThreadId);
		writer.Write((byte)this.Ranked);
		writer.Write((byte)this.PlayerRank);
		writer.Write(this.Checksum);
		
		writer.Flush();
	}
	
	public override void ReadFromStream(Stream s) {
		BanchoReader reader = new(s);

		this.Id           = reader.ReadInt16();
		this.BeatmapId    = reader.ReadInt32();
		this.BeatmapSetId = reader.ReadInt32();
		this.ThreadId     = reader.ReadInt32();
		this.Ranked       = reader.ReadByte();
		this.PlayerRank   = (Enums.Rankings)reader.ReadByte();
		this.Checksum     = reader.ReadString();
	}
}
