using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Kettu;

namespace osu_server;

public class Clientb497 : Client {
	public const int USER_ID_BAD_PASSWORD = -1;
	public const int USER_ID_BAD_VERSION  = -2;

	public Clientb497() {
		this.Type         = Enums.ServerType.b497;
		this.ServerSettings = Program.b497Status;
	}
	
	protected override void HandleData(byte[] data) {
		this.Connected = true;
		
		if (!this.LoggedIn) {
			string   loginString      = Encoding.UTF8.GetString(data).Replace("\r", "");
			string[] splitLoginString = loginString.Split("\n");

			this.Username = splitLoginString[0].Trim();
			//Make sure the user doesnt log in twice
			this.KickOutLoggedIn(this.Username);
			string password = splitLoginString[1].Trim();

			string[] splitInfoLine = splitLoginString[2].Trim().Split("|");

			string buildName = splitInfoLine[0].Trim();
			this.TimeZone    = Convert.ToInt32(splitInfoLine[1].Trim());
			this.DisplayCity = splitInfoLine[2].Trim() == "1";

			this.LoggedIn = true;

			if (buildName != "b497") {
				this.SendLoginResponse(LoginResult.WrongVersion);
				this.Client.Close();
				return;
			}
			
			Logger.Log(@$"User: {this.Username} logged in on version {buildName}! tz:{this.TimeZone} dc:{this.DisplayCity}");
			
			this.SendProtocolNegotiation();
			this.UserId = Global.userId++;
			this.SendLoginResponse(LoginResult.OK);
			
			this.OnLoginSuccess();
		}
		else {
			MemoryStream rawStream = new(data);
			BanchoReader rawReader = new(rawStream);
			
			Enums.PacketId pid         = (Enums.PacketId)rawReader.ReadInt16();
			bool           compression = rawReader.ReadBoolean();
			uint           length      = rawReader.ReadUInt32();

			MemoryStream rawPayload = new MemoryStream(rawReader.ReadBytes((int)length));
			
			rawReader.Close();
			rawStream.Close();

			Stream payload;

			if(compression) {
				payload = new GZipStream(rawPayload, CompressionMode.Decompress);
			}
			else {
				payload = rawPayload;
			}

			Logger.Log($"Received packet {pid}!");
			
			switch (pid) {
				case Enums.PacketId.Osu_Exit: {
					this.Client.Close();
					
					break;
				}
				case Enums.PacketId.Osu_RequestStatusUpdate: {
					Global.ConnectedClients
						  .ForEach(x => this.SendClientUpdate(x, Enums.Completeness.Full));
					
					break;
				}
				case Enums.PacketId.Osu_SendIrcMessage: {
					BanchoMessage message = new();
					message.ReadFromStream(payload);

					message.Sender = this.Username;
					
					Logger.Log($"<{message.Target}> {message.Sender}: {message.Message}");

					Global.ConnectedClients.ForEach(x => {
						if (x.UserId != this.UserId)
							x.SendMessage(message);
					});
					
					break;
				}
			}
		}
		
		this.LastPing = Stopwatch.GetTimestamp();
	}

	protected override void BackgroundThreadMain() {
		for (;;) {
			if (!this.RunBackgroundThread) return;

			long currentTime = Stopwatch.GetTimestamp();
			if (currentTime / Stopwatch.Frequency - this.LastPing / Stopwatch.Frequency > this.ServerSettings.PingInterval) {
				this.SendPing();
				this.LastPing = currentTime;
			}
			
			Thread.Sleep(500);
		}
	}
	
	public override void SendLoginResponse(LoginResult loginResult) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);
		
		switch (loginResult) {
			case LoginResult.OK: {
				writer.Write(this.UserId);
				break;
			}
			case LoginResult.BadPassword: {
				writer.Write(USER_ID_BAD_PASSWORD);
				break;
			}
			case LoginResult.WrongVersion: {
				writer.Write(USER_ID_BAD_VERSION);
				break;
			}
		}
		
		writer.Flush();
		
		this.SendPacket(Enums.PacketId.Bancho_LoginReply, stream.ToArray());
	}
	
	public override void SendPing() {
		this.SendBlankPacket(Enums.PacketId.Bancho_Ping);
	}
	
	public override void SendUserDisconnect(Client client) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);
		
		writer.Write(client.UserId);
		writer.Flush();
		
		this.SendPacket(Enums.PacketId.Bancho_HandleOsuQuit, stream.ToArray());
	}
	
	public override void SendProtocolNegotiation() {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);
		
		writer.Write(this.ServerSettings.ProtocolVersion);
		writer.Flush();
		
		this.SendPacket(Enums.PacketId.Bancho_ProtocolNegotiation, stream.ToArray());
	}
	
	public override void SendClientUpdate(Client client, Enums.Completeness completeness) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);
		
		writer.Write(client.UserId);
		writer.Write((byte)completeness);
		
		client.Status.WriteToStream(writer, true);
		
		if (completeness > Enums.Completeness.StatusOnly)
		{
			writer.Write(client.RankedScore);
			writer.Write(client.Accuracy);
			writer.Write(client.PlayCount);
			writer.Write(client.TotalScore);
			writer.Write(client.Rank);
		}
		
		if (completeness == Enums.Completeness.Full)
		{
			writer.Write(client.Username);
			writer.Write(client.AvatarFilename);
			writer.Write((byte) (client.TimeZone + 24));
			writer.Write(client.Location);
			writer.Write((byte)client.Permission);
		}
		
		writer.Flush();
		
		this.SendPacket(Enums.PacketId.Bancho_HandleOsuUpdate, stream.ToArray());
	}
	
	public override void SendPacket(Enums.PacketId pid, byte[] data) {
		lock (this._lock) {
			using MemoryStream stream = new();
			using BanchoWriter writer = new(stream);

			writer.Write((short)pid);        // Packet ID
			writer.Write(false);             // Compression
			writer.Write((uint)data.Length); // Packet length
			writer.Write(data.ToArray());    //Write the data
			writer.Flush();

			this.SendData(stream.ToArray());
		}
	}
	
	public override void SendBlankPacket(Enums.PacketId pid) {
		lock(this._lock) {
			using MemoryStream stream = new();
			using BanchoWriter writer = new(stream);

			writer.Write((short)pid); // Packet ID
			writer.Write(false);      // Compression
			writer.Write((uint)0);    // Packet length of 0 (no data)
			writer.Flush();

			this.SendData(stream.ToArray());
		}
	}
	
	public override void SendMessage(BanchoMessage message) {
		MemoryStream stream = new();
		message.WriteToStream(stream);
		
		this.SendPacket(Enums.PacketId.Bancho_SendIrcMessage, stream.ToArray());
	}
}
