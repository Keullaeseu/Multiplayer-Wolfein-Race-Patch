using System.Reflection;
using System.Text;
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
///     The action is located by signature (void with a single LocalTargetInfo
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

        // Action declared directly on the comp: __instance IS the comp.
        // Action on a generated closure: resolve the comp from its field.
        if (enterPodLambda.DeclaringType != restPodType)
        {
            compField = FindCompField(restPodType, enterPodLambda.DeclaringType);

            if (compField == null)
                return;
        }

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

    // The enter-pod gizmo action is the only method shaped as
    // void (LocalTargetInfo) in this type's generated code. Scans nested
    // closure/state-machine types plus the comp type itself (in case the
    // mod refactored the lambda into a named method). On failure logs every
    // nested type and method signature so the layout can be identified.
    private static MethodInfo FindEnterPodLambda(Type restPodType)
    {
        const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.Static |
                                     BindingFlags.DeclaredOnly;

        var matches = new List<MethodInfo>();
        var report = new StringBuilder();

        foreach (var nestedType in restPodType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            report.AppendLine($"{LogPrefix} Nested type: {nestedType.Name}");

            foreach (var candidate in nestedType.GetMethods(members))
            {
                report.AppendLine($"{LogPrefix}   {Describe(candidate)}");

                if (MatchesTargetAction(candidate))
                    matches.Add(candidate);
            }
        }

        foreach (var ownMethod in restPodType.GetMethods(members))
        {
            if (ownMethod.IsSpecialName)
                continue;

            if (MatchesTargetAction(ownMethod))
            {
                report.AppendLine($"{LogPrefix} Own match: {Describe(ownMethod)}");
                matches.Add(ownMethod);
            }
        }

        if (matches.Count == 1)
        {
            Log.Message($"{LogPrefix} Enter-pod action: {Describe(matches[0])}.");
            return matches[0];
        }

        Log.Warning(
            $"{LogPrefix} Expected exactly 1 enter-pod target action, " +
            $"found {matches.Count}. Method layout:{report}");

        return null;
    }

    private static bool MatchesTargetAction(MethodInfo candidate)
    {
        if (candidate.ReturnType != typeof(void))
            return false;

        var parms = candidate.GetParameters();

        return parms.Length == 1 && parms[0].ParameterType == typeof(LocalTargetInfo);
    }

    private static string Describe(MethodInfo candidate)
    {
        var builder = new StringBuilder();

        builder.Append(candidate.DeclaringType?.Name ?? "?");
        builder.Append('.');
        builder.Append(candidate.Name);
        builder.Append(candidate.IsStatic ? " static " : " instance ");
        builder.Append(candidate.ReturnType.Name);
        builder.Append('(');

        var parms = candidate.GetParameters();

        for (var index = 0; index < parms.Length; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(parms[index].ParameterType.Name);
        }

        builder.Append(')');

        return builder.ToString();
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
        var comp = (compField != null ? compField.GetValue(__instance) : __instance) as ThingComp;

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