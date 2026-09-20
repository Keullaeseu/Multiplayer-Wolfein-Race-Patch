using HarmonyLib;
using Multiplayer.Compat;
using Verse;
using Verse.AI;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Patch Wolfein Sprint ability to work with Multiplayer
/// </summary>
public class WolfeinSprintPatch
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
        PatchJobExposeData();
        PatchStartNextToil();
    }

    private static void PatchJobExposeData()
    {
        var _method =
            AccessTools.Method(typeof(Job), "ExposeData");

        if (_method == null)
        {
            Log.Error($"{LogPrefix} Could not find Verse.AI.Job.ExposeData().");
            return;
        }

        MpCompat.harmony.Patch(
            _method,
            new HarmonyMethod(
                typeof(WolfeinSprintPatch),
                nameof(JobExposeDataPrefix)),
            new HarmonyMethod(
                typeof(WolfeinSprintPatch),
                nameof(JobExposeDataPostfix)));

        Log.Message($"{LogPrefix} Patched Verse.AI.Job.ExposeData().");
    }

    private static void PatchStartNextToil()
    {
        var _method = AccessTools.Method(typeof(JobDriver), "TryActuallyStartNextToil");
        if (_method == null)
        {
            Log.Error($"{LogPrefix} Could not find " + "JobDriver.TryActuallyStartNextToil().");
            return;
        }

        MpCompat.harmony.Patch(
            _method,
            new HarmonyMethod(
                typeof(WolfeinSprintPatch),
                nameof(TryActuallyStartNextToilPrefix)));

        Log.Message($"{LogPrefix} Patched " + "Verse.AI.JobDriver.TryActuallyStartNextToil().");
    }

    private static void JobExposeDataPrefix(Job __instance, out SavedVerbState __state)
    {
        __state = null;

        if (!IsCastJumpJob(__instance))
            return;

        if (Scribe.mode != LoadSaveMode.Saving)
            return;

        var _verb = __instance.verbToUse;

        if (!IsSprintVerb(_verb))
            return;

        __instance.verbToUse = null;

        __state = new SavedVerbState
        {
            Job = __instance,
            Verb = _verb
        };
    }

    private static void JobExposeDataPostfix(SavedVerbState __state)
    {
        if (Scribe.mode != LoadSaveMode.Saving)
            return;

        if (__state?.Job != null)
            __state.Job.verbToUse = __state.Verb;
    }

    private static void TryActuallyStartNextToilPrefix(JobDriver __instance)
    {
        if (!IsCastJumpDriver(__instance))
            return;

        EnsureSprintVerb(__instance);
    }

    private static void EnsureSprintVerb(JobDriver _driver)
    {
        var _job = _driver.job;

        if (!IsCastJumpJob(_job))
            return;

        if (_job.verbToUse != null)
            return;

        var _pawn = _driver.pawn;
        if (_pawn == null)
        {
            Log.Warning($"{LogPrefix} CastJump has no pawn.");
            return;
        }

        if (!HasWolfeinSprintAbility(_pawn))
            return;

        var _sprintVerb = FindSprintVerb(_pawn);
        if (_sprintVerb == null)
        {
            Log.Warning($"{LogPrefix} Could not find Sprint Jump verb for " + $"{_pawn.LabelShort}.");
            return;
        }

        _job.verbToUse = _sprintVerb;
    }

    private static Verb FindSprintVerb(Pawn _pawn)
    {
        if (_pawn?.abilities?.abilities == null)
            return null;

        foreach (var _ability in _pawn.abilities.abilities)
        {
            var _verb = _ability?.verb;

            if (IsSprintVerb(_verb))
                return _verb;
        }

        return null;
    }

    private static bool HasWolfeinSprintAbility(Pawn _pawn)
    {
        if (_pawn?.abilities?.abilities == null)
            return false;

        foreach (var _ability in _pawn.abilities.abilities)
            if (_ability?.def?.defName == WolfeinSprintAbilityDefName)
                return true;

        return false;
    }

    private static bool IsSprintVerb(Verb _verb)
    {
        return _verb?.GetType().FullName == AbilitySprintName;
    }

    private static bool IsCastJumpJob(Job _job)
    {
        return _job?.def?.defName == CastJumpJobDefName;
    }

    private static bool IsCastJumpDriver(JobDriver _driver)
    {
        return IsCastJumpJob(_driver?.job);
    }

    private sealed class SavedVerbState
    {
        public Job Job;
        public Verb Verb;
    }
}