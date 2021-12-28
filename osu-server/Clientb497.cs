using System.Runtime.CompilerServices;
using System.Text;
using Kettu;

namespace osu_server;

public class Clientb497 {
	public const int USER_ID_BAD_PASSWORD = -1;
	public const int USER_ID_BAD_VERSION  = -2;

	public static void HandleMessage(ref Client client, byte[] message) {
		if (!client.LoggedIn) {
			string   loginString      = Encoding.UTF8.GetString(message).Replace("\r", "");
			string[] splitLoginString = loginString.Split("\n");

			client.Username = splitLoginString[0].Trim();
			string password = splitLoginString[1].Trim();

			string[] splitInfoLine = splitLoginString[2].Trim().Split("|");

			string buildName = splitInfoLine[0].Trim();
			client.TimeZone    = Convert.ToInt32(splitInfoLine[1].Trim());
			client.DisplayCity = splitInfoLine[2].Trim() == "1";

			client.LoggedIn = true;

			if (buildName != "b497") {
				client.SendLoginResponse(Client.LoginResult.WrongVersion);
				client.TcpClient.Close();
				return;
			}
			
			Logger.Log(@$"User: {client.Username} logged in on version {buildName}! tz:{client.TimeZone} dc:{client.DisplayCity}");
			
			client.SendLoginResponse(Client.LoginResult.OK);
			client.SendProtocolNegotiation();
			client.SendClientUpdate(ref client, Enums.Completeness.Full);
			client.StartBackgroundThread();

			client.OnLoginComplete();
		}
		else {
			using MemoryStream stream = new(message);
			using BanchoReader reader = new(stream);

			Enums.PacketId pid = (Enums.PacketId)reader.ReadInt16();

			Logger.Log($"Received packet {pid}!");
			switch (pid) {
				case Enums.PacketId.Osu_Exit: {
					client.TcpClient.Close();
					
					break;
				}
			}
		}
	}

	public static void SendPing(ref Client client) {
		Logger.Log($"Sending Ping!");
		if(client.Stream.Socket.Connected)
			client.SendBlankPacket(Enums.PacketId.Bancho_Ping);
	}

	public static void OnDisconnect(ref Client client) {
		
	}

	public static void SendDisconnectPacket(ref Client clientToNotify, ref Client disconnectedClient) {
		using MemoryStream stream = new();
		using BanchoWriter writer = new(stream);
		
		writer.Write(disconnectedClient.UserId);
		
		writer.Flush();
		clientToNotify.SendPacket(Enums.PacketId.Bancho_HandleOsuQuit, stream);
	}
	
	public static void SendProtocolNegotiation(ref Client client, int version) {
		MemoryStream stream = new();
		BanchoWriter writer = new(stream);
		
		writer.Write(version);
		
		writer.Flush();
		client.SendPacket(Enums.PacketId.Bancho_ProtocolNegotiation, stream);

		stream.Close();
	}
	
	public static void SendClientUpdate(ref Client clientToSendTo, ref Client updatedClient, Enums.Completeness completeness) {
		MemoryStream stream = new();
		BanchoWriter writer = new(stream);
		
		writer.Write(updatedClient.UserId);
		writer.Write((byte)completeness);
		
		updatedClient.Status.WriteToStream(writer, true);
		
		if (completeness > Enums.Completeness.StatusOnly)
		{
			writer.Write(updatedClient.RankedScore);
			writer.Write(updatedClient.Accuracy);
			writer.Write(updatedClient.PlayCount);
			writer.Write(updatedClient.TotalScore);
			writer.Write(updatedClient.Rank);
		}
		if (completeness == Enums.Completeness.Full)
		{
			writer.Write(updatedClient.Username);
			writer.Write(updatedClient.AvatarFilename);
			writer.Write((byte) (updatedClient.TimeZone + 24));
			writer.Write(updatedClient.Location);
			writer.Write((byte)updatedClient.Permission);
		}
		
		writer.Flush();
		clientToSendTo.SendPacket(Enums.PacketId.Bancho_HandleOsuUpdate, stream);

		stream.Close();
	}

	public static void SendLoginResponse(ref Client client, Client.LoginResult result) {
		MemoryStream stream = new();
		BanchoWriter writer = new(stream);

		switch (result) {
			case Client.LoginResult.OK: {
				writer.Write(client.UserId);
				break;
			}
			case Client.LoginResult.BadPassword: {
				writer.Write(USER_ID_BAD_PASSWORD);
				break;
			}
			case Client.LoginResult.WrongVersion: {
				writer.Write(USER_ID_BAD_VERSION);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof (result), result, null);
		}

		writer.Flush();
		client.SendPacket(Enums.PacketId.Bancho_LoginReply, stream);

		stream.Close();
	}
}
