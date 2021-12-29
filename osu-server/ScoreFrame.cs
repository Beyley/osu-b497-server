namespace osu_server; 

public class ScoreFrame : Serializable {
	public byte   Id;
	public int    Time;
	public ushort Count100;
	public ushort Count300;
	public ushort Count50;
	public ushort CountGeki;
	public ushort CountKatu;
	public ushort CountMiss;
	public ushort CurrentCombo;
	public ushort MaxCombo;
	public int    CurrentHp;
	public bool   Pass;
	public bool   Perfect;
	public int    TotalScore;
	
	public ScoreFrame(Stream s) {
		this.ReadFromStream(s);
	}
	
	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);
		
		writer.Write(this.Time);
		writer.Write(this.Id);
		writer.Write(this.Count300);
		writer.Write(this.Count100);
		writer.Write(this.Count50);
		writer.Write(this.CountGeki);
		writer.Write(this.CountKatu);
		writer.Write(this.CountMiss);
		writer.Write(this.TotalScore);
		writer.Write(this.MaxCombo);
		writer.Write(this.CurrentCombo);
		writer.Write(this.Perfect);
		writer.Write((byte)(this.Pass ? this.CurrentHp : 254));
	}
	public override void ReadFromStream(Stream s) {
		BanchoReader reader = new(s);

		this.Time         = reader.ReadInt32();
		this.Id           = reader.ReadByte();
		this.Count300     = reader.ReadUInt16();
		this.Count100     = reader.ReadUInt16();
		this.Count50      = reader.ReadUInt16();
		this.CountGeki    = reader.ReadUInt16();
		this.CountKatu    = reader.ReadUInt16();
		this.CountMiss    = reader.ReadUInt16();
		this.TotalScore   = reader.ReadInt32();
		this.MaxCombo     = reader.ReadUInt16();
		this.CurrentCombo = reader.ReadUInt16();
		this.Perfect      = reader.ReadBoolean();
		this.CurrentHp    = reader.ReadByte();
		if (this.CurrentHp == 254)
		{
			this.CurrentHp = 0;
			this.Pass         = false;
		}
		else
			this.Pass = true;
	}
}
