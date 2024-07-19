using System;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public class LODMeshGenerator : EditorWindow {
        #region Save Location

        static private string LastSaveLocationString;

        #endregion // Save Location

        #region Public Properties

        public Mesh Source;
        [Range(0.01f, 1)] public float Quality1 = 0.5f;
        [Range(0, 1)] public float Quality2 = 0.2f;

        // generation settings
        [Range(0.1f, 16)] public float Aggressiveness = 7;
        public bool PreserveBorderEdges = false;
        public bool PreserveUVSeamEdges = false;
        public bool PreserveUVFoldoverEdges = false;
        public bool PreserveSurfaceCurvature = true;

        // preview
        public Material RenderMaterial;

        #endregion // Public Properties

        #region State

        [NonSerialized] private UnityEditor.Editor m_SourceMeshEditor;
        [NonSerialized] private UnityEditor.Editor m_LOD1MeshEditor;
        [NonSerialized] private UnityEditor.Editor m_LOD2MeshEditor;

        [NonSerialized] private Mesh m_LOD1;
        [NonSerialized] private Mesh m_LOD2;

        [SerializeField] private Vector2 m_Scroll;
        [SerializeField] private Vector2 m_PreviewDir;

        #endregion // State
    }
}