using HarmonyLib;
using Multiplayer.Compat;
using Verse;
using Verse.AI;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Patch Wolfein Sprint ability (Wolfein.Verb_CastAbilitySprint, a
///     CastJump-based jump ability) to behave in Multiplayer.
///     Ability casting + CastJump jobs are synced by Multiplayer core, so no
///     gizmo sync is needed here. This is only a deterministic safety net:
///     if a CastJump job ever arrives without its sprint verb (e.g. after a
///     save/load cycle or a missed verb sync), re-attach the pawn's sprint
///     verb deterministically instead of erroring.
/// </summary>
public class WolfeinSprint
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Sprint Patch]";

    private const string AbilitySprintName = "Wolfein.Verb_CastAbilitySprint";
    private const string WolfeinSprintAbilityDefName = "Wolfein_Sprint";
    private const string CastJumpJobDefName = "CastJump";

    /// <summary>
    ///     Harmony Patch
    /// </summary>
    public static void Patch()
    {
        PatchStartNextToil();
    }

    private static void PatchStartNextToil()
    {
        var method = AccessTools.Method(typeof(JobDriver), nameof(JobDriver.TryActuallyStartNextToil));
        if (method == null)
        {
            Log.Error($"{LogPrefix} Could not find " + "JobDriver.TryActuallyStartNextToil().");
            return;
        }

        MpCompat.harmony.Patch(
            method,
            new HarmonyMethod(
                typeof(WolfeinSprint),
                nameof(TryActuallyStartNextToilPrefix)));

        Log.Message($"{LogPrefix} Patched " + "Verse.AI.JobDriver.TryActuallyStartNextToil().");
    }

    private static void TryActuallyStartNextToilPrefix(JobDriver __instance)
    {
        // No MP check needed: this runs in the sim on all clients, is fully
        // deterministic (iterates the pawn's own ability list), and only fills
        // in a missing verb. It never consumes Rand or touches the UI.
        if (!IsCastJumpDriver(__instance))
            return;

        EnsureSprintVerb(__instance);
    }

    private static void EnsureSprintVerb(JobDriver driver)
    {
        var job = driver.job;

        if (!IsCastJumpJob(job))
            return;

        if (job.verbToUse != null)
            return;

        var pawn = driver.pawn;
        if (pawn == null)
        {
            Log.Warning($"{LogPrefix} CastJump has no pawn.");
            return;
        }

        if (!HasWolfeinSprintAbility(pawn))
            return;

        var sprintVerb = FindSprintVerb(pawn);
        if (sprintVerb == null)
        {
            Log.Warning($"{LogPrefix} Could not find Sprint Jump verb for " + $"{pawn.LabelShort}.");
            return;
        }

        job.verbToUse = sprintVerb;
    }

    private static Verb FindSprintVerb(Pawn pawn)
    {
        if (pawn?.abilities?.abilities == null)
            return null;

        foreach (var ability in pawn.abilities.abilities)
        {
            var verb = ability?.verb;

            if (IsSprintVerb(verb))
                return verb;
        }

        return null;
    }

    private static bool HasWolfeinSprintAbility(Pawn pawn)
    {
        if (pawn?.abilities?.abilities == null)
            return false;

        foreach (var ability in pawn.abilities.abilities)
            if (ability?.def?.defName == WolfeinSprintAbilityDefName)
                return true;

        return false;
    }

    private static bool IsSprintVerb(Verb verb)
    {
        return verb?.GetType().FullName == AbilitySprintName;
    }

    private static bool IsCastJumpJob(Job job)
    {
        return job?.def?.defName == CastJumpJobDefName;
    }

    private static bool IsCastJumpDriver(JobDriver driver)
    {
        return IsCastJumpJob(driver?.job);
    }
}