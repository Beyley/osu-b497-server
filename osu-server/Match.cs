using Kettu;

namespace osu_server;

public class Match : Serializable {
	[Flags]
	public enum SlotStatusEnum {
		Open      = 1,
		Locked    = 2,
		NotReady  = 4,
		Ready     = 8,
		NoMap     = 16,
		Playing   = 32,
		Complete  = 64,
		HasPlayer = 124
	}

	public const int        SLOT_COUNT = 8;

	public Enums.Mods       ActiveMods;
	public string           BeatmapChecksum;
	public int              BeatmapId = -1;
	public string           BeatmapName;
	public string           GameName;
	public int              HostId;
	public bool             InProgress;
	public int              MatchId;
	public Enums.MatchTypes MatchType;
	public Enums.PlayModes  PlayMode;
	public int[]            SlotId     = new int[SLOT_COUNT];
	public SlotStatusEnum[] SlotStatus = new SlotStatusEnum[SLOT_COUNT];
	
	public readonly bool[]    SkipRequested = new bool[SLOT_COUNT];
	private         bool      _finishedLoading;
	public          bool      IsActive;
	internal        bool[]    PlayerLoaded = new bool[SLOT_COUNT];
	internal        Client?[] Players      = new Client[SLOT_COUNT];

	public Channel Channel;
		
	public Client GetHost() {
		return Global.GetUserById(this.HostId)!;
	}

	public bool HandlePlayerJoin(Client client) {
		lock (this) {
			//Find the lowest slot
			int i = 0;
			while (true)
			{
				if (i > SLOT_COUNT - 1)
					return false;
				if (this.SlotStatus[i] == SlotStatusEnum.Open) {
					break;
				}
				i++;
			}
			
			this.SetSlot(i, client);

			this.UpdatePlayers(true);
		}
		
		return true;
	}

	public bool HandlePlayerLeave(Client client) {
		int slot = this.FindPlayerFromId(client.UserId);
		if (slot < 0) {
			Logger.Log($"{client} not in match!");
			return false;
		}
		
		this.SetSlot(slot, null);
		
		if(this.GetHost().UserId == client.UserId) 
			this.HandleHostLeft();
		else
			this.UpdatePlayers(true);
		
		if (!this.IsActive) return true;

		//Make sure they dont hold us up forever
		this.CheckIfAllPlayersLoaded();
		this.CheckIfAllPlayersComplete();

		Global.GlobalMatchUpdate(this);
		
		return true;
	}

	public void HandleUserChangeSlot(Client client, int newId) {
		int currentId = this.FindPlayerFromId(client.UserId);

		SlotStatusEnum newSlotStatus = this.SlotStatus[newId];

		if ((newSlotStatus & SlotStatusEnum.Open) == 0)
			return;

		this.SlotId[newId]     = this.SlotId[currentId];
		this.SlotStatus[newId] = this.SlotStatus[currentId];
		this.Players[newId]    = this.Players[currentId];

		this.SetSlot(currentId, null);
		
		// this.SetSlot(newId, client);
		
		this.UpdatePlayers(false);
	}

	public void HandlePlayerReady(Client client) {
		int i = this.FindPlayerFromId(client.UserId);

		if (i == -1) {
			Logger.Log($"{client.Username} attempted invalid ready!");
			return;
		}

		//Ignore readying while the game is in progress
		if (this.InProgress) return;

		this.SlotStatus[i] = SlotStatusEnum.Ready;
		
		this.UpdatePlayers(false);
	}
	
	public void HandlePlayerNotReady(Client client) {
		int i = this.FindPlayerFromId(client.UserId);

		if (i == -1) {
			Logger.Log($"{client.Username} attempted invalid unready!");
			return;
		}

		//Ignore readying while the game is in progress
		if (this.InProgress) return;

		this.SlotStatus[i] = SlotStatusEnum.NotReady;
		
		this.UpdatePlayers(false);
	}

	public void HandleStart() {
		if(this.InProgress) return;
		
		this.InProgress = true;

		for (int i = 0; i < this.Players.Length; i++) {
			Client? player = this.Players[i];

			if(player != null)
				this.SlotStatus[i] = SlotStatusEnum.Playing;
			
			player?.HandleMatchStart();
		}

		this.UpdatePlayers(true);
	}
	
	private bool AllPlayersLoaded
	{
		get
		{
			int playersLoaded = 0;
			for (int i = 0; i < 8; i++)
				if (this.PlayerLoaded[i]) playersLoaded++;
			return playersLoaded == this.SlotsUsed;
		}
	}

	public void HandlePlayerLoadComplete(Client client) {
		if (!this.InProgress) return;

		this.PlayerLoaded[this.FindPlayerFromId(client.UserId)] = true;
		
		this.CheckIfAllPlayersLoaded();
	}

	public bool AllPlayersSkip {
		get {
			for (int i = 0; i < this.Players.Length; i++) {
				Client? player = this.Players[i];
				
				if(player != null)
					if (!this.SkipRequested[i])
						return false;
			}

			return true;
		}
	}
	
	public void HandlePlayerSkip(Client client) {
		if (!this.InProgress) return;

		this.SkipRequested[this.FindPlayerFromId(client.UserId)] = true;

		if (this.AllPlayersSkip) {
			foreach (Client? player in this.Players) {
				player?.HandleMatchSkip();
			}
		}
	}

	public void CheckIfAllPlayersLoaded() {
		if (!this.InProgress || !this.AllPlayersLoaded) return;
		
		if (this._finishedLoading) return;
		this._finishedLoading = true;
		
		foreach (Client? client in this.Players)
			client?.HandleMatchAllPlayersLoaded();
	}

	public void CheckIfAllPlayersComplete() {
		if (!this.InProgress || !this.AllPlayersFinished) return;

		this.InProgress = false;
		this._finishedLoading = false;
		
		for (int i = 0; i < this.Players.Length; i++)
		{
			if (this.Players[i] != null)
				this.SlotStatus[i] = SlotStatusEnum.NotReady;

			this.SkipRequested[i] = false;
			this.PlayerLoaded[i]     = false;
		}
		
		this.UpdatePlayers(true);
		
		foreach (Client? player in this.Players)
			player?.HandleMatchComplete();
	}
	
	private bool AllPlayersFinished
	{
		get
		{
			for (int i = 0; i < 8; i++)
				if (this.SlotStatus[i] == SlotStatusEnum.Playing)
					return false;
			return true;
		}
	}
	
	public void HandlePlayerComplete(Client client) {
		if(!this.InProgress) return;

		int i = this.FindPlayerFromId(client.UserId);
		
		this.SlotStatus[i] = SlotStatusEnum.Complete;
	}

	public void HandlePlayerFailed(Client client) {
		if (client.CurrentMatch != this) return;

		int id = this.FindPlayerFromId(client.UserId);
		foreach (Client? player in this.Players) {
			player?.HandlePlayerFail(id);
		}
	}

	public void HandlePlayerNoMap(Client client) {
		if (this.InProgress || client.CurrentMatch != this) return;

		int i = this.FindPlayerFromId(client.UserId);

		this.SlotStatus[i] = SlotStatusEnum.NoMap;
		
		this.UpdatePlayers(false);
	}

	public void ToggleLock(int id) {
		if(this.InProgress) return;

		if (id == this.FindPlayerFromId(this.HostId)) return;

		bool locked = this.SlotStatus[id] == SlotStatusEnum.Locked;

		this.SlotStatus[id] = locked ? SlotStatusEnum.Open : SlotStatusEnum.Locked;
		
		this.UpdatePlayers(true);
	}

	public void ResetReady() {
		for (int i = 0; i < this.SlotStatus.Length; i++) {
			SlotStatusEnum status = this.SlotStatus[i];
			
			if(status == SlotStatusEnum.Ready)
				this.SlotStatus[i] = SlotStatusEnum.NotReady;
		}
		
		this.UpdatePlayers(false);
	}
	
	public void ChangeSettings(Match match) {
		if(this.InProgress) return;
		
		bool beatmapChange = this.BeatmapChecksum != match.BeatmapChecksum;
		this.BeatmapChecksum = match.BeatmapChecksum;
		this.BeatmapId       = match.BeatmapId;
                
		this.BeatmapName = match.BeatmapName;
		this.GameName    = match.GameName;

		if (this.PlayMode != match.PlayMode || this.MatchType != match.MatchType)
			this.ActiveMods = 0;

		this.MatchType = match.MatchType;
		this.PlayMode  = match.PlayMode;

		if (beatmapChange)
		{
			this.ResetReady();
			for (int i = 0; i < 8; i++)
				if (this.SlotStatus[i] == SlotStatusEnum.NoMap)
					this.SlotStatus[i] = SlotStatusEnum.NotReady;
		}
		
		this.UpdatePlayers(true);
	}

	public void HandleChangeMods(Enums.Mods mods) {
		if(this.ActiveMods != mods)
			this.ResetReady();
		
		this.ActiveMods = mods;
		
		this.UpdatePlayers(true);
	}

	

	public void HandleUserScoreUpdate(Client client, ScoreFrame frame) {
		frame.Id = (byte)this.FindPlayerFromId(client.UserId);

		foreach (Client? player in this.Players) 
			player?.HandleMatchScoreUpdate(frame);
	}

	public void HandlePlayerGetMap(Client client) {
		if (this.InProgress || client.CurrentMatch != this) return;

		int i = this.FindPlayerFromId(client.UserId);

		this.SlotStatus[i] = SlotStatusEnum.NotReady;
		
		this.UpdatePlayers(false);
	}
	
	public void HandleHostLeft() {
		Logger.Log($"Host left! SlotsUsed:{this.SlotsUsed}");
		
		if (this.SlotsUsed >= 1) {
			for (int i = 0; i < this.SlotStatus.Length; i++) {
				SlotStatusEnum slotStatusEnum = this.SlotStatus[i];
				if ((slotStatusEnum & SlotStatusEnum.HasPlayer) != 0) {
					this.TransferHost(i);
				}
			}
		}
		else {
			this.Disband();
		}
	}

	public void Disband() {
		this.IsActive = false;

		Global.MatchDisband(this);
	}
	
	public void TransferHost(int i) {
		//Dont set the host to a blank slot!
		if (this.Players[i] == null)
			return;

		this.HostId = this.Players[i]!.UserId;
		this.Players[i]!.NotifyOfMatchHostTransfer();
		
		Global.GlobalMatchUpdate(this);
	}
	
	private void UpdatePlayers(bool lobbyUpdate)
	{
		if (!this.IsActive) return;

		//send updates to all players in match.
		for (int i = 0; i < 8; i++) {
			this.Players[i]?.HandleMatchUpdate(this);
		}
		
		// if (this.Players[i] != null)

		if (lobbyUpdate)
			Global.GlobalMatchUpdate(this);
	}

	public void SetSlot(int i, Client? client) {
		if (client == null)
		{
			this.SlotId[i] = -1;
			if (this.SlotStatus[i] != SlotStatusEnum.Locked)
				this.SlotStatus[i] = SlotStatusEnum.Open;
			this.Players[i] = null;
		}
		else
		{
			this.SlotId[i]     = client.UserId;
			this.SlotStatus[i] = SlotStatusEnum.NotReady;
			this.Players[i]    = client;
		}
	}
	
	public Match(Enums.MatchTypes matchType, Enums.PlayModes playMode, string gameName, string beatmapName, string beatmapChecksum, int beatmapId, Enums.Mods activeMods, int hostId) {
		this.GameName        = gameName;
		this.MatchType       = Enums.MatchTypes.Standard;
		this.BeatmapName     = beatmapName;
		this.BeatmapChecksum = beatmapChecksum;
		this.BeatmapId       = beatmapId;
		this.ActiveMods      = activeMods;
		this.HostId          = hostId;
		this.PlayMode        = playMode;

		for (int i = 0; i < SLOT_COUNT; i++) {
			this.SlotStatus[i] = SlotStatusEnum.Open;
			this.SlotId[i]     = -1;
		}
	}

	public Match() {}

	public int SlotsUsed {
		get {
			int count = 0;
			for (int i = 0; i < SLOT_COUNT; i++)
				if ((this.SlotStatus[i] & SlotStatusEnum.HasPlayer) > 0)
					count++;
			return count;
		}
	}

	public int SlotOpenCount {
		get {
			int count = 0;
			for (int i = 0; i < SLOT_COUNT; i++)
				if (this.SlotStatus[i] != SlotStatusEnum.HasPlayer)
					count++;
			return count;
		}
	}

	public int SlotReadyCount {
		get {
			int count = 0;
			for (int i = 0; i < SLOT_COUNT; i++)
				if (this.SlotStatus[i] == SlotStatusEnum.Ready)
					count++;
			return count;
		}
	}

	public int FindPlayerFromId(int userId) {
		int pos = 0;
		while (pos < SLOT_COUNT && this.SlotId[pos] != userId)
			pos++;
		if (pos > 7)
			return -1;
		return pos;
	}

	public override void WriteToStream(Stream s) {
		BanchoWriter writer = new(s);
		writer.Write((byte)this.MatchId);
		writer.Write(this.InProgress);
		writer.Write((byte)this.MatchType);
		writer.Write((short)this.ActiveMods);
		writer.Write(this.GameName);
		writer.Write(this.BeatmapName);
		writer.Write(this.BeatmapId);
		writer.Write(this.BeatmapChecksum);
		for (int i = 0; i < SLOT_COUNT; i++)
			writer.Write((byte)this.SlotStatus[i]);

		for (int i = 0; i < SLOT_COUNT; i++)
			if ((this.SlotStatus[i] & SlotStatusEnum.HasPlayer) > 0)
				writer.Write(this.SlotId[i]);
		writer.Write(this.HostId);

		writer.Write((byte)this.PlayMode);
	}
	public override void ReadFromStream(Stream s) {
		BanchoReader reader = new(s);

		this.MatchId         = reader.ReadByte();
		this.InProgress      = reader.ReadBoolean();
		this.MatchType       = (Enums.MatchTypes)reader.ReadByte();
		this.ActiveMods      = (Enums.Mods)reader.ReadInt16();
		this.GameName        = reader.ReadString();
		this.BeatmapName     = reader.ReadString();
		this.BeatmapId       = reader.ReadInt32();
		this.BeatmapChecksum = reader.ReadString();
		for (int i = 0; i < SLOT_COUNT; i++)
			this.SlotStatus[i] = (SlotStatusEnum)reader.ReadByte();

		for (int i = 0; i < SLOT_COUNT; i++)
			this.SlotId[i] = (this.SlotStatus[i] & SlotStatusEnum.HasPlayer) > 0 ? reader.ReadInt32() : -1;

		this.HostId = reader.ReadInt32();

		this.PlayMode = (Enums.PlayModes)reader.ReadByte();
	}
}
