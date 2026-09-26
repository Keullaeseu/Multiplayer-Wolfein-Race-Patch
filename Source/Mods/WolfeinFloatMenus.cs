using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs Wolfein right-click float menu actions (all of which queue jobs
///     via TryTakeOrderedJob with captured pawn/target state).
///     These must go through RegisterLambdaDelegate - not RegisterLambdaMethod -
///     so the captured locals and LocalTargetInfo arguments are synced.
///     See AncientUrbanRuins (BuildingTrader/GetFloatMenuOptions) and
///     GiddyUp2 (AddMountingOptions) in Multiplayer-Compatibility.
/// </summary>
public class WolfeinFloatMenus
{
    private const string LogPrefix = "[Multiplayer Wolfein Race FloatMenu Patch]";

    public static void Patch()
    {
        PatchOperatableMortar();
        PatchPickUpInjector();
        PatchAdministerInjector();
    }

    private static void PatchOperatableMortar()
    {
        const string typeName = "Wolfein.CompOperatableMortar";
        const string methodName = "CompFloatMenuOptions";

        var type = AccessTools.TypeByName(typeName);

        if (type == null)
        {
            Log.Warning($"{LogPrefix} Could not find {typeName}.");
            return;
        }

        // Ordinal 0: () => pawn.jobs.TryTakeOrderedJob(MakeJob(Wolfein_OperateAutoMortar)).
        MpCompat.RegisterLambdaDelegate(type, methodName, 0);
    }

    private static void PatchPickUpInjector()
    {
        const string typeName = "Wolfein.FloatMenuOptionProvider_PickUpInjector";
        const string methodName = "GetOptionsFor";

        var type = AccessTools.TypeByName(typeName);

        if (type == null)
        {
            Log.Warning($"{LogPrefix} Could not find {typeName}.");
            return;
        }

        // Ordinal 0: PickUpOne, ordinal 1: PickUpAll, ordinal 2: PickUpSome
        // (opens Dialog_Slider whose confirm callback queues the job; the
        // dialog path is covered by syncing the outer lambda).
        MpCompat.RegisterLambdaDelegate(
            type,
            methodName, 0, 1, 2);
    }

    private static void PatchAdministerInjector()
    {
        const string typeName = "Wolfein.FloatMenuOptionProvider_AdministerInjector";
        const string methodName = "GetOptionsFor";

        var type = AccessTools.TypeByName(typeName);

        if (type == null)
        {
            Log.Warning($"{LogPrefix} Could not find {typeName}.");
            return;
        }

        // Ordinal 0: () => giver.jobs.TryTakeOrderedJob(MakeJob(Wolfein_AdministerInjector)).
        MpCompat.RegisterLambdaDelegate(type, methodName, 0);
    }
}