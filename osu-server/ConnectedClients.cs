namespace osu_server;

public class ConnectedClients {
	public const int MAX_USERS = 16;

	public static readonly Client[] CLIENTS = new Client[MAX_USERS];

	public static int GetNextOpenId() {
		for (int i = 0; i < CLIENTS.Length; i++) {
			ref Client client = ref CLIENTS[i];
			if (client.Connected == false) {
				return i;
			}
		}

		return -1;
	}
}
