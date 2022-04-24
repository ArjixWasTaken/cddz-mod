using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace CloneDroneInTheDangerZone
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("Clone Drone in the Danger Zone.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "com.arjix.cddz.mods";
        private const string modName = "BlaBla";
        private const string modVersion = "69.4.20";
        private readonly Harmony harmony = new Harmony(modGUID);
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin '{modGUID}' is loaded!");
            harmony.PatchAll();
        }

        void Update()
        {
            if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyUp(KeyCode.B))
            {
                // Give 5 skill points to the player
                Singleton<UpgradeManager>.Instance.SetAvailableSkillPoints(
                    Singleton<UpgradeManager>.Instance.GetAvailableSkillPoints() + 5
                );
                Logger.LogInfo("Gave 5 skill points to the player");
            } else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyUp(KeyCode.J)) {
                // toggle infinite energy
                Data.HasInfiniteEnergy = !Data.HasInfiniteEnergy;
                CharacterTracker.Instance.GetPlayer().GetEnergySource().HasInfiniteEnergy = Data.HasInfiniteEnergy;
                Logger.LogInfo($"Infinite Energy: {Data.HasInfiniteEnergy}!");
            }
        }
    }

    public static class Data
    {
        public static bool HasInfiniteEnergy = false;
        public static float AimTimeScale = 0.1f;
    }

    [HarmonyPatch]
    class Steam_patcher
    {
        // Disables the steam integration with the game.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "Initialize")]
        static bool Start(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    class UpgradeManager_patcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UpgradeManager), "GetUpgrade")]
        static bool GetUpgrade(ref UpgradeDescription __result, UpgradeType upgradeType, int level = 1)
        {
            ref List<UpgradeDescription> upgradeDescriptions = ref Singleton<UpgradeManager>.Instance.UpgradeDescriptions;
            for (int i = 0; i < upgradeDescriptions.Count; i++)
            {
                UpgradeDescription upgrade = upgradeDescriptions[i];
                if (upgrade.UpgradeType == upgradeType && upgrade.Level == level)
                {
                    __result = upgrade;
                    if (__result.UpgradeType == UpgradeType.AimTime)
                    {
                        ((AimTimeUpgrade)__result).NewTimeScale = Data.AimTimeScale;
                    }
                    return false;
                }
            }
            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(EnergySource), "Consume")]
    class EnergySourcePatches
    {
        [HarmonyPrefix]
        static void setInfiniteEnergyRef(ref bool ___HasInfiniteEnergy)
        {
            ___HasInfiniteEnergy = Data.HasInfiniteEnergy;
        }
    }
}
