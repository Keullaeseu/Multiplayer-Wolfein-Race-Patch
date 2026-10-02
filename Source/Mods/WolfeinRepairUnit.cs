using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Rest Pod (Wolfein.CompThingContainer_IntegratedRepairUnit) gizmos.
///     The mod IL shows both actions as instance methods directly on the comp
///     (b__12_0 exits via CancelLoad, b__12_1 enters with a LocalTargetInfo);
///     the only nested type is the iterator state machine. Name-based
///     delegate lookup (MpCompat.RegisterLambdaDelegate) re-resolves the
///     generated nested name and fails here
///     ("Couldn't find type ...+[Ref ...]"), so both actions are synced as
///     methods instead: their instance is the syncable comp itself.
/// </summary>
public class WolfeinRepairUnit
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Repair Unit Patch]";

    private const string CompThingContainerIntegratedRepairUnit = "Wolfein.CompThingContainer_IntegratedRepairUnit";

    public static void Patch()
    {
        var restPodType = AccessTools.TypeByName(CompThingContainerIntegratedRepairUnit);

        if (restPodType == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find {CompThingContainerIntegratedRepairUnit}.");

            return;
        }

        var synced = WolfeinLambdaSync.SyncParentLambdas(restPodType, "CompGetGizmosExtra", typeof(void));
        synced += WolfeinLambdaSync.SyncParentLambdas(restPodType, "CompGetGizmosExtra", typeof(void),
            typeof(LocalTargetInfo));

        if (synced != 2)
        {
            Log.Warning(
                $"{LogPrefix} Expected 2 rest pod actions, synced {synced}.");
            return;
        }

        // Safety net (same pattern as MiliraRaceJobs.PatchCancelLoad in the
        // sibling Milira patch, not modified here): syncing the named method
        // as well means the exit action stays synced even if a future mod
        // build moves the lambdas again. The mod IL shows CancelLoad is only
        // ever called from the exit gizmo, never from ticks, so this cannot
        // double-fire sim logic.
        SyncCancelLoad(restPodType);

        Log.Message($"{LogPrefix} Patched {CompThingContainerIntegratedRepairUnit}.CompGetGizmosExtra().");
    }

    private static void SyncCancelLoad(Type restPodType)
    {
        var method = AccessTools.DeclaredMethod(restPodType, "CancelLoad");

        if (method == null)
        {
            Log.Warning($"{LogPrefix} Could not find CancelLoad.");
            return;
        }

        MP.RegisterSyncMethod(method);
        Log.Message($"{LogPrefix} Synced {CompThingContainerIntegratedRepairUnit}.CancelLoad.");
    }
}