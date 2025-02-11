using System;
using BeauUtil;
using FieldDay;
using FieldDay.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Pennycook {
    static public class RaycastJobs {
        static private Unsafe.ArenaHandle s_RaycastsAllocator;
        static private UnityJobAtomicsHelper s_AtomicsConverter;

        private const int ConeRingRayStart = 8;
        private const int ConeRingRayIncrement = 4;

        static private int GetTotalRaycastsForRingCount(int rings) {
            rings = Math.Max(rings, 0);

            int count = 1 + ConeRingRayStart * rings;
            while(rings-- > 0) {
                count += ConeRingRayIncrement * rings;
            }
            return count;
        }

        #region Lifecycle

        static public void Initialize() {
            s_RaycastsAllocator = Game.Memory.CreateArena(256 * Unsafe.KiB, "Raycasts", Unsafe.AllocatorFlags.ZeroOnAllocate);
            s_AtomicsConverter = new UnityJobAtomicsHelper(256);

            GameLoop.OnFrameAdvance.Register(Reset);
        }

        static public void Shutdown() {
            Game.Memory.DestroyArena(ref s_RaycastsAllocator);
            s_AtomicsConverter.Reset();
        }

        static public void Reset() {
            s_RaycastsAllocator.Reset();
            s_AtomicsConverter.Reset();
        }

        #endregion // Lifecycle

        #region Shapes

        /// <summary>
        /// Cone-shaped raycast with a smooth bottom.
        /// Raycast distances are uniform.
        /// </summary>
        static public unsafe RaycastJob SmoothConeCast(Vector3 origin, Vector3 direction, float radius, float distance, int resolution, LayerMask mask, int resultsPerRaycast = 1, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal) {
            int totalRayCount = GetTotalRaycastsForRingCount(resolution);

            UnsafeSpan<RaycastCommand> commands = s_RaycastsAllocator.AllocSpan<RaycastCommand>(totalRayCount);
            UnsafeSpan<float> weights = s_RaycastsAllocator.AllocSpan<float>(totalRayCount);
            UnsafeSpan<RaycastHit> hits = s_RaycastsAllocator.AllocSpan<RaycastHit>(totalRayCount * resultsPerRaycast);
            UnsafeSpan<ScoredIndex> sorted = s_RaycastsAllocator.AllocSpan<ScoredIndex>(totalRayCount * resultsPerRaycast);

            QueryParameters queryParams = new QueryParameters(mask, false, triggerInteraction, false);

            RaycastJob job;
            job.JobHandle = default;
            job.Input = new RaycastJobInput() {
                Origin = origin,
                Direction = direction.normalized,
                Distance = distance,
                Radius = radius,
                Resolution = resolution,
                QueryParams = queryParams
            };
            job.Raycasts = commands;
            job.RaycastWeights = weights;
            job.Results = hits;
            job.ResultsPerRaycast = resultsPerRaycast;
            job.ResultScores = sorted;
            job.KickFrame = Frame.InvalidIndex;
            return job;
        }

        #endregion // Shapes

        #region RaycastJob

        static public void Kick(ref RaycastJob job) {
            if (job.KickFrame != Frame.InvalidIndex) {
                return;
            }

            NativeArray<RaycastCommand> nativeRays = s_AtomicsConverter.Convert(job.Raycasts);
            NativeArray<RaycastHit> nativeResults = s_AtomicsConverter.Convert(job.Results);
            NativeArray<ScoredIndex> nativeScored = s_AtomicsConverter.Convert(job.ResultScores);

            NativeArray<float> nativeWeights;
            if (job.RaycastWeights.IsNullOrEmpty) {
                nativeWeights = default;
            } else {
                nativeWeights = s_AtomicsConverter.Convert(job.RaycastWeights);
            }

            // kick off raycast generation
            var physicsScene = Physics.defaultPhysicsScene;
            SmoothRaycastSetupJob setup = new SmoothRaycastSetupJob() {
                Commands = nativeRays,
                Direction = job.Input.Direction,
                Origin = job.Input.Origin,
                Distance = job.Input.Distance,
                QueryParams = job.Input.QueryParams,
                Radius = job.Input.Radius,
                Resolution = job.Input.Resolution,
                Scene = physicsScene,
                Weights = nativeWeights
            };

            JobHandle raycastGen = setup.Schedule();

            // kick off raycasts
            JobHandle raycasts = RaycastCommand.ScheduleBatch(nativeRays, nativeResults, 8, job.ResultsPerRaycast, raycastGen);

            JobHandle scoring;
            if (job.RaycastWeights.IsNullOrEmpty) {
                // score raycasts w/ no weighting
                UnweightedRaycastScoringJob scoringJob = new UnweightedRaycastScoringJob() {
                    Results = nativeResults,
                    ResultScores = nativeScored,
                };
                scoring = scoringJob.ScheduleBatch(job.Results.Length, job.ResultsPerRaycast, raycasts);
            } else {
                // score raycasts
                WeightedRaycastScoringJob scoringJob = new WeightedRaycastScoringJob() {
                    Results = nativeResults,
                    ResultScores = nativeScored,
                    Weights = nativeWeights
                };
                scoring = scoringJob.ScheduleBatch(job.Results.Length, job.ResultsPerRaycast, raycasts);
            }

            // sort by score
            ScoredIndexSortJob sortingJob = new ScoredIndexSortJob() {
                ResultScores = nativeScored
            };
            JobHandle sorting = sortingJob.Schedule(scoring);
            
            job.JobHandle = sorting;
            job.KickFrame = Frame.Index;
            JobHandle.ScheduleBatchedJobs();
        }

        static public TComponent Analyze<TComponent>(ref RaycastJob job, out RaycastHit raycast) where TComponent : Component {
            return Analyze<TComponent>(job.JobHandle, job.ResultScores, job.Results, out raycast);
        }

        static public TComponent Analyze<TComponent, TArg>(ref RaycastJob job, Predicate<TComponent, TArg> predicate, TArg predicateArg, out RaycastHit raycast) where TComponent : Component {
            return Analyze<TComponent, TArg>(job.JobHandle, job.ResultScores, job.Results, predicate, predicateArg, out raycast);
        }

        static public TComponent Analyze<TComponent>(ref RaycastJob job, Predicate<TComponent> predicate, out RaycastHit raycast) where TComponent : Component {
            return Analyze<TComponent>(job.JobHandle, job.ResultScores, job.Results, predicate, out raycast);
        }

        #endregion // RaycastJob

        #region Analysis

        static private TComponent Analyze<TComponent>(JobHandle jobHandle, UnsafeSpan<ScoredIndex> scoring, UnsafeSpan<RaycastHit> hits, out RaycastHit raycast) where TComponent : Component {
            jobHandle.Complete();
            
            foreach (var scored in scoring) {
                if (scored.Index < 0 || scored.Index >= hits.Length) {
                    break;
                }

                RaycastHit hit = hits.SafeGet(scored.Index);
                TComponent component = RetrieveFromRaycast<TComponent>(hit);

                if (component) {
                    raycast = hit;
                    return component;
                }
            }

            raycast = default;
            return null;
        }

        static private TComponent Analyze<TComponent, TArg>(JobHandle jobHandle, UnsafeSpan<ScoredIndex> scoring, UnsafeSpan<RaycastHit> hits, Predicate<TComponent, TArg> predicate, TArg predicateArg, out RaycastHit raycast) where TComponent : Component {
            if (predicate == null) {
                return Analyze<TComponent>(jobHandle, scoring, hits, out raycast);
            }

            jobHandle.Complete();

            foreach (var scored in scoring) {
                if (scored.Index < 0 || scored.Index >= hits.Length) {
                    break;
                }

                RaycastHit hit = hits.SafeGet(scored.Index);
                TComponent component = RetrieveFromRaycast<TComponent>(hit);

                if (component && predicate(component, predicateArg)) {
                    raycast = hit;
                    return component;
                }
            }

            raycast = default;
            return null;
        }

        static private TComponent Analyze<TComponent>(JobHandle jobHandle, UnsafeSpan<ScoredIndex> scoring, UnsafeSpan<RaycastHit> hits, Predicate<TComponent> predicate, out RaycastHit raycast) where TComponent : Component {
            if (predicate == null) {
                return Analyze<TComponent>(jobHandle, scoring, hits, out raycast);
            }

            jobHandle.Complete();

            foreach (var scored in scoring) {
                if (scored.Index < 0 || scored.Index >= hits.Length) {
                    break;
                }

                RaycastHit hit = hits.SafeGet(scored.Index);
                TComponent component = RetrieveFromRaycast<TComponent>(hit);

                if (component && predicate(component)) {
                    raycast = hit;
                    return component;
                }
            }

            raycast = default;
            return null;
        }

        static private TComponent RetrieveFromRaycast<TComponent>(RaycastHit hit) where TComponent : Component {
            if (hit.colliderInstanceID == 0) {
                return null;
            }
            
            Collider col = hit.collider;
            Rigidbody rigidbody;
            TComponent component = col.GetComponent<TComponent>();
            if (component == null && (rigidbody = col.attachedRigidbody)) {
                component = rigidbody.GetComponent<TComponent>();
            }
            if (component == null) {
                component = col.GetComponentInParent<TComponent>();
            }

            return component;
        }

        #endregion // Analysis

        #region Types

        [BurstCompile]
        private struct SmoothRaycastSetupJob : IJob {
            [ReadOnly]
            public int Resolution;
            [ReadOnly]
            public Vector3 Origin;
            [ReadOnly]
            public Vector3 Direction;
            [ReadOnly]
            public float Distance;
            [ReadOnly]
            public float Radius;
            [ReadOnly]
            public PhysicsScene Scene;
            [ReadOnly]
            public QueryParameters QueryParams;

            public NativeArray<float> Weights;
            public NativeArray<RaycastCommand> Commands;

            public void Execute() {
                if (Weights.IsCreated) {
                    Weights[0] = 1;
                }
                Commands[0] = new RaycastCommand(Scene, Origin, Direction, QueryParams, Distance);

                Vector3 end = Origin + Direction * Distance;
                Vector3 right = Vector3.Cross(Direction, Vector3.up);
                Vector3 up = Vector3.Cross(right, Direction);

                int commandCount = 1;
                for (int ring = 0; ring < Resolution; ring++) {
                    int countInRing = ConeRingRayStart + ring * ConeRingRayIncrement;
                    float ringDist = Radius * ((1f + ring) / Resolution);
                    float angleIncrement = Mathf.PI * 2 / countInRing;
                    float weight = 1 - ((1f + ring) / (Resolution + 1));
                    weight *= weight;

                    for (int point = 0; point < countInRing; point++) {
                        Vector3 pnt = end
                            + ringDist * Mathf.Cos(angleIncrement * point) * right
                            + ringDist * Mathf.Sin(angleIncrement * point) * up;
                        Vector3 vec = pnt - Origin;
                        vec.Normalize();

                        if (Weights.IsCreated) {
                            Weights[commandCount] = weight;
                        }
                        Commands[commandCount++] = new RaycastCommand(Scene, Origin, vec, QueryParams, Distance);
                    }
                }
            }
        }

        [BurstCompile]
        private struct WeightedRaycastScoringJob : IJobParallelForBatch {
            [ReadOnly]
            public NativeArray<RaycastHit> Results;

            [ReadOnly]
            public NativeArray<float> Weights;

            public NativeArray<ScoredIndex> ResultScores;

            public void Execute(int startIndex, int count) {
                int raycastIndex = startIndex / count;
                int i = 0;
                float weight = Weights[raycastIndex];
                for(; i < count; i++) {
                    RaycastHit hit = Results[startIndex + i];
                    if (hit.colliderInstanceID == 0) {
                        break;
                    }

                    float dist = hit.distance;

                    int score = (int) ((ushort.MaxValue - (int) (dist * 100 * (2f - weight))) * weight);
                    ResultScores[startIndex + i] = new ScoredIndex() {
                        Index = startIndex + i,
                        Score = score
                    };
                }
                for(; i < count; i++) {
                    ResultScores[startIndex + i] = new ScoredIndex() {
                        Index = -1,
                        Score = -ushort.MaxValue
                    };
                }
            }
        }

        [BurstCompile]
        private struct UnweightedRaycastScoringJob : IJobParallelForBatch {
            [ReadOnly]
            public NativeArray<RaycastHit> Results;

            public NativeArray<ScoredIndex> ResultScores;

            public void Execute(int startIndex, int count) {
                int raycastIndex = startIndex / count;
                int i = 0;
                for (; i < count; i++) {
                    RaycastHit hit = Results[startIndex + i];
                    if (hit.colliderInstanceID == 0) {
                        break;
                    }

                    float dist = hit.distance;

                    int score = ushort.MaxValue - (int) (dist * 100);
                    ResultScores[startIndex + i] = new ScoredIndex() {
                        Index = startIndex + i,
                        Score = score
                    };
                }
                for (; i < count; i++) {
                    ResultScores[startIndex + i] = new ScoredIndex() {
                        Index = -1,
                        Score = -ushort.MaxValue
                    };
                }
            }
        }

        private unsafe struct ScoredIndexSortJob : IJob {
            public NativeArray<ScoredIndex> ResultScores;

            public void Execute() {
                ScoredIndex* ptr = (ScoredIndex*) NativeArrayUnsafeUtility.GetUnsafePtr(ResultScores);
                Unsafe.Quicksort(ptr, ResultScores.Length, CompareScoresPtr);
            }

#if UNITY_EDITOR
            static private readonly Comparison<ScoredIndex> CompareScoresPtr = CompareScores;
#else
            static private readonly delegate*<ScoredIndex, ScoredIndex, int> CompareScoresPtr = &CompareScores;
#endif // UNITY_EDITOR

            static private int CompareScores(ScoredIndex a, ScoredIndex b) {
                return b.Score - a.Score;
            }
        }

#endregion // Types
    }

    public struct RaycastJobInput {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Radius;
        public float Distance;
        public QueryParameters QueryParams;
        public int Resolution;
    }

    public struct RaycastJob {
        public JobHandle JobHandle;
        public ushort KickFrame;
        public RaycastJobInput Input;
        public UnsafeSpan<RaycastCommand> Raycasts;
        public UnsafeSpan<float> RaycastWeights;
        public int ResultsPerRaycast;
        public UnsafeSpan<RaycastHit> Results;
        public UnsafeSpan<ScoredIndex> ResultScores;

        public void Complete() {
            JobHandle.Complete();
        }

        public bool IsValid() {
            return ResultsPerRaycast > 0;
        }

        public void Clear() {
            this = default;
        }
    }

    public struct ScoredIndex {
        public int Score;
        public int Index;

        public override string ToString() {
            if (Index < 0) {
                return "[Invalid]";
            } else {
                return string.Concat(Score, '@', Index);
            }
        }
    }
}