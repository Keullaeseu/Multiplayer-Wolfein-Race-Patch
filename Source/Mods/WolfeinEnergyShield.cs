using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Energy Shield recharge gizmo (Wolfein.CompChargeEnergyShield).
///     The worn-gizmo action resets the shield and consumes a charge,
///     which is sim-side state and must run on every client.
/// </summary>
public class WolfeinEnergyShield
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Energy Shield Patch]";

    private const string CompChargeEnergyShield = "Wolfein.CompChargeEnergyShield";

    public static void Patch()
    {
        var energyShieldType = AccessTools.TypeByName(CompChargeEnergyShield);

        if (energyShieldType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {CompChargeEnergyShield}.");

            return;
        }

        // Ordinal 0: the recharge Action in CompGetWornGizmosExtra.
        MpCompat.RegisterLambdaMethod(energyShieldType, "CompGetWornGizmosExtra", 0);

        Log.Message($"{LogPrefix} Patched {CompChargeEnergyShield}.CompGetWornGizmosExtra().");
    }
}