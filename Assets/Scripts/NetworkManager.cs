
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkInitParameters
{
	public enum Operation
	{
		NONE,
		HOST,
		CLIENT
	};

	public Operation op_id;
	public string address;
	public ushort port;
}

public struct PlayerInput
{
	public bool legit;
	public float xAxis, yAxis;
	public bool up, down, left, right;
}

public class GameFrame
{
    public bool valid = false;
    public uint frame_id;
    public PlayerInput input_player1 = new PlayerInput();
    public PlayerInput input_player2 = new PlayerInput();
    public GameState state = new GameState();
}

public class GameLogic
{
	uint GaemFrame_index;
	uint oldest_frame;
	uint current_frame;
    uint newest_frame;

	const int GAEMFRAME_BUFFSIZE = 128;
	GameFrame[] MemoryFrame = new GameFrame[GAEMFRAME_BUFFSIZE];

	public void Init()
	{
		for(int i = 0; i < GAEMFRAME_BUFFSIZE; i++)
		{
			MemoryFrame[i] = new GameFrame();
		}
	}

	public bool IsInit()
	{
		return true;
	}

	public void GetNewInput(out PlayerInput input)
	{
		input.legit = true;

		input.xAxis = 0.0f;
		input.yAxis = 0.0f;
		input.up = false;
		input.down = false;
		input.left = false;
		input.right = false;
	}

	public bool TryAddNewFrame()
	{
		if(current_frame != (newest_frame + 1));

		uint Index = (GaemFrame_index + 1) & (GAEMFRAME_BUFFSIZE - 1);
		GameFrame CurrentFrame = MemoryFrame[Index];
		uint NextIndex = (Index + 1) & (GAEMFRAME_BUFFSIZE - 1);
		GameFrame NextFrame = MemoryFrame[NextIndex];

		if (!NextFrame.valid || ((NextFrame.input_player1.legit) && (NextFrame.input_player2.legit)))
		{
			GaemFrame_index = Index;
			newest_frame++;
			CurrentFrame.frame_id = newest_frame;

			//Last frame of the ring has not legit input
			CurrentFrame.input_player1.legit = false;
			CurrentFrame.input_player2.legit = false;
			CurrentFrame.valid = true;

			if (MemoryFrame[0].frame_id < GAEMFRAME_BUFFSIZE)
				oldest_frame = MemoryFrame[0].frame_id;
			else
			{
				int OldestGaemFraem_index = ((int)(GaemFrame_index + 1)) & (GAEMFRAME_BUFFSIZE - 1);
				oldest_frame = MemoryFrame[OldestGaemFraem_index].frame_id;
			}

			return true;
		}

		return false;
	}

	public uint Logic_OldestFrameId()
	{
		return oldest_frame + 1;
	}

	public uint NewestFrameId()
	{
		return newest_frame;
	}

	private int GetIndexFromFrameId(uint frame_id)
	{
		if(!(frame_id >= oldest_frame && frame_id <= newest_frame))
			Debug.LogWarning("!(frame_id >= oldest_frame && frame_id <= newest_frame)");

		return ((int)(GaemFrame_index + (frame_id - newest_frame))) & (GAEMFRAME_BUFFSIZE - 1);
	}

	public bool IsInputPlayer1Legit(uint frame_id)
	{
		int index = GetIndexFromFrameId(frame_id);
		GameFrame frame = MemoryFrame[index];
		return frame.input_player1.legit;
	}

	public bool IsInputPlayer2Legit(uint frame_id)
	{
		int index = GetIndexFromFrameId(frame_id);
		GameFrame frame = MemoryFrame[index];
		return frame.input_player2.legit;
	}

	public void SetInputPlayer1(PlayerInput input, uint currentFrameID)
	{

	}

	public void SetInputPlayer2(PlayerInput input, uint currentFrameID)
	{

	}

	public void Update()
	{
		if (current_frame > newest_frame)
			return;

		if (!(current_frame > oldest_frame))
			Debug.LogWarning("!(current_frame > oldest_frame)");

		int index = GetIndexFromFrameId(current_frame - 1);
		GameFrame frame = MemoryFrame[index];

		while (current_frame <= newest_frame)
		{
			int next_index = GetIndexFromFrameId(current_frame);
			GameFrame next_frame = MemoryFrame[next_index];

            next_frame.state.CopyFrom(frame.state);

			if (!next_frame.input_player1.legit)
			{
				next_frame.input_player1 = frame.input_player1;
				next_frame.input_player1.legit = false;
			}

			if (!next_frame.input_player2.legit)
			{
				next_frame.input_player2 = frame.input_player2;
				next_frame.input_player2.legit = false;
			}

			index = next_index;
			GameFrame last_frame = frame;
			frame = next_frame;

			//Logic_UpdateFrame(frame);
			current_frame++;
		}
	}
}

public class NetworkManager : MonoBehaviour
{

	static NetworkManager _instance = null;
	static public NetworkManager Instance { get { return _instance; } }

	GameLogic gameLogic = new GameLogic();

	public struct StoreInput
	{
		public PlayerInput input;
		public uint updateId;
	};

	const uint STORE_INPUT_LENGTH = 160;
	StoreInput[] storeInputs = new StoreInput[STORE_INPUT_LENGTH];

	enum NetworkStatus : byte
	{
		Created = 0,
		WaitingPeer,
		Closed,
		LocalRunning,
		HostWarming,
		ClientWarming,
		HostRunning,
		ClientRunning,
	}

	NetworkStatus status = NetworkStatus.Created;
	byte counter;
	uint startUpdateId;
	uint localUpdateId;
	uint remoteUpdateId;
	short localAdvantage;
	short remoteAdvantage;

	ConnectionConfig connectionConfig;
	HostTopology topology;
	int channelId;
	int hostId;
	int connectionId;

	void Awake()
	{
		DontDestroyOnLoad(gameObject);

		if (_instance != null)
		{
			Debug.LogError("-- DON'T CREATE MORE THAN ONE NETWORKMANAGER INSTANCES YOU SON OF A BEACH --");
		}
		else
		{
			_instance = this;
		}
	}

	enum CustomPacketId : byte
	{
		ID_QUALITY_REPORT = 0,
		ID_QUALITY_REPLY,
		ID_INPUT,
		ID_START
	};

	public void Init(NetworkInitParameters initParams)
	{
		for (int i = 0; i < STORE_INPUT_LENGTH; i++)
		{
			storeInputs[i].updateId = 0;
		}

		switch (initParams.op_id)
		{
			case NetworkInitParameters.Operation.NONE:
				NetworkTransport.Shutdown();
				status = NetworkStatus.Closed;
				break;
			case NetworkInitParameters.Operation.HOST:
				NetworkTransport.Init();
				connectionConfig = new ConnectionConfig();
				channelId = connectionConfig.AddChannel(QosType.Reliable);
				topology = new HostTopology(connectionConfig, 16);
				hostId = NetworkTransport.AddHost(topology, initParams.port);
				connectionId = -1;
				status = NetworkStatus.WaitingPeer;
				break;
			case NetworkInitParameters.Operation.CLIENT:
				NetworkTransport.Init();
				connectionConfig = new ConnectionConfig();
				channelId = connectionConfig.AddChannel(QosType.Reliable);
				topology = new HostTopology(connectionConfig, 16);
				hostId = NetworkTransport.AddHost(topology, initParams.port);
				byte error;
				connectionId = NetworkTransport.Connect(hostId, initParams.address, initParams.port, 0, out error);
				status = NetworkStatus.WaitingPeer;
				break;
		}
	}

	public bool IsInit()
	{
		return status != NetworkStatus.Created;
	}

	public void Update()
	{
		if (!IsInit())
			return;

		int recHostIdOut;
		int connectionIdOut;
		int channelIdOut;
		byte[] recBuffer = new byte[1024];
		int bufferSize = 1024;
		int dataSize;
		byte error;
		NetworkEventType recData = NetworkTransport.Receive(out recHostIdOut, out connectionIdOut, out channelIdOut, recBuffer, bufferSize, out dataSize, out error);
		switch (recData)
		{
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent:
				if (connectionIdOut == connectionId)
				{
					//Connected succesfully
				}
				else
				{
					connectionId = connectionIdOut;
					SetWarming(true);
				}
				break;
			case NetworkEventType.DataEvent:
				Parse(recBuffer, dataSize);
				break;
			case NetworkEventType.DisconnectEvent:
				status = NetworkStatus.Closed;
				break;
		}

		bool skip_next_frame = false;
		switch (status)
		{
			case NetworkStatus.WaitingPeer:
				break;
			case NetworkStatus.HostWarming:
				SendQuality(CustomPacketId.ID_QUALITY_REPORT);

				int diff = (int) localAdvantage * (int) remoteAdvantage;
				if (diff >= -9 && startUpdateId == 0)
				{
					counter++;
					if (counter > 30)
					{
						startUpdateId = localUpdateId + 250;
						SendStart();
					}
				}
				else
				{
					counter = 0;
				}
				break;
			case NetworkStatus.ClientWarming:
				if (startUpdateId != 0 && startUpdateId <= localUpdateId)
				{
					if (startUpdateId != localUpdateId)
						Debug.LogWarning("startUpdateId != localUpdateId");
					
					SetRunning();
				}
				else if (localAdvantage > remoteAdvantage)
				{
					skip_next_frame = true;
				}

				if (!skip_next_frame)
					localUpdateId++;

				break;
			case NetworkStatus.HostRunning:
			case NetworkStatus.ClientRunning:
				if (localAdvantage > remoteAdvantage)
				{
					counter++;
					if (counter > 3)
					{
						counter = 0;
						skip_next_frame = true;
					}
				}
				else
				{
					counter = 0;
				}

				//NEW FRAME
				skip_next_frame = skip_next_frame || !gameLogic.TryAddNewFrame();
				if (skip_next_frame)
				{
					SendQuality(CustomPacketId.ID_QUALITY_REPORT);
				}
				else
				{
					PlayerInput input;
					gameLogic.GetNewInput(out input);
					SendInput(input);

					uint currentFrameID = (localUpdateId - startUpdateId);
					if (status == NetworkStatus.HostRunning)
					{
						if (gameLogic.IsInputPlayer1Legit(currentFrameID))
							Debug.LogWarning("gameLogic.IsInputPlayer1Legit(currentFrameID");

						gameLogic.SetInputPlayer1(input, currentFrameID);
					}
					else
					{
						if(gameLogic.IsInputPlayer2Legit(currentFrameID))
							Debug.LogWarning("gameLogic.IsInputPlayer2Legit(currentFrameID)");

						gameLogic.SetInputPlayer2(input, currentFrameID);
					}
				}

				uint frame_id = gameLogic.NewestFrameId();
				for (int i = 0; i < STORE_INPUT_LENGTH; i++)
				{
					uint remoteFrameID = (storeInputs[i].updateId - startUpdateId);
					if (storeInputs[i].updateId != 0 && remoteFrameID <= frame_id)
					{
						if (status == NetworkStatus.HostRunning)
						{
							gameLogic.SetInputPlayer2(storeInputs[i].input, remoteFrameID);
						}
						else
						{
							gameLogic.SetInputPlayer1(storeInputs[i].input, remoteFrameID);
						}

						storeInputs[i].updateId = 0;
					}
				}

				gameLogic.Update();
				if (!skip_next_frame)
					localUpdateId++;

				break;
			case NetworkStatus.Closed:
				if (!gameLogic.IsInit())
				{
					gameLogic.Init();
				}

				status = NetworkStatus.LocalRunning;
				break;
			case NetworkStatus.LocalRunning:
				{
					PlayerInput input;
					gameLogic.GetNewInput(out input);
					gameLogic.TryAddNewFrame();
					uint updateId = gameLogic.NewestFrameId();
					gameLogic.SetInputPlayer1(input, updateId);
					gameLogic.SetInputPlayer2(input, updateId);
					gameLogic.Update();
					break;
				}
		}
	}

	private void SetWarming(bool host)
	{
		status = host ? NetworkStatus.HostWarming : NetworkStatus.ClientWarming;
		counter = 0;
		localUpdateId = 0;
		remoteUpdateId = 0;
		localAdvantage = 0;
		remoteAdvantage = 0;
		startUpdateId = 0;
	}

	private void SetRunning()
	{
		status = (status == NetworkStatus.HostWarming) ? NetworkStatus.HostRunning : NetworkStatus.ClientRunning;
		gameLogic.Init();
	}

	private void Parse(byte[] recBuffer, int dataSize)
	{
		MemoryStream stream = new MemoryStream(recBuffer, 0, dataSize, false);
		BinaryReader reader = new BinaryReader(stream);
		CustomPacketId packetId = (CustomPacketId)reader.ReadByte();

		switch (packetId)
		{
			case CustomPacketId.ID_QUALITY_REPORT:
				switch (status)
				{
					case NetworkStatus.HostRunning:
					case NetworkStatus.ClientRunning:
					case NetworkStatus.HostWarming:
					case NetworkStatus.ClientWarming:
						ReadQuality(reader);
						SendQuality(CustomPacketId.ID_QUALITY_REPLY);
						break;
				}
				break;
			case CustomPacketId.ID_QUALITY_REPLY:
				switch (status)
				{
					case NetworkStatus.HostRunning:
					case NetworkStatus.ClientRunning:
					case NetworkStatus.HostWarming:
					case NetworkStatus.ClientWarming:
						ReadQuality(reader);
						break;
				}
				break;
			case CustomPacketId.ID_INPUT:
				switch (status)
				{
					case NetworkStatus.HostRunning:
					case NetworkStatus.ClientRunning:
						PlayerInput input;
						uint remoteInputUpdateId;
						ReadInput(reader, out input, out remoteInputUpdateId);
						int i = 0;
						for (; i < STORE_INPUT_LENGTH; i++)
						{
							if (storeInputs[i].updateId == 0)
							{
								storeInputs[i].updateId = remoteInputUpdateId;
								storeInputs[i].input = input;
								break;
							}
						}
						break;
				}
				break;
			case CustomPacketId.ID_START:
				ReadStart(reader);
				break;
		}
	}

	private void ReadQuality(BinaryReader reader)
	{
		remoteUpdateId = reader.ReadUInt32();
		uint ack_localUpdateId = reader.ReadUInt32();
		uint travelTime = (localUpdateId - ack_localUpdateId) / 2;
		remoteAdvantage = (short)((int)(remoteUpdateId + travelTime) - (int)ack_localUpdateId);
		localAdvantage = (short)((int)(localUpdateId + travelTime) - (int)remoteUpdateId);
	}

	private void SendQuality(CustomPacketId id_packet)
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);
		writer.Write((byte)id_packet);
		writer.Write(localUpdateId);
		writer.Write(remoteUpdateId);

		byte[] msg = stream.ToArray();
		byte error;
		NetworkTransport.Send(hostId, connectionId, channelId, msg, msg.Length, out error);
	}

	private void ReadInput(BinaryReader reader, out PlayerInput input, out uint remoteInputUpdateId)
	{
		remoteInputUpdateId = reader.ReadUInt32();
		if (remoteUpdateId < remoteInputUpdateId)
			remoteUpdateId = remoteInputUpdateId;

		uint ack_localUpdateId = reader.ReadUInt32();

		uint travelTime = (localUpdateId - ack_localUpdateId) / 2;
		remoteAdvantage = (short)((int)(remoteUpdateId + travelTime) - (int)ack_localUpdateId);
		localAdvantage = (short)((int)(localUpdateId + travelTime) - (int)remoteUpdateId);

		input.legit = true;
		input.xAxis = (float) reader.ReadDouble();
		input.yAxis = (float) reader.ReadDouble();
		input.up = reader.ReadBoolean();
		input.down = reader.ReadBoolean();
		input.left = reader.ReadBoolean();
		input.right = reader.ReadBoolean();
	}

	void SendInput(PlayerInput input)
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);

		writer.Write((byte)CustomPacketId.ID_INPUT);
		writer.Write(localUpdateId);
		writer.Write(remoteUpdateId);

		writer.Write((double) input.xAxis);
		writer.Write((double) input.yAxis);
		writer.Write(input.up);
		writer.Write(input.down);
		writer.Write(input.left);
		writer.Write(input.right);

		byte[] msg = stream.ToArray();
		byte error;
		NetworkTransport.Send(hostId, connectionId, channelId, msg, msg.Length, out error);
	}

	private void ReadStart(BinaryReader reader)
	{
		startUpdateId = reader.ReadUInt32();
	}

	private void SendStart()
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);

		writer.Write((byte)CustomPacketId.ID_START);
		writer.Write(startUpdateId);

		byte[] msg = stream.ToArray();
		byte error;
		NetworkTransport.Send(hostId, connectionId, channelId, msg, msg.Length, out error);
	}
}
