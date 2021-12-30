using System.Runtime.CompilerServices;

namespace osu_server;

public class Global {
	public static List<Client> ConnectedClients = new();
	public static List<Client> ClientsInLobby   = new();
	public static List<Match>  Matches = new();

	public static void CreateMatch(Match match) {
		match.MatchId  = MatchId++;
		match.IsActive = true;
		
		Matches.Add(match);

		Client host = match.GetHost();
		if (match.HandlePlayerJoin(host))
			host.JoinMatch(match);
		else
			host.JoinMatchFailed();
		
		ClientsInLobby.ForEach(x => x.HandleMatchUpdate(match));
	}

	public static void GlobalMatchUpdate(Match match) {
		lock (ConnectedClients)
			ConnectedClients.ForEach(x => x.HandleMatchUpdate(match));
	}

	public static void MatchDisband(Match match) {
		lock(ConnectedClients)
			ConnectedClients.ForEach(x => x.HandleMatchDisband(match));

		Matches.RemoveAll(x => x.MatchId == match.MatchId);
	}

	public static Match? GetMatchFromId(int id) {
		return Matches.FirstOrDefault(x => x.MatchId == id, null);
	}
	
	public static Client? GetUserById(int id) {
		return ConnectedClients.FirstOrDefault(x => x.UserId == id, null);
	}

	public static int UserId = 0;
	public static int MatchId = 0;

	public static List<Channel> ChatChannels = new() {
		new("#osu", true),
		new("#taiko", false),
		new("#cbt", false)
	};
}
