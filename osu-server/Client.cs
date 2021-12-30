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
	public  float  Accuracy       = -1f;
	public  string AvatarFilename = "";

	public bool Connected;

	public bool DisplayCity;

	public List<Client> Friends = new();

	public long   LastPing = UnixTime.Now();
	public int    Level    = -1;
	public string Location = "";

	protected object            Lock = new();
	public    bool              LoggedIn;
	public    Enums.Permissions Permission  = Enums.Permissions.Normal;
	public    int               PlayCount   = -1;
	public    ushort            Rank        = 0;
	public    long              RankedScore = -1;

	public Queue<ReplayFrameBundle> ReplayFrameQueue    = new();
	public bool                     RunBackgroundThread = true;

	public Client?      SpectatorHost = null;
	public List<Client> Spectators    = new();
	public Match?       CurrentMatch  = null;

	public ClientStatus Status     = new();
	public int          TimeZone   = 0;
	public long         TotalScore = -1;

	public Enums.ServerType Type;
	public int              UserId = 0;

	public string Username = "";

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
			
			Global.ConnectedClients.ForEach(x => x.SendUserDisconnect(this));
		}
		
		this.LoggedIn            = false;
		this.Connected           = false;
		this.RunBackgroundThread = false;

		if (this.SpectatorHost != null) {
			foreach (Client spectator in this.SpectatorHost.Spectators.Where(spectator => spectator != this))
				spectator.NotifyAboutFellowSpectatorLeave(this);

			this.SpectatorHost.NotifyHostAboutSpectatorLeave(this);
		}

		if (Global.ClientsInLobby.Contains(this)) {
			Global.ClientsInLobby.ForEach(x => x.NotifyAboutLobbyLeave(this));
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
	public abstract void MakeChannelAvailable(Channel          channel);
	public abstract void RevokeChannel(Channel                 channel);
	public abstract void ShowChannelJoinSuccess(Channel        channel);
	public abstract void SendBeatmapInfoReply(BeatmapInfoReply reply);
	public abstract void Announce(string                       message);
	public abstract void GetAttention();
	public abstract void NotifyHostAboutNewSpectator(Client    client);
	public abstract void NotifyHostAboutSpectatorFail(Client   client);
	public abstract void NotifyHostAboutSpectatorLeave(Client   client);
	public abstract void NotifyAboutFellowSpectatorJoin(Client  client);
	public abstract void NotifyAboutFellowSpectatorLeave(Client client);
	public abstract void SendSpectatorFrames(ReplayFrameBundle  bundle);
	public abstract void NotifyAboutLobbyJoin(Client            client);
	public abstract void NotifyAboutLobbyLeave(Client           client);
	public abstract void JoinMatch(Match                        match);
	public abstract void LeaveMatch();
	public abstract void JoinMatchFailed();
	public abstract void HandleMatchUpdate(Match match);
	public abstract void NotifyOfMatchHostTransfer();
	public abstract void HandleMatchDisband(Match match);
	public abstract void HandleMatchStart();
	public abstract void HandleMatchAllPlayersLoaded();
	public abstract void HandlePlayerFail(int              id);
	public abstract void HandleMatchScoreUpdate(ScoreFrame frame);
	public abstract void HandleMatchComplete();
}
