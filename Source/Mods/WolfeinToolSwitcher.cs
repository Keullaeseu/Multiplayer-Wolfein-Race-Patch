using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs the weapon-mode switch gizmo on Wolfein.CompToolSwitcher
///     (IPawnWeaponGizmoProvider.GetWeaponGizmos).
///     The gizmo action cycles currentGroupIndex, rebuilds melee verbs,
///     plays a tick sound and dirties the renderer - all sim-side state
///     that must run on every client.
///     The mod IL shows it as an instance method directly on the comp
///     (b__20_0), synced by signature so ordinals cannot drift. The display
///     classes of other methods (RemoveDefaultMeleeVerbs, queue helper) are
///     left alone.
/// </summary>
public class WolfeinToolSwitcher
{
    private const string LogPrefix = "[Multiplayer Wolfein Race ToolSwitcher Patch]";

    private const string CompToolSwitcher = "Wolfein.CompToolSwitcher";

    public static void Patch()
    {
        var compType = AccessTools.TypeByName(CompToolSwitcher);

        if (compType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {CompToolSwitcher}.");
            return;
        }

        var synced = WolfeinLambdaSync.SyncParentLambdas(compType, "GetWeaponGizmos", typeof(void));

        if (synced != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected 1 tool switcher action, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {CompToolSwitcher}.GetWeaponGizmos().");
    }
}