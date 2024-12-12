using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Pennycook.Tablet {
    public class TabletPhoto : IPoolConstructHandler {
        public Texture2D Texture;
        public AsyncHandle UploadHandle;
        public string Tag;
        public DateTime Timestamp;

        public Vector2Int CachedSize;
        public GraphicsFormat CachedFormat;
        public NativeArray<byte> CachedCPUData;

        void IPoolConstructHandler.OnConstruct() {
        }

        void IPoolConstructHandler.OnDestruct() {
            UploadHandle.Cancel();
            UnityHelper.SafeDestroy(ref Texture);
            CachedCPUData = default;
        }
    }

    static public partial class PhotoUtility {
        static public void CopyRTToTextureCentered(RenderTexture src, Texture2D dst) {
            Assert.True(dst.width <= src.width && dst.height <= src.height, "");
            int offsetX = (src.width - dst.width) >> 1;
            int offsetY = (src.height - dst.height) >> 1;

            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = src;

            dst.ReadPixels(new Rect(offsetY, offsetY, dst.width, dst.height), 0, 0);

            RenderTexture.active = prevRT;
        }
    }
}