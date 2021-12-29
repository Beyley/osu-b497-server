using System.Diagnostics;
using System.Net.Sockets;
using EeveeTools.Servers.TCP;

namespace osu_server;

public abstract class Client : TcpClientHandler {
	public enum LoginResult {
		OK,
		BadPassword,
		WrongVersion
	}

	public string            Username       = "";
	public int               UserId         = 0;
	public string            AvatarFilename = "";
	public int               PlayCount      = -1;
	public ClientStatus      Status         = new(); 
	public int               TimeZone       = 0;
	public long              RankedScore    = -1;
	public long              TotalScore     = -1;
	public string            Location       = "";
	public int               Level          = -1;
	public float             Accuracy       = -1f;
	public ushort            Rank           = 0;
	public Enums.Permissions Permission     = Enums.Permissions.Normal;

	public bool DisplayCity;
	
	public Enums.ServerType Type;

	public bool Connected = false;
	public bool LoggedIn  = false;

	public ServerStatus ServerSettings;

	private Thread _backgroundThread;
	public  bool   RunBackgroundThread = true;

	public long LastPing = Stopwatch.GetTimestamp();

	protected object _lock = new();

	protected void OnLoginSuccess() {
		lock(Global.ConnectedClients)
			Global.ConnectedClients.Add(this);
		
		lock(Global.ConnectedClients)
			Global.ConnectedClients.ForEach(x => x.SendClientUpdate(this, Enums.Completeness.Full));

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
		lock(Global.ConnectedClients)
			Global.ConnectedClients.Remove(this);
		
		this.LoggedIn  = false;
		this.Connected = false;
		this.RunBackgroundThread = false;
		
		lock(Global.ConnectedClients)
			Global.ConnectedClients.ForEach(x => x.SendUserDisconnect(this));
	}

	public abstract void SendLoginResponse(LoginResult loginResult);
	public abstract void SendPing();
	public abstract void SendUserDisconnect(Client client);
	public abstract void SendProtocolNegotiation();
	public abstract void SendClientUpdate(Client   client, Enums.Completeness completeness);
	public abstract void SendPacket(Enums.PacketId pid,    byte[]             data);
	public abstract void SendBlankPacket(Enums.PacketId pid);
	public abstract void SendMessage(BanchoMessage message);
}
