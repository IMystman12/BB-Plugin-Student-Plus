using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

[BepInPlugin("imystman12.baldifull.studentplus", "BB+ Plugin Student Plus", "1.0")]
public class PluginCore : BaseUnityPlugin
{
    public static PluginCore instance;
    public void Awake()
    {
        new Harmony("imystman12.baldifull.studentplus").PatchAll();
        instance = this;
        while (true)
        {
            InternetStation.Check();
        }
    }
}

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