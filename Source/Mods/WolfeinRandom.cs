using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Patch Wolfein Random
/// </summary>
public class WolfeinRandomPatch
{
    private const string LogPrefix =
        "[Multiplayer Wolfein Race Random Patch]";

    private static readonly Random CosmeticRandom = new();

    public static void Patch()
    {
        PatchRandom();
    }

    private static void PatchRandom()
    {
        var _genStepGenPawnAroundMapCenterDefendBaseType =
            AccessTools.TypeByName("Wolfein.GenStep_GenPawnAroundMapCenter_DefendBase");
        var _seedPartMethodInfo = _genStepGenPawnAroundMapCenterDefendBaseType != null
            ? AccessTools.DeclaredPropertyGetter(_genStepGenPawnAroundMapCenterDefendBaseType, "SeedPart")
            : null;
        if (_seedPartMethodInfo == null)
        {
            Log.Error(
                $"{LogPrefix} Could not find Wolfein.GenStep_GenPawnAroundMapCenter_DefendBase:SeedPart.");
            return;
        }

        MpCompat.harmony.Patch(_seedPartMethodInfo,
            new HarmonyMethod(typeof(WolfeinRandomPatch), "SeedPartStatic"));

        var _compAdditionalGraphicType = AccessTools.TypeByName("Wolfein.CompAdditionalGraphic");
        var _compAdditionalGraphicConstructorInfo = _compAdditionalGraphicType != null
            ? AccessTools.DeclaredConstructor(_compAdditionalGraphicType, Type.EmptyTypes)
            : null;
        if (_compAdditionalGraphicConstructorInfo == null)
        {
            Log.Error(
                $"{LogPrefix} Could not find Wolfein.CompAdditionalGraphic.");

            return;
        }

        MpCompat.harmony.Patch(_compAdditionalGraphicConstructorInfo,
            transpiler: new HarmonyMethod(typeof(WolfeinRandomPatch), "RemoveRandomFromAdditionalGraphicCtor"));
    }

    // GenStep_GenPawnAroundMapCenter_DefendBase SeedPart (original seed is 341125487)
    private static bool SeedPartStatic(ref int __result)
    {
        __result = 341125487;
        return false;
    }

    private static IEnumerable<CodeInstruction> RemoveRandomFromAdditionalGraphicCtor(
        IEnumerable<CodeInstruction> _instructions)
    {
        var _randomRange = AccessTools.Method(typeof(Rand), "Range", [
            typeof(float),
            typeof(float)
        ]);
        var _cosmeticRange = AccessTools.Method(typeof(WolfeinRandomPatch), "CosmeticFloatRange");
        if (_randomRange == null || _cosmeticRange == null)
        {
            Log.Error(
                $"{LogPrefix} Could not find the Random Range methods.");

            yield break;
        }

        foreach (var _instruction in _instructions)
        {
            if (_instruction.Calls(_randomRange))
                _instruction.operand = _cosmeticRange;
            yield return _instruction;
        }
    }

    private static float CosmeticFloatRange(float _minimum, float _maximum)
    {
        return (float)(CosmeticRandom.NextDouble() * (_maximum - (double)_minimum)) + _minimum;
    }
}