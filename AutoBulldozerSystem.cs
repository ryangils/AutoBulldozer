using System.Collections.Generic;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace AutoBulldozer
{
    /// <summary>
    /// Periodically scans for abandoned / condemned / destroyed buildings and
    /// marks them for deletion (the game's own systems handle the actual removal,
    /// including sub-buildings, upgrades and renters).
    /// </summary>
    public partial class AutoBulldozerSystem : GameSystemBase
    {
        /// <summary>Simulation frames per in-game day.</summary>
        public const int kFramesPerDay = 262144;

        /// <summary>Upper bound of the "sweeps per day" setting; also the system's base tick rate.</summary>
        public const int kMaxSweepsPerDay = 64;

        /// <summary>Per-category demolition counters since game start (shown in options UI).</summary>
        public static int TotalAbandoned;
        public static int TotalCondemned;
        public static int TotalDestroyed;

        public static int TotalDemolished => TotalAbandoned + TotalCondemned + TotalDestroyed;

        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_AbandonedQuery;
        private EntityQuery m_CondemnedQuery;
        private EntityQuery m_DestroyedQuery;

        // First simulation frame at which we saw each building abandoned, used for
        // the grace period. In-memory only: after loading a save the grace period
        // simply restarts, which errs on the side of demolishing later, not sooner.
        private readonly Dictionary<Entity, uint> m_AbandonedSince = new Dictionary<Entity, uint>();

        private uint m_LastSweepFrame;

        public static void ResetStatistics()
        {
            TotalAbandoned = 0;
            TotalCondemned = 0;
            TotalDestroyed = 0;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            m_AbandonedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Abandoned>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                },
            });

            m_CondemnedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Condemned>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                },
            });

            m_DestroyedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Destroyed>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                },
            });
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // Tick at the fastest configurable rate; OnUpdate throttles down to the
            // configured sweeps-per-day so the slider takes effect immediately.
            return kFramesPerDay / kMaxSweepsPerDay;
        }

        protected override void OnUpdate()
        {
            var setting = Mod.Setting;
            if (setting == null || !setting.EnableMod)
            {
                return;
            }

            var frame = m_SimulationSystem.frameIndex;
            var sweepsPerDay = Clamp(setting.SweepsPerDay, 1, kMaxSweepsPerDay);
            var sweepInterval = (uint)(kFramesPerDay / sweepsPerDay);

            // frame < m_LastSweepFrame means a different (older) save was loaded; sweep now.
            if (frame >= m_LastSweepFrame && frame - m_LastSweepFrame < sweepInterval)
            {
                return;
            }

            m_LastSweepFrame = frame;

            if (setting.DemolishAbandoned)
            {
                var graceFrames = (uint)((long)Clamp(setting.AbandonedGraceDays, 0, 30) * kFramesPerDay);
                TotalAbandoned += DemolishAbandoned(frame, graceFrames);
            }
            else
            {
                m_AbandonedSince.Clear();
            }

            if (setting.DemolishCondemned)
            {
                TotalCondemned += Demolish(m_CondemnedQuery, "condemned");
            }

            if (setting.DemolishDestroyed)
            {
                TotalDestroyed += Demolish(m_DestroyedQuery, "destroyed");
            }
        }

        /// <summary>
        /// Demolishes abandoned buildings, optionally only after they have been
        /// abandoned for <paramref name="graceFrames"/> simulation frames.
        /// </summary>
        private int DemolishAbandoned(uint frame, uint graceFrames)
        {
            if (m_AbandonedQuery.IsEmptyIgnoreFilter)
            {
                m_AbandonedSince.Clear();
                return 0;
            }

            if (graceFrames == 0)
            {
                m_AbandonedSince.Clear();
                return Demolish(m_AbandonedQuery, "abandoned");
            }

            var entities = m_AbandonedQuery.ToEntityArray(Allocator.Temp);
            var stillAbandoned = new HashSet<Entity>();
            var demolished = 0;

            foreach (var entity in entities)
            {
                if (!m_AbandonedSince.TryGetValue(entity, out var since) || since > frame)
                {
                    // Newly abandoned (or a save was loaded); start its grace period.
                    m_AbandonedSince[entity] = frame;
                    stillAbandoned.Add(entity);
                    continue;
                }

                if (frame - since >= graceFrames)
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    demolished++;
                }
                else
                {
                    stillAbandoned.Add(entity);
                }
            }

            entities.Dispose();

            // Drop entries for buildings that were demolished, re-occupied,
            // bulldozed by the player, or belong to a previously loaded city.
            var stale = new List<Entity>();
            foreach (var tracked in m_AbandonedSince.Keys)
            {
                if (!stillAbandoned.Contains(tracked))
                {
                    stale.Add(tracked);
                }
            }

            foreach (var entity in stale)
            {
                m_AbandonedSince.Remove(entity);
            }

            if (demolished > 0)
            {
                Mod.Log.Info($"Demolished {demolished} abandoned building(s) past their grace period. Session total: {TotalDemolished + demolished}.");
            }

            return demolished;
        }

        private int Demolish(EntityQuery query, string reason)
        {
            if (query.IsEmptyIgnoreFilter)
            {
                return 0;
            }

            var count = query.CalculateEntityCount();
            if (count == 0)
            {
                return 0;
            }

            // Adding the Deleted tag lets the game's deletion systems tear the
            // building down cleanly (network edges, renters, sub-objects, etc.).
            EntityManager.AddComponent<Deleted>(query);

            Mod.Log.Info($"Demolished {count} {reason} building(s). Session total: {TotalDemolished + count}.");

            return count;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
