using System.Reflection;
using HarmonyLib;
using Multiplayer_Wolfein_Race_Patch.Source.Mods;
using Multiplayer.API;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRacePatch.Source.Mods;

/// <summary>
///     Multiplayer Patch for Wolfein Race by MelonDove, Ancot, Last Update: 12 Sep @ 1:58pm 2026
///     https://steamcommunity.com/sharedfiles/filedetails/?id=3473140562
/// </summary>
[MpCompatFor("MelonDove.WolfeinRace")]
public class WolfeinRacePatch
{
    private const string LogPrefix =
        "[Multiplayer Wolfein Race Patch]";

    private const string CompCauseHediffArtificialMoonApparatus =
        "Wolfein.CompCauseHediff_ArtificialMoonApparatus";

    private const string CompThingContainerIntegratedRepairUnit =
        "Wolfein.CompThingContainer_IntegratedRepairUnit";

    private const string CompChargeEnergyShield =
        "Wolfein.CompChargeEnergyShield";

    public WolfeinRacePatch(ModContentPack _content)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        RegisterEnergyShield();
        RegisterIntegratedRepairUnit();
        RegisterArtificialMoonToggle();

        WolfeinSprintPatch.Patch();
        WolfeinRandomPatch.Patch();
        WolfeinIncidentPatch.Patch();

        Log.Message($"{LogPrefix} Initialized.");
    }

    #region Registers

    /// <summary>
    ///     Energy Shield
    /// </summary>
    private static void RegisterEnergyShield()
    {
        var _energyShieldType = AccessTools.TypeByName(CompChargeEnergyShield);

        if (_energyShieldType == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find {CompChargeEnergyShield}.");

            return;
        }

        MpCompat.RegisterLambdaMethod(
            _energyShieldType,
            "CompGetWornGizmosExtra", 0);
    }

    /// <summary>
    ///     Rest Pod
    /// </summary>
    private static void RegisterIntegratedRepairUnit()
    {
        var _restPodType = AccessTools.TypeByName(CompThingContainerIntegratedRepairUnit);

        if (_restPodType == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find {CompThingContainerIntegratedRepairUnit}.");

            return;
        }

        MpCompat.RegisterLambdaMethod(
            _restPodType,
            "CompGetGizmosExtra", 0, 1);
    }

    /// <summary>
    ///     Artificial Moon
    /// </summary>
    private static void RegisterArtificialMoonToggle()
    {
        var _compType = AccessTools.TypeByName(CompCauseHediffArtificialMoonApparatus);

        if (_compType == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find {CompCauseHediffArtificialMoonApparatus}.");

            return;
        }

        var _toggleMethod =
            FindArtificialMoonToggleMethod(_compType);

        if (_toggleMethod == null)
        {
            Log.Warning(
                $"{LogPrefix} Could not find the generated " +
                "Artificial Moon toggle method.");

            return;
        }

        MP.RegisterSyncMethod(_toggleMethod);

        Log.Message(
            $"{LogPrefix} Registered sync method " +
            $"{_toggleMethod.Name}.");
    }

    private static MethodInfo FindArtificialMoonToggleMethod(
        Type _compType)
    {
        return _compType
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly)
            .FirstOrDefault(_method =>
                _method.Name.StartsWith(
                    "<CompGetGizmosExtra>b__",
                    StringComparison.Ordinal) &&
                _method.ReturnType == typeof(void) &&
                _method.GetParameters().Length == 0);
    }

    #endregion
}