using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs player-driven gizmo actions on
///     Wolfein.Building_TurretGunForceAiming (custom Building_Turret).
///     GetGizmos declares three Action lambdas in order:
///     ordinal 0 = ExtractShell, ordinal 1 = StopForceAttack,
///     ordinal 2 = HoldFire toggle (ordinal 3 is the isActive getter,
///     UI-only and not synced).
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
        var type = AccessTools.TypeByName(TurretType);

        if (type == null)
        {
            Log.Warning($"{LogPrefix} Could not find {TurretType}.");
            return;
        }

        // All three are plain Actions with no arguments.
        MpCompat.RegisterLambdaMethod(
            type,
            "GetGizmos", 0, 1, 2);

        Log.Message($"{LogPrefix} Patched {TurretType}.GetGizmos().");
    }
}