using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Spawners.Components;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Spawners.EntitySystems;

public sealed partial class SpawnPointSystem : EntitySystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    [SubscribeLocalEvent]
    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // Genesis-Start
        // Prefer job/department-restricted points first. Use general late-join points
        // as the fallback so jobs without special configuration keep normal behavior.
        var matchingLateJoinPositions = new List<EntityCoordinates>();
        var genericLateJoinPositions = new List<EntityCoordinates>();
        var matchingJobPositions = new List<EntityCoordinates>();
        var genericJobPositions = new List<EntityCoordinates>();
        var useJobPointsForLateJoin = args.Station != null
            && TryComp<StationSpawningComponent>(args.Station.Value, out var stationSpawning)
            && stationSpawning.UseJobSpawnPointsForLateJoin;

        // Explicit alwaysUseSpawner jobs must use their own restricted job spawners,
        // even when their station also has generic LateJoin/Arrivals points.
        var alwaysUseSpawner = args.Job != null
            && ProtoMan.TryIndex(args.Job.Value, out JobPrototype? jobPrototype)
            && jobPrototype?.AlwaysUseSpawner == true;
        // Genesis-End

        // TODO: Cache all this if it ends up important.
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Genesis-Edit: On configured stations, Job points can supplement dedicated late-join points.
            if (_gameTicker.RunLevel == GameRunLevel.InRound)
            {
                if (spawnPoint.SpawnType == SpawnPointType.LateJoin &&
                    MatchesJobOrDepartment(spawnPoint, args.Job))
                {
                    if (HasRoleRestriction(spawnPoint))
                        matchingLateJoinPositions.Add(xform.Coordinates);
                    else
                        genericLateJoinPositions.Add(xform.Coordinates);
                }
                else if ((useJobPointsForLateJoin || alwaysUseSpawner) &&
                         spawnPoint.SpawnType == SpawnPointType.Job &&
                         MatchesJobOrDepartment(spawnPoint, args.Job))
                {
                    if (HasRoleRestriction(spawnPoint))
                        matchingJobPositions.Add(xform.Coordinates);
                    else
                        genericJobPositions.Add(xform.Coordinates);
                }
            }
            else if (spawnPoint.SpawnType == SpawnPointType.Job &&
                     (args.Job == null ||
                      MatchesJobOrDepartment(spawnPoint, args.Job) &&
                      (!alwaysUseSpawner || HasRoleRestriction(spawnPoint))))
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        // Genesis-Start
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
        {
            if (alwaysUseSpawner)
            {
                // Do not fall back to Arrivals or unscoped spawn points for these jobs.
                // Their own job/department-restricted spawn point is a hard requirement.
                if (matchingJobPositions.Count > 0)
                    possiblePositions.AddRange(matchingJobPositions);
            }
            else if (matchingLateJoinPositions.Count > 0)
                possiblePositions.AddRange(matchingLateJoinPositions);
            else if (matchingJobPositions.Count > 0)
                possiblePositions.AddRange(matchingJobPositions);
            else if (genericLateJoinPositions.Count > 0)
                possiblePositions.AddRange(genericLateJoinPositions);
            else if (genericJobPositions.Count > 0)
                possiblePositions.AddRange(genericJobPositions);
        }
        // Genesis-End

        if (possiblePositions.Count == 0)
        {
            // Genesis-Start
            // Never send players to an unrelated map when a station explicitly opts in
            // to late-join-by-job spawn points but has no valid point for the selected role.
            if ((_gameTicker.RunLevel == GameRunLevel.InRound && useJobPointsForLateJoin) || alwaysUseSpawner)
            {
                Log.Error($"No valid late-join job spawn point found on station {ToPrettyString(args.Station)} for job {args.Job}; refusing to use an unrelated spawn point.");
                return;
            }
            // Genesis-End

            // Ok we've still not returned, but we need to put them /somewhere/.
            // TODO: Refactor gameticker spawning code so we don't have to do this!
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

            if (points2.MoveNext(out _, out var xform))
            {
                Log.Error($"Unable to pick a valid spawn point, picking random spawner as a backup.\nRunLevel: {_gameTicker.RunLevel} Station: {ToPrettyString(args.Station)} Job: {args.Job}");
                possiblePositions.Add(xform.Coordinates);
            }
            else
            {
                Log.Error($"No spawn points were available!\nRunLevel: {_gameTicker.RunLevel} Station: {ToPrettyString(args.Station)} Job: {args.Job}");
                return;
            }
        }

        var spawnLoc = _random.Pick(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }

    // Genesis-Start
    private bool MatchesJobOrDepartment(SpawnPointComponent point, ProtoId<JobPrototype>? job)
    {
        if (job == null)
            return point.Job == null && point.Departments.Count == 0;

        // An explicit job_id takes precedence and is an exact restriction.
        if (point.Job != null)
            return point.Job == job;

        // An unscoped point is a general-purpose point for any job.
        if (point.Departments.Count == 0)
            return true;

        foreach (var departmentId in point.Departments)
        {
            if (ProtoMan.TryIndex(departmentId, out var department) && department.Roles.Contains(job.Value))
                return true;
        }

        return false;
    }

    private static bool HasRoleRestriction(SpawnPointComponent point)
    {
        return point.Job != null || point.Departments.Count > 0;
    }
    // Genesis-End
}
