using UnityEngine;
using UnityEngine.SceneManagement;

static public class GamePadUtils
{
	public const float deadZoneLow = 0.3f;
	public const float deadZoneHigh = 1.0f;

	static public void ApplyRadialDeadZone(out float pOutX, out float pOutY, float x, float y)
	{
		float mag = Mathf.Sqrt(x * x + y * y);

		if (mag > deadZoneLow)
		{
			float legalRange = 1.0f - (deadZoneHigh - deadZoneLow);
			float normalizedMag = Mathf.Min(1.0f, (mag - deadZoneLow) / legalRange);
			float scale = normalizedMag / mag;
			pOutX = x * scale;
			pOutY = y * scale;
		}
		else
		{
			pOutX = 0.0f;
			pOutY = 0.0f;
		}
	}
}

public struct PlayerInput
{
	public bool legit;
	public float xAxis, yAxis;

	public bool justUp, justDown, justLeft, justRight;
	public bool up, down, left, right;

	public void Reset()
	{
		legit = true;
		xAxis = yAxis = 0.0f;
		justUp = justDown = justLeft = justRight = up = down = left = right = false;
	}
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
	uint GaemFrame_index = GAEMFRAME_BUFFSIZE;
	uint oldest_frame;
	uint current_frame;
	uint newest_frame;

	public Logic logic = new Logic();
	private Visuals visuals = null;

	public enum InitState
	{
		NONE,
		LOADING,
		LOADED,
	}

	InitState initState = InitState.NONE;

	const int GAEMFRAME_BUFFSIZE = 128;
	GameFrame[] MemoryFrame = new GameFrame[GAEMFRAME_BUFFSIZE];

	public void Deinit()
	{
		switch (initState)
		{
			case InitState.NONE:
				break;
			case InitState.LOADING:
			case InitState.LOADED:
				SceneManager.LoadScene(1, LoadSceneMode.Single);
				initState = InitState.NONE;
				break;
		}
	}

	public void Init()
	{
		switch (initState)
		{
			case InitState.NONE:
				SceneManager.LoadScene(2, LoadSceneMode.Single);

				for (int i = 0; i < GAEMFRAME_BUFFSIZE; i++)
				{
					MemoryFrame[i] = new GameFrame();
				}

				GaemFrame_index = 0;
				oldest_frame = 0;
				current_frame = 1;
				newest_frame = 0;

				initState = InitState.LOADING;
				break;
			case InitState.LOADING:
				GameObject visualsObject = GameObject.FindGameObjectWithTag("GameController");
				if (visualsObject != null)
				{
					visuals = visualsObject.GetComponent<Visuals>();
					if (visuals != null)
					{
						GameFrame frame = MemoryFrame[0];
						frame.state = logic.InitFirstState(visuals);
						initState = InitState.LOADED;
					}
				}
				break;
			case InitState.LOADED:
				break;
		}
	}

	public bool IsInit()
	{
		return initState == InitState.LOADED;
	}

	public void GetNewInput(out PlayerInput input)
	{
		input.legit = true;

		float xAxis = Input.GetAxisRaw("Horizontal1");
		float yAxis = Input.GetAxisRaw("Vertical1");

		GamePadUtils.ApplyRadialDeadZone(out input.xAxis, out input.yAxis, xAxis, yAxis);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        input.justUp = Input.GetButtonDown("YM1");
		input.justDown = Input.GetButtonDown("AM1");
		input.justLeft = Input.GetButtonDown("XM1");
		input.justRight = Input.GetButtonDown("BM1");

		input.up = Input.GetButton("YM1");
		input.down = Input.GetButton("AM1");
		input.left = Input.GetButton("XM1");
		input.right = Input.GetButton("BM1");
#else
		input.justUp = Input.GetButtonDown("Y1");
		input.justDown = Input.GetButtonDown("A1");
		input.justLeft = Input.GetButtonDown("X1");
		input.justRight = Input.GetButtonDown("B1");

		input.up = Input.GetButton("Y1");
		input.down = Input.GetButton("A1");
		input.left = Input.GetButton("X1");
		input.right = Input.GetButton("B1");
#endif
    }

	public void GetNewInput2(out PlayerInput input)
	{
		input.legit = true;

		float xAxis = Input.GetAxis("Horizontal2");
		float yAxis = Input.GetAxis("Vertical2");

		GamePadUtils.ApplyRadialDeadZone(out input.xAxis, out input.yAxis, xAxis, yAxis);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		input.justUp = Input.GetButtonDown("YM2");
		input.justDown = Input.GetButtonDown("AM2");
		input.justLeft = Input.GetButtonDown("XM2");
		input.justRight = Input.GetButtonDown("BM2");

		input.up = Input.GetButton("YM2");
		input.down = Input.GetButton("AM2");
		input.left = Input.GetButton("XM2");
		input.right = Input.GetButton("BM2");
#else
		input.justUp = Input.GetButtonDown("Y2");
        input.justDown = Input.GetButtonDown("A2");
        input.justLeft = Input.GetButtonDown("X2");
        input.justRight = Input.GetButtonDown("B2");

        input.up = Input.GetButton("Y2");
        input.down = Input.GetButton("A2");
        input.left = Input.GetButton("X2");
        input.right = Input.GetButton("B2");
#endif
    }

    public bool TryAddNewFrame()
	{
		if (current_frame != (newest_frame + 1)) ;

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
		if (!(frame_id >= oldest_frame && frame_id <= newest_frame))
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

	public void SetInputPlayer1(PlayerInput input, uint frame_id)
	{
		int index = GetIndexFromFrameId(frame_id);
		GameFrame frame = MemoryFrame[index];
		SetInput(ref frame.input_player1, input, frame_id);
	}

	public void SetInputPlayer2(PlayerInput input, uint frame_id)
	{
		int index = GetIndexFromFrameId(frame_id);
		GameFrame frame = MemoryFrame[index];
		SetInput(ref frame.input_player2, input, frame_id);
	}

	private void SetInput(ref PlayerInput dest, PlayerInput input, uint frame_id)
	{
		if (!dest.legit)
		{
			bool predictionWasRight = (
				dest.xAxis == input.xAxis && dest.yAxis == input.yAxis &&
				dest.up == input.up && dest.down == input.down &&
				dest.left == input.left && dest.right == input.right &&
				dest.justUp == input.justUp && dest.justDown == input.justDown &&
				dest.justLeft == input.justLeft && dest.justRight == input.justRight
			);

			if (!predictionWasRight)
			{
				//rollback until
				if (current_frame > frame_id)
					current_frame = frame_id;
			}

			dest = input;
			dest.legit = true; //Now is legit :)
		}
	}

	public void Update(bool isHost)
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
			frame = next_frame;

			logic.UpdateState(ref frame);
			current_frame++;
		}

		visuals.UpdateFrom(frame);
		visuals.ownPlayer = isHost ? 0 : 1;

		points1 = frame.state.players[0].points;
		points2 = frame.state.players[1].points;
		winnerPlayer = frame.state.winnerPlayer;
	}

	public int points1;
	public int points2;
	public int winnerPlayer;
}