using System.Diagnostics;
using EeveeTools.Helpers;
using EeveeTools.Servers.TCP;

namespace osu_server;

public abstract class Client : TcpClientHandler {
	public enum LoginResult {
		Ok,
		BadPassword,
		WrongVersion
	}

	private Thread _backgroundThread;

	protected object Lock          = new();
	public    float  Accuracy       = -1f;
	public    string AvatarFilename = "";

	public bool Connected;

	public bool DisplayCity;

	public List<Client> Friends    = new();
	public List<Client> Spectators = new();

	public Client? SpectatorHost = null;

	public long              LastPing = UnixTime.Now();
	public int               Level    = -1;
	public string            Location = "";
	public bool              LoggedIn;
	public Enums.Permissions Permission          = Enums.Permissions.Normal;
	public int               PlayCount           = -1;
	public ushort            Rank                = 0;
	public long              RankedScore         = -1;
	public bool              RunBackgroundThread = true;

	public ServerStatus ServerSettings;
	public ClientStatus Status     = new();
	public int          TimeZone   = 0;
	public long         TotalScore = -1;

	public Enums.ServerType Type;
	public int              UserId = 0;

	public string Username = "";

	public Queue<ReplayFrameBundle> ReplayFrameQueue = new();

	protected void OnLoginSuccess() {
		lock (Global.ConnectedClients) {
			Global.ConnectedClients.Add(this);
		}

		lock (Global.ConnectedClients) {
			Global.ConnectedClients.ForEach(x => x.SendClientUpdate(this, Enums.Completeness.Full));
		}

		this._backgroundThread = new(this.BackgroundThreadMain);
		this._backgroundThread.Start();
	}

	protected abstract void BackgroundThreadMain();

	public void KickOutLoggedIn(string username) {
		lock (Global.ConnectedClients) {
			Client user = Global.ConnectedClients.FirstOrDefault(x => x.Username == username, null);

			user?.Client.Close();
		}
	}

	protected override void HandleDisconnect() {
		lock (Global.ConnectedClients) {
			Global.ConnectedClients.Remove(this);
		}

		this.LoggedIn            = false;
		this.Connected           = false;
		this.RunBackgroundThread = false;

		lock (Global.ConnectedClients) {
			Global.ConnectedClients.ForEach(x => x.SendUserDisconnect(this));
		}
	}

	public abstract void SendLoginResponse(LoginResult loginResult);
	public abstract void SendPing();
	public abstract void SendUserDisconnect(Client client);
	public abstract void SendProtocolNegotiation();
	public abstract void SendClientUpdate(Client        client, Enums.Completeness completeness);
	public abstract void SendPacket(Enums.PacketId      pid,    byte[]             data);
	public abstract void SendBlankPacket(Enums.PacketId pid);
	public abstract void SendMessage(BanchoMessage      message);
	public abstract void SendPermissions();
	public abstract void SendFriendsList();
	public abstract void MakeChannelAvailable(Channel           channel);
	public abstract void RevokeChannel(Channel                  channel);
	public abstract void ShowChannelJoinSuccess(Channel         channel);
	public abstract void SendBeatmapInfoReply(BeatmapInfoReply  reply);
	public abstract void Announce(string                        message);
	public abstract void NotifyHostAboutNewSpectator(Client     client);
	public abstract void NotifyHostAboutSpectatorLeave(Client   client);
	public abstract void NotifyAboutFellowSpectatorJoin(Client  client);
	public abstract void NotifyAboutFellowSpectatorLeave(Client client);
	public abstract void SendSpectatorFrames(ReplayFrameBundle  bundle);
}
