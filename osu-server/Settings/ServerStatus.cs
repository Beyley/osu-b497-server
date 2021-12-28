using System.Net.Sockets;

namespace osu_server;

public abstract class ServerStatus {
	public TcpListener Listener;

	public          bool  Started = false;
	public abstract short Port            { get; }
	public abstract int   ProtocolVersion { get; }
	public abstract byte PingInterval    { get; }
}
