using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Artificial Moon toggle (Wolfein.CompCauseHediff_ArtificialMoonApparatus).
///     The mod IL shows both lambdas as instance methods directly on the comp:
///     b__21_0 is the bool isActive getter (UI-only), b__21_1 is the void
///     toggleAction (flips switchOn + applies effects). The void return-type
///     filter picks exactly the toggle, so the getter can never be synced by
///     accident.
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
            Log.Warning(
                $"{LogPrefix} Could not find {CompCauseHediffArtificialMoonApparatus}.");

            return;
        }

        var synced = WolfeinLambdaSync.SyncParentLambdas(compType, "CompGetGizmosExtra", typeof(void));

        if (synced != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected 1 artificial moon toggle, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {CompCauseHediffArtificialMoonApparatus}.CompGetGizmosExtra().");
    }
}