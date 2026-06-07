using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace AutoPlayMod
{
    [BepInPlugin("com.cownow.autoplay", "AutoPlay", "1.0.0")]
    public class AutoPlay : BaseUnityPlugin
    {
        private ConfigEntry<KeyCode> configToggleKey;
        private ConfigEntry<bool> configPlaySoundFeedback;
        private ConfigEntry<bool> configModEnabled;

        private void Awake()
        {
            var harmony = new Harmony("com.cownow.autoplay");
            harmony.PatchAll();

            configToggleKey = Config.Bind("General", "ToggleKey", KeyCode.F8, "Autoplay Toggle Key");
            configPlaySoundFeedback = Config.Bind("General", "PlaySoundFeedback", true, "Play sound feedback when autoplay is toggled");
            configModEnabled = Config.Bind("General", "ModEnabled", true, "Enable or disable the AutoPlay mod");

            Logger.LogInfo("========================================");
            Logger.LogInfo("Auto Play Mod v1.0.0");
            Logger.LogInfo("Auto play songs but normally score");
            Logger.LogInfo("                --by CowNow");
            Logger.LogInfo("========================================");
        }
    }

    public class AutoPlayMod
    {
        // Mod Configuration
        private static KeyCode toggleKey = KeyCode.F8;
        private static bool modEnabled = true;
        private static bool playSoundFeedback = true;

        private static string customAutoPlayText = "AutoPlay (Mod)";

        // Autoplay Status
        private static bool customAutoPlayEnabled = false;
        private static GUIStyle notificationStyle;
        private static string notificationText = "";
        private static float notificationTime = 0f;
        private static float notificationDuration = 2f;

        [HarmonyPatch(typeof(scrUIController), "Update")]
        public class UIController_Update
        {
            public static void Postfix()
            {
                if (!modEnabled) return;

                if (Input.GetKeyDown(toggleKey))
                {
                    ToggleAutoPlay();
                }
            }

            private static void ToggleAutoPlay()
            {
                customAutoPlayEnabled = !customAutoPlayEnabled;
                Debug.Log($"[AutoPlayToggleMod] AutoPlay Status: {customAutoPlayEnabled}");
                PlayToggleFeedback();
            }

            private static void PlayToggleFeedback()
            {
                if (!playSoundFeedback) return;

                try
                {
                    string soundName = customAutoPlayEnabled ? "sndPowerUp" : "sndPowerDown";
                    AudioMixerGroup mixerGroup = RDUtils.GetMixerGroup("ConductorPlaySound");
                    AudioManager.Play(soundName, AudioSettings.dspTime, mixerGroup, 1.5f);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AutoPlayMod] Failed to play sound: {e.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(scrMarginTracker), "AddHit")]
        public class MarginTracker_AddHit
        {
            public static bool Prefix(ref HitMargin hit)
            {
                if (!modEnabled || !customAutoPlayEnabled) return true;

                if (hit == HitMargin.Auto) hit = HitMargin.Perfect;

                return true;
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "SwitchChosen")]
        public class Planet_SwitchChosen_SetAutoFlags
        {
            public static void Prefix(scrPlanet __instance)
            {
                if (!modEnabled || !customAutoPlayEnabled) return;

                scrFloor currentFloor = __instance.currfloor;

                if (currentFloor != null)
                {
                    currentFloor.auto = true;

                    if (currentFloor.nextfloor != null)
                    {
                        currentFloor.nextfloor.auto = true;
                    }
                }
            }

            public static void Postfix(scrPlanet __instance)
            {
                if (!modEnabled || !customAutoPlayEnabled) return;

                scrFloor currentFloor = __instance.currfloor;

                if (currentFloor != null)
                {
                    currentFloor.auto = false;

                    if (currentFloor.nextfloor != null)
                    {
                        currentFloor.nextfloor.auto = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrPlayer), "Update")]
        public class Player_Update
        {
            public static void Postfix(scrPlayer __instance)
            {
                if (!modEnabled || !customAutoPlayEnabled) return;

                try
                {
                    scrController controller = scrController.instance;
                    if (controller == null || controller.state != States.PlayerControl)
                    {
                        return;
                    }

                    scrPlanet chosenPlanet = __instance.planetarySystem?.chosenPlanet;

                    if (chosenPlanet != null && chosenPlanet.AutoShouldHitNow())
                    {
                        __instance.Hit(isAuto: false);
                        Debug.Log("[AutoPlayMod] Hit!");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AutoPlayMod] Failed to hit: {e}");
                }
            }
        }
        [HarmonyPatch(typeof(scrShowIfDebug), "Update")]
        public class ShowIfDebug_Update
        {
            public static void Postfix(scrShowIfDebug __instance)
            {
                if (!modEnabled) return;

                var txtField = typeof(scrShowIfDebug).GetField("txt",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (txtField != null)
                {
                    var txt = txtField.GetValue(__instance) as UnityEngine.UI.Text;
                    if (txt != null && txt.enabled)
                    {
                        if (txt.text.Contains("AutoPlay") || txt.text.Contains("autoplay") || txt.text == RDString.Get("status.autoplay"))
                        {
                            txt.text = customAutoPlayText;
                        }
                    }
                }
            }
        }
    }
}
