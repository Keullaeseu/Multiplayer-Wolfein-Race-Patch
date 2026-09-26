using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Artificial Moon toggle (Wolfein.CompCauseHediff_ArtificialMoonApparatus).
///     CompGetGizmosExtra declares two lambdas:
///     ordinal 0 is `isActive` (Func{bool}, UI-only getter),
///     ordinal 1 is `toggleAction` (Action, flips switchOn + applies effects).
///     Only the toggle needs syncing.
/// </summary>
public class WolfeinArtificialMoon
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Artificial Moon Patch]";

    private const string CompCauseHediffArtificialMoonApparatus = "Wolfein.CompCauseHediff_ArtificialMoonApparatus";

    public static void Patch()
    {
        var compType = AccessTools.TypeByName(CompCauseHediffArtificialMoonApparatus);

        if (compType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {CompCauseHediffArtificialMoonApparatus}.");
            return;
        }

        // Ordinal 1 == toggleAction. Matches RegisterLambdaMethod usage
        // in Multiplayer-Compatibility (e.g. CommonSense DoCleanComp, AlphaBiomes).
        MpCompat.RegisterLambdaMethod(compType, "CompGetGizmosExtra", 1);

        Log.Message($"{LogPrefix} Patched {CompCauseHediffArtificialMoonApparatus}.CompGetGizmosExtra().");
    }
}