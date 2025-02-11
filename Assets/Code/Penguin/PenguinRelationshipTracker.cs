using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Filters;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Pennycook {
    [RequireComponent(typeof(PenguinBrain))]
    public sealed class PenguinRelationshipTracker : ScriptActorComponent {
        // family
        public PenguinBrain Mate;
        public PenguinBrain Child;

        public bool IsPursued;

        public float CloseDistance;

        public ParticleSystem Regurg;

        #region Leaf

        
        #endregion // Leaf
    }

    static public partial class PenguinUtility {
        
        static public bool IsClose(PenguinRelationshipTracker rel) {
            if(rel.Mate != null) {
                return Vector3.Distance(rel.gameObject.transform.position, rel.Mate.transform.position) <= rel.CloseDistance;
            } else if(rel.Child != null) {
                return Vector3.Distance(rel.gameObject.transform.position, rel.Child.transform.position) <= rel.CloseDistance;
            }
            return false;
        }
        static public bool IsClose(PenguinRelationshipTracker rel, Vector3 pos) {
            return Vector3.Distance(rel.gameObject.transform.position, pos) <= rel.CloseDistance;
        }

        static public bool IsPursuing(PenguinRelationshipTracker rel) {
            if(rel.Mate != null) {
                return rel.Mate.Relationships.IsPursued;
            } else if(rel.Child != null) {
                return rel.Child.Relationships.IsPursued;
            }
            return false;
        }

        static public Vector3 GetFacingDirection(PenguinRelationshipTracker rel) {
            Vector3 mateChickForward = Vector3.forward;
            if(rel.Mate != null) {
                mateChickForward = -rel.Mate.transform.forward;
            } else if(rel.Child != null) {
                mateChickForward = -rel.Child.transform.forward;
            }
            return mateChickForward;
        }
    }
}