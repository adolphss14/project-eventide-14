using Content.Shared.Station.Systems;
using Content.Shared.Spawners.EntitySystems;

namespace Content.Shared.Station.Components;

/// <summary>
/// Controls spawning on the given station, tracking spawners present on it.
/// </summary>
[RegisterComponent, Access(typeof(StationSpawningSystem), typeof(SpawnPointSystem))]
public sealed partial class StationSpawningComponent : Component
{
    // Genesis-Start
    /// <summary>
    /// Allows late joiners to use job-specific spawn points on this station when
    /// no matching dedicated LateJoin point is available.
    /// </summary>
    [DataField("use_job_spawn_points_for_late_join")]
    public bool UseJobSpawnPointsForLateJoin;
    // Genesis-End
}
