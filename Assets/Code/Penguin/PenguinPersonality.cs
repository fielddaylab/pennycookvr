using System;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Filters;
using UnityEngine;

namespace Pennycook {
    [CreateAssetMenu(menuName = "Pennycook/Penguin/Personality")]
    public sealed class PenguinPersonality : NamedAsset {
        #region Types

        [Serializable]
        public struct PlayerParams {
            [Header("Wide Bubble")]
            public float DistanceBubble;
            public float BubbleAttack;

            [Header("Too Close Bubble")]
            public float CloseDistanceBubble;
            public float CloseBubbleAttack;

            [Header("Default Anxiety Params")]
            public float DefaultDecay;
            public SignalLatchWindow AnxietyLatching;
        }

        [Serializable]
        public struct FamilyParams {
            // TODO: Mate defensiveness?
        }

        [Serializable]
        public struct SocialParams {
            [Header("Distance From Other Penguins")]
            public float PreferredStandingDistance;
            public float PreferredStandingDistanceRange;
            public float TooCloseAnxiety;
            public float TooFarAnxiety;

            [Header("Looking At/Greeting Other Penguins")]
            public float LookBubble;
            public float GreetBubble;

            [Header("Default Anxiety Params")]
            public float DefaultDecay;
            public SignalLatchWindow AnxietyLatching;
        }

        [Serializable]
        public struct WanderParams {
            public float IdleWaitDuration;
            public float IdleWaitDurationRandom;
            public float WanderDistance;
            public float WanderDistanceRandom;
        }

        #endregion // Types

        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public PlayerParams Player = new PlayerParams() {
            DistanceBubble = 8,
            BubbleAttack = 30,
            
            CloseDistanceBubble = 1,
            CloseBubbleAttack = 2,

            DefaultDecay = 8,
            AnxietyLatching = SignalLatchWindow.Full,
        };

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public FamilyParams Family = new FamilyParams();

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public SocialParams Social = new SocialParams() {
            PreferredStandingDistance = 3f,
            PreferredStandingDistanceRange = 8f,
            TooCloseAnxiety = 0,
            TooFarAnxiety = 0,

            LookBubble = 2,
            GreetBubble = 1,

            DefaultDecay = 3,
            AnxietyLatching = SignalLatchWindow.Full
        };

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public WanderParams Wander = new WanderParams() {
            IdleWaitDuration = 4,
            IdleWaitDurationRandom = 8,

            WanderDistance = 2,
            WanderDistanceRandom = 10,
        };

        #endregion // Inspector

        // TODO: Margo sound curiosity
        //[Header("Sound Curiosity")]
        //public float SoundDistanceBubble = 
    }
}