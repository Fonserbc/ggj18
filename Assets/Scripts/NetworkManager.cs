
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;

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

public class NetworkManager : MonoBehaviour
{
	static NetworkManager _instance = null;
	static public NetworkManager Instance { get { return _instance; } }

	public Constants constants;

	public GameObject ScoreBoards;
	public UnityEngine.UI.Text ScorePlayer1;
	public UnityEngine.UI.Text ScorePlayer2;
	public GameObject PauseMenu;
	public GameObject WarmingScreen;

	Type networkConnectionClass = typeof(NetworkConnection);
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

	ConnectionConfig connectionConfig = null;
	HostTopology topology = null;
	NetworkConnection connection = null;

	enum ConnectState
	{
		None,
		Resolving,
		Resolved,
		Connecting,
		Connected,
		Disconnected,
		Failed
	}

	ConnectState AsyncConnect = ConnectState.None;

	public bool IsConnected { get { return AsyncConnect == ConnectState.Connected; } }

	int channelId = 0;
	int hostId = 0;
	int connectionId = 0;

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

		gameLogic.logic.c = constants;
	}

	enum CustomPacketId : byte
	{
		ID_QUALITY_REPORT = 0,
		ID_QUALITY_REPLY,
		ID_INPUT,
		ID_START
	};

	public void Deinit()
	{
		if (connectionConfig != null)
		{
			if(connection != null)
				connection.Disconnect();

			NetworkTransport.Shutdown();
			connectionConfig = null;
			topology = null;
			connection = null;

			channelId = 0;
			hostId = 0;
			connectionId = 0;
		}

		status = NetworkStatus.Created;
	}

	public void Init(NetworkInitParameters initParams)
	{
		for (int i = 0; i < STORE_INPUT_LENGTH; i++)
		{
			storeInputs[i].updateId = 0;
		}

		switch (initParams.op_id)
		{
			case NetworkInitParameters.Operation.NONE:
				Deinit();
				status = NetworkStatus.Closed;
				break;
			case NetworkInitParameters.Operation.HOST:
				NetworkTransport.Init();
				connectionConfig = new ConnectionConfig();
				channelId = connectionConfig.AddChannel(QosType.Reliable);
				topology = new HostTopology(connectionConfig, 1);
				hostId = NetworkTransport.AddHost(topology, initParams.port);
				connectionId = -1;
				status = NetworkStatus.WaitingPeer;
				break;
			case NetworkInitParameters.Operation.CLIENT:
				NetworkTransport.Init();
				connectionConfig = new ConnectionConfig();
				channelId = connectionConfig.AddChannel(QosType.Reliable);
				topology = new HostTopology(connectionConfig, 1);
				hostId = NetworkTransport.AddHost(topology, initParams.port + 1);

				byte error;
				connectionId = NetworkTransport.Connect(hostId, initParams.address, initParams.port, 0, out error);
				connection = (NetworkConnection)Activator.CreateInstance(networkConnectionClass);
				connection.Initialize(initParams.address, hostId, connectionId, topology);

				status = NetworkStatus.WaitingPeer;
				break;
		}
	}

	public bool IsInit()
	{
		return status != NetworkStatus.Created;
	}

	public void FixedUpdate()
	{
		if (status != NetworkStatus.Closed && status != NetworkStatus.Created && status != NetworkStatus.LocalRunning)
		{
			NetworkEventType networkEvent;

			do
			{
				int connectionIdOut;
				int channelIdOut;

				byte[] recBuffer = new byte[1024];
				int bufferSize = 1024;
				int dataSize;
				byte error;

				networkEvent = NetworkTransport.ReceiveFromHost(hostId, out connectionIdOut, out channelIdOut, recBuffer, bufferSize, out dataSize, out error);
				switch (networkEvent)
				{
					case NetworkEventType.Nothing:
						break;
					case NetworkEventType.ConnectEvent:
						if (connectionIdOut == connectionId)
						{
							//Connected succesfully
							SetWarming(false);
						}
						else
						{
							HandleConnect(connectionIdOut, error);
							SetWarming(true);
						}
						break;
					case NetworkEventType.DataEvent:
						Parse(recBuffer, dataSize);
						break;
					case NetworkEventType.DisconnectEvent:
						connection = null;
						status = NetworkStatus.Closed;
						break;
					default:
						Debug.LogError("Unknown network message type received: " + networkEvent);
						break;
				}
			}
			while (networkEvent != NetworkEventType.Nothing);
		}
			
		bool skip_next_frame = false;
		switch (status)
		{
			case NetworkStatus.Created:
				gameLogic.Deinit();
				break;
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

				ClientWarmingHandle(ref skip_next_frame);
				break;
			case NetworkStatus.ClientWarming:
				ClientWarmingHandle(ref skip_next_frame);
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
					PlayerInput input = new PlayerInput();

					if (PauseMenu != null && PauseMenu.activeSelf)
						input.Reset();
					else
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

                gameLogic.Update(status == NetworkStatus.HostRunning);
				if (!skip_next_frame)
					localUpdateId++;

				break;
			case NetworkStatus.Closed:
				gameLogic.Init();

				if (gameLogic.IsInit())
				{
					status = NetworkStatus.LocalRunning;
				}

				break;
			case NetworkStatus.LocalRunning:
				{
					if (PauseMenu != null && PauseMenu.activeSelf)
					{
					}
					else
					{
						PlayerInput input;
						PlayerInput input2;

						gameLogic.GetNewInput(out input);
						gameLogic.GetNewInput2(out input2);

						gameLogic.TryAddNewFrame();
						uint updateId = gameLogic.NewestFrameId();
						gameLogic.SetInputPlayer1(input, updateId);
						gameLogic.SetInputPlayer2(input2, updateId);
						gameLogic.Update(true);
					}
					
					break;
				}
		}

		if (connection != null)
		{
			connection.FlushChannels();
		}

		// -- UI ---------------------------- //

		if (status == NetworkStatus.ClientWarming || status == NetworkStatus.HostWarming)
		{
			if (WarmingScreen != null && !WarmingScreen.activeSelf)
				WarmingScreen.SetActive(true);
		}
		else
		{
			if (WarmingScreen != null && WarmingScreen.activeSelf)
				WarmingScreen.SetActive(false);
		}

		if (status == NetworkStatus.HostRunning || status == NetworkStatus.ClientRunning || status == NetworkStatus.LocalRunning)
		{
			if (PauseMenu != null)
			{
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
				if (Input.GetButton("StartM1") && !PauseMenu.activeSelf)
					PauseMenu.SetActive(true);
#else
				if (Input.GetButton("Start1") && !PauseMenu.activeSelf)
					PauseMenu.SetActive(true);
#endif
			}

			if (ScoreBoards != null)
			{
				if(!ScoreBoards.activeSelf)
					ScoreBoards.SetActive(true);

				if (ScorePlayer1 != null)
				{
					ScorePlayer1.text = string.Format("{0}", gameLogic.points1);
				}

				if (ScorePlayer2 != null)
				{
					ScorePlayer2.text = string.Format("{0}", gameLogic.points2);
				}
			}
		}
		else
		{
			if (ScoreBoards != null && ScoreBoards.activeSelf)
				ScoreBoards.SetActive(false);

			if (PauseMenu != null && PauseMenu.activeSelf)
				PauseMenu.SetActive(false);
		}
	}

	private void ClientWarmingHandle(ref bool skip_next_frame)
	{
		gameLogic.Init();

		if (startUpdateId != 0 && startUpdateId <= localUpdateId)
		{
			if (startUpdateId != localUpdateId)
				Debug.LogWarning("startUpdateId != localUpdateId");

			if (gameLogic.IsInit())
			{
				SetRunning();
			}
		}
		else if (localAdvantage > remoteAdvantage)
		{
			skip_next_frame = true;
		}

		if (!skip_next_frame)
			localUpdateId++;
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
	}

	private void Parse(byte[] recBuffer, int dataSize)
	{
		NetworkReader reader = new NetworkReader(recBuffer);

		while (reader.Position < dataSize)
		{
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
				default:
					Debug.LogError("Packed ID UKNOWN: " + packetId);
					break;
			}
		}
	}

	private void ReadQuality(NetworkReader reader)
	{
		remoteUpdateId = reader.ReadUInt32();
		uint ack_localUpdateId = reader.ReadUInt32();
		uint travelTime = (localUpdateId - ack_localUpdateId) / 2;
		remoteAdvantage = (short)((int)(remoteUpdateId + travelTime) - (int)ack_localUpdateId);
		localAdvantage = (short)((int)(localUpdateId + travelTime) - (int)remoteUpdateId);
	}

	private void SendQuality(CustomPacketId id_packet)
	{
		NetworkWriter writer = new NetworkWriter();
		writer.Write((byte)id_packet);
		writer.Write(localUpdateId);
		writer.Write(remoteUpdateId);
		connection.SendWriter(writer, channelId);
	}

	private void ReadInput(NetworkReader reader, out PlayerInput input, out uint remoteInputUpdateId)
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

		input.justUp = reader.ReadBoolean();
		input.justDown = reader.ReadBoolean();
		input.justLeft = reader.ReadBoolean();
		input.justRight = reader.ReadBoolean();

		input.up = reader.ReadBoolean();
		input.down = reader.ReadBoolean();
		input.left = reader.ReadBoolean();
		input.right = reader.ReadBoolean();
	}

	void SendInput(PlayerInput input)
	{
		NetworkWriter writer = new NetworkWriter();

		writer.Write((byte)CustomPacketId.ID_INPUT);
		writer.Write(localUpdateId);
		writer.Write(remoteUpdateId);

		writer.Write((double) input.xAxis);
		writer.Write((double) input.yAxis);

		writer.Write(input.justUp);
		writer.Write(input.justDown);
		writer.Write(input.justLeft);
		writer.Write(input.justRight);

		writer.Write(input.up);
		writer.Write(input.down);
		writer.Write(input.left);
		writer.Write(input.right);

		connection.SendWriter(writer, channelId);
	}

	private void ReadStart(NetworkReader reader)
	{
		startUpdateId = reader.ReadUInt32();
	}

	private void SendStart()
	{
		NetworkWriter writer = new NetworkWriter();

		writer.Write((byte) CustomPacketId.ID_START);
		writer.Write(startUpdateId);

		connection.SendWriter(writer, channelId);
	}

	void HandleConnect(int connectionId, byte error)
	{
		if (LogFilter.logDebug)
		{
			Debug.Log("NetworkServerSimple accepted client:" + connectionId);
		}

		if (error != 0)
		{
			Debug.LogError("OnConnectError error:" + error);
			return;
		}

		string address;
		int port;
		NetworkID networkId;
		NodeID node;
		byte error2;
		NetworkTransport.GetConnectionInfo(hostId, connectionId, out address, out port, out networkId, out node, out error2);

		connection = (NetworkConnection) Activator.CreateInstance(networkConnectionClass);
		connection.Initialize(address, hostId, connectionId, topology);
	}
}
