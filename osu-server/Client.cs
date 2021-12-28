using System.Diagnostics;
using System.Net.Sockets;

namespace osu_server;

public struct Client {
	public enum LoginResult {
		OK,
		BadPassword,
		WrongVersion
	}

	public string            Username;
	public int               UserId;
	public string            AvatarFilename;
	public int               PlayCount;
	public ClientStatus      Status;
	public int               TimeZone;
	public long              RankedScore;
	public long              TotalScore;
	public string            Location;
	public int               Level;
	public float             Accuracy;
	public ushort            Rank;
	public Enums.Permissions Permission;

	public bool DisplayCity;

	public TcpClient        TcpClient;
	public NetworkStream    Stream;
	public BanchoWriter     Writer;
	public Enums.ServerType Type;

	public bool Connected = false;
	public bool LoggedIn  = false;

	public ServerStatus ServerStatus;

	private Thread _backgroundThread;
	public  bool   RunBackgroundThread;

	public long LastPing = Stopwatch.GetTimestamp();

	private object _lock = new();

	public Client(TcpClient tcpClient, Enums.ServerType type, ServerStatus serverStatus) {
		this.Username       = "";
		this.UserId         = 0;
		this.AvatarFilename = "";
		this.PlayCount      = -1;
		this.Status         = new();
		this.TimeZone       = 0;
		this.RankedScore    = -1;
		this.TotalScore     = -1;
		this.Location       = "";
		this.Level          = -1;
		this.Accuracy       = -1f;
		this.Rank           = 0;
		this.Permission     = Enums.Permissions.Normal;

		this.DisplayCity = false;

		this.Stream       = tcpClient.GetStream();
		this.Writer       = new BanchoWriter(this.Stream);
		this.Type         = type;
		this.ServerStatus = serverStatus;
		this.TcpClient    = tcpClient;

		this.RunBackgroundThread = true;
		this._backgroundThread   = null;
	}
	
	private void BackgroundThreadMain() {
		for (;;) {
			if (!this.RunBackgroundThread)
				break;

			if (((float)Stopwatch.GetTimestamp() / Stopwatch.Frequency) - ((float)this.LastPing / Stopwatch.Frequency) > this.ServerStatus.PingInterval) {
				this.SendPing();

				this.LastPing = Stopwatch.GetTimestamp();
			}
			
			Thread.Sleep(500);
		}
	}

	public void HandleMessage(byte[] data) {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.HandleMessage(ref this, data);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public void StartBackgroundThread() {
		this._backgroundThread = new(this.BackgroundThreadMain);
		
		this._backgroundThread.Start();
	}

	public void StopBackgroundThread() {
		this.RunBackgroundThread = false;
	}

	public void SendDisconnectPacket(ref Client client) {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.SendDisconnectPacket(ref this, ref client);
				break;
			}
		}
	}
	
	public void SendPing() {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.SendPing(ref this);
				break;
			}
		}
	}

	public void SendPacket(Enums.PacketId pid, MemoryStream data) {
		lock (this._lock) {
			this.Writer.Write((short)pid);        // Packet ID
			this.Writer.Write(false);             // Compression
			this.Writer.Write((uint)data.Length); // Packet length
			this.Writer.Write(data.ToArray());    //Write the data
			this.Writer.Flush();
			this.Stream.Flush();
		}
	}
	
	public void SendBlankPacket(Enums.PacketId pid) {
		lock (this._lock) {
			this.Writer.Write((short)pid);        // Packet ID
			this.Writer.Write(false);             // Compression
			this.Writer.Write((uint)0);    // Packet length of 0 (no data)
			this.Writer.Flush();
			this.Stream.Flush();
		}
	}

	public void SendClientUpdate(ref Client client, Enums.Completeness completeness) {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.SendClientUpdate(ref this, ref client, completeness);
				break;
			}
		}
	}

	public void SendLoginResponse(LoginResult result) {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.SendLoginResponse(ref this, result);
				break;
			}
		}
	}

	public void SendProtocolNegotiation() {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.SendProtocolNegotiation(ref this, this.ServerStatus.ProtocolVersion);
				break;
			}
		}
	}

	public void HandleDisconnect() {
		switch (this.Type) {
			case Enums.ServerType.b497: {
				Clientb497.OnDisconnect(ref this);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		for (var i = 0; i < ConnectedClients.CLIENTS.Length; i++) {
			ref Client clientToNotify = ref ConnectedClients.CLIENTS[i];
			
			if(clientToNotify.LoggedIn && clientToNotify.UserId != this.UserId)
				clientToNotify.SendDisconnectPacket(ref this);
		}
		
		this.Connected = false;
		this.LoggedIn  = false;
		
		this.StopBackgroundThread();
		this.RunBackgroundThread = false;
	}

	public void OnLoginComplete() {
		for (var i = 0; i < ConnectedClients.CLIENTS.Length; i++) {
			ref Client clientToNotify = ref ConnectedClients.CLIENTS[i];

			if (clientToNotify.LoggedIn && clientToNotify.UserId != this.UserId) {
				clientToNotify.SendClientUpdate(ref this, Enums.Completeness.Full);
				this.SendClientUpdate(ref clientToNotify, Enums.Completeness.Full);
			}
		}
	}
}
