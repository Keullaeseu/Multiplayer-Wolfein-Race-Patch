using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Syncs the Wolfein Sprint jump (Wolfein.Verb_CastAbilitySprint).
///     Vanilla Multiplayer auto-syncs OrderForceTarget only for verb types in
///     the RimWorld assembly. The sprint verb inherits its OrderForceTarget
///     override from AncotLibrary.Verb_CastAbilityJump_Custom (calls
///     JumpUtility_Custom.OrderJump, which issues a CastJump job with
///     verbToUse), so it must be registered explicitly - the same pattern as
///     MiliraRaceAbilityShortFly in the sibling Milira patch project (which is
///     not modified here). Harmony only patches the declared implementation,
///     so an override on the sprint verb itself is preferred and the Ancot
///     base is used otherwise. The rest of the chain (TryCastShot, DoJump,
///     flyer ticks) is pure sim and re-executes deterministically on all
///     clients once the order is synced. The sibling Ancot patch does not
///     sync jump verbs, so there is no double registration.
/// </summary>
public class WolfeinSprint
{
    private const string LogPrefix = "[Multiplayer Wolfein Race Sprint Patch]";

    private const string AbilitySprintName = "Wolfein.Verb_CastAbilitySprint";
    private const string AbilityJumpBaseName = "AncotLibrary.Verb_CastAbilityJump_Custom";

    public static void Patch()
    {
        PatchOrderForceTarget();
    }

    private static void PatchOrderForceTarget()
    {
        var sprintType = AccessTools.TypeByName(AbilitySprintName);

        if (sprintType == null)
        {
            Log.Warning($"{LogPrefix} Type not found: {AbilitySprintName}.");
            return;
        }

        // Prefer an override declared on the sprint verb itself: resolving
        // through the derived type hands Harmony a method reference it
        // refuses to patch ("Patch the declared method ... instead").
        var method = AccessTools.DeclaredMethod(sprintType, "OrderForceTarget", new[] { typeof(LocalTargetInfo) });

        if (method == null)
        {
            var baseType = AccessTools.TypeByName(AbilityJumpBaseName);

            if (baseType == null)
            {
                Log.Warning($"{LogPrefix} Type not found: {AbilityJumpBaseName}.");
                return;
            }

            // Inherited unchanged by the sprint verb (verified against
            // Ancot-Library-Decomp and Wolfein-Race-IL): patching the base
            // covers it through virtual dispatch.
            method = AccessTools.DeclaredMethod(baseType, "OrderForceTarget", new[] { typeof(LocalTargetInfo) });
        }

        if (method == null)
        {
            Log.Warning($"{LogPrefix} Could not find OrderForceTarget(LocalTargetInfo) for {AbilitySprintName}.");
            return;
        }

        MP.RegisterSyncMethod(method);
        Log.Message($"{LogPrefix} Synced {method.DeclaringType?.FullName}.OrderForceTarget.");
    }
}