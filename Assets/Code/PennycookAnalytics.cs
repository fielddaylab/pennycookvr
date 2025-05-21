using UnityEngine;
using System.Globalization;
using System.Text;
using BeauData;
using BeauUtil;
using FieldDay;
using FieldDay.Data;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using FieldDay.XR;
using System;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Collections;

namespace Pennycook.Data {
//[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct LogGazeData
{
    public unsafe fixed float pos[3];
    public unsafe fixed float rot[4];

    public void Write(Vector3 pos, Quaternion rot) {
        unsafe {
            fixed(float* pPos = this.pos) {
                *(Vector3*) pPos = pos;
            }
            fixed (float* pRot = this.rot) {
                *(Quaternion*) pRot = rot;
            }
        }
    }
	
	/*public void WritePos(Vector3 pos) {
		unsafe {
            fixed(float* pPos = this.pos) {
                *(Vector3*) pPos = pos;
            }
        }
	}*/
}

public static class RunMode {
  public enum ApplicationRunMode {
    Device,
    Editor,
    Simulator
  }
  public static ApplicationRunMode Current {
    get {
      #if UNITY_EDITOR
      return UnityEngine.Device.Application.isEditor && !UnityEngine.Device.Application.isMobilePlatform ? ApplicationRunMode.Editor : ApplicationRunMode.Simulator;
      #else
      return ApplicationRunMode.Device;
      #endif
    }
  }
}

public enum HandType : byte {
    LEFT,
    RIGHT
}

[Serializable]
public struct GrabLogInfo {
    public Vector3 pos;
    public Quaternion rot;
    public bool is_grab_toggle;
    public HandType hand;

    public GrabLogInfo(Vector3 position, Quaternion rotation, bool isGrab, HandType h) {
        pos = position;
        rot = rotation;
        is_grab_toggle = isGrab;
        hand = h;
    }
}

public class PennycookAnalytics : SharedStateComponent
{
	public static bool FirebaseEnabled { get; set; }
    public static int logVersion = 1;
    
	static string _DB_NAME = "PENNYCOOK";

    [NonSerialized] float seconds_at_start = 0f;
	
	OGD.OGDLog _ogdLog;
	
	OGD.FirebaseConsts _firebase;

	[SerializeField]
	bool _loggingEnabled = true;

    [NonSerialized] int _viewportDataCount = 0;
    const int MAX_VIEWPORT_DATA = 36;
    LogGazeData[] _viewportData = new LogGazeData[MAX_VIEWPORT_DATA];
	LogGazeData[] _leftHandData = new LogGazeData[MAX_VIEWPORT_DATA];
	LogGazeData[] _rightHandData = new LogGazeData[MAX_VIEWPORT_DATA];

    StringBuilder m_GazeBuilder = new StringBuilder(2048);
	StringBuilder m_RotBuilder = new StringBuilder(128);
	StringBuilder m_PosBuilder = new StringBuilder(128);
	
	[NonSerialized] private string m_HardwareId;
    [NonSerialized] private bool m_Debug = false;

    void Start()
    {
		VRGame.Events.Register<int>(GameEvents.DayCompleted, LogDayCompleted);
        VRGame.Events.Register<GrabLogInfo>(GameEvents.PlayerGrab, LogGrabGesture);
        VRGame.Events.Register<GrabLogInfo>(GameEvents.PlayerRelease, LogGrabRelease);
        
        m_HardwareId = GenerateHardwareId();

        //Debug.Log("Starting analytics");
		OGD.OGDLogConsts c = new OGD.OGDLogConsts();
		c.AppId = _DB_NAME;
		c.AppVersion = UnityEngine.Application.version;
		c.ClientLogVersion = logVersion;
		_ogdLog = new OGD.OGDLog(c);

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		
		//if(RunMode.Current != RunMode.ApplicationRunMode.Device) {
			_loggingEnabled = false;
		//}
		
        if (!string.IsNullOrEmpty(_firebase.ApiKey)) {
            _ogdLog.UseFirebase(_firebase);
        }
        
        _ogdLog.SetDebug(m_Debug);
    }
	
	void OnDestroy()
	{
		if(_ogdLog != null)
		{
			_ogdLog.Dispose();
			_ogdLog = null;
		}
	}

    public void LogDayBegin(int beginDay) {
        _ogdLog.BeginEvent("day_begin");
        _ogdLog.EventParam("day_index", beginDay);
        _ogdLog.SubmitEvent();
    }

    private void LogDayCompleted(int completedDay) {
        _ogdLog.BeginEvent("day_complete");
        _ogdLog.EventParam("day_index", completedDay);
        _ogdLog.SubmitEvent();
    }

    private void LogGazeGameState(Vector3 pos, Quaternion quat)
    {
		m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
		m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
		
        _ogdLog.GameStateParam("pos", m_PosBuilder.ToString());
        _ogdLog.GameStateParam("rot", m_RotBuilder.ToString());
    }

    #region Logging
	
	public void SetUserID(int code)
	{
		if(_loggingEnabled)
		{
			_ogdLog.SetUserId(code.ToString());
		}
	}
	
	void SetGameState()
	{		
        XRUserRig player = Find.State<XRUserRig>();
        PlayerProgressState state = Find.State<PlayerProgressState>();
        PlayerMovementState moveState = Find.State<PlayerMovementState>();

		if(player != null)
        {
            _ogdLog.BeginGameState();
            _ogdLog.GameStateParam("seconds_from_launch", UnityEngine.Time.time-seconds_at_start);
            _ogdLog.GameStateParam("day", state.DayIndex+1);
            _ogdLog.GameStateParam("location", PlayerMovementUtility.IsInside(Find.State<PlayerMovementState>()) ? "INSIDE" : "OUTSIDE");

            LogGazeGameState(player.Head.transform.position, player.Head.transform.rotation);
            
            //_ogdLog.GameStateParam("margo_mode", state.DayIndex+1);
            //_ogdLog.GameStateParam("margo_taskbar", state.DayIndex+1);

            _ogdLog.SubmitGameState();
        }
	}
	
	public void LogSessionStart()
	{
		//Debug.Log("Session start");
        
		if(_loggingEnabled)
		{	
            seconds_at_start = UnityEngine.Time.time;
			
            SetGameState();
			
			_ogdLog.ResetSessionId();
            _ogdLog.BeginEvent("session_start");
            _ogdLog.SubmitEvent();
        }
	}
	

	public void LogStartGame()
	{
        if(_loggingEnabled)
		{
            
			SetGameState();
            
			/*long sessionID = _ogdLog.GetSessionId();
			UnityEngine.Random.seed = (int)sessionID;
			RNG.Instance = new System.Random((int)sessionID);
			
			//RNG.Instance.
			
            _ogdLog.BeginEvent("device_identifier");
            _ogdLog.EventParam("hardware_uuid", m_HardwareId);
            _ogdLog.SubmitEvent();*/
			
            _ogdLog.BeginEvent("game_start");
            _ogdLog.SubmitEvent();
        }
	}
	
	public void LogLevelComplete()
	{
        if(_loggingEnabled)
		{
			SetGameState();
			
            _ogdLog.BeginEvent("level_complete");
            _ogdLog.SubmitEvent();

        }
	}

    public void LogHeadsetOn()
    {
		if(_loggingEnabled)
		{
            //seconds_at_start = UnityEngine.Time.time;
            SetGameState();
			
			_ogdLog.BeginEvent("headset_on");
			_ogdLog.SubmitEvent();

		}
    }
	
	public void LogHeadsetOff()
    {
		if(_loggingEnabled)
		{
			SetGameState();
			
			_ogdLog.BeginEvent("headset_off");
			_ogdLog.SubmitEvent();

		}
    }

    public void LogGrabGesture(GrabLogInfo info)
    {
        if(_loggingEnabled)
		{
			SetGameState();
			
			//m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
			//m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
            m_PosBuilder.Clear().Append("[").AppendNoAlloc(info.pos.x, 3).Append(',').AppendNoAlloc(info.pos.y, 3).Append(',').AppendNoAlloc(info.pos.z, 3).Append(',').Append("]");
		    m_RotBuilder.Clear().Append("[").AppendNoAlloc(info.rot.x, 3).Append(',').AppendNoAlloc(info.rot.y, 3).Append(',').AppendNoAlloc(info.rot.z, 3).Append(',').AppendNoAlloc(info.rot.w, 3).Append("]");
		
            _ogdLog.BeginEvent("grab_gesture");
            _ogdLog.EventParam("pos", m_PosBuilder.ToString());
            _ogdLog.EventParam("rot", m_RotBuilder.ToString());
            _ogdLog.EventParam("is_grab_toggle", info.is_grab_toggle);
            _ogdLog.EventParam("hand", EnumLookup.Get(info.hand));
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabRelease(GrabLogInfo info)
    {
        if(_loggingEnabled)
		{
			SetGameState();
			
			//m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
			//m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
            m_PosBuilder.Clear().Append("[").AppendNoAlloc(info.pos.x, 3).Append(',').AppendNoAlloc(info.pos.y, 3).Append(',').AppendNoAlloc(info.pos.z, 3).Append(',').Append("]");
		    m_RotBuilder.Clear().Append("[").AppendNoAlloc(info.rot.x, 3).Append(',').AppendNoAlloc(info.rot.y, 3).Append(',').AppendNoAlloc(info.rot.z, 3).Append(',').AppendNoAlloc(info.rot.w, 3).Append("]");
		
            _ogdLog.BeginEvent("release_gesture");
            _ogdLog.EventParam("pos", m_PosBuilder.ToString());
            _ogdLog.EventParam("rot", m_RotBuilder.ToString());
            _ogdLog.EventParam("is_grab_toggle", info.is_grab_toggle);
            _ogdLog.EventParam("hand", EnumLookup.Get(info.hand));
            _ogdLog.SubmitEvent();
        }
    }

    public void LogAudioStarted(VoxRequestHandle h, StringHash32 lineCode)
    {
        if(_loggingEnabled)
		{
			SetGameState();

            string speaker = ReflectionCache.AnalyticsNameUpper(VoxUtility.GetCharacterId(h).ToDebugString());
            //Debug.Log("AUDIO STARTED: " + lineCode.ToDebugString() + " " + speaker);

            _ogdLog.BeginEvent("dialog_audio_start");
            _ogdLog.EventParam("dialog_id", lineCode.ToDebugString());
            _ogdLog.EventParam("dialog_type", "STORY");
            _ogdLog.EventParam("speaker", speaker);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogAudioComplete(VoxRequestHandle h, StringHash32 lineCode)
    {
        if(_loggingEnabled)
		{
			SetGameState();
			
            string speaker = ReflectionCache.AnalyticsNameUpper(VoxUtility.GetCharacterId(h).ToDebugString());
            //Debug.Log("AUDIO COMPLETE: " + lineCode.ToDebugString() + " " + speaker);

            _ogdLog.BeginEvent("dialog_audio_end");
            _ogdLog.EventParam("dialog_id", lineCode.ToDebugString());
            _ogdLog.EventParam("dialog_type", "STORY");
            _ogdLog.EventParam("speaker", speaker);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogObjectHighlighted(string name, bool isNormal)
    {
        if(_loggingEnabled)
		{
			SetGameState();

            _ogdLog.BeginEvent("object_highlighted");
            _ogdLog.EventParam("highlight_object", name);
            _ogdLog.EventParam("highlight_type", isNormal ? "OBJECT" : "DESTINATION");
            _ogdLog.SubmitEvent();
        }	
    }

    public void LogObjectUnhighlighted(string name, bool isNormal)
    {
        if(_loggingEnabled)
		{
			SetGameState();
            
            _ogdLog.BeginEvent("object_unhighlighted");
            _ogdLog.EventParam("highlight_object", name);
            _ogdLog.EventParam("highlight_type", isNormal ? "OBJECT" : "DESTINATION");
            _ogdLog.SubmitEvent();
        }	
    }
    
    public void LogArgoHelp(bool left)
    {
        if(_loggingEnabled)
		{
			SetGameState();
			
            _ogdLog.BeginEvent("click_argo_help");
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogArgoFun(bool left)
    {
        if(_loggingEnabled)
		{
			SetGameState();
			
            _ogdLog.BeginEvent("click_argo_funfact");
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabStationHandle(bool left)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_station_handle");
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseStationHandle(bool left)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("release_station_handle");
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabWorkBenchHandle(bool left, float height)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_workbench_handle");
            _ogdLog.EventParam("start_height", height);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseWorkBenchHandle(bool left, float height)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("release_workbench_handle");
            _ogdLog.EventParam("end_height", height);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogTestUplink(bool left, int sceneIndex)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            string stationName = "";
            if(sceneIndex == 0)
            {
                stationName = "WEST";
            }
            else if(sceneIndex == 1)
            {
                stationName = "NORTHWEST";
            }
            else if(sceneIndex == 2)
            {
                stationName = "SOUTH";
            }

            _ogdLog.BeginEvent("click_test_uplink");
            _ogdLog.EventParam("station_name", stationName);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabTrash(bool left, float angle)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_trash");
            _ogdLog.EventParam("start_angle", angle);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseTrash(bool left, float angle)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("release_trash");
            _ogdLog.EventParam("end_angle", angle);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabPuzzleObject(bool left, string objectName)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_puzzle_object");
            _ogdLog.EventParam("object", objectName);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleasePuzzleObject(bool left, string objectName, Vector3 pos, Quaternion quat)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
		    m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
		
            _ogdLog.BeginEvent("release_puzzle_object");
            _ogdLog.EventParam("object", objectName);
            _ogdLog.EventParam("pos", m_PosBuilder.ToString());
            _ogdLog.EventParam("rot", m_RotBuilder.ToString());
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    //left param is tricky here...
    public void LogPlacePuzzleObject(bool left, string objectName, string destination)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("place_puzzle_object");
            _ogdLog.EventParam("object", objectName);
            _ogdLog.EventParam("destination", destination);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    //todo hook up still...
    public void LogPlaceArgoToSled(bool left)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("place_argo_to_sled");
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogDiscardObject( string objectName)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("discard_object");
            _ogdLog.EventParam("object", objectName);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogLocationTransition(string locationName)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("location_transition");
            _ogdLog.EventParam("to", locationName);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogStartPuzzle(string puzzleName)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("start_puzzle");
            _ogdLog.EventParam("puzzle", puzzleName);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogCompletePuzzle(string puzzleName)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("complete_puzzle");
            _ogdLog.EventParam("puzzle", puzzleName);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabSolarHandle(bool left, float startAngle, int greenBars)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_solar_handle");
            _ogdLog.EventParam("start_angle", startAngle);
            _ogdLog.EventParam("start_alignment", greenBars);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseSolarHandle(bool left, float endAngle, int greenBars)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("release_solar_handle");
            _ogdLog.EventParam("end_angle", endAngle);
            _ogdLog.EventParam("end_alignment", greenBars);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabDoorHandle(bool left, float startAngle)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_logger_handle");
            _ogdLog.EventParam("start_angle", startAngle);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseDoorHandle(bool left, float endAngle)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("release_logger_handle");
            _ogdLog.EventParam("end_angle", endAngle);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabDataPuck(bool left, string shape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_data_puck");
            _ogdLog.EventParam("puck_shape", shape);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseDataPuck(bool left, string shape, Vector3 pos, Quaternion quat)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
		    m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
		
            _ogdLog.BeginEvent("release_data_puck");
            _ogdLog.EventParam("puck_shape", shape);
            _ogdLog.EventParam("pos", m_PosBuilder.ToString());
            _ogdLog.EventParam("rot", m_RotBuilder.ToString());
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogPlaceDataPuck(bool left, string objectName)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("place_data_puck");
            _ogdLog.EventParam("puck_shape", objectName);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogRotateDrawer(bool left)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("click_rotate_drawer");
            //todo add drawer contents
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabPropeller(bool left, string shape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_propeller");
            _ogdLog.EventParam("propeller_shape", shape);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogPlacePropeller(bool left, string shape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("place_propeller");
            _ogdLog.EventParam("propeller_shape", shape);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleasePropeller(bool left, string shape, Vector3 pos, Quaternion quat)
    {
        if(_loggingEnabled)
        {
            SetGameState();
            
            m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
		    m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
		
            _ogdLog.BeginEvent("release_propeller");
            //todo - add propeller shape
            _ogdLog.EventParam("propeller_shape", shape);
            _ogdLog.EventParam("pos", m_PosBuilder.ToString());
            _ogdLog.EventParam("rot", m_RotBuilder.ToString());
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGrabBatteryComponent(bool left, string shape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("grab_battery_component");
            _ogdLog.EventParam("battery_shape", shape);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogPlaceBatteryComponent(bool left, string shape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("place_battery_component");
            _ogdLog.EventParam("battery_shape", shape);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogReleaseBatteryComponent(bool left, string shape, Vector3 pos, Quaternion quat)
    {
        if(_loggingEnabled)
        {
            SetGameState();
            
            m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
		    m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
		
            _ogdLog.BeginEvent("release_battery_component");
            //todo - add propeller shape
            _ogdLog.EventParam("battery_shape", shape);
            _ogdLog.EventParam("pos", m_PosBuilder.ToString());
            _ogdLog.EventParam("rot", m_RotBuilder.ToString());
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogClickTemperatureComponent(bool left, int buttonPosition, string oldShape, string newShape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("click_cycle_temperature_component");
            _ogdLog.EventParam("button_position", buttonPosition);
            _ogdLog.EventParam("old_shape", oldShape);
            _ogdLog.EventParam("new_shape", newShape);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogEpilogueStart()
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("epilogue_start");
            _ogdLog.SubmitEvent();
        }
    }

    //when to do this?
    public void LogEpilogueEnd()
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("epilogue_end");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogBatteryBoxOpen()
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("battery_box_open");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogBatteryBoxClose()
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("battery_box_close");
            _ogdLog.SubmitEvent();
        }
    }

    public void LogTestPropeller(bool left, bool isCorrect, string shape)
    {
        if(_loggingEnabled)
        {
            SetGameState();

            _ogdLog.BeginEvent("click_test_propeller");
            _ogdLog.EventParam("propeller_shape", shape);
            _ogdLog.EventParam("is_correct", isCorrect);
            _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
            _ogdLog.SubmitEvent();
        }
    }

	public unsafe bool LogGaze(Vector3 p, Quaternion q, uint gazeLogFrameCount, bool sendToServer=false)//, string scene)
	{
		/*if(_loggingEnabled)
		{
			if(_viewportDataCount < MAX_VIEWPORT_DATA)
			{
                PlayerHandRig hands = Find.State<PlayerHandRig>();

                _viewportData[_viewportDataCount].Write(p, q);
                //_viewportData[_viewportDataCount].rot = (q.x.ToString("F3")+","+q.y.ToString("F3")+","+q.z.ToString("F3")+","+q.w.ToString("F3"));
				
				Vector3 leftPos = Vector3.zero;
				Quaternion leftRot = Quaternion.identity;;
				
				hands.GetHandTransform(true, out leftPos, out leftRot);
                _leftHandData[_viewportDataCount].Write(leftPos, leftRot);
				
				Vector3 rightPos = Vector3.zero;;
                Quaternion rightRot = Quaternion.identity;
				
				hands.GetHandTransform(false, out rightPos, out rightRot);
                _rightHandData[_viewportDataCount].Write(rightPos, rightRot);

				_viewportDataCount++;
			}
			else
			{
				sendToServer = true;
			}

            if(sendToServer)
            {
                WriteGazeData(m_GazeBuilder, "gaze_data_package", _viewportData, _viewportDataCount);
                //Debug.Log(gazeLogFrameCount);
                //Debug.Log(_viewportDataCount);
                //Debug.Log(m_GazeBuilder);
                SetGameState();
                _ogdLog.Log("viewport_data", m_GazeBuilder);

                WriteGazeData(m_GazeBuilder, "left_hand_data_package", _leftHandData, _viewportDataCount);
				SetGameState();
                //Debug.Log(m_GazeBuilder);
                _ogdLog.Log("left_hand_data", m_GazeBuilder);

                WriteGazeData(m_GazeBuilder, "right_hand_data_package", _rightHandData, _viewportDataCount);
				SetGameState();
                //Debug.Log(m_GazeBuilder);
                _ogdLog.Log("right_hand_data", m_GazeBuilder);

                _viewportDataCount = 0;
				return true;
            }
		}*/
		
		return false;
		/*if (FirebaseEnabled)
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart, new Parameter(FirebaseAnalytics.ParameterLevelName, "antarctica"), new Parameter("start", UnityEngine.Time.time-seconds_at_start));
                //new Parameter("app_version", logVersion));
        }	*/
	}

    static private unsafe void WriteGazeData(StringBuilder sb, string paramName, LogGazeData[] data, int count) {
        sb.Clear().Append("{\"").Append(paramName).Append("\":\"[");
        if (count > 0) {
            for (int i = 0; i < count; i++) {
                AppendGazeFrame(sb, data[i]);
                sb.Append(',');
            }
        }
        sb.Length--; // eliminate last comma
        sb.Append("]\"}");
    }

    static private unsafe void AppendGazeFrame(StringBuilder sb, LogGazeData data) {
        sb.Append("{\\\"pos\\\":[").AppendNoAlloc(data.pos[0], 3).Append(',').AppendNoAlloc(data.pos[1], 3).Append(',').AppendNoAlloc(data.pos[2], 3).Append(']')
            .Append(",\\\"rot\\\":[").AppendNoAlloc(data.rot[0], 3).Append(',').AppendNoAlloc(data.rot[1], 3).Append(',').AppendNoAlloc(data.rot[2], 3).Append(',').AppendNoAlloc(data.rot[3], 3).Append("]}");
    }

    public void LogGazeBegin(string object_id)
    {
        if(_loggingEnabled)
		{
			//Debug.Log("Gaze begin: " + object_id);
			
			SetGameState();
				
            _ogdLog.BeginEvent("gaze_object_begin");
            _ogdLog.EventParam("object_id", object_id);
            _ogdLog.SubmitEvent();
        }
    }

    public void LogGazeEnd(string object_id)
    {
        if(_loggingEnabled)
		{
			//Debug.Log("Gaze end: " + object_id);
			
			SetGameState();
			
            _ogdLog.BeginEvent("gaze_object_end");
            _ogdLog.EventParam("object_id", object_id);
            _ogdLog.SubmitEvent();

        }
    }

    static private unsafe string GenerateHardwareId() {
        using (SHA256 sha = SHA256.Create()) {
            byte[] nameData = sha.ComputeHash(Encoding.UTF8.GetBytes(_DB_NAME));
            byte[] hashData = sha.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier));
            StringBuilder sb = new StringBuilder(64);
            for(int i = 0; i < hashData.Length; i++) {
                sb.Append((nameData[i] ^ hashData[i]).ToString("x2"));
            }
            return sb.ToString();
        }
    }

    #endregion // Logging
}
}
