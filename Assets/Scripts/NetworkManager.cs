
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

public struct InputData
{

}

public class GameLogic
{
	public void Init()
	{
		
	}

	public bool IsInit()
	{
		return true;
	}

	public void GetNewInput(out InputData input)
	{

	}

	public bool TryAddNewFrame()
	{
		return true;
	}

	public uint NewestFrameId()
	{
		return 0;
	}

	public bool IsInputPlayer1Legit(uint currentFrameID)
	{
		return false;
	}

	public bool IsInputPlayer2Legit(uint currentFrameID)
	{
		return false;
	}

	public void SetInputPlayer1(InputData input, uint currentFrameID)
	{

	}

	public void SetInputPlayer2(InputData input, uint currentFrameID)
	{

	}

	public void Update()
	{
		throw new NotImplementedException();
	}
}

public class NetworkManager : MonoBehaviour
{

	static NetworkManager _instance = null;
	static public NetworkManager Instance { get { return _instance; } }

	GameLogic gameLogic = new GameLogic();

	public struct StoreInput
	{
		public InputData input;
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

		if (!IsInit())
		{
			NetworkTransport.Init();
			connectionConfig = new ConnectionConfig();
			channelId = connectionConfig.AddChannel(QosType.Reliable);
			topology = new HostTopology(connectionConfig, 16);
		}

		switch (initParams.op_id)
		{
			case NetworkInitParameters.Operation.NONE:
				status = NetworkStatus.Closed;
				break;
			case NetworkInitParameters.Operation.HOST:
				hostId = NetworkTransport.AddHost(topology, initParams.port);
				connectionId = -1;
				status = NetworkStatus.WaitingPeer;
				break;
			case NetworkInitParameters.Operation.CLIENT:
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
					InputData input;
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
					InputData input;
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
						InputData input;
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

	private void ReadInput(BinaryReader reader, out InputData input, out uint remoteInputUpdateId)
	{
		remoteInputUpdateId = reader.ReadUInt32();
		if (remoteUpdateId < remoteInputUpdateId)
			remoteUpdateId = remoteInputUpdateId;

		uint ack_localUpdateId = reader.ReadUInt32();

		uint travelTime = (localUpdateId - ack_localUpdateId) / 2;
		remoteAdvantage = (short)((int)(remoteUpdateId + travelTime) - (int)ack_localUpdateId);
		localAdvantage = (short)((int)(localUpdateId + travelTime) - (int)remoteUpdateId);

		// Y AQUI SE LEE EL INPUT SEGUN TOQUE, POR EJEMPLO
		//input.action = reader.ReadInt32();
		//input.x = reader.ReadInt16();
		//input.y = reader.ReadInt16();
	}

	void SendInput(InputData input)
	{
		MemoryStream stream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(stream);

		writer.Write((byte)CustomPacketId.ID_INPUT);
		writer.Write(localUpdateId);
		writer.Write(remoteUpdateId);

		// Y AQUI SE ESCRIBE EL INPUT SEGUN TOQUE, POR EJEMPLO
		// writer.Write(input.action);
		// writer.Write(input.x);
		// writer.Write(input.y);

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
