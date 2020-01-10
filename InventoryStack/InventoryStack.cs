using Harmony;
using System.Reflection;

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
    /// Enables additional debug logging
    /// </summary>
    public static bool Debug { get { return false; } }

    /// <summary>
    /// Harmony instance
    /// </summary>
    private HarmonyInstance harmony;

    /// <summary>
    /// Harmony instance ID
    /// </summary>
    private static readonly string harmonyID = "com.github.dakotahawkins.raft-inventory-stack";

    /// <summary>
    /// Called when mod is loaded
    /// </summary>
    public void Start()
    {
        harmony = HarmonyInstance.Create(harmonyID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        RConsole.Log("InventoryStack has been loaded!");
    }

    /// <summary>
    /// Called every frame (unused)
    /// </summary>
    public void Update() { }

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
        RConsole.Log("InventoryStack has been unloaded!");
    }
}

/// <summary>
/// Patches the PlayerInventory.RemoveCostMultiple method
/// </summary>
/// <remarks>
/// Adds prefix and postfix functionality to reverse player inventory order before and after items
/// are removed for building or crafting
/// </remarks>
[HarmonyPatch(typeof(PlayerInventory), "RemoveCostMultiple")]
internal static class RemoveCostMultiplePatch
{
    /// <summary>
    /// PlayerInventory.RemoveCostMultiple prefix remembers the currently selected hotbar slot index
    /// and reverses the player inventory
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
            RConsole.LogError("PlayerInventory.RemoveCostMultiple.Prefix:\tnull Hotbar");
            return;
        }

        // Remember the currently selected hotbar slot index (necessary when crafting, but not when
        // building)
        __state = __instance.hotbar.GetSelectedSlotIndex();

        DebugLogHotbarSelection("Prefix Before Reverse", __instance);

        // Reverse the inventory
        __instance.allSlots.Reverse();

        DebugLogHotbarSelection("Prefix After Reverse", __instance);
    }

    /// <summary>
    /// PlayerInventory.RemoveCostMultiple postfix re-reverses the player inventory to restore its
    /// original order and resets the currently selected hotbar slot index
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
            RConsole.LogError(
                "PlayerInventory.RemoveCostMultiple.Postfix:\tnull Hotbar selected index"
            );
            return;
        }
        if (null == __instance.hotbar)
        {
            RConsole.LogError("PlayerInventory.RemoveCostMultiple.Postfix:\tnull Hotbar");
            return;
        }

        DebugLogHotbarSelection("Postfix Before Reverse", __instance);

        // Reverse the inventory to restore original order
        __instance.allSlots.Reverse();

        DebugLogHotbarSelection("Postfix After Reverse", __instance);

        // Re-set the selected hotbar slot index to its original value (necessary when crafting, but
        // not when building)
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
        if (!InventoryStack.Debug)
        {
            return;
        }

        RConsole.Log(string.Format(
            "PlayerInventory.RemoveCostMultiple.{0}:\tSelected Hotbar Selection:\t{1}\t{2}",
            calledWhen,
            inventory.hotbar.GetSelectedSlotIndex(),
            null != inventory.GetSelectedHotbarItem() ?
                inventory.GetSelectedHotbarItem().UniqueName : "Nothing"
        ));
    }
}
