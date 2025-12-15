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
    public static List<bool> playersConnected = new List<bool>();
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
public static class InternetStation
{
    static Dictionary<string, byte[]> infos = new Dictionary<string, byte[]>();
    static Socket baseSocket;
    static List<Socket> availableSockets = new List<Socket>();
    static Socket newSocket => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static bool AllConnected
    {
        get
        {
            for (int i = 0; i < availableSockets.Count; i++)
            {
                if (!availableSockets[i].Connected)
                {
                    return false;
                }
            }
            return true;
        }
    }
    static byte[] tempDatas;
    static Socket tempSocket;
    static Message tempMessage;
    public static byte[] ToBytes<T>(T nm)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(nm));
    }

    public static T ToMessage<T>(byte[] nm)
    {
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(nm));
    }

    public static void SetUp(IPEndPoint IP)
    {
        baseSocket = newSocket;
        baseSocket.Bind(IP);
    }
    public static bool Connect(IPEndPoint IP)
    {
        try
        {
            tempSocket = newSocket;
            tempSocket.Connect(IP);
            availableSockets.Add(tempSocket);
            return tempSocket.Connected;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Try to connect player {IP.ToString()}! But it's failed. Exception: {e.ToString()}");
            return false;
        }
    }
    public static byte[] Get(string key)
    {
        if (infos.ContainsKey(key))
        {
            return infos[key];
        }
        return default;
    }
    public static void Set(string key, byte[] value)
    {
        foreach (Socket socket in availableSockets)
        {
            if (socket.Connected)
            {
                socket.Send(ToBytes(new Message() { key = key, value = value }));
            }
        }
    }
    public static void Check()
    {
        foreach (Socket socket in availableSockets)
        {
            if (socket.Connected && socket.Available > 0)
            {
                tempDatas = new byte[socket.Available];
                socket.Receive(tempDatas);
                tempMessage = ToMessage<Message>(tempDatas);
                if (infos.ContainsKey(tempMessage.key))
                {
                    infos[tempMessage.key] = tempMessage.value;
                }
                else
                {
                    infos.Add(tempMessage.key, tempMessage.value);
                }
            }
        }
    }
    public static void ClearAll()
    {
        foreach (var item in availableSockets)
        {
            item.Shutdown(SocketShutdown.Both);
            item.Close();
            item.Dispose();
        }
        availableSockets.Clear();
        infos.Clear();
    }
    struct Message
    {
        public string key;
        public byte[] value;
    }
}