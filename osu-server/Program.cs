using System.Net;
using System.Net.Sockets;
using Kettu;
using osu_server;
Logger.AddLogger(new ConsoleLogger());
Logger.StartLogging();

IPAddress ip = IPAddress.Any;

ServerStatus b497Status = new ServerSettingsb497();

Thread b497Thread = new(() => {
	b497Status.Listener = new(ip, b497Status.Port);

	b497Status.Started = true;

	b497Status.Listener.Start();

	while (b497Status.Started) {
		TcpClient tcpClient = b497Status.Listener.AcceptTcpClient();

		tcpClient.NoDelay             = true;
		tcpClient.SendTimeout         = 4000;
		tcpClient.ReceiveTimeout      = 8000;
		tcpClient.LingerState.Enabled = false;
		
		new Thread(() => {
			int i = ConnectedClients.GetNextOpenId();
			ConnectedClients.CLIENTS[i] = new(tcpClient, Enums.ServerType.b497, b497Status);

			ref Client client = ref ConnectedClients.CLIENTS[i];

			client.UserId = i;
			
			client.Connected = true;
			
			#region read data from stream until it closes

			//Create Buffer to hold Data In
			byte[] readBuffer = new byte[4096];
			//Read While Connected
			while (tcpClient.Connected && client.Stream.Socket.Connected) {
				Console.WriteLine(tcpClient.Available);
				if (tcpClient.Available != 0 && client.Stream.DataAvailable && client.Stream.CanRead) {
					int bytesRecieved = client.Stream.Read(readBuffer, 0, readBuffer.Length);
					//Cut Buffer
					byte[] destinationBuffer = new byte[bytesRecieved];
					Buffer.BlockCopy(readBuffer, 0, destinationBuffer, 0, bytesRecieved);
					// Console.WriteLine($"{destinationBuffer[0]} {destinationBuffer[1]}");
					//Invoke Method
					client.HandleMessage(destinationBuffer);
				}
				Thread.Sleep(25);
			}

			#endregion

			client.HandleDisconnect();
		}).Start();
	}
});

Logger.Log("Starting b497 Server!");
b497Thread.Start();
Logger.Log("Press enter to stop the server!");
Console.ReadLine();
b497Status.Started = false;
b497Status.Listener.Stop();
