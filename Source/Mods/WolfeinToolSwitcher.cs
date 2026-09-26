using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs the weapon-mode switch gizmo on Wolfein.CompToolSwitcher
///     (IPawnWeaponGizmoProvider.GetWeaponGizmos).
///     The gizmo action cycles currentGroupIndex, rebuilds melee verbs,
///     plays a tick sound and dirties the renderer - all sim-side state
///     that must run on every client.
/// </summary>
public class WolfeinToolSwitcher
{
    private const string LogPrefix = "[Multiplayer Wolfein Race ToolSwitcher Patch]";

    private const string CompToolSwitcher = "Wolfein.CompToolSwitcher";

    public static void Patch()
    {
        var type = AccessTools.TypeByName(CompToolSwitcher);

        if (type == null)
        {
            Log.Warning($"{LogPrefix} Could not find {CompToolSwitcher}.");
            return;
        }

        // Ordinal 0: the mode-cycle Action in GetWeaponGizmos.
        // Plain Action with no arguments -> RegisterLambdaMethod.
        // Matches e.g. CommonSense DoCleanComp in Multiplayer-Compatibility.
        MpCompat.RegisterLambdaMethod(
            type,
            "GetWeaponGizmos", 0);

        Log.Message($"{LogPrefix} Patched {CompToolSwitcher}.GetWeaponGizmos().");
    }
}
