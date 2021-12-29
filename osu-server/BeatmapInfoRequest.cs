namespace osu_server; 

public class BeatmapInfoRequest : Serializable {
	public List<string> Filenames;
	public List<int>    Ids;

	public BeatmapInfoRequest(List<string> filenames, List<int> ids) {
		this.Filenames = filenames;
		this.Ids       = ids;
	}

	public BeatmapInfoRequest() {
		this.Filenames = new();
		this.Ids       = new();
	}
	
	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);
		
		writer.Write(this.Filenames);
		writer.Write(this.Ids);
		
		writer.Flush();
	}
	
	public override void ReadFromStream(Stream s) {
		BanchoReader reader = new(s);

		this.Filenames = reader.ReadStringList();
		this.Ids       = reader.ReadIntList();
	}
}
