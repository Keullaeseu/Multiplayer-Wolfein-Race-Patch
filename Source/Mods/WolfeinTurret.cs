using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs player-driven gizmo actions on
///     Wolfein.Building_TurretGunForceAiming (custom Building_Turret).
///     The mod IL shows the actions as instance methods directly on the
///     building: b__70_0 flips holdFire, b__70_2 extracts the shell,
///     b__70_3 stops the forced attack. b__70_1 is the bool isActive getter
///     (UI-only) and is excluded by the void return-type filter, so it can
///     never be synced by accident.
///     Forced-target assignment itself goes through the verb target command
///     (Command_VerbTarget with the turret's AttackVerb), which Multiplayer
///     core already syncs like vanilla turret OrderAttack.
/// </summary>
public class WolfeinTurret
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Turret Patch]";

    private const string TurretType = "Wolfein.Building_TurretGunForceAiming";

    public static void Patch()
    {
        var turretType = AccessTools.TypeByName(TurretType);

        if (turretType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {TurretType}.");
            return;
        }

        var synced = WolfeinLambdaSync.SyncParentLambdas(turretType, "GetGizmos", typeof(void));

        if (synced != 3)
        {
            Log.Warning(
                $"{LogPrefix} Expected 3 turret actions, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {TurretType}.GetGizmos().");
    }
}