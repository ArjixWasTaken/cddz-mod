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
        
        private static Dictionary<KeyCode, Action> keybindsUp = new();
        private static Dictionary<KeyCode, Action> keybindsDown = new();

        public static long oldTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        public static long newTime = oldTime;

        private void Awake()
        {
            Plugin.logger = Logger;

            // Register all the keybinds
            keybindsUp.Add(KeyCode.B, AddFiveSP);
            keybindsUp.Add(KeyCode.J, ToggleInfiniteEnergy);
            keybindsUp.Add(KeyCode.E, KillClosestEnemy);
            keybindsUp.Add(KeyCode.T, ToggleTimestop);

            keybindsDown.Add(KeyCode.Space, Fly);

            harmony.PatchAll();
            Logger.LogInfo($"Plugin '{modGUID}' is loaded!");
        }

        private void AddFiveSP()
        {
            Singleton<UpgradeManager>.Instance.SetAvailableSkillPoints(
                Singleton<UpgradeManager>.Instance.GetAvailableSkillPoints() + 5
            );
            Logger.LogMessage("Gave 5 skill points to the player");
        }

        private void ToggleInfiniteEnergy()
        {
            Data.HasInfiniteEnergy = !Data.HasInfiniteEnergy;
            player.GetEnergySource().HasInfiniteEnergy = Data.HasInfiniteEnergy;
            Logger.LogMessage($"Infinite Energy: {Data.HasInfiniteEnergy}!");
        }

        private void ToggleTimestop()
        {
            if (Data.TimestopEnabled)
            {
                Singleton<AIManager>.Instance.DeactivateEnemyAI();
            } else
            {
                Singleton<AIManager>.Instance.ActivateEnemyAI();
            }
            Data.TimestopEnabled = !Data.TimestopEnabled;
        }

        private void Fly()
        {
            if (newTime - oldTime > 100)
            {
                oldTime = newTime;
                player.AddVelocity(new Vector3(0f, 5f, 0f));
            }
        }

        private void KillClosestEnemy()
        {
            Character target = Singleton<CharacterTracker>.Instance.GetClosestLivingEnemyCharacter(player.transform.position);
            if (target != null)
            {
                target.Kill((Character)player, DamageSourceType.EnergyBeam);
            }
        }

        public static FirstPersonMover player;

        void Update()
        {
            newTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            try
            {
                player = (FirstPersonMover)CharacterTracker.Instance.GetPlayer();
            }
            catch (Exception e) { }

            if (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
            {
                foreach (KeyValuePair<KeyCode, Action> keybind in keybindsUp)
                {
                    if (Input.GetKeyUp(keybind.Key))
                    {
                        keybind.Value();
                    }
                }

                foreach (KeyValuePair<KeyCode, Action> keybind in keybindsDown)
                {
                    if (Input.GetKey(keybind.Key))
                    {
                        keybind.Value();
                    }
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
                    AddFiveSP();
                }
                if (GUILayout.Button("Toggle timestop"))
                {
                    ToggleTimestop();
                }
                Data.HasInfiniteEnergy = GUILayout.Toggle(Data.HasInfiniteEnergy, "Infinite Energy");
                GUILayout.EndArea();
            }
        }
    }

    public static class Data
    {
        public static bool HasInfiniteEnergy = false;
        public static float AimTimeScale = 0.05f;
        public static bool showGUI = false;
        public static bool TimestopEnabled = false;
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
    class EscMenuHook
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EscMenu), "Show")]
        static bool Show()
        {
            Data.showGUI = true;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EscMenu), "Hide")]
        static bool Hide()
        {
            Data.showGUI = false;
            return true;
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
        static void Consume(ref bool ___HasInfiniteEnergy)
        {
            ___HasInfiniteEnergy = Data.HasInfiniteEnergy;
        }
    }
}
