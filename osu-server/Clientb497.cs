using System.IO.Compression;
using System.Text;
using EeveeTools.Helpers;
using ICSharpCode.SharpZipLib.GZip;
using Kettu;
using Lzma.Streams;

namespace osu_server;

public class Clientb497 : Client {
	public const int USER_ID_BAD_PASSWORD = -1;
	public const int USER_ID_BAD_VERSION  = -2;

	public Clientb497() {
		this.Type           = Enums.ServerType.b497;
		this.ServerSettings = Program.B497Status;
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
			this.UserId = Global.UserId++;
			this.SendLoginResponse(LoginResult.Ok);

			this.SendPermissions();
			this.SendFriendsList();

			Global.ChatChannels.ForEach(this.MakeChannelAvailable);

			this.ShowChannelJoinSuccess(Global.ChatChannels.First(x => x.Name == "#osu"));

			this.OnLoginSuccess();
		}
		else {
			MemoryStream rawStream = new(data);
			BanchoReader rawReader = new(rawStream);

			Enums.PacketId pid = (Enums.PacketId)rawReader.ReadInt16();

			bool compression = rawReader.ReadBoolean();
			uint length      = rawReader.ReadUInt32();

			MemoryStream rawPayload = new(rawReader.ReadBytes((int)length));

			rawReader.Close();
			rawStream.Close();

			Stream payload;

			payload = compression ? new GZipStream(rawPayload, CompressionMode.Decompress) : rawPayload;

			BanchoReader reader = new(payload);

			if (pid != Enums.PacketId.Osu_Pong)
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
				case Enums.PacketId.Osu_ChannelJoin: {
					string channel = reader.ReadString();

					this.ShowChannelJoinSuccess(Global.ChatChannels.First(x => x.Name == channel));

					break;
				}
				case Enums.PacketId.Osu_FriendAdd: {
					int userid = reader.ReadInt32();

					this.Friends.Add(Global.ConnectedClients.First(x => x.UserId == userid));

					break;
				}
				case Enums.PacketId.Osu_FriendRemove: {
					int userid = reader.ReadInt32();

					this.Friends.Remove(Global.ConnectedClients.First(x => x.UserId == userid));

					break;
				}
				case Enums.PacketId.Osu_SendUserStatus: {
					this.Status.ReadFromStream(payload);

					Logger.Log($"{this.Status.Status} {this.Status.StatusText} | mods:{this.Status.CurrentMods}");

					lock (Global.ConnectedClients) {
						Global.ConnectedClients.ForEach(x => x.SendClientUpdate(this, Enums.Completeness.StatusOnly));
					}

					break;
				}
				case Enums.PacketId.Osu_SendIrcMessagePrivate: {
					BanchoMessage message = new();
					message.ReadFromStream(payload);

					message.Sender = this.Username;

					Logger.Log($"<{message.Target}> {message.Sender}: {message.Message}");

					Client target = Global.ConnectedClients.FirstOrDefault(x => x.Username == message.Target, null);

					if (target == null)
						this.SendMessage(new BanchoMessage("BanchoBot", "That user does not seem to exist", this.Username));
					else
						target.SendMessage(message);

					break;
				}
				case Enums.PacketId.Osu_BeatmapInfoRequest: {
					BeatmapInfoRequest request = new();
					request.ReadFromStream(payload);

					BeatmapInfoReply reply = new();
					reply.BeatmapInfoList = new();

					foreach (int requestId in request.Ids)
						reply.BeatmapInfoList.Add(new() {
							BeatmapId    = requestId,
							BeatmapSetId = 0,
							Checksum     = "",
							Id           = requestId,
							PlayerRank   = Enums.Rankings.None,
							Ranked       = 1,
							ThreadId     = 0
						});

					this.SendBeatmapInfoReply(reply);

					break;
				}
				case Enums.PacketId.Osu_StartSpectating: {
					int id = reader.ReadInt32();

					Client? host = Global.ConnectedClients.FirstOrDefault(x => x.UserId == id, null);

					if (host == null) {
						this.Announce("Could not find that player!");
					}
					else {
						this.SpectatorHost = host;

						foreach (Client spectator in this.SpectatorHost.Spectators)
							this.NotifyAboutFellowSpectatorJoin(spectator);

						this.SpectatorHost.NotifyHostAboutNewSpectator(this);
					}

					break;
				}
				case Enums.PacketId.Osu_StopSpectating: {
					if (this.SpectatorHost != null) {
						this.SpectatorHost.NotifyHostAboutSpectatorLeave(this);

						foreach (Client spectator in this.SpectatorHost.Spectators)
							spectator.NotifyAboutFellowSpectatorLeave(this);

						this.SpectatorHost = null;
					}
					else {
						this.Announce("You are not spectating anyone!");
					}

					break;
				}
				case Enums.PacketId.Osu_SpectateFrames: {
					ReplayFrameBundle bundle = new();
					bundle.ReadFromStream(payload);

					foreach (Client spectator in this.Spectators)
						spectator.ReplayFrameQueue.Enqueue(bundle);

					break;
				}
			}
		}
	}

	protected override void BackgroundThreadMain() {
		for (;;) {
			if (!this.RunBackgroundThread) {
				Logger.Log("Stopping pong thread");
				return;
			}
			
			long currentTime   = UnixTime.Now();
			long timeSincePing = currentTime - this.LastPing;

			if (timeSincePing > this.ServerSettings.TimeBeforeDisconnect) {
				this.Client.Close();
				
				Thread.Sleep(100);

				continue;
			}

			if (timeSincePing > this.ServerSettings.PingInterval) {
				this.SendPing();
				this.LastPing = currentTime;
			}

			if (this.ReplayFrameQueue.TryDequeue(out ReplayFrameBundle bundle))
				this.SendSpectatorFrames(bundle);

			Thread.Sleep(50);
		}
	}

	public override void SendLoginResponse(LoginResult loginResult) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		switch (loginResult) {
			case LoginResult.Ok: {
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

		client.Status.WriteToStream(stream);

		if (completeness > Enums.Completeness.StatusOnly) {
			writer.Write(client.RankedScore);
			writer.Write(client.Accuracy);
			writer.Write(client.PlayCount);
			writer.Write(client.TotalScore);
			writer.Write(client.Rank);
		}

		if (completeness == Enums.Completeness.Full) {
			writer.Write(client.Username);
			writer.Write(client.AvatarFilename);
			writer.Write((byte)(client.TimeZone + 24));
			writer.Write(client.Location);
			writer.Write((byte)client.Permission);
		}

		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_HandleOsuUpdate, stream.ToArray());
	}

	public override void SendPacket(Enums.PacketId pid, byte[] data) {
		lock (this.Lock) {
			using MemoryStream stream = new();
			using BanchoWriter writer = new(stream);

			bool compression = false;
			// if (data.Length > 150) {
			// 	compression = true;
			// 	
			// 	MemoryStream writeBuffer = new();
			// 	GZipOutputStream comp = new(writeBuffer);
			// 	comp.WriteBytes(data);
			// 	comp.Close();
			//
			// 	Logger.Log($"Sending compressed packet {pid} original:{data.Length} new:{writeBuffer.Length}");
			// 	
			// 	data = writeBuffer.ToArray();
			// }

			writer.Write((short)pid);        // Packet ID
			writer.Write(compression);       // Compression
			writer.Write((uint)data.Length); // Packet length
			writer.Write(data.ToArray());    // Write the data
			writer.Flush();

			this.SendData(stream.ToArray());

			this.LastPing = UnixTime.Now();
		}
	}

	public override void SendBlankPacket(Enums.PacketId pid) {
		lock (this.Lock) {
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
	public override void SendPermissions() {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write((int)this.Permission);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_LoginPermissions, stream.ToArray());
	}
	public override void SendFriendsList() {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		List<int> friends = this.Friends.Select(friend => friend.UserId).ToList();

		writer.Write(friends);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_FriendsList, stream.ToArray());
	}
	public override void MakeChannelAvailable(Channel channel) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(channel.Name);
		writer.Flush();

		this.SendPacket(channel.Autojoin ? Enums.PacketId.Bancho_ChannelAvailableAutojoin : Enums.PacketId.Bancho_ChannelAvailable, stream.ToArray());
	}
	public override void RevokeChannel(Channel channel) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(channel.Name);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_ChannelRevoked, stream.ToArray());
	}
	public override void ShowChannelJoinSuccess(Channel channel) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(channel.Name);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_ChannelJoinSuccess, stream.ToArray());
	}
	public override void SendBeatmapInfoReply(BeatmapInfoReply reply) {
		using MemoryStream stream = new();

		reply.WriteToStream(stream);

		this.SendPacket(Enums.PacketId.Bancho_BeatmapInfoReply, stream.ToArray());
	}
	public override void Announce(string message) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(message);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_Announce, stream.ToArray());
	}
	public override void NotifyHostAboutNewSpectator(Client client) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(client.UserId);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_SpectatorJoined, stream.ToArray());

		this.Spectators.Add(client);
	}
	public override void NotifyHostAboutSpectatorLeave(Client client) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(client.UserId);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_SpectatorLeft, stream.ToArray());

		this.Spectators.Remove(client);
	}
	public override void NotifyAboutFellowSpectatorJoin(Client client) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(client.UserId);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_FellowSpectatorJoined, stream.ToArray());
	}
	public override void NotifyAboutFellowSpectatorLeave(Client client) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);

		writer.Write(client.UserId);
		writer.Flush();

		this.SendPacket(Enums.PacketId.Bancho_FellowSpectatorLeft, stream.ToArray());
	}
	public override void SendSpectatorFrames(ReplayFrameBundle bundle) {
		using MemoryStream stream = new();

		bundle.WriteToStream(stream);

		this.SendPacket(Enums.PacketId.Bancho_SpectateFrames, stream.ToArray());
	}
}
