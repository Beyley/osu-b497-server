namespace osu_server;

public class ServerSettingsb497 : ServerStatus {
	public override short Port            => 13381;
	public override int   ProtocolVersion => 1;
	public override byte  PingInterval    => 1;
}
