using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Energy Shield recharge gizmo (Wolfein.CompChargeEnergyShield).
///     The worn-gizmo action resets the shield and consumes a charge,
///     which is sim-side state and must run on every client.
///     The mod IL shows it as an instance method directly on the comp
///     (b__8_0), synced by signature so ordinals cannot drift.
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
            Log.Warning(
                $"{LogPrefix} Could not find {CompChargeEnergyShield}.");

            return;
        }

        var synced = WolfeinLambdaSync.SyncParentLambdas(energyShieldType, "CompGetWornGizmosExtra", typeof(void));

        if (synced != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected 1 energy shield action, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {CompChargeEnergyShield}.CompGetWornGizmosExtra().");
    }
}