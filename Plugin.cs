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
            keybindsUp.Add(KeyCode.E, KillAllEnemies);
            keybindsUp.Add(KeyCode.R, Reincarnate);
            keybindsUp.Add(KeyCode.T, ToggleTimestop);

            keybindsDown.Add(KeyCode.Space, Fly);

            harmony.PatchAll();
            Logger.LogInfo($"Plugin '{modGUID}' is loaded!");
        }

        private class Message
        {
            public long time;
            public string content;

            public Message(string msg)
            {
                this.time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                this.content = msg;
            }
        }

        private void SendMessageInConsole(string message)
        {
            scrollPosition.y = messages.Count * 20;
            messages.Add(new Message(message));
        }

        private void AddFiveSP()
        {
            Singleton<UpgradeManager>.Instance.SetAvailableSkillPoints(
                Singleton<UpgradeManager>.Instance.GetAvailableSkillPoints() + 5
            );
            SendMessageInConsole("Gave 5 skill points to the player");
        }

        private void ToggleInfiniteEnergy()
        {
            Data.HasInfiniteEnergy = !Data.HasInfiniteEnergy;
            player.GetEnergySource().HasInfiniteEnergy = Data.HasInfiniteEnergy;
            SendMessageInConsole($"Infinite Energy: {Data.HasInfiniteEnergy}!");
        }

        private void ToggleTimestop()
        {
            if (Data.TimestopEnabled)
            {
                Singleton<AIManager>.Instance.DeactivateEnemyAI();
            }
            else
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

        private void KillAllEnemies()
        {
            CharacterTracker.Instance.GetEnemyCharacters().ForEach(c => c.Kill((Character)player, DamageSourceType.EnergyBeam));
        }

        private void Reincarnate()
        {
            Character target = CharacterTracker.Instance.GetClosestLivingEnemyCharacter(player.transform.position);
            if (target != null)
            {
                player.TransferConsciousnessTo((FirstPersonMover)target);
                Data.HasTransferredConsciousness = true;
            }
        }

        public static FirstPersonMover player;

        private void OnLevelDefeated()
        {
            if (Data.HasTransferredConsciousness)
            {
                int count = 0;
                Character ally = CharacterTracker.Instance.GetPlayerAlly();
                while (ally != null)
                {
                    count++;
                    ally.Kill((Character)player, DamageSourceType.EnergyBeam);
                    ally = CharacterTracker.Instance.GetPlayerAlly();
                }
                Data.HasTransferredConsciousness = false;
                if (count > 1)
                {
                    SendMessageInConsole($"Killed {count} allies");
                }
                else
                {
                    SendMessageInConsole($"Killed an ally");
                }
            }
        }

        void Update()
        {
            newTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            try
            {
                player = (FirstPersonMover)CharacterTracker.Instance.GetPlayer();
            }
            catch (Exception e) { }

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].time + 5000 < newTime)
                {
                    messages.RemoveAt(i);
                    i--;
                }
            }

            if (!Data.HasSetEventListeners)
            {
                try
                {
                    if (GlobalEventManager.Instance != null)
                    {
                        // set the event listeners
                        GlobalEventManager.Instance.AddEventListener("LevelDefeated", OnLevelDefeated);
                        Data.HasSetEventListeners = true;
                    }
                }
                catch (Exception e) { }
            }

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

        public static GUIStyle style = new()
        {
            fontSize = 18,
            normal = new()
            {
                textColor = Color.white
            }
        };
        static Rect windowRect = new(25, 25, 300, 800);
        private static List<Message> messages = new();
        public Vector2 scrollPosition = Vector2.zero;
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

            if (player != null)
            {
                float height = 280;
                float width = Screen.width * 0.18f;
                GUILayout.BeginHorizontal();

                Rect rectBox = new Rect(0, (Screen.height * 0.75f) - height, width, height);
                Rect viewRect = new Rect(rectBox.x, rectBox.y, rectBox.width, messages.Count * 20f);

                GUI.Box(rectBox, GUIContent.none);

                scrollPosition = GUI.BeginScrollView(rectBox, scrollPosition, viewRect, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

                int viewCount = 15;
                int maxCharacterLength = ((int)width / 20) * 2;
                int firstIndex = (int)scrollPosition.y / 20;

                Rect contentPos = new Rect(rectBox.x, viewRect.y + (firstIndex * 20f), rectBox.width, 20f);

                for (int i = firstIndex; i < Mathf.Min(messages.Count, firstIndex + viewCount); i++)
                {
                    string text = messages[i].content;
                    if (text.Length > maxCharacterLength)
                    {
                        text = text.Substring(0, maxCharacterLength - 3) + "...";
                    }
                    GUI.Label(contentPos, text, style);
                    contentPos.y += 20f;
                }

                GUI.EndScrollView();
                GUILayout.EndHorizontal();
            }
        }
    }

    public static class Data
    {
        public static bool HasInfiniteEnergy = false;
        public static float AimTimeScale = 0.05f;
        public static bool showGUI = false;
        public static bool TimestopEnabled = false;
        public static bool HasSetEventListeners = false;
        public static bool HasTransferredConsciousness = false;
    }

    [HarmonyPatch]
    class SteamManager_patcher
    {
        // Disables the steam integration with the game.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "Initialize")]
        static bool Initialize(ref bool __result)
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
