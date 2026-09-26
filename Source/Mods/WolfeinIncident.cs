using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs Wolfein incident workers (Drone Raid).
///     Uses the standard Multiplayer-Compatibility pattern
///     (PatchingUtilities.PatchPushPopRand, see AlphaBiomes)
///     instead of a hand-rolled seeded Rand.PushState.
/// </summary>
public class WolfeinIncident
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Incident Patch]";

    public static void Patch()
    {
        DroneRaidPatch();
    }

    private static void DroneRaidPatch()
    {
        const string method = "Wolfein.IncidentWorker_DroneRaid:TryExecuteWorker";

        var target = AccessTools.DeclaredMethod(method) ?? AccessTools.Method(method);

        if (target == null)
        {
            Log.Warning($"{LogPrefix} Could not find {method}.");

            return;
        }

        // Surrounds TryExecuteWorker with Rand.PushState/PopState so the
        // PawnGenerator/CellFinder/Rand calls inside don't leak into (or read
        // a diverged) global Rand state. All clients execute the incident with
        // the same parms, so the isolated sequence stays deterministic.
        PatchingUtilities.PatchPushPopRand(target);

        Log.Message($"{LogPrefix} Patched IncidentWorker_DroneRaid.TryExecuteWorker().");
    }
}
