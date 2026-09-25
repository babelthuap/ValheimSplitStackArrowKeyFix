using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SplitStackArrowKeyFix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.babelthuap.splitstackarrowkeyfix";
        public const string PluginName = "SplitStackArrowKeyFix";
        public const string PluginVersion = "1.1.1";

        private readonly Harmony harmony = new(PluginGUID);

        private void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} loaded");
        }
    }

    // Helper class to efficiently locate and check the status of the Split Dialog
    public static class SplitDialogHelper
    {
        private static FieldInfo splitDialogField;

        public static GameObject GetSplitDialog()
        {
            if (InventoryGui.instance == null) return null;

            if (splitDialogField == null)
            {
                splitDialogField = AccessTools.Field(typeof(InventoryGui), "m_splitDialog");
                if (splitDialogField == null) return null;
            }

            object splitDialogObj = splitDialogField.GetValue(InventoryGui.instance);
            return splitDialogObj as GameObject ?? (splitDialogObj as Component)?.gameObject;
        }

        public static bool IsSplitDialogOpen()
        {
            GameObject dialog = GetSplitDialog();
            return dialog != null && dialog.activeInHierarchy;
        }
    }

    public static class SplitDialogState
    {
        public static float LastClosedTime = -10f;
    }

    [HarmonyPatch(typeof(InventoryGui), "OnSplitOk")]
    public static class InventoryGui_OnSplitOk_Patch
    {
        public static void Prefix() => SplitDialogState.LastClosedTime = Time.unscaledTime;
    }

    [HarmonyPatch(typeof(InventoryGui), "OnSplitCancel")]
    public static class InventoryGui_OnSplitCancel_Patch
    {
        public static void Prefix() => SplitDialogState.LastClosedTime = Time.unscaledTime;
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateSplitDialog")]
    public static class InventoryGui_UpdateSplitDialog_Patch
    {
        private const float RepeatDelay = 0.5f;
        private const float RepeatRate = 0.05f;

        private static float aHoldTime = 0f;
        private static float dHoldTime = 0f;
        private static float nextRepeatTime = 0f;

        public static bool Prefix(InventoryGui __instance)
        {
            GameObject splitDialogGo = SplitDialogHelper.GetSplitDialog();

            if (splitDialogGo != null && splitDialogGo.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    MethodInfo onSplitOkMethod = AccessTools.Method(typeof(InventoryGui), "OnSplitOk");
                    if (onSplitOkMethod != null)
                    {
                        SplitDialogState.LastClosedTime = Time.unscaledTime;
                        onSplitOkMethod.Invoke(__instance, null);
                        return false;
                    }
                }

                bool usingArrows = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
                bool usingAD = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

                if (usingAD)
                {
                    Slider splitSlider = splitDialogGo.GetComponentInChildren<Slider>(true);

                    if (splitSlider != null)
                    {
                        float deltaTime = Time.unscaledDeltaTime;
                        float currentTime = Time.unscaledTime;

                        if (Input.GetKeyDown(KeyCode.A))
                        {
                            splitSlider.value -= 1f;
                            aHoldTime = 0f;
                        }
                        else if (Input.GetKey(KeyCode.A))
                        {
                            aHoldTime += deltaTime;
                            if (aHoldTime >= RepeatDelay && currentTime >= nextRepeatTime)
                            {
                                splitSlider.value -= 1f;
                                nextRepeatTime = currentTime + RepeatRate;
                            }
                        }

                        if (Input.GetKeyDown(KeyCode.D))
                        {
                            splitSlider.value += 1f;
                            dHoldTime = 0f;
                        }
                        else if (Input.GetKey(KeyCode.D))
                        {
                            dHoldTime += deltaTime;
                            if (dHoldTime >= RepeatDelay && currentTime >= nextRepeatTime)
                            {
                                splitSlider.value += 1f;
                                nextRepeatTime = currentTime + RepeatRate;
                            }
                        }
                    }
                }
                else
                {
                    aHoldTime = 0f;
                    dHoldTime = 0f;
                }

                if (usingArrows || usingAD)
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Suppress character movement when the Split Dialog is open
    [HarmonyPatch(typeof(ZInput))]
    public static class ZInput_Patch
    {
        [HarmonyPatch("GetButton")]
        [HarmonyPatch("GetButtonDown")]
        [HarmonyPatch("GetButtonUp")]
        [HarmonyPrefix]
        public static bool Prefix(string name, ref bool __result)
        {
            bool isMovement = name == "Forward" || name == "Backward" || name == "Left" || name == "Right" || name == "Jump";

            if (isMovement && (SplitDialogHelper.IsSplitDialogOpen() || Time.unscaledTime <= SplitDialogState.LastClosedTime + 0.25f))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Character), "Jump")]
    public static class Character_Jump_Patch
    {
        public static bool Prefix(Character __instance)
        {
            if (__instance == Player.m_localPlayer && (SplitDialogHelper.IsSplitDialogOpen() || Time.unscaledTime <= SplitDialogState.LastClosedTime + 0.25f))
            {
                return false;
            }

            return true;
        }
    }
}
