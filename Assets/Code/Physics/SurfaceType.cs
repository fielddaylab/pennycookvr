using BeauUtil;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.Components;
using FieldDay.Physics;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Surface Type Configuration")]
    public sealed class SurfaceType : NamedAsset {
        public PhysicMaterial Material;

        [AudioEventRef] public StringHash32 GripSFX;

        static public SurfaceType Resolve(Collider collider) {
            SurfaceTag surf = collider.ResolveComponent<SurfaceTag>();
            return surf ? surf.Type : Default;
        }

        static public SurfaceType Resolve(Collision collision) {
            SurfaceTag surf = collision.ResolveComponent<SurfaceTag>();
            return surf ? surf.Type : Default;
        }

        static public SurfaceType Resolve(RaycastHit raycastHit) {
            SurfaceTag surf = raycastHit.ResolveComponent<SurfaceTag>();
            return surf ? surf.Type : Default;
        }

        static public SurfaceType Default {
            get; internal set;
        }
    }
}