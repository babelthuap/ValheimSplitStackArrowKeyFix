using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SplitStackArrowKeyFix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.babelthuap.splitstackarrowkeyfix";
        public const string PluginName = "SplitStackArrowKeyFix";
        public const string PluginVersion = "1.0.0";

        private readonly Harmony harmony = new(PluginGUID);

        private void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo($"{PluginName} loaded");
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateSplitDialog")]
    public static class InventoryGui_UpdateSplitDialog_Patch
    {
        public static bool Prefix()
        {
            // Skip Valheim's custom UpdateSplitDialog logic since Unity's native Slider
            // handles it properly.
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
            {
                return false;
            }
            return true;
        }
    }
}
