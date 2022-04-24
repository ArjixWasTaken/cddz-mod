using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneDroneInTheDangerZone
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("Clone Drone in the Danger Zone.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource logger;
        private const string modGUID = "com.arjix.cddz.mods";
        private const string modName = "BlaBla";
        private const string modVersion = "69.4.20";
        private readonly Harmony harmony = new Harmony(modGUID);
        private void Awake()
        {
            Plugin.logger = Logger;
            // Plugin startup logic
            Logger.LogInfo($"Plugin '{modGUID}' is loaded!");
            harmony.PatchAll();
        }

        public static long oldTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        public static long newTime = oldTime;
        public static FirstPersonMover player;

        void Update()
        {
            Plugin.newTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            try
            {
                player = (FirstPersonMover)CharacterTracker.Instance.GetPlayer();
            }
            catch (Exception e) { }

            if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyUp(KeyCode.B))
            {
                // Give 5 skill points to the player
                Singleton<UpgradeManager>.Instance.SetAvailableSkillPoints(
                    Singleton<UpgradeManager>.Instance.GetAvailableSkillPoints() + 5
                );
                Logger.LogMessage("Gave 5 skill points to the player");
            } else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyUp(KeyCode.J)) {
                // toggle infinite energy
                Data.HasInfiniteEnergy = !Data.HasInfiniteEnergy;
                player.GetEnergySource().HasInfiniteEnergy = Data.HasInfiniteEnergy;
                Logger.LogMessage($"Infinite Energy: {Data.HasInfiniteEnergy}!");
            } else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKey(KeyCode.Space))
            {
                // Similar to the jetpack hack in minecraft.
                if (Plugin.newTime - Plugin.oldTime > 100)
                {
                    Plugin.oldTime = Plugin.newTime;
                    player.AddVelocity(new Vector3(0f, 5f, 0f));
                }
            } else if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && Input.GetKeyUp(KeyCode.E))
            {
                Character target = Singleton<CharacterTracker>.Instance.GetClosestLivingEnemyCharacter(player.transform.position);
                if (target != null)
                {
                    target.Kill((Character)player, DamageSourceType.EnergyBeam);
                }
            }
        }

        static Rect windowRect = new(25, 25, 300, 70);
        void OnGUI()
        {
            if (Data.showGUI)
            {
                GUILayout.BeginArea(windowRect);
                if (GUILayout.Button("+5 Skill Points"))
                {
                    Singleton<UpgradeManager>.Instance.SetAvailableSkillPoints(
                        Singleton<UpgradeManager>.Instance.GetAvailableSkillPoints() + 5
                    );
                    Logger.LogMessage("Gave 5 skill points to the player");
                }
                Data.HasInfiniteEnergy = GUILayout.Toggle(Data.HasInfiniteEnergy, "Infinite Energy");
                GUILayout.EndArea();
            }
        }
    }

    public static class Data
    {
        public static bool HasInfiniteEnergy = false;
        public static float AimTimeScale = 0.1f;
        public static bool showGUI = false;
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

    [HarmonyPatch(typeof(EscMenu), "Show")]
    class EscMenuShow
    {
        [HarmonyPrefix]
        static void Show()
        {
            Data.showGUI = true;
        }
    }

    [HarmonyPatch(typeof(EscMenu), "Hide")]
    class EscMenuHide
    {
        [HarmonyPrefix]
        static void Hide()
        {
            Data.showGUI = false;
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
