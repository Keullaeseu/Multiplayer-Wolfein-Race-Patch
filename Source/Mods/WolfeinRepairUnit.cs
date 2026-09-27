using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Compat;
using Verse;
using Verse.AI;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Rest Pod (Wolfein.CompThingContainer_IntegratedRepairUnit) gizmos.
///     Gizmo 0 (Command_Action, no args) goes through RegisterLambdaMethod.
///     Gizmo 1 (Command_Target, takes LocalTargetInfo) needs delegate sync
///     (captured pod comp + target). A plain sync method cannot be used here:
///     Multiplayer would try to serialize the compiler-generated closure
///     object itself, which has no sync worker.
///     The lambda is located by signature (void with a single LocalTargetInfo
///     parameter) instead of by generated name or ordinal, because the mod
///     update produces runtime lambda type names that name-based lookup
///     (MpCompat.RegisterLambdaDelegate) cannot resolve
///     ("Couldn't find type ...+[Ref ...]"). A Harmony prefix with
///     (object __instance, LocalTargetInfo target) needs no generated names
///     either, and forwards into a synced wrapper on this stable class.
/// </summary>
public class WolfeinRepairUnit
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Repair Unit Patch]";

    private const string CompThingContainerIntegratedRepairUnit = "Wolfein.CompThingContainer_IntegratedRepairUnit";

    private const string EnterContainerJobDefName = "Ancot_EnterContainer";

    private static FieldInfo compField;

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

        PatchEnterPodTarget(restPodType);

        Log.Message($"{LogPrefix} Patched {CompThingContainerIntegratedRepairUnit}.CompGetGizmosExtra().");
    }

    private static void PatchEnterPodTarget(Type restPodType)
    {
        var enterPodLambda = FindEnterPodLambda(restPodType);

        if (enterPodLambda == null)
            return;

        compField = FindCompField(restPodType, enterPodLambda.DeclaringType);

        if (compField == null)
            return;

        MpCompat.harmony.Patch(
            enterPodLambda,
            new HarmonyMethod(typeof(WolfeinRepairUnit), nameof(EnterRestPodPrefix)));

        var syncedMethod = AccessTools.DeclaredMethod(typeof(WolfeinRepairUnit), nameof(SyncedEnterRestPod));

        if (syncedMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find {nameof(SyncedEnterRestPod)}.");
            return;
        }

        MP.RegisterSyncMethod(syncedMethod);
    }

    // The enter-pod gizmo action is the only lambda shaped as
    // void (LocalTargetInfo) anywhere in this type's generated code.
    private static MethodInfo FindEnterPodLambda(Type restPodType)
    {
        var candidates = new List<MethodInfo>();

        foreach (var nestedType in restPodType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        foreach (var candidate in nestedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance | BindingFlags.Static |
                                                        BindingFlags.DeclaredOnly))
        {
            if (candidate.ReturnType != typeof(void))
                continue;

            var parms = candidate.GetParameters();

            if (parms.Length != 1 || parms[0].ParameterType != typeof(LocalTargetInfo))
                continue;

            candidates.Add(candidate);
        }

        if (candidates.Count != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected exactly 1 enter-pod target lambda, " +
                $"found {candidates.Count}.");

            return null;
        }

        return candidates[0];
    }

    // The closure holds the comp in a field typed exactly as the comp class
    // (the decompiled source shows the lambda only captures `this`).
    private static FieldInfo FindCompField(Type restPodType, Type displayType)
    {
        var matches = new List<FieldInfo>();

        foreach (var field in displayType.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance | BindingFlags.DeclaredOnly))
            if (field.FieldType == restPodType)
                matches.Add(field);

        if (matches.Count != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected exactly 1 comp field on {displayType.Name}, " +
                $"found {matches.Count}.");

            return null;
        }

        return matches[0];
    }

    private static bool EnterRestPodPrefix(object __instance, LocalTargetInfo target)
    {
        var comp = compField.GetValue(__instance) as ThingComp;

        if (comp == null)
        {
            Log.Warning($"{LogPrefix} Could not resolve rest pod comp, running original gizmo action.");
            return true;
        }

        SyncedEnterRestPod(comp, target);
        return false;
    }

    private static void SyncedEnterRestPod(ThingComp comp, LocalTargetInfo target)
    {
        var pawn = target.Pawn;

        if (pawn == null)
            return;

        if (comp?.parent == null)
            return;

        var enterDef = DefDatabase<JobDef>.GetNamed(EnterContainerJobDefName, false);

        if (enterDef == null)
        {
            Log.Warning($"{LogPrefix} Could not find {EnterContainerJobDefName} job def.");
            return;
        }

        var job = new Job(enterDef, new LocalTargetInfo(comp.parent));
        pawn.jobs.TryTakeOrderedJob(job);
    }
}