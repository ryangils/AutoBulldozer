using Game;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
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
        public static int TotalRenovated;

        public static int TotalDemolished => TotalAbandoned + TotalCondemned + TotalDestroyed;

        private SimulationSystem m_SimulationSystem;
        private IconCommandSystem m_IconCommandSystem;
        private EntityQuery m_AbandonedQuery;
        private EntityQuery m_CondemnedQuery;
        private EntityQuery m_DestroyedQuery;
        private EntityQuery m_BuildingConfigQuery;

        private uint m_LastSweepFrame;

        public static void ResetStatistics()
        {
            TotalAbandoned = 0;
            TotalCondemned = 0;
            TotalDestroyed = 0;
            TotalRenovated = 0;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_IconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
            m_BuildingConfigQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());

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
                if (setting.RenovateAbandoned)
                {
                    TotalRenovated += RenovateAbandoned(frame, graceFrames);
                }
                else
                {
                    TotalAbandoned += DemolishAbandoned(frame, graceFrames);
                }
            }

            if (setting.DemolishCondemned)
            {
                TotalCondemned += Demolish(m_CondemnedQuery, "condemned");
            }

            if (setting.DemolishDestroyed)
            {
                TotalDestroyed += DemolishDestroyed();
            }
        }

        /// <summary>
        /// Renovates abandoned buildings past their grace period instead of
        /// demolishing them: strips the abandoned state, repairs the building's
        /// condition and puts the property back on the rental market so new
        /// tenants can move in. If the underlying problem persists (usually rent
        /// exceeding what households can pay), the building may abandon again.
        /// </summary>
        private int RenovateAbandoned(uint frame, uint graceFrames)
        {
            if (m_AbandonedQuery.IsEmptyIgnoreFilter || m_BuildingConfigQuery.IsEmptyIgnoreFilter)
            {
                return 0;
            }

            var config = m_BuildingConfigQuery.GetSingleton<BuildingConfigurationData>();
            var iconBuffer = m_IconCommandSystem.CreateCommandBuffer();
            var entities = m_AbandonedQuery.ToEntityArray(Allocator.Temp);
            var renovated = 0;

            foreach (var entity in entities)
            {
                var abandonedSince = EntityManager.GetComponentData<Abandoned>(entity).m_AbandonmentTime;
                if (graceFrames != 0 && (abandonedSince > frame || frame - abandonedSince < graceFrames))
                {
                    continue;
                }

                EntityManager.RemoveComponent<Abandoned>(entity);

                // Repair the condition, otherwise the game re-abandons immediately.
                if (EntityManager.HasComponent<BuildingCondition>(entity))
                {
                    EntityManager.SetComponentData(entity, new BuildingCondition { m_Condition = 0 });
                }

                // Put the property back on the rental market.
                if (!EntityManager.HasComponent<PropertyOnMarket>(entity)
                    && !EntityManager.HasComponent<PropertyToBeOnMarket>(entity))
                {
                    EntityManager.AddComponent<PropertyToBeOnMarket>(entity);
                }

                // Clear the abandoned warning icon.
                iconBuffer.Remove(entity, config.m_AbandonedNotification);

                renovated++;
            }

            entities.Dispose();

            if (renovated > 0)
            {
                Mod.Log.Info($"Renovated {renovated} abandoned building(s) back onto the market. Session total renovated: {TotalRenovated + renovated}.");
            }

            return renovated;
        }

        /// <summary>
        /// Clears destroyed buildings, but only zoned growables — the game regrows
        /// a new building on the freed zone cells. Service, signature and other
        /// player-placed buildings are left alone so their services aren't lost and
        /// the player can rebuild them deliberately.
        /// </summary>
        private int DemolishDestroyed()
        {
            if (m_DestroyedQuery.IsEmptyIgnoreFilter)
            {
                return 0;
            }

            var entities = m_DestroyedQuery.ToEntityArray(Allocator.Temp);
            var demolished = 0;

            foreach (var entity in entities)
            {
                if (!EntityManager.HasComponent<PrefabRef>(entity))
                {
                    continue;
                }

                // Only zoned growables carry SpawnableBuildingData on their prefab;
                // everything else (services, signatures, unique/placed buildings) is skipped.
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
                if (!EntityManager.HasComponent<SpawnableBuildingData>(prefab))
                {
                    continue;
                }

                EntityManager.AddComponent<Deleted>(entity);
                demolished++;
            }

            entities.Dispose();

            if (demolished > 0)
            {
                Mod.Log.Info($"Cleared {demolished} destroyed growable building(s). Session total: {TotalDemolished + demolished}.");
            }

            return demolished;
        }

        /// <summary>
        /// Demolishes abandoned buildings, optionally only after they have been
        /// abandoned for <paramref name="graceFrames"/> simulation frames. Uses the
        /// game's own <see cref="Abandoned.m_AbandonmentTime"/> timestamp, so the
        /// grace period is exact and survives saving/loading.
        /// </summary>
        private int DemolishAbandoned(uint frame, uint graceFrames)
        {
            if (m_AbandonedQuery.IsEmptyIgnoreFilter)
            {
                return 0;
            }

            if (graceFrames == 0)
            {
                return Demolish(m_AbandonedQuery, "abandoned");
            }

            var entities = m_AbandonedQuery.ToEntityArray(Allocator.Temp);
            var demolished = 0;

            foreach (var entity in entities)
            {
                var abandonedSince = EntityManager.GetComponentData<Abandoned>(entity).m_AbandonmentTime;

                // A timestamp in the future can only come from odd save states; wait it out.
                if (abandonedSince <= frame && frame - abandonedSince >= graceFrames)
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    demolished++;
                }
            }

            entities.Dispose();

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
