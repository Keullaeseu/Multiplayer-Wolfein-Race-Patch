using HarmonyLib;
using Multiplayer.Compat;
using RimWorld;
using Verse;

namespace Multiplayer_Wolfein_Race_Patch.Source.Mods;

public class WolfeinIncidentPatch
{
    private const string LogPrefix =
        "[Multiplayer Wolfein Race Incident Patch]";

    public static void Patch()
    {
        DroneRaidPatch();
    }

    private static void DroneRaidPatch()
    {
        var _incidentType = AccessTools.TypeByName(
            "Wolfein.IncidentWorker_DroneRaid");

        if (_incidentType == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find " +
                "Wolfein.IncidentWorker_DroneRaid.");

            return;
        }

        var _tryExecuteWorker = AccessTools.Method(
            _incidentType,
            "TryExecuteWorker",
            [
                typeof(IncidentParms)
            ]);

        if (_tryExecuteWorker == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find " +
                "TryExecuteWorker(IncidentParms).");

            return;
        }

        MpCompat.harmony.Patch(
            _tryExecuteWorker,
            new HarmonyMethod(
                typeof(WolfeinIncidentPatch),
                nameof(DroneRaidSeededPrefix)),
            finalizer: new HarmonyMethod(
                typeof(WolfeinIncidentPatch),
                nameof(DroneRaidSeededFinalizer)));
    }

    private static void DroneRaidSeededPrefix(IncidentParms parms)
    {
        int _seed;

        if (parms.target is Map _map)
        {
            _seed = Gen.HashCombineInt(
                _map.uniqueID,
                Find.TickManager.TicksGame);

            _seed = Gen.HashCombineInt(
                _seed,
                parms.spawnCenter.GetHashCode());

            _seed = Gen.HashCombineInt(
                _seed,
                parms.points.GetHashCode());
        }
        else
        {
            _seed = Find.TickManager.TicksGame;
        }

        Rand.PushState(_seed);
    }

    private static Exception DroneRaidSeededFinalizer(Exception __exception, bool __state)
    {
        if (__state)
            Rand.PopState();

        return __exception;
    }
}