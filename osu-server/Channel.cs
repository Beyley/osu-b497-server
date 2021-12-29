namespace osu_server; 

public class Channel {
	public string Name;
	public bool   Autojoin;

	public Channel(string name, bool autojoin) {
		this.Name     = name;
		this.Autojoin = autojoin;
	}
}
