using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using FieldDay;
using ScriptableBake;
using UnityEngine;

public class MargoFace : MonoBehaviour, IBaked {
    #region Types

    private struct SparkleRecord {
        public SpriteRenderer Renderer;
        public float TimeUntilChange;
    }

    public enum PartId : ushort {
        Mouth,
        LEye,
        LEye_Pupil,
        REye,
        REye_Pupil,
        LBrow,
        RBrow
    }

    public enum MouthState : ushort {
        Open,
        Closed,
        Smirk,
        Frown,
        Frown_Right,
        Open_Smaller
    }

    public enum EyeState : ushort {
        Open,
        Half,
        Wink,
        Blink,
        Error
    }

    public enum EyebrowState : ushort {
        Hidden,
        Worried,
        Intrigued
    }

    public enum BackgroundState : ushort {
        Normal,
        Celebrate,
        Pleased,
        Alert
    }

    public enum SparkleState : ushort {
        Off,
        Top,
        All
    }

    #endregion // Types

    #region Inspector

    [SerializeField] private Camera m_Camera;

    [Header("Parts")]
    [SerializeField] private SpriteRenderer[] m_Parts = new SpriteRenderer[7];
    [SerializeField, HideInInspector] private Transform[] m_PartTransforms = Array.Empty<Transform>();
    [SerializeField, HideInInspector] private Vector2[] m_PartOrigins = Array.Empty<Vector2>();

    [Header("Sparkles")]
    [SerializeField] private SpriteRenderer[] m_Sparkles;
    [SerializeField] private SpriteRenderer[] m_AdditionalSparkles;
    [SerializeField] private Sprite[] m_SparkleFrames;
    [SerializeField] private Color[] m_SparkleColors;
    [SerializeField] private float m_SparkleUpdateDelay = 0.1f;

    [Header("Sprites")]
    [SerializeField] private Sprite m_DefaultIris;
    [SerializeField] private Sprite m_NarrowedEye;
    [SerializeField] private Sprite m_WinkEye;
    [SerializeField] private Sprite m_BlinkEye;
    [SerializeField] private Sprite m_OpenMouth;
    [SerializeField] private Sprite m_SmirkMouth;
    [SerializeField] private Sprite m_ClosedMouth;
    [SerializeField] private Sprite m_OpenMouthSmall;

    [Header("Colors")]
    [SerializeField] private Color32 m_DefaultBackground;
    [SerializeField] private Color32 m_CelebrationBackground;
    [SerializeField] private Color32 m_AlertBackground;
    [SerializeField] private Color32 m_PleasedBackground;

    #endregion // Inspector

    [NonSerialized] private RandomDeck<Color> m_SparkleColorPicker;
    [NonSerialized] private RandomDeck<Sprite> m_SparkleFramePicker;
    [NonSerialized] private SparkleRecord[] m_ActiveSparkles;
    [NonSerialized] private int m_ActiveSparkleCount;

    #region Unity Events

    private void Awake() {
        m_SparkleColorPicker = new RandomDeck<Color>(m_SparkleColors.Length);
        m_SparkleFramePicker = new RandomDeck<Sprite>(m_SparkleFrames);

        m_ActiveSparkles = new SparkleRecord[m_Sparkles.Length + m_AdditionalSparkles.Length];
        m_ActiveSparkleCount = 0;

        int idx = 0;
        for(int i = 0; i < m_Sparkles.Length; i++) {
            m_ActiveSparkles[idx++].Renderer = m_Sparkles[i];
        }
        for (int i = 0; i < m_AdditionalSparkles.Length; i++) {
            m_ActiveSparkles[idx++].Renderer = m_AdditionalSparkles[i];
        }
    }

    private void LateUpdate() {
        float dt = Frame.DeltaTime;
        if (dt <= 0) {
            return;
        }

        for(int i = 0; i < m_ActiveSparkleCount; i++) {
            ref SparkleRecord record = ref m_ActiveSparkles[i];
            record.TimeUntilChange -= dt;
            if (record.TimeUntilChange <= 0) {
                record.TimeUntilChange += m_SparkleUpdateDelay;

                Sprite nextSprite;
                do {
                    nextSprite = m_SparkleFramePicker.Next();
                }
                while (nextSprite == record.Renderer.sprite);
                record.Renderer.sprite = nextSprite;

                Color nextColor;
                do {
                    nextColor = m_SparkleColorPicker.Next();
                } while (nextColor == record.Renderer.color);
                record.Renderer.color = nextColor;
            }
        }
    }

    #endregion // Unity Events

    #region Operations

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBackgroundColor(Color color) {
        m_Camera.backgroundColor = color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSpriteEnabled(PartId part, bool enabled) {
        m_Parts[(int) part].enabled = enabled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSprite(PartId part, Sprite sprite) {
        m_Parts[(int) part].sprite = sprite;
    }

    private void SetSpriteFlip(PartId part, bool x, bool y) {
        SpriteRenderer rend = m_Parts[(int) part];
        rend.flipX = x;
        rend.flipY = y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSpriteFlipX(PartId part, bool x) {
        m_Parts[(int) part].flipX = x;
    }

    private void RegenerateSparkleColors() {
        m_SparkleColorPicker.Clear();

        Color bgColor = m_Camera.backgroundColor;
        foreach(var color in m_SparkleColors) {
            if (color != bgColor) {
                m_SparkleColorPicker.Add(color);
            }
        }
    }

    private unsafe void RegenerateSparkleDelays() {
        if (m_ActiveSparkleCount <= 0) {
            return;
        }

        int* indices = stackalloc int[m_ActiveSparkleCount];
        for(int i = 0; i < m_ActiveSparkleCount; i++) {
            indices[i] = i;
        }

        RNG.Instance.Shuffle(indices, m_ActiveSparkleCount);

        for(int i = 0; i < m_ActiveSparkleCount; i++) {
            int idx = indices[i];

            ref SparkleRecord record = ref m_ActiveSparkles[idx];
            record.Renderer.sprite = m_SparkleFramePicker.Next();
            record.Renderer.color = m_SparkleColorPicker.Next();
            record.TimeUntilChange = RNG.Instance.NextFloat(m_SparkleUpdateDelay);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSpriteFlipY(PartId part, bool y) {
        m_Parts[(int) part].flipY = y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSpriteOffset(PartId part, Vector2 offset) {
        m_PartTransforms[(int) part].localPosition = m_PartOrigins[(int) part] + offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetSpriteOffset(PartId part) {
        m_PartTransforms[(int) part].localPosition = m_PartOrigins[(int) part];
    }

    public void SetBackgroundState(BackgroundState backgroundState) {
        switch (backgroundState) {
            case BackgroundState.Normal: {
                SetBackgroundColor(m_DefaultBackground);
                break;
            }
            case BackgroundState.Alert: {
                SetBackgroundColor(m_AlertBackground);
                break;
            }
            case BackgroundState.Celebrate: {
                SetBackgroundColor(m_CelebrationBackground);
                break;
            }
            case BackgroundState.Pleased: {
                SetBackgroundColor(m_PleasedBackground);
                break;
            }
        }
    }

    public void SetLeftEyeState(EyeState eyeState) {
        switch (eyeState) {
            case EyeState.Open: {
                SetSpriteEnabled(PartId.LEye_Pupil, true);
                SetSprite(PartId.LEye, m_DefaultIris);
                break;
            }
            case EyeState.Half: {
                SetSpriteEnabled(PartId.LEye_Pupil, false);
                SetSprite(PartId.LEye, m_NarrowedEye);
                break;
            }
            case EyeState.Wink: {
                SetSpriteEnabled(PartId.LEye_Pupil, false);
                SetSprite(PartId.LEye, m_WinkEye);
                break;
            }
            case EyeState.Blink: {
                SetSpriteEnabled(PartId.LEye_Pupil, false);
                SetSprite(PartId.LEye, m_BlinkEye);
                break;
            }
            case EyeState.Error: {
                SetSpriteEnabled(PartId.LEye_Pupil, false);
                SetSprite(PartId.LEye, m_DefaultIris);
                break;
            }
        }
    }

    public void SetRightEyeState(EyeState eyeState) {
        switch (eyeState) {
            case EyeState.Open: {
                SetSpriteEnabled(PartId.REye_Pupil, true);
                SetSprite(PartId.REye, m_DefaultIris);
                break;
            }
            case EyeState.Half: {
                SetSpriteEnabled(PartId.REye_Pupil, false);
                SetSprite(PartId.REye, m_NarrowedEye);
                break;
            }
            case EyeState.Wink: {
                SetSpriteEnabled(PartId.REye_Pupil, false);
                SetSprite(PartId.REye, m_WinkEye);
                break;
            }
            case EyeState.Blink: {
                SetSpriteEnabled(PartId.REye_Pupil, false);
                SetSprite(PartId.REye, m_BlinkEye);
                break;
            }
            case EyeState.Error: {
                SetSpriteEnabled(PartId.REye_Pupil, false);
                SetSprite(PartId.REye, m_DefaultIris);
                break;
            }
        }
    }

    public void SetMouthState(MouthState mouthState) {
        switch (mouthState) {
            case MouthState.Open: {
                SetSprite(PartId.Mouth, m_OpenMouth);
                ResetSpriteOffset(PartId.Mouth);
                SetSpriteFlip(PartId.Mouth, false, false);
                break;
            }
            case MouthState.Closed: {
                SetSprite(PartId.Mouth, m_ClosedMouth);
                ResetSpriteOffset(PartId.Mouth);
                SetSpriteFlip(PartId.Mouth, false, false);
                break;
            }
            case MouthState.Smirk: {
                SetSprite(PartId.Mouth, m_SmirkMouth);
                SetSpriteOffset(PartId.Mouth, new Vector2(-2, 0));
                SetSpriteFlip(PartId.Mouth, false, false);
                break;
            }
            case MouthState.Frown: {
                SetSprite(PartId.Mouth, m_SmirkMouth);
                SetSpriteOffset(PartId.Mouth, new Vector2(-2, 0));
                SetSpriteFlip(PartId.Mouth, false, true);
                break;
            }
            case MouthState.Frown_Right: {
                SetSprite(PartId.Mouth, m_SmirkMouth);
                ResetSpriteOffset(PartId.Mouth);
                SetSpriteFlip(PartId.Mouth, true, true);
                break;
            }
            case MouthState.Open_Smaller: {
                SetSprite(PartId.Mouth, m_OpenMouthSmall);
                ResetSpriteOffset(PartId.Mouth);
                SetSpriteFlip(PartId.Mouth, false, false);
                break;
            }
        }
    }

    public void SetBrowState(PartId browIndex, EyebrowState browState) {
        switch (browState) {
            case EyebrowState.Hidden: {
                SetSpriteEnabled(browIndex, false);
                break;
            }
            case EyebrowState.Worried: {
                SetSpriteEnabled(browIndex, true);
                SetSpriteFlipY(browIndex, true);
                break;
            }
            case EyebrowState.Intrigued: {
                SetSpriteEnabled(browIndex, true);
                SetSpriteFlipY(browIndex, false);
                break;
            }
        }
    }

    public void SetSparkleState(SparkleState sparkleState) {
        foreach (var sparkle in m_Sparkles) {
            sparkle.enabled = sparkleState >= SparkleState.Top;
        }
        foreach (var sparkle in m_AdditionalSparkles) {
            sparkle.enabled = sparkleState >= SparkleState.All;
        }

        switch (sparkleState) {
            case SparkleState.Off: {
                m_ActiveSparkleCount = 0;
                break;
            }
            case SparkleState.Top: {
                m_ActiveSparkleCount = m_Sparkles.Length;
                RegenerateSparkleColors();
                RegenerateSparkleDelays();
                break;
            }
            case SparkleState.All: {
                m_ActiveSparkleCount = m_ActiveSparkles.Length;
                RegenerateSparkleColors();
                RegenerateSparkleDelays();
                break;
            }
        }
    }

    #endregion // Operations

    #region IBaked

#if UNITY_EDITOR

    private void BuildData() {
        m_PartTransforms = new Transform[m_Parts.Length];
        m_PartOrigins = new Vector2[m_Parts.Length];

        for (int i = 0; i < m_PartTransforms.Length; i++) {
            if (m_Parts[i]) {
                m_PartTransforms[i] = m_Parts[i].transform;
                m_PartOrigins[i] = m_PartTransforms[i].localPosition;
            }
        }
    }

    int IBaked.Order => 0;

    bool IBaked.Bake(BakeFlags flags, BakeContext context) {
        //m_Camera.orthographicSize = m_Camera.targetTexture.height / 2;
        BuildData();
        return true;
    }

#endif // UNITY_EDITOR

    #endregion // IBaked
}