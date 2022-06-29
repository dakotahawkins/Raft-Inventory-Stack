//--------------------------------------------------------------------------------------------------
// <copyright file="InventoryStack.cs" company="Dakota Hawkins">
//     Copyright (c) Dakota Hawkins. All rights reserved.
// </copyright>
// <summary>Implementation class for the Inventory Stack mod.</summary>
// <license>
//     MIT License
//
//     Copyright(c) 2022 Dakota Hawkins
//
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.
// </license>
//--------------------------------------------------------------------------------------------------
#pragma warning disable SA1009 // ClosingParenthesisMustBeSpacedCorrectly
#pragma warning disable SA1111 // ClosingParenthesisMustBeOnLineOfLastParameter
namespace DakotaHawkins
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// Main mod class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1711:Identifiers should not have incorrect suffix",
        Justification = "Don't restrict the mod's name"
    )]
    public class InventoryStack : Mod
    {
        /// <summary>
        /// Harmony instance ID.
        /// </summary>
        private const string MyHarmonyID = "com.github.dakotahawkins.raft-inventory-stack";

        /// <summary>
        /// Prefix logs with cyan mod name.
        /// </summary>
        private const string MyLogPrefix =
            "[" + "<color=#00ffff>Inventory Stack</color>" + "] ";

        /// <summary>
        /// Enables or disables additional debug logging.
        /// </summary>
        /// <remarks>
        /// Toggle with the console command "InventoryStack ( d | debug )".
        /// </remarks>
        private static bool debugEnabled = false;

        /// <summary>
        /// Harmony instance.
        /// </summary>
        private Harmony harmony;

        /// <summary>
        /// Runs an inventory stack command or prints help.
        /// </summary>
        /// <remarks>
        /// Called by the console command "InventoryStack".
        /// </remarks>
        /// <param name="args">Individual command.</param>
        [ConsoleCommand(
            name: "InventoryStack",
            docs: "Run \"InventoryStack ( h | help )\" for more information."
        )]
        public static void RunCommand(string[] args)
        {
            string command = args.FirstOrDefault();
            if (command.IsNullOrEmpty() || new[] { "h", "help" }.Contains(command))
            {
                PrintCommandHelp();
                return;
            }

            if (new[] { "d", "debug" }.Contains(command))
            {
                ToggleDebug();
                return;
            }

            PrintCommandHelp("Unrecognized command.");
        }

        /// <summary>
        /// Called when mod is loaded.
        /// </summary>
        public void Start()
        {
            this.harmony = new Harmony(MyHarmonyID);
            this.harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log(LogType.Log, "loaded");
        }

        /// <summary>
        /// Called when mod is unloaded.
        /// </summary>
        public void OnModUnload()
        {
            this.harmony.UnpatchAll(MyHarmonyID);
            InventoryStack.Destroy(this.gameObject); // Please do not remove that line!
            Log(LogType.Log, "unloaded");
        }

        /// <summary>
        /// Writes a formatted log message to the console.
        /// </summary>
        /// <param name="type">Type of log message.</param>
        /// <param name="log">Log message.</param>
        private static void Log(UnityEngine.LogType type, string log)
        {
            log = MyLogPrefix + log;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    Debug.LogError(log);
                    break;
                case LogType.Assert:
                    Debug.LogAssertion(log);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(log);
                    break;
                case LogType.Log:
                default:
                    Debug.Log(log);
                    break;
            }
        }

        /// <summary>
        /// Writes a formatted log message to the console if debugging is enabled.
        /// </summary>
        /// <param name="type">Type of log message.</param>
        /// <param name="log">Log message.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "CodeQuality",
            "IDE0051:Remove unused private members",
            Justification = "Future use."
        )]
        private static void LogDebug(UnityEngine.LogType type, string log)
        {
            if (!debugEnabled)
            {
                return;
            }

            Log(type, log);
        }

        /// <summary>
        /// Prints the command help to the console.
        /// </summary>
        /// <param name="error">Optional error message.</param>
        /// <remarks>
        /// Called by the console command "InventoryStack ( h | help )".
        /// </remarks>
        private static void PrintCommandHelp(string error = "")
        {
            List<string> helpMessage = new List<string>
            {
                string.Empty,
                "Usage: <b>InventoryStack <i>[command]</i></b>",
                string.Empty,
                "Executes the specified InventoryStack command.",
                string.Empty,
                "Commands:",
                "    <b>h</b>, <b>help</b>:   Displays this message",
                "    <b>d</b>, <b>debug</b>:  Toggles additional debug logging",
                string.Empty,
            };

            if (!error.IsNullOrEmpty())
            {
                helpMessage.InsertRange(
                    0,
                    new List<string>
                    {
                        string.Empty,
                        "Error: " + error,
                    }
                );
            }

            foreach (string helpLine in helpMessage)
            {
                Log(LogType.Log, helpLine);
            }
        }

        /// <summary>
        /// Toggles additional Inventory Stack debug logging.
        /// </summary>
        /// <remarks>
        /// Called by the console command "InventoryStack ( d | debug )".
        /// </remarks>
        [ConsoleCommand(name: "debug", docs: "")]
        private static void ToggleDebug()
        {
            debugEnabled = !debugEnabled;
            Log(
                LogType.Log,
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} additional debug logging",
                    debugEnabled ? "Enabled" : "Disabled"
                )
            );
        }

        /// <summary>
        /// Patches the Inventory RemoveCostMultiple and
        /// RemoveCostMultipleIncludeSecondaryInventories methods.
        /// </summary>
        /// <remarks>
        /// Adds prefix and postfix functionality to reverse inventory order before and after items
        /// are removed for building or crafting.
        /// </remarks>
        [HarmonyPatch(typeof(Inventory), "RemoveCostMultiple")]
        [HarmonyPatch(typeof(Inventory), "RemoveCostMultipleIncludeSecondaryInventories")]
        private static class RemoveCostMultiplePatch
        {
            /// <summary>
            /// Reverses the inventory.
            /// </summary>
            /// <param name="inventory">Inventory.</param>
            private static void ReverseInventory(Inventory inventory)
            {
                inventory.allSlots.Reverse();
                if (inventory.secondInventory != null)
                {
                    inventory.secondInventory.allSlots.Reverse();
                }
            }

            /// <summary>
            /// Prefix reverses the inventory.
            /// </summary>
            /// <param name="__instance">Inventory.</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "CodeQuality",
                "IDE0051:Remove unused private members",
                Justification = "Called by game."
            )]
            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "StyleCop.CSharp.NamingRules",
                "SA1313:Parameter names should begin with lower-case letter",
                Justification = "Name mandated by Harmony.")
            ]
            private static void Prefix(Inventory __instance)
            {
                // Reverse the inventory
                RemoveCostMultiplePatch.ReverseInventory(__instance);
            }

            /// <summary>
            /// Postfix re-reverses the inventory to restore its original order.
            /// </summary>
            /// <param name="__instance">Inventory.</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "CodeQuality",
                "IDE0051:Remove unused private members",
                Justification = "Called by game."
            )]
            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "StyleCop.CSharp.NamingRules",
                "SA1313:Parameter names should begin with lower-case letter",
                Justification = "Name mandated by Harmony.")
            ]
            private static void Postfix(Inventory __instance)
            {
                // Reverse the inventory to restore original order
                RemoveCostMultiplePatch.ReverseInventory(__instance);
            }
        }
    }
} // namespace DakotaHawkins
