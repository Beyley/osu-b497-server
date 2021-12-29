namespace osu_server;

public class BanchoMessage : Serializable {
	public string Message;
	public string Sender;
	public string Target;

	public BanchoMessage(string sender, string message, string target) {
		this.Sender  = sender;
		this.Message = message;
		this.Target  = target;
	}

	public BanchoMessage() {
		this.Sender  = "";
		this.Message = "";
		this.Target  = "";
	}

	public override void WriteToStream(Stream s) {
		using BanchoWriter writer = new(s);

		writer.Write(this.Sender);
		writer.Write(this.Message);
		writer.Write(this.Target);

		writer.Flush();
	}

	public override void ReadFromStream(Stream s) {
		using BanchoReader reader = new(s);

		this.Sender  = reader.ReadString();
		this.Message = reader.ReadString();
		this.Target  = reader.ReadString();
	}
}
