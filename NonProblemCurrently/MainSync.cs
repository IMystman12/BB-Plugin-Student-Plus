using System.Collections;
using System.Collections.Generic;
using System.Net;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class Main
{
    //bugs:audio listener,locker
    public static bool offline;
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
            //playersConnected.Add(false);
            //     PluginCore.instance.StartCoroutine(KeepConnect(IP, playersConnected.Count - 1));
        }
    }

    static IEnumerator KeepConnect(IPEndPoint IP, int index)
    {
        while (!InternetStation.Connect(IP))
        {
            yield return null;
        }
        playersConnected[index] = true;
        yield break;
    }

    [HarmonyPatch(typeof(GameLoader), "LoadLevel"), HarmonyPrefix]
    public static bool LoadLevel(SceneObject sceneObject)
    {
        stuckingWithPlayerNum = 1;
        roomName = sceneObject.name + " " + PlayerFileManager.Instance.lifeMode.ToString() + " " + PlayerFileManager.Instance.inventoryChallenge.ToString() + " " + PlayerFileManager.Instance.mapChallenge.ToString() + " " + PlayerFileManager.Instance.timeLimitChallenge.ToString();
        Debug.LogWarning("Joining room: " + roomName);
        return true;
    }

    [HarmonyPatch(typeof(ElevatorScreen), "Update"), HarmonyPrefix]
    public static bool ElvUpdate(ElevatorScreen __instance)
    {
        InternetStation.ClearAll();
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