using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Rendering;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pennycook.Tablet {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate, 20000)]
    public sealed class TabletCountingMarkerRenderer : SharedStateSystemBehaviour<TabletCountingMarkerRenderState, TabletToolState, TabletControlState> {
        public override bool HasWork() {
            return base.HasWork()
                && m_StateA.RenderCamera.isActiveAndEnabled
                && m_StateB.CurrentTool == TabletTool.Count;
        }

        public unsafe override void ProcessWork(float deltaTime) {
            RenderParams renderParms = new RenderParams(m_StateA.RenderMaterial);
            renderParms.camera = m_StateA.RenderCamera;
            renderParms.layer = LayerMasks.Default_Index;
            renderParms.renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask;

            DefaultInstancedMeshParams* instanceData = stackalloc DefaultInstancedMeshParams[64];
            InstancedMeshBuffer<DefaultInstancedMeshParams> instanceBuff = new InstancedMeshBuffer<DefaultInstancedMeshParams>(instanceData, 64, renderParms, m_StateA.RenderMesh);

            Matrix4x4 billboardMat, scaledBillboardMat;
            CameraUtility.GetBillboardingMatrix(m_StateA.RenderCamera, out billboardMat);
            scaledBillboardMat = billboardMat * Matrix4x4.Scale(Vector3.one * m_StateA.RenderScale);

            DefaultInstancedMeshParams instParms = default;

            foreach (var c in Find.Components<TabletCountable>()) {
                if (!c.IsCounted) {
                    continue;
                }

                if (c.Group.State != TabletCountingGroupState.InProgress) {
                    continue;
                }

                Vector3 worldPos = c.transform.position;
                worldPos.y += 2.5f;

                // TODO: scale this

                Geom.SetTranslation(ref scaledBillboardMat, worldPos);
                instParms.objectToWorld = scaledBillboardMat;
                instanceBuff.Queue(ref instParms);
            }

            instanceBuff.Flush();
            instanceBuff.Dispose();
        }
    }
}