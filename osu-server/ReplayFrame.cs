namespace osu_server; 

[Flags]
public enum ButtonState
{
	None   = 0,
	Left1  = 1,
	Right1 = 2,
	Left2  = 4,
	Right2 = 8
}

public class ReplayFrame : Serializable {
	public          float       MouseX;
	public          float       MouseY;
	public          bool        MouseLeft;
	public          bool        MouseRight;
	public          bool        MouseLeft1;
	public          bool        MouseRight1;
	public          bool        MouseLeft2;
	public          bool        MouseRight2;
	public          ButtonState ButtonState;
	public          int         Time;

	public ReplayFrame(Stream s) {
		this.ReadFromStream(s);
	}
	
	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);
		writer.Write((byte)this.ButtonState);
		writer.Write((byte)0);
		writer.Write(this.MouseX);
		writer.Write(this.MouseY);
		writer.Write(this.Time);
	}
	public override void ReadFromStream(Stream s) {
		BanchoReader reader = new(s);

		this.ButtonState = (ButtonState)reader.ReadByte();
		this.SetButtonStates(this.ButtonState);
            
		byte bt = reader.ReadByte();
		if (bt > 0)
			this.SetButtonStates(ButtonState.Right1);

		this.MouseX = reader.ReadSingle();
		this.MouseY = reader.ReadSingle();
		this.Time   = reader.ReadInt32();
	}
	
	private void SetButtonStates(ButtonState buttonState) {
		this.MouseLeft   |= (buttonState & (ButtonState.Left1 | ButtonState.Left2))   > 0;
		this.MouseLeft1  |= (buttonState & ButtonState.Left1)                         > 0;
		this.MouseLeft2  |= (buttonState & ButtonState.Left2)                         > 0;
		this.MouseRight  |= (buttonState & (ButtonState.Right1 | ButtonState.Right2)) > 0;
		this.MouseRight1 |= (buttonState & ButtonState.Right1)                        > 0;
		this.MouseRight2 |= (buttonState & ButtonState.Right2)                        > 0;
	}
}
