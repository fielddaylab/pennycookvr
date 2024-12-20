using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/PhysicsDB")]
    public sealed class PhysicsDB : GlobalAsset {
        [Required] public SurfaceType DefaultSurface;

        public override void Mount() {
            SurfaceType.Default = DefaultSurface;
        }
    }
}