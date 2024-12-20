using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.Scenes;

namespace Pennycook {
    public sealed class SurfaceTag : BatchedComponent, IScenePreload {
        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            if (TypeId.IsEmpty) {
                Type = null;
            } else {
                Type = Find.NamedAsset<SurfaceType>(TypeId);
            }
            return null;
        }

        [AssetName(typeof(SurfaceType))] public StringHash32 TypeId;
        [NonSerialized] public SurfaceType Type;
    }
}