namespace osu_server;

public class Channel {
	public bool   Autojoin;
	public string Name;

	public Channel(string name, bool autojoin) {
		this.Name     = name;
		this.Autojoin = autojoin;
	}
}
