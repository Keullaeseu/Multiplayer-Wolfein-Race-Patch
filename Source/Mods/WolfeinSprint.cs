using HarmonyLib;
using Verse;
using Verse.AI;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Patch Wolfein Sprint ability to work with Multiplayer
/// </summary>
public class WolfeinSprintPatch
{
    private const string LogPrefix =
        "[Multiplayer Wolfein Race Sprint Patch]";

    private const string SprintVerbTypeName =
        "Wolfein.Verb_CastAbilitySprint";

    private const string CastJumpJobDefName =
        "CastJump";

    /// <summary>
    ///     Harmony Patch
    /// </summary>
    public static void Patch()
    {
        var _harmony = new Harmony(
            "MultiplayerWolfeinSprintPatch");

        PatchJobExposeData(_harmony);
        PatchStartNextToil(_harmony);
    }

    private static void PatchJobExposeData(Harmony _harmony)
    {
        var _method =
            AccessTools.Method(typeof(Job), "ExposeData");

        if (_method == null)
        {
            Log.Error(
                $"{LogPrefix} Could not find Verse.AI.Job.ExposeData().");
            return;
        }

        _harmony.Patch(
            _method,
            new HarmonyMethod(
                typeof(WolfeinSprintPatch),
                nameof(JobExposeDataPrefix)),
            new HarmonyMethod(
                typeof(WolfeinSprintPatch),
                nameof(JobExposeDataPostfix)));

        Log.Message(
            $"{LogPrefix} Patched Verse.AI.Job.ExposeData().");
    }

    private static void PatchStartNextToil(Harmony _harmony)
    {
        var _method =
            AccessTools.Method(
                typeof(JobDriver),
                "TryActuallyStartNextToil");

        if (_method == null)
        {
            Log.Error(
                $"{LogPrefix} Could not find " +
                "JobDriver.TryActuallyStartNextToil().");
            return;
        }

        _harmony.Patch(
            _method,
            new HarmonyMethod(
                typeof(WolfeinSprintPatch),
                nameof(TryActuallyStartNextToilPrefix)));

        Log.Message(
            $"{LogPrefix} Patched " +
            "Verse.AI.JobDriver.TryActuallyStartNextToil().");
    }

    private static void JobExposeDataPrefix(
        Job __instance,
        out SavedVerbState __state)
    {
        __state = null;

        if (!IsCastJumpJob(__instance))
            return;

        if (Scribe.mode != LoadSaveMode.Saving)
            return;

        var _verb = __instance.verbToUse;

        if (_verb == null)
            return;

        __instance.verbToUse = null;

        __state = new SavedVerbState
        {
            Job = __instance,
            Verb = _verb
        };
    }

    private static void JobExposeDataPostfix(
        SavedVerbState __state)
    {
        if (Scribe.mode != LoadSaveMode.Saving)
            return;

        if (__state?.Job != null)
            __state.Job.verbToUse = __state.Verb;
    }

    private static void TryActuallyStartNextToilPrefix(
        JobDriver __instance)
    {
        if (!IsCastJumpDriver(__instance))
            return;

        EnsureSprintVerb(__instance);
    }

    private static bool IsCastJumpJob(Job _job)
    {
        return _job?.def?.defName == CastJumpJobDefName;
    }

    private static bool IsCastJumpDriver(JobDriver _driver)
    {
        return IsCastJumpJob(_driver?.job);
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
            Log.Warning(
                $"{LogPrefix} CastJump has no pawn.");
            return;
        }

        var _sprintVerb = FindSprintVerb(_pawn);

        if (_sprintVerb == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find Sprint verb for " +
                $"{_pawn.LabelShort}.");
            return;
        }

        _job.verbToUse = _sprintVerb;

        Log.Message(
            $"{LogPrefix} Restored Sprint verb for " +
            $"{_pawn.LabelShort}.");
    }

    private static Verb FindSprintVerb(Pawn _pawn)
    {
        if (_pawn?.abilities?.abilities == null)
            return null;

        foreach (var _ability in _pawn.abilities.abilities)
        {
            var _verb = _ability?.verb;

            if (_verb == null)
                continue;

            if (_verb.GetType().FullName == SprintVerbTypeName)
                return _verb;
        }

        return null;
    }

    private sealed class SavedVerbState
    {
        public Job Job;
        public Verb Verb;
    }
}