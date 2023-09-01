﻿using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

namespace AwesomeQuarries;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{

    public const int TILETYPE_ROCKY_RED_DESERT      = 18;
    public const int BIOMETYPE_ROUGH_SOIL           = 12;
    public const int BIOMETYPE_BUNYIP_DEN           = 14;
    public const string CONFIG_GENERAL              = "General";
    public const int UNITY_LEFT_CLICK               = 0;
    public const int UNITY_RIGHT_CLICK              = 1;
    public const int UNITY_MIDDLE_CLICK             = 2;

    /// <summary>
    /// Config toggle for if the mod gives verbose logging notifications
    /// </summary>
    private readonly ConfigEntry<bool> _verbose;

    /// <summary>
    /// Item ID to use when calculating cost to convert a tile to rocky soil
    /// </summary>
    private readonly ConfigEntry<int> _costItemID;

    /// <summary>
    /// Number of cost item to consume when converting a single tile. Set to 0 to remove conversion cost.
    /// </summary>
    private readonly ConfigEntry<int> _costItemQuantity;

    /// <summary>
    /// Harmony patcher
    /// </summary>
    private readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);


    /// <summary>
    /// Plugin constructor - run when the plugin is first generated
    /// </summary>
    public Plugin()
    {
        // assign to config values - has to be in the plugin constructor
        // format is config file section, field name, default value, description
        _verbose = Config.Bind(CONFIG_GENERAL, "Verbose", false, "Enable to see more information about mod behavior in the form of chat notifications and logging events.");
        _costItemID = Config.Bind(CONFIG_GENERAL, "CostItemID", 26, "Item ID to use when calculating cost to convert a tile to rocky soil.");
        _costItemQuantity = Config.Bind(CONFIG_GENERAL, "CostItemQuantity", 3, "Number of items to consume as a cost to convert a tile to rocky soil.");
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} constructor triggered.");
    }


    /// <summary>
    /// Triggered when the plugin is activated (not constructed)
    /// </summary>
    private void Awake()
    {
        ApplyHarmonyPatches();
    }


    /// <summary>
    /// Patches native game routines with all harmony patches declared.
    /// </summary>
    private void ApplyHarmonyPatches()
    {
        _harmony.PatchAll();
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} finished merging harmony patches!");
    }


    /// <summary>
    /// Forces any tile that is of the rocky soil type to be recognized as a quarry instead of the normally calculated biome
    /// checkbiomType(sic)
    /// </summary>
    [HarmonyPatch(typeof(GenerateMap), nameof(GenerateMap.checkBiomType))]
    class PatchBiomeCheck
    {
        static void Postfix(int x, int y, ref int __result)
        {

            // Don't change bunyip dens to rough soil or else it stops bush devil spawns
            if (__result == BIOMETYPE_BUNYIP_DEN){
                return;
            }

            int tileType = WorldManager.manageWorld.tileTypeMap[x, y];
            if (tileType == TILETYPE_ROCKY_RED_DESERT){
                __result = BIOMETYPE_ROUGH_SOIL;
            }

        }
    }


    /// <summary>
    /// Prevents items respawning under quarries
    /// placeABurriedItem(sic)
    /// </summary>
    [HarmonyPatch(typeof(GenerateMap), nameof(GenerateMap.placeABurriedItem))]
    class PatchBuriedItemsInQuarries
    {
        static void Postfix(int x, int y, float percentChance, InventoryItemLootTable useTable = null)
        {
            // x,y is actually x,z but for harmony purposes we have to override with identically named arguments
            int tileType = WorldManager.manageWorld.tileTypeMap[x, y];
            if (tileType == TILETYPE_ROCKY_RED_DESERT){
                WorldManager.manageWorld.onTileMap[x, y] = -1;
            }
        }
    }


    /// <summary>
    /// Simple in game logging interface that respects verbose flag
    /// </summary>
    /// <param name="output"></param>
    private void Log(string output){
        if (_verbose.Value){
            NotificationManager.manage.createChatNotification(output);
        }
    }


    /// <summary>
    /// Update method called regularly during normal gameplay
    /// </summary>
    private void Update()
    {

        if (Input.GetMouseButtonDown(UNITY_RIGHT_CLICK)){

            // Guard clause - Ensure player isn't in a menu
            if (Inventory.inv.isMenuOpen()){
                return;
            }

            int invIndex = Inventory.inv.selectedSlot;
            int heldItem = Inventory.inv.invSlots[invIndex].itemNo;
            int heldQty = Inventory.inv.invSlots[invIndex].stack;
            int checkItem = _costItemID.Value;
            int checkQty = _costItemQuantity.Value;

            // Get the target tile location
            int x = Mathf.RoundToInt(NetworkMapSharer.share.localChar.myInteract.tileHighlighter.transform.position.x / 2f);
            int y = Mathf.RoundToInt(NetworkMapSharer.share.localChar.myInteract.tileHighlighter.transform.position.y / 2f);
            int z = Mathf.RoundToInt(NetworkMapSharer.share.localChar.myInteract.tileHighlighter.transform.position.z / 2f);
            
            // check the different types involved
            WorldManager WM = WorldManager.manageWorld;
            int groundType = WM.tileTypeMap[x, z];
            int biomeType = GenerateMap.generate.checkBiomType(x, z);
            
            // Guard clause - Don't trigger with empty hands
            if (heldItem <= 0){
                Log("FAIL: No item held in hands");
                return;
            }

            // Guard clause - Make sure we are holding the right item
            if (heldItem != checkItem){
                string heldItemName = Inventory.inv.allItems[heldItem].getInvItemName();
                string checkItemName = Inventory.inv.allItems[checkItem].getInvItemName();
                Log("FAIL: Wrong item held expected " + checkItemName + "(ID " + checkItem +") found " + heldItemName + "(ID " + heldItem + ")");
                return;
            }

            // Guard clause - Make sure target tile doesn't have objects on the tile
            if (WM.isOnTileEmpty(x,z) is false){
                Log("FAIL: Target tile is not empty");
                return;
            }

            // Guard clause - Make sure the held stack has enough of the item
            if (heldQty < checkQty){
                Log("FAIL: Not enough items held expected " + checkQty.ToString() + " found " + heldQty.ToString());
                return;
            }

            // Guard clause - Make sure the ground isn't already what we're trying to change it into
            if (groundType == TILETYPE_ROCKY_RED_DESERT){
                Log("FAIL: This tile is already a natural quarry tile");
                return;
            }

            Log("Target location: " + x.ToString() + ", " + y.ToString() + ", " + z.ToString());
            Log("Tile Type before change: " + groundType.ToString());
            Log("Biome Type before change: " + biomeType.ToString());

            // Actually do the update of the tile type
            NetworkMapSharer.share.RpcUpdateTileType(TILETYPE_ROCKY_RED_DESERT, x, z);

            // subtract the cost item
            Inventory.inv.invSlots[invIndex].stack -= checkQty;
            Inventory.inv.invSlots[invIndex].refreshSlot();
            
            // Update player animation if stack is empty so they don't keep holding the consumed item
            if (Inventory.inv.invSlots[invIndex].stack <= 0){
                Inventory.inv.consumeItemInHand();
            }

        }

    }

}
