using HarmonyLib;
using Multiplayer.Compat;
using Verse;
using Random = UnityEngine.Random;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Patch Wolfein Random usage:
///     - GenStep SeedPart must not consume synced Verse.Rand (map-gen seed
///     would depend on current Rand state and diverge across clients).
///     - CompAdditionalGraphic.randTime is a pure visual bobbing offset,
///     so it must not consume synced Verse.Rand either. Redirect it to
///     UnityEngine.Random (unsynced, main-thread cosmetic), the standard
///     approach for visual-only randomness.
/// </summary>
public class WolfeinRandom
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Random Patch]";

    public static void Patch()
    {
        PatchRandom();
    }

    private static void PatchRandom()
    {
        var genStepType = AccessTools.TypeByName("Wolfein.GenStep_GenPawnAroundMapCenter_DefendBase");
        var seedPartMethodInfo = genStepType != null
            ? AccessTools.DeclaredPropertyGetter(genStepType, "SeedPart")
            : null;
        if (seedPartMethodInfo == null)
        {
            Log.Error($"{LogPrefix} Could not find Wolfein.GenStep_GenPawnAroundMapCenter_DefendBase:SeedPart.");
            return;
        }

        MpCompat.harmony.Patch(seedPartMethodInfo,
            new HarmonyMethod(typeof(WolfeinRandom), nameof(SeedPartStatic)));

        var compAdditionalGraphicType = AccessTools.TypeByName("Wolfein.CompAdditionalGraphic");
        var compAdditionalGraphicConstructorInfo = compAdditionalGraphicType != null
            ? AccessTools.DeclaredConstructor(compAdditionalGraphicType, Type.EmptyTypes)
            : null;
        if (compAdditionalGraphicConstructorInfo == null)
        {
            Log.Error($"{LogPrefix} Could not find Wolfein.CompAdditionalGraphic.");
            return;
        }

        MpCompat.harmony.Patch(compAdditionalGraphicConstructorInfo,
            transpiler: new HarmonyMethod(typeof(WolfeinRandom), nameof(RemoveSyncedRandFromAdditionalGraphicCtor)));
    }

    // GenStep_GenPawnAroundMapCenter_DefendBase SeedPart (original: 341125487 + Rand.Range(0, 99999)).
    // SeedPart is used to derive the map-gen seed; consuming synced Rand here
    // makes the seed depend on whatever Rand state each client happens to have.
    // A constant keeps map generation deterministic across clients.
    private static bool SeedPartStatic(ref int __result)
    {
        __result = 341125487;
        return false;
    }

    private static IEnumerable<CodeInstruction> RemoveSyncedRandFromAdditionalGraphicCtor(
        IEnumerable<CodeInstruction> instructions)
    {
        var instructionsList = instructions.ToList();

        var syncedRange = AccessTools.Method(typeof(Rand), nameof(Rand.Range), [
            typeof(float),
            typeof(float)
        ]);
        var cosmeticRange = AccessTools.Method(typeof(WolfeinRandom), nameof(CosmeticFloatRange));

        // Never wipe the constructor body on failure: yield the original
        // instructions unmodified so the comp still initializes correctly.
        if (syncedRange == null || cosmeticRange == null)
        {
            Log.Error($"{LogPrefix} Could not find the Random Range methods, " +
                      "leaving CompAdditionalGraphic constructor unpatched.");

            foreach (var original in instructionsList)
                yield return original;

            yield break;
        }

        var patched = false;

        foreach (var instruction in instructionsList)
        {
            if (instruction.Calls(syncedRange))
            {
                instruction.operand = cosmeticRange;
                patched = true;
            }

            yield return instruction;
        }

        if (!patched)
            Log.Warning($"{LogPrefix} No synced Rand.Range found in CompAdditionalGraphic constructor.");
    }

    // Visual-only bobbing offset (floatOffset in CompTick). Must not touch
    // synced Verse.Rand. UnityEngine.Random is unsynced and safe for main-thread
    // cosmetic use; divergence here only affects the shimmer phase, not the sim.
    private static float CosmeticFloatRange(float minimum, float maximum)
    {
        return Random.Range(minimum, maximum);
    }
}