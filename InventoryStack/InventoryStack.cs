/// MIT License
///
/// Copyright(c) 2020 Dakota Hawkins
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.

namespace DakotaHawkins
{
    using System.Reflection;

    using Harmony;
    using UnityEngine;

    /// <summary>
    /// Main mod class
    /// </summary>
    [ModTitle("Inventory Stack")]
    [ModDescription(
        "Makes it easier to stay organized!"
        + " Building and crafting will use items from the \"end\" of your inventory"
        + "instead of the \"beginning.\""
    )]
    [ModAuthor("Dakota Hawkins")]
    [ModIconUrl("https://raw.githubusercontent.com/dakotahawkins/Raft-Inventory-Stack/master/ModResources/icon.jpg")] // 128x128px .jpg
    [ModWallpaperUrl("https://raw.githubusercontent.com/dakotahawkins/Raft-Inventory-Stack/master/ModResources/banner.jpg")] // 330x100px .jpg
    [ModVersionCheckUrl("https://raw.githubusercontent.com/dakotahawkins/Raft-Inventory-Stack/master/ModResources/version.txt")]
    [ModVersion("v1.0.0")]
    [RaftVersion("Update 10.07 4497220")] // Recommended version
    [ModIsPermanent(false)]
    public class InventoryStack : Mod
    {
        /// <summary>
        /// Harmony instance ID
        /// </summary>
        private static readonly string harmonyID = "com.github.dakotahawkins.raft-inventory-stack";

        /// <summary>
        /// Prefix logs with cyan mod name
        /// </summary>
        private static readonly string logPrefix =
            "[" + "<color=#00ffff>Inventory Stack</color>" + "] ";

        /// <summary>
        /// Enables or disables additional debug logging
        /// </summary>
        /// <remarks>
        /// Toggle with the console command "InventoryStackDebug"
        /// </remarks>
        private static bool debugEnabled = false;

        /// <summary>
        /// Harmony instance
        /// </summary>
        private HarmonyInstance harmony;

        /// <summary>
        /// Called when mod is loaded
        /// </summary>
        public void Start()
        {
            harmony = HarmonyInstance.Create(harmonyID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Register "InventoryStackDebug" console command
            RConsole.registerCommand(
                typeof(InventoryStack),
                "Toggles additional Inventory Stack debug logging",
                "InventoryStackDebug",
                ToggleDebug
            );

            Log(LogType.Log, "loaded");
        }

        /// <summary>
        /// Called when mod is unloaded
        /// </summary>
        public void OnModUnload()
        {
            // https://github.com/pardeike/Harmony/issues/175
            // harmony.UnpatchAll(harmonyID);
            harmony.Unpatch(
                typeof(PlayerInventory).GetMethod("RemoveCostMultiple"),
                HarmonyPatchType.Postfix,
                harmonyID
            );
            harmony.Unpatch(
                typeof(PlayerInventory).GetMethod("RemoveCostMultiple"),
                HarmonyPatchType.Prefix,
                harmonyID
            );

            Destroy(gameObject); // Please do not remove that line!

            Log(LogType.Log, "unloaded");
        }

        /// <summary>
        /// Toggles additional Inventory Stack debug logging
        /// </summary>
        /// <remarks>
        /// Called by the console command "InventoryStackDebug"
        /// </remarks>
        public void ToggleDebug()
        {
            debugEnabled = !debugEnabled;
            Log(
                LogType.Log,
                string.Format("{0} additional debug logging", debugEnabled ? "Enabled" : "Disabled")
            );
        }

        /// <summary>
        /// Writes a formatted log message to the console
        /// </summary>
        /// <param name="type">Type of log message</param>
        /// <param name="log">Log message</param>
        private static void Log(UnityEngine.LogType type, string log)
        {
            RConsole.Log(type, logPrefix + log);
        }

        /// <summary>
        /// Writes a formatted log message to the console if debugging is enabled
        /// </summary>
        /// <param name="type">Type of log message</param>
        /// <param name="log">Log message</param>
        private static void LogDebug(UnityEngine.LogType type, string log)
        {
            if (!debugEnabled)
            {
                return;
            }

            Log(type, log);
        }

        /// <summary>
        /// Patches the PlayerInventory.RemoveCostMultiple method
        /// </summary>
        /// <remarks>
        /// Adds prefix and postfix functionality to reverse player inventory order before and after
        /// items are removed for building or crafting
        /// </remarks>
        [HarmonyPatch(typeof(PlayerInventory), "RemoveCostMultiple")]
        private static class RemoveCostMultiplePatch
        {
            /// <summary>
            /// PlayerInventory.RemoveCostMultiple prefix remembers the currently selected hotbar slot
            /// index and reverses the player inventory
            /// </summary>
            /// <param name="__instance">Player's inventory</param>
            /// <param name="__state">Returns the player's currently selected hotbar slot index</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Code Quality",
                "IDE0051:Remove unused private members",
                Justification = "Dynamically called at run-time"
            )]
            private static void Prefix(PlayerInventory __instance, out int? __state)
            {
                __state = null;

                if (null == __instance.hotbar)
                {
                    Log(
                        LogType.Error,
                        "PlayerInventory.RemoveCostMultiple.Prefix:\tnull Hotbar"
                    );
                    return;
                }

                // Remember the currently selected hotbar slot index (necessary when crafting, but not
                // when building)
                __state = __instance.hotbar.GetSelectedSlotIndex();

                DebugLogHotbarSelection("Prefix Before Reverse", __instance);

                // Reverse the inventory
                __instance.allSlots.Reverse();

                DebugLogHotbarSelection("Prefix After Reverse", __instance);
            }

            /// <summary>
            /// PlayerInventory.RemoveCostMultiple postfix re-reverses the player inventory to restore
            /// its original order and resets the currently selected hotbar slot index
            /// </summary>
            /// <param name="__instance">Player's inventory</param>
            /// <param name="__state">Player's originally selected hotbar slot index</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Code Quality",
                "IDE0051:Remove unused private members",
                Justification = "Dynamically called at run-time"
            )]
            private static void Postfix(PlayerInventory __instance, int? __state)
            {
                if (null == __state)
                {
                    Log(
                        LogType.Error,
                        "PlayerInventory.RemoveCostMultiple.Postfix:\tnull Hotbar selected index"
                    );
                    return;
                }
                if (null == __instance.hotbar)
                {
                    Log(
                        LogType.Error,
                        "PlayerInventory.RemoveCostMultiple.Postfix:\tnull Hotbar"
                    );
                    return;
                }

                DebugLogHotbarSelection("Postfix Before Reverse", __instance);

                // Reverse the inventory to restore original order
                __instance.allSlots.Reverse();

                DebugLogHotbarSelection("Postfix After Reverse", __instance);

                // Re-set the selected hotbar slot index to its original value (necessary when crafting,
                // but not when building)
                __instance.hotbar.SetSelectedSlotIndex(__state ?? default(int));

                DebugLogHotbarSelection("Postfix After Reset", __instance);
            }

            /// <summary>
            /// Utility function to log the player's hotbar selection
            /// </summary>
            /// <param name="calledWhen">When we're logging this info</param>
            /// <param name="inventory">Player's inventory</param>
            private static void DebugLogHotbarSelection(string calledWhen, PlayerInventory inventory)
            {
                LogDebug(
                    LogType.Log,
                    string.Format(
                        "PlayerInventory.RemoveCostMultiple.{0}:\tSelected Hotbar Selection:\t{1}\t{2}",
                        calledWhen,
                        inventory.hotbar.GetSelectedSlotIndex(),
                        null != inventory.GetSelectedHotbarItem() ?
                            inventory.GetSelectedHotbarItem().UniqueName : "Nothing"
                    )
                );
            }
        }
    }
} // namespace DakotaHawkins
