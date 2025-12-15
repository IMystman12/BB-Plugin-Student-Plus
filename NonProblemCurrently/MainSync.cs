using System.Collections.Generic;
using System.Net;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class Main
{
    //bugs:audio listener,locker
    static string roomName;
    static int stuckingWithPlayerNum;
    static List<IPEndPoint> playerIds = new List<IPEndPoint>();
    public static List<string> playerIdConverteds = new List<string>();
    static List<bool> playersConnected = new List<bool>();
    public static void AddPlayerIP(IPEndPoint IP)
    {
        if (!playerIds.Contains(IP))
        {
            playerIds.Add(IP);
            playerIdConverteds.Add(IP.ToString());
        }
    }
    [HarmonyPatch(typeof(GameLoader), "LoadLevel"), HarmonyPrefix]
    public static bool LoadLevel(SceneObject sceneObject)
    {
        stuckingWithPlayerNum = 0;
        roomName = sceneObject.name + " " + PlayerFileManager.Instance.lifeMode.ToString() + " " + PlayerFileManager.Instance.inventoryChallenge.ToString() + " " + PlayerFileManager.Instance.mapChallenge.ToString() + " " + PlayerFileManager.Instance.timeLimitChallenge.ToString();
        Debug.LogWarning("Joining room: " + roomName);
        return true;
    }

    [HarmonyPatch(typeof(ElevatorScreen), "Update"), HarmonyPrefix]
    public static bool ElvUpdate(ElevatorScreen __instance)
    {
        if (InternetStation.Connect(playerIds[stuckingWithPlayerNum]))
        {
            stuckingWithPlayerNum++;
            if (stuckingWithPlayerNum >= playerIds.Count)
            {
                __instance.StartGame();
            }
        }
        return false;
    }
}