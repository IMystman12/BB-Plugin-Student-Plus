using System;
using System.IO;
using HarmonyLib;
using Rewired;
using Steamworks;
using UnityEngine;

[HarmonyPatch]
public static class InputSync
{
    public static string playerFileName => $"Player_{Main.playerIdConverteds[playerNum]}";
    public static int playerNum;
    [HarmonyPatch(typeof(PlayerMovement), "Update"), HarmonyPrefix]
    public static bool MovementUpdate(PlayerMovement __instance)
    {
        playerNum = __instance.pm.playerNumber;
        return true;
    }

    [HarmonyPatch(typeof(ItemManager), "Update"), HarmonyPrefix]
    public static bool ItemMgrUpdate(ItemManager __instance)
    {
        playerNum = __instance.pm.playerNumber;
        return true;
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate"), HarmonyPrefix]
    public static bool CamLateUpdate(GameCamera __instance)
    {
        playerNum = __instance.camNum;
        return true;
    }

    [HarmonyPatch(typeof(PlayerClick), "Update"), HarmonyPrefix]
    public static bool ClickerUpdate(PlayerClick __instance)
    {
        playerNum = __instance.pm.playerNumber;
        return true;
    }

    [HarmonyPatch(typeof(Jumprope), "Update"), HarmonyPrefix]
    public static bool JumpropeUpdate(Jumprope __instance)
    {
        playerNum = __instance.player.playerNumber;
        return true;
    }

    [HarmonyPatch(typeof(HideableLocker), "Clicked"), HarmonyPrefix]
    public static bool HideableLockerCameraReset(HideableLocker __instance)
    {
        Debug.LogError("Not available!");
        return false;
    }

    [HarmonyPatch(typeof(CoreGameManager), "Update"), HarmonyPrefix]
    public static bool CoreGameUpdate(CoreGameManager __instance)
    {
        __instance.disablePause = true;
        playerNum = 0;
        return true;
    }

    [HarmonyPatch(typeof(Map), "Update"), HarmonyPrefix]
    public static bool MapUpdate()
    {
        playerNum = 0;
        return true;
    }

    [HarmonyPatch(typeof(CursorController), "Update"), HarmonyPrefix]
    public static bool CursorUpdate()
    {
        playerNum = 0;
        return true;
    }

    [HarmonyPatch(typeof(InputManager), "GetDigitalInput"), HarmonyPostfix]
    public static void GetDigitalInput(string id, bool onDown, ref bool __result)
    {
        if (playerNum != 0)
        {
            __result = InternetStation.Get(Path.Combine(playerFileName, id, onDown.ToString()))[0] != 0;
        }
        else
        {
            InternetStation.Set(Path.Combine(playerFileName, id, onDown.ToString()), new byte[1] { (byte)(__result ? 1 : 0) });
        }
    }

    [HarmonyPatch(typeof(Player), "GetAxis", typeof(string)), HarmonyPostfix]
    public static void RewiredGetAxis(string actionName, ref float __result)
    {
        if (playerNum != 0)
        {
            __result = BitConverter.ToSingle(InternetStation.Get(Path.Combine(playerFileName, actionName)), 0);
        }
        else
        {
            InternetStation.Set(Path.Combine(playerFileName, actionName), BitConverter.GetBytes(__result));
        }
    }

    [HarmonyPatch(typeof(SteamInput), "GetAnalogActionData"), HarmonyPostfix]
    public static void SteamGetAnalogActionData(InputHandle_t inputHandle, InputAnalogActionHandle_t analogActionHandle, ref InputAnalogActionData_t __result)
    {
        Debug.LogError("Steam not ava!");
    }
}