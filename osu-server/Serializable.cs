namespace osu_server; 

public abstract class Serializable {
	public abstract void WriteToStream(Stream  s);
	public abstract void ReadFromStream(Stream s);
}
