using System.Net;
using EeveeTools.Servers.TCP;
using Kettu;
using osu_server;

internal class Program {
	public static void Main(string[] args) {
		Logger.AddLogger(new ConsoleLogger());
		Logger.StartLogging();

		IPAddress ip = IPAddress.Any;


		Thread b497Thread = new(() => {
			TcpServer server = new(ip.ToString(), ServerSettingsb497.Port, typeof(Clientb497));

			server.Start();
		});

		Logger.Log("Starting b497 Server!");
		b497Thread.Start();
		Logger.Log("Press enter to stop the server!");
		Console.ReadLine();
		// B497Status.Started = false;
		// B497Status.Listener.Stop();
	}
}
