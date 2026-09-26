using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Rest Pod (Wolfein.CompThingContainer_IntegratedRepairUnit) gizmos.
///     Gizmo 0 (Command_Action, no args) goes through RegisterLambdaMethod.
///     Gizmo 1 (Command_Target, takes LocalTargetInfo) must go through
///     RegisterLambdaDelegate so the target is synced. Using
///     RegisterLambdaMethod for it would not sync the target correctly.
///     See: AncientUrbanRuins (BuildingTrader/GetFloatMenuOptions),
///     GiddyUp2 (AddMountingOptions) in Multiplayer-Compatibility.
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

        // Ordinal 0: () => CancelLoad() - plain Action, no arguments.
        MpCompat.RegisterLambdaMethod(restPodType, "CompGetGizmosExtra", 0);

        // Ordinal 1: (LocalTargetInfo target) => { ... TryTakeOrderedJob ... }
        // Delegate sync handles the LocalTargetInfo argument + captured `this`.
        MpCompat.RegisterLambdaDelegate(restPodType, "CompGetGizmosExtra", 1);

        Log.Message($"{LogPrefix} Patched {CompThingContainerIntegratedRepairUnit}.CompGetGizmosExtra().");
    }
}