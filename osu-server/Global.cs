namespace osu_server;

public class Global {
	public static List<Client> ConnectedClients = new();

	public static int userId = 0;

	public static List<Channel> ChatChannels = new() {
		new("#osu", true), 
		new("#taiko", false), 
		new("#cbt", false)
	};
}
