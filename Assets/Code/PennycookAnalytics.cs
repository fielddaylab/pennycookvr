using UnityEngine;
using System.Globalization;
using System.Text;
using BeauData;
using BeauUtil;
using FieldDay;
using FieldDay.Data;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using FieldDay.XR;
using Leaf.Runtime;
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

    public enum DirectionType : byte {
        FORWARD,
        BACKWARD,
        LEFT,
        RIGHT
    }

    public enum RotationDirectionType : byte {
        CW,
        CCW
    }

    public enum MargoMode : byte {
        SCAN, 
        WARP, 
        PHOTO, 
        COUNT
    }

    public enum GrabbableType : byte {
        PROP,
        MARGO, 
        LEG_BAND, 
        PENGUIN, 
        ARM_BAND, 
        BACK_TRACKER
    }

    public enum MargoModeType : byte {
        SCAN,
        WARP,
        PHOTO,
        COUNT
    }

    public enum TaskType : byte {
        SCAN,
        COUNT,
        PHOTO,
        TAG,
        RECOVER
    }


    public enum TagType : byte {
        LEG,
        ARM,
        BACK
    }

    public enum ScanObject : byte
    {
        PENGUIN,
        CHICK,
        EGG,
        NEST,
        GATE
    }

    public enum TagLocation : byte
    {
        CASE,
        PENGUIN
    }

    public enum BehaviorType : byte
    {
        MATING_DANCE,
        REGURGITATION
    }

    public enum DockLocation : byte
    {
        TENT,
        CASE
    }

    public struct GrababbleInfo
    {
        public StringHash32 ID;
        public GrabbableType type;

        public GrababbleInfo(StringHash32 i, GrabbableType t)
        {
            ID = i;
            type = t;
        }
    }

    public struct CaptureInfo {
        public Vector3 pos;
        public Quaternion rot;

        public CaptureInfo(Vector3 position, Quaternion rotation) {
            pos = position;
            rot = rotation;
        }
    }

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

    public struct NavigateInfo {
        public Vector3 pos;
        public DirectionType dirType;
        public float navigationAmount;
        public NavigateInfo(Vector3 position, DirectionType d, float navAmount) {
            pos = position;
            dirType = d;
            navigationAmount = navAmount;
        }
    }

    public struct RotateInfo {
        public Quaternion rot;
        public RotationDirectionType dirType;
        public float rotationAmount;

        public RotateInfo(Quaternion rotation, RotationDirectionType d, float rotAmount) {
            rot = rotation;
            dirType = d;
            rotationAmount = rotAmount;
        }
    }

    public struct BadNavigateInfo {
        public Vector3 new_pos;
        public Quaternion new_rot;

        public BadNavigateInfo(Vector3 pos, Quaternion rot) {
            new_pos = pos;
            new_rot = rot;
        }
    }

    public struct CountInfo {
        public StringHash32 ID;
        public int currentCount;
        
        public CountInfo(int c, StringHash32 s) {
            currentCount = c;
            ID = s;
        }
    }

    public struct MargoTaskInfo {
        public StringHash32 ID;
        public TaskType type;

        public MargoTaskInfo(StringHash32 s, TaskType t) {
            ID = s;
            type = t;
        }
    }

    public struct TagTaskInfo {
        public StringHash32 ID;

        public TaskType taskType;

        public TagType type;

        public TagTaskInfo(StringHash32 s, TaskType task, TagType t) {
            ID = s;
            taskType = task;
            type = t;
        }
    }

    public struct TutorialInfo {
        public StringHash32 ID;
        public StringHash32 Text;

        public TutorialInfo(StringHash32 s, StringHash32 t) {
            ID = s;
            Text = t;
        }
    }

    public struct ScanObjectInfo {
        public StringHash32 ID;
        public ScanObject ObjType;

        public ScanObjectInfo(StringHash32 i, ScanObject oType) {
            ID = i;
            ObjType = oType;
        }
    }

    public struct BehaviorCaptureInfo {
        public StringHash32 PenguinID;
        public BehaviorType Type;
        public BehaviorCaptureInfo(StringHash32 id, BehaviorType t) {
            PenguinID = id;
            Type = t;
        }
    }

    public struct CaseTransformInfo
    {
        public Vector3 Pos;
        public Vector3 OldPos;
        public CaseTransformInfo(Vector3 v, Vector3 v2)
        {
            Pos = v;
            OldPos = v2;
        }
    }

    public struct PlaceTagInfo
    {
        public TagLocation tagLoc;
        public TagType tagType;
        public StringHash32 ID;
        public PlaceTagInfo(TagLocation t, TagType t2, StringHash32 i)
        {
            tagLoc = t;
            tagType = t2;
            ID = i;
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
            VRGame.Events.Register<NavigateInfo>(GameEvents.PlayerNavigate, LogPlayerNavigate);
            VRGame.Events.Register<RotateInfo>(GameEvents.PlayerRotate, LogPlayerRotate);
            VRGame.Events.Register<BadNavigateInfo>(GameEvents.PlayerBadNavigate, LogPlayerBadNavigate);
            VRGame.Events.Register<int>(GameEvents.MargoModeSwitch, LogMargoModeSwitch);
            VRGame.Events.Register<Tablet.TabletWarpPoint>(GameEvents.PlayerWarpWithMargo, LogWarpWithMargo);
            VRGame.Events.Register<Tablet.TabletWarpPoint>(GameEvents.PlayerWarpWalking, LogWarpByWalking);
            VRGame.Events.Register(GameEvents.TentDoorOpened, LogTentOpened);
            VRGame.Events.Register(GameEvents.TentDoorClosed, LogTentClosed);
            VRGame.Events.Register<CountInfo>(GameEvents.PenguinCounted, LogPenguinCounted);
            VRGame.Events.Register<MargoTaskInfo>(GameEvents.MargoTaskAssigned, LogMargoTaskAssigned);
            VRGame.Events.Register<MargoTaskInfo>(GameEvents.MargoTaskCompleted, LogMargoTaskCompleted);
            VRGame.Events.Register<TagTaskInfo>(GameEvents.TagTaskAssigned, LogTagTaskAssigned);
            VRGame.Events.Register<TagTaskInfo>(GameEvents.TagTaskCompleted, LogTagTaskCompleted);
            VRGame.Events.Register<TutorialInfo>(GameEvents.TutorialShown, LogTutorialShown);
            VRGame.Events.Register<TutorialInfo>(GameEvents.TutorialHidden, LogTutorialHidden);
            VRGame.Events.Register<int>(GameEvents.TriggerScanned, LogTriggerScan);
            VRGame.Events.Register<ScanObjectInfo>(GameEvents.ObjectScanned, LogObjectScanned);
            VRGame.Events.Register<CaptureInfo>(GameEvents.TriggerPhoto, LogTriggerPhoto);
            VRGame.Events.Register<BehaviorCaptureInfo>(GameEvents.PhotoBehavior, LogPhotoBehavior);
            VRGame.Events.Register<CaseTransformInfo>(GameEvents.CaseTransform, LogCaseTransform);
            VRGame.Events.Register(GameEvents.MargoSync, LogMargoSync);
            VRGame.Events.Register<GrababbleInfo>(GameEvents.ObjectGrabbed, LogObjectGrabbed);
            VRGame.Events.Register<GrababbleInfo>(GameEvents.ObjectReleased, LogObjectReleased);
            VRGame.Events.Register<PlaceTagInfo>(GameEvents.PlaceTag, LogPlaceTag);
            VRGame.Events.Register<PlaceTagInfo>(GameEvents.RemoveTag, LogPlaceTag);
            VRGame.Events.Register<int>(GameEvents.DockMargo, LogDockMargo);

            m_HardwareId = GenerateHardwareId();

            //Debug.Log("Starting analytics");
            OGD.OGDLogConsts c = new OGD.OGDLogConsts();
            c.AppId = _DB_NAME;
            c.AppVersion = UnityEngine.Application.version;
            c.ClientLogVersion = logVersion;
            _ogdLog = new OGD.OGDLog(c);

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if(RunMode.Current != RunMode.ApplicationRunMode.Device) {
                _loggingEnabled = false;
            }

            if (!string.IsNullOrEmpty(_firebase.ApiKey))
            {
                _ogdLog.UseFirebase(_firebase);
            }

            _ogdLog.SetDebug(m_Debug);
        }

        void OnDestroy()
        {
            VRGame.Events?.DeregisterAllForContext(this);

            if (_ogdLog != null)
            {
                _ogdLog.Dispose();
                _ogdLog = null;
            }
        }

        public string GetHardwareID() { return m_HardwareId; }

        private void LogObjectGrabbed(GrababbleInfo g)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("object_grabbed");
                _ogdLog.EventParam("object_id", g.ID.ToDebugString());
                _ogdLog.EventParam("object_type", EnumLookup.Get(g.type));
                _ogdLog.SubmitEvent();
            }
        }

        private void LogObjectReleased(GrababbleInfo g)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("object_released");
                _ogdLog.EventParam("object_id", g.ID.ToDebugString());
                _ogdLog.EventParam("object_type", EnumLookup.Get(g.type));
                _ogdLog.SubmitEvent();
            }
        }

        private void LogPlaceTag(PlaceTagInfo p)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("place_tag");
                _ogdLog.EventParam("tag_location", EnumLookup.Get(p.tagLoc));
                _ogdLog.EventParam("tag_type", EnumLookup.Get(p.tagType));

                if (p.tagLoc == Data.TagLocation.PENGUIN)
                {
                    _ogdLog.EventParam("penguin_id", p.ID.ToDebugString());
                }
                else
                {
                    _ogdLog.EventParam("penguin_id", "None");
                }
                _ogdLog.SubmitEvent();
            }
        }

        private void LogRemoveTag(PlaceTagInfo p)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("remove_tag");
                _ogdLog.EventParam("tag_location", EnumLookup.Get(p.tagLoc));
                _ogdLog.EventParam("tag_type", EnumLookup.Get(p.tagType));

                if (p.tagLoc == TagLocation.PENGUIN)
                {
                    _ogdLog.EventParam("penguin_id", p.ID.ToDebugString());
                }
                else
                {
                    _ogdLog.EventParam("penguin_id", "None");
                }
                _ogdLog.SubmitEvent();
            }
        }

        private void LogDockMargo(int d)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("dock_margo");
                _ogdLog.EventParam("dock_location", EnumLookup.Get((DockLocation)d));
                _ogdLog.SubmitEvent();
            }
        }

        private void LogMargoSync()
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("click_margo_sync");

                _ogdLog.SubmitEvent();
            }
        }

        private void LogCaseTransform(CaseTransformInfo c)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("case_relocated");

                m_PosBuilder.Clear().Append("[").AppendNoAlloc(c.OldPos.x, 3).Append(',').AppendNoAlloc(c.OldPos.y, 3).Append(',').AppendNoAlloc(c.OldPos.z, 3).Append("]");

                _ogdLog.EventParam("old_position", m_PosBuilder.ToString());

                m_PosBuilder.Clear().Append("[").AppendNoAlloc(c.Pos.x, 3).Append(',').AppendNoAlloc(c.Pos.y, 3).Append(',').AppendNoAlloc(c.Pos.z, 3).Append("]");

                _ogdLog.EventParam("position", m_PosBuilder.ToString());

                _ogdLog.SubmitEvent();
            }
        }

        private void LogTriggerScan(int i)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("trigger_scan");
                _ogdLog.SubmitEvent();
            }
        }
        private void LogTriggerPhoto(CaptureInfo c)
        {
            if (_loggingEnabled)
            {
                m_PosBuilder.Clear().Append("[").AppendNoAlloc(c.pos.x, 3).Append(',').AppendNoAlloc(c.pos.y, 3).Append(',').AppendNoAlloc(c.pos.z, 3).Append("]");
                m_RotBuilder.Clear().Append("[").AppendNoAlloc(c.rot.x, 3).Append(',').AppendNoAlloc(c.rot.y, 3).Append(',').AppendNoAlloc(c.rot.z, 3).Append(',').AppendNoAlloc(c.rot.w, 3).Append("]");

                _ogdLog.BeginEvent("trigger_image_capture");
                _ogdLog.EventParam("margo_position", m_PosBuilder.ToString());
                _ogdLog.EventParam("margo_orientation", m_RotBuilder.ToString());
                _ogdLog.SubmitEvent();
            }
        }

        private void LogPhotoBehavior(BehaviorCaptureInfo c)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("trigger_image_capture");
                _ogdLog.EventParam("behavior", EnumLookup.Get(c.Type));
                _ogdLog.EventParam("penguin_id", c.PenguinID.ToDebugString());
                _ogdLog.SubmitEvent();
            }
        }

        private void LogObjectScanned(ScanObjectInfo o)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("object_scanned");
                _ogdLog.EventParam("object_type", EnumLookup.Get(o.ObjType));
                _ogdLog.EventParam("object_id", o.ID.ToDebugString());
                _ogdLog.SubmitEvent();
            }
        }

        public void LogDayBegin(int beginDay)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("day_begin");
                _ogdLog.EventParam("day_index", beginDay);
                _ogdLog.SubmitEvent();
            }
        }

        private void LogDayCompleted(int completedDay)
        {
            if (_loggingEnabled)
            {
                _ogdLog.BeginEvent("day_complete");
                _ogdLog.EventParam("day_index", completedDay);
                _ogdLog.SubmitEvent();
            }
        }

        private void LogGazeGameState(Vector3 pos, Quaternion quat)
        {
            m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append("]");
            m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");

            _ogdLog.GameStateParam("pos", m_PosBuilder.ToString());
            _ogdLog.GameStateParam("rot", m_RotBuilder.ToString());
        }

        #region Logging

        public void SetUserID(int code)
        {
            if (_loggingEnabled)
            {
                _ogdLog.SetUserId(code.ToString());
            }
        }

        void SetGameState()
        {
            PlayerRig player = Find.State<PlayerRig>();

            PlayerProgressState state = Find.State<PlayerProgressState>();
            PlayerMovementState moveState = Find.State<PlayerMovementState>();

            if (player != null)
            {
                _ogdLog.BeginGameState();
                _ogdLog.GameStateParam("seconds_from_launch", UnityEngine.Time.time - seconds_at_start);
                _ogdLog.GameStateParam("day", state.DayIndex + 1);
                _ogdLog.GameStateParam("location", PlayerMovementUtility.IsInside(Find.State<PlayerMovementState>()) ? "INSIDE" : "OUTSIDE");

                LogGazeGameState(player.HeadRoot.transform.position, player.HeadLook.transform.rotation);

                Tablet.TabletToolState t = Find.State<Tablet.TabletToolState>();
                if (t != null)
                {
                    _ogdLog.GameStateParam("margo_mode", EnumLookup.Get((MargoMode)t.CurrentToolIndex));
                }
                //_ogdLog.GameStateParam("margo_taskbar", state.DayIndex+1);

                _ogdLog.SubmitGameState();
            }
        }

        public void LogSessionStart()
        {
            //Debug.Log("Session start");

            if (_loggingEnabled)
            {
                seconds_at_start = UnityEngine.Time.time;

                SetGameState();

                _ogdLog.ResetSessionId();
                _ogdLog.BeginEvent("session_start");
                _ogdLog.SubmitEvent();
            }
        }
        
        public long GetSessionID() { return _ogdLog.GetSessionId();}

        public void LogStartGame()
        {
            if (_loggingEnabled)
            {

                SetGameState();

                long sessionID = _ogdLog.GetSessionId();
                UnityEngine.Random.seed = (int)sessionID;
                RNG.Instance = new System.Random((int)sessionID);

                //RNG.Instance.

                _ogdLog.BeginEvent("device_identifier");
                _ogdLog.EventParam("hardware_uuid", m_HardwareId);
                _ogdLog.SubmitEvent();

                _ogdLog.BeginEvent("game_start");
                _ogdLog.SubmitEvent();
            }
        }

        private void LogLevelComplete()
        {
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("level_complete");
                _ogdLog.SubmitEvent();

            }
        }

        private void LogHeadsetOn()
        {
            if (_loggingEnabled)
            {
                //seconds_at_start = UnityEngine.Time.time;
                SetGameState();

                _ogdLog.BeginEvent("headset_on");
                _ogdLog.SubmitEvent();

            }
        }

        public void LogHeadsetOff()
        {
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("headset_off");
                _ogdLog.SubmitEvent();

            }
        }

        public void LogGrabGesture(GrabLogInfo info)
        {
            if (_loggingEnabled)
            {
                SetGameState();

                //m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
                //m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
                m_PosBuilder.Clear().Append("[").AppendNoAlloc(info.pos.x, 3).Append(',').AppendNoAlloc(info.pos.y, 3).Append(',').AppendNoAlloc(info.pos.z, 3).Append("]");
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
            if (_loggingEnabled)
            {
                SetGameState();

                //m_PosBuilder.Clear().Append("[").AppendNoAlloc(pos.x, 3).Append(',').AppendNoAlloc(pos.y, 3).Append(',').AppendNoAlloc(pos.z, 3).Append(',').Append("]");
                //m_RotBuilder.Clear().Append("[").AppendNoAlloc(quat.x, 3).Append(',').AppendNoAlloc(quat.y, 3).Append(',').AppendNoAlloc(quat.z, 3).Append(',').AppendNoAlloc(quat.w, 3).Append("]");
                m_PosBuilder.Clear().Append("[").AppendNoAlloc(info.pos.x, 3).Append(',').AppendNoAlloc(info.pos.y, 3).Append(',').AppendNoAlloc(info.pos.z, 3).Append("]");
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
            if (_loggingEnabled)
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
            if (_loggingEnabled)
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

        public void LogPlayerNavigate(NavigateInfo navInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();

                m_PosBuilder.Clear().Append("[").AppendNoAlloc(navInfo.pos.x, 3).Append(',').AppendNoAlloc(navInfo.pos.y, 3).Append(',').AppendNoAlloc(navInfo.pos.z, 3).Append("]");

                _ogdLog.BeginEvent("joystick_navigate");
                _ogdLog.EventParam("new_pos", m_PosBuilder.ToString());
                _ogdLog.EventParam("navigation_direction", EnumLookup.Get(navInfo.dirType));
                _ogdLog.EventParam("navigation_amount", navInfo.navigationAmount);
                _ogdLog.SubmitEvent();
            }
        }

        public void LogPlayerRotate(RotateInfo rotInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();

                m_RotBuilder.Clear().Append("[").AppendNoAlloc(rotInfo.rot.x, 3).Append(',').AppendNoAlloc(rotInfo.rot.y, 3).Append(',').AppendNoAlloc(rotInfo.rot.z, 3).Append(',').AppendNoAlloc(rotInfo.rot.w, 3).Append("]");

                _ogdLog.BeginEvent("joystick_rotate");
                _ogdLog.EventParam("new_rot", m_RotBuilder.ToString());
                _ogdLog.EventParam("rotation_direction", EnumLookup.Get(rotInfo.dirType));
                _ogdLog.EventParam("rotation_amount", rotInfo.rotationAmount);
                _ogdLog.SubmitEvent();
            }
        }
        public void LogPlayerBadNavigate(BadNavigateInfo navInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();

                m_PosBuilder.Clear().Append("[").AppendNoAlloc(navInfo.new_pos.x, 3).Append(',').AppendNoAlloc(navInfo.new_pos.y, 3).Append(',').AppendNoAlloc(navInfo.new_pos.z, 3).Append("]");
                m_RotBuilder.Clear().Append("[").AppendNoAlloc(navInfo.new_rot.x, 3).Append(',').AppendNoAlloc(navInfo.new_rot.y, 3).Append(',').AppendNoAlloc(navInfo.new_rot.z, 3).Append(',').AppendNoAlloc(navInfo.new_rot.w, 3).Append("]");

                _ogdLog.BeginEvent("teleport_from_invalid_location");
                _ogdLog.EventParam("new_pos", m_PosBuilder.ToString());
                _ogdLog.EventParam("new_rot", m_RotBuilder.ToString());
                _ogdLog.SubmitEvent();
            }
        }

        public void LogMargoModeSwitch(int mode)
        {
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("switch_margo_mode");
                _ogdLog.EventParam("new_mode", EnumLookup.Get((MargoMode)mode));
                _ogdLog.SubmitEvent();
            }
        }

        public void LogWarpWithMargo(Tablet.TabletWarpPoint warpPoint)
        {
            if (_loggingEnabled && warpPoint)
            {
                SetGameState();

                ScriptActor actor = ScriptUtility.Actor(warpPoint);

                _ogdLog.BeginEvent("warp_to_point");
                _ogdLog.EventParam("point_id", actor.Id.ToDebugString());

                m_PosBuilder.Clear().Append("[").AppendNoAlloc(warpPoint.transform.position.x, 3).Append(',').AppendNoAlloc(warpPoint.transform.position.y, 3).Append(',').AppendNoAlloc(warpPoint.transform.position.z, 3).Append("]");

                _ogdLog.EventParam("point_location", m_PosBuilder.ToString());

                _ogdLog.SubmitEvent();
            }
        }

        public void LogWarpByWalking(Tablet.TabletWarpPoint warpPoint)
        {
            if (_loggingEnabled && warpPoint)
            {
                SetGameState();

                ScriptActor actor = ScriptUtility.Actor(warpPoint);

                _ogdLog.BeginEvent("walk_to_warp_point");
                _ogdLog.EventParam("point_id", actor.Id.ToDebugString());

                m_PosBuilder.Clear().Append("[").AppendNoAlloc(warpPoint.transform.position.x, 3).Append(',').AppendNoAlloc(warpPoint.transform.position.y, 3).Append(',').AppendNoAlloc(warpPoint.transform.position.z, 3).Append("]");

                _ogdLog.EventParam("point_location", m_PosBuilder.ToString());

                _ogdLog.SubmitEvent();
            }
        }

        public void LogTentOpened()
        {
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("tent_door_opened");
                _ogdLog.SubmitEvent();
            }
        }

        public void LogTentClosed()
        {
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("tent_door_closed");
                _ogdLog.SubmitEvent();
            }
        }

        public void LogMargoTaskAssigned(MargoTaskInfo taskInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();
                _ogdLog.BeginEvent("margo_task_assigned");
                _ogdLog.EventParam("task_id", taskInfo.ID.ToDebugString());
                _ogdLog.EventParam("task_type", EnumLookup.Get(taskInfo.type));
                _ogdLog.SubmitEvent();
            }
        }

        public void LogMargoTaskCompleted(MargoTaskInfo taskInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();
                _ogdLog.BeginEvent("margo_task_completed");
                _ogdLog.EventParam("task_id", taskInfo.ID.ToDebugString());
                _ogdLog.EventParam("task_type", EnumLookup.Get(taskInfo.type));
                _ogdLog.SubmitEvent();
            }
        }

        public void LogTagTaskAssigned(TagTaskInfo taskInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();
                _ogdLog.BeginEvent("tag_task_assigned");
                _ogdLog.EventParam("task_id", taskInfo.ID.ToDebugString());
                _ogdLog.EventParam("task_type", EnumLookup.Get(taskInfo.taskType));
                _ogdLog.EventParam("tag_type", EnumLookup.Get(taskInfo.type));
                _ogdLog.SubmitEvent();
            }
        }

        public void LogTagTaskCompleted(TagTaskInfo taskInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();
                _ogdLog.BeginEvent("tag_task_completed");
                _ogdLog.EventParam("task_id", taskInfo.ID.ToDebugString());
                _ogdLog.EventParam("task_type", EnumLookup.Get(taskInfo.taskType));
                _ogdLog.EventParam("tag_type", EnumLookup.Get(taskInfo.type));
                _ogdLog.SubmitEvent();
            }
        }

        private void LogTutorialShown(TutorialInfo tInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();
                _ogdLog.BeginEvent("margo_tooltip_displayed");
                _ogdLog.EventParam("tooltip_id", tInfo.ID.ToDebugString());
                _ogdLog.EventParam("tooltip_content", tInfo.Text.ToDebugString());
                _ogdLog.SubmitEvent();
            }
        }

        private void LogTutorialHidden(TutorialInfo tInfo)
        {
            if (_loggingEnabled)
            {
                SetGameState();
                _ogdLog.BeginEvent("margo_tooltip_hidden");
                _ogdLog.EventParam("tooltip_id", tInfo.ID.ToDebugString());
                _ogdLog.EventParam("tooltip_content", tInfo.Text.ToDebugString());
                _ogdLog.SubmitEvent();
            }
        }

        public void LogPenguinCounted(CountInfo c)
        {
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("penguin_counted");
                _ogdLog.EventParam("running_count", c.currentCount);
                _ogdLog.EventParam("penguin_id", c.ID.ToDebugString());
                _ogdLog.SubmitEvent();
            }
        }

        public void LogObjectHighlighted(string name, bool isNormal)
        {
            if (_loggingEnabled)
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
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("object_unhighlighted");
                _ogdLog.EventParam("highlight_object", name);
                _ogdLog.EventParam("highlight_type", isNormal ? "OBJECT" : "DESTINATION");
                _ogdLog.SubmitEvent();
            }
        }

        public void LogGrabWorkBenchHandle(bool left, float height)
        {
            if (_loggingEnabled)
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
            if (_loggingEnabled)
            {
                SetGameState();

                _ogdLog.BeginEvent("release_workbench_handle");
                _ogdLog.EventParam("end_height", height);
                _ogdLog.EventParam("hand", left ? "LEFT" : "RIGHT");
                _ogdLog.SubmitEvent();
            }
        }


        public unsafe bool LogGaze(Vector3 p, Quaternion q, uint gazeLogFrameCount, bool sendToServer = false)//, string scene)
        {
            if (_loggingEnabled)
            {
                if (_viewportDataCount < MAX_VIEWPORT_DATA)
                {
                    PlayerRig hands = Find.State<PlayerRig>();

                    _viewportData[_viewportDataCount].Write(p, q);
                    _leftHandData[_viewportDataCount].Write(hands.LeftHand.Raw.transform.position, hands.LeftHand.Raw.transform.rotation);
                    _rightHandData[_viewportDataCount].Write(hands.RightHand.Raw.transform.position, hands.RightHand.Raw.transform.rotation);

                    _viewportDataCount++;
                }
                else
                {
                    sendToServer = true;
                }

                if (sendToServer)
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
            }

            return false;
            /*if (FirebaseEnabled)
            {
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart, new Parameter(FirebaseAnalytics.ParameterLevelName, "antarctica"), new Parameter("start", UnityEngine.Time.time-seconds_at_start));
                    //new Parameter("app_version", logVersion));
            }	*/
        }

        static private unsafe void WriteGazeData(StringBuilder sb, string paramName, LogGazeData[] data, int count)
        {
            sb.Clear().Append("{\"").Append(paramName).Append("\":\"[");
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    AppendGazeFrame(sb, data[i]);
                    sb.Append(',');
                }
            }
            sb.Length--; // eliminate last comma
            sb.Append("]\"}");
        }

        static private unsafe void AppendGazeFrame(StringBuilder sb, LogGazeData data)
        {
            sb.Append("{\\\"pos\\\":[").AppendNoAlloc(data.pos[0], 3).Append(',').AppendNoAlloc(data.pos[1], 3).Append(',').AppendNoAlloc(data.pos[2], 3).Append(']')
                .Append(",\\\"rot\\\":[").AppendNoAlloc(data.rot[0], 3).Append(',').AppendNoAlloc(data.rot[1], 3).Append(',').AppendNoAlloc(data.rot[2], 3).Append(',').AppendNoAlloc(data.rot[3], 3).Append("]}");
        }

        public void LogGazeBegin(string object_id)
        {
            if (_loggingEnabled)
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
            if (_loggingEnabled)
            {
                //Debug.Log("Gaze end: " + object_id);

                SetGameState();

                _ogdLog.BeginEvent("gaze_object_end");
                _ogdLog.EventParam("object_id", object_id);
                _ogdLog.SubmitEvent();

            }
        }

        static private unsafe string GenerateHardwareId()
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] nameData = sha.ComputeHash(Encoding.UTF8.GetBytes(_DB_NAME));
                byte[] hashData = sha.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier));
                StringBuilder sb = new StringBuilder(64);
                for (int i = 0; i < hashData.Length; i++)
                {
                    sb.Append((nameData[i] ^ hashData[i]).ToString("x2"));
                }
                return sb.ToString();
            }
        }

        #endregion // Logging
    }
}
