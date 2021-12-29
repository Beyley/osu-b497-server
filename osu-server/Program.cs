using System.Net;
using EeveeTools.Servers.TCP;
using Kettu;
using osu_server;

internal class Program {
	public static ServerStatus b497Status = new ServerSettingsb497();
	
	public static void Main(string[] args) {
		Logger.AddLogger(new ConsoleLogger());
		Logger.StartLogging();

		IPAddress ip = IPAddress.Any;


		Thread b497Thread = new(() => {
			TcpServer server = new(ip.ToString(), b497Status.Port, typeof(Clientb497));

			server.Start();
		});

		Logger.Log("Starting b497 Server!");
		b497Thread.Start();
		Logger.Log("Press enter to stop the server!");
		Console.ReadLine();
		b497Status.Started = false;
		b497Status.Listener.Stop();
	}
}
