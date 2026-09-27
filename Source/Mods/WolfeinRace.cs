using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Multiplayer Patch for Wolfein Race by MelonDove, Ancot, Last Update: 27 Sep @ 8:08am 2026
///     https://steamcommunity.com/sharedfiles/filedetails/?id=3473140562
///     Entry point: schedules <see cref="LatePatch" /> once mods are loaded,
///     which delegates to one patch class per Wolfein feature.
/// </summary>
[MpCompatFor("MelonDove.WolfeinRace")]
public class WolfeinRace
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Patch]";

    public WolfeinRace(ModContentPack content)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        // Each block is isolated: a bad lambda ordinal in one Wolfein update
        // must not prevent the remaining patches from registering.
        SafePatch(WolfeinEnergyShield.Patch);
        SafePatch(WolfeinRepairUnit.Patch);
        SafePatch(WolfeinArtificialMoon.Patch);

        SafePatch(WolfeinSprint.Patch);
        SafePatch(WolfeinRandom.Patch);
        SafePatch(WolfeinIncident.Patch);
        SafePatch(WolfeinToolSwitcher.Patch);
        SafePatch(WolfeinTurret.Patch);
        SafePatch(WolfeinFloatMenus.Patch);

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void SafePatch(Action patch)
    {
        try
        {
            patch();
        }
        catch (Exception exception)
        {
            Log.Error($"{LogPrefix} Patch {patch.Method.Name} failed: {exception}");
        }
    }
}