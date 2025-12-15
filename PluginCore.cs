using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Rewired;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
    #region "Find Players"
    static string roomName;
    public static List<int> playerIds = new List<int>();
    [HarmonyPatch(typeof(GameLoader), "LoadLevel"), HarmonyPrefix]
    public static bool LoadLevel(SceneObject sceneObject)
    {
        playerIds.Clear();
        playerIds.Add(0);
        roomName = sceneObject.name + " " + PlayerFileManager.Instance.lifeMode.ToString() + " " + PlayerFileManager.Instance.inventoryChallenge.ToString() + " " + PlayerFileManager.Instance.mapChallenge.ToString() + " " + PlayerFileManager.Instance.timeLimitChallenge.ToString();
        Debug.LogWarning("Joining room: " + roomName);
        return true;
    }

    [HarmonyPatch(typeof(ElevatorScreen), "Update"), HarmonyPrefix]
    public static bool ElvUpdate(ElevatorScreen __instance)
    {
        if (started)
        {
            return true;
        }
        bool flag = playerIds.Count < 4;
        if (!flag)
        {
            CoreGameManager.Instance.setPlayers = playerIds.Count;
            started = true;
        }
        return flag;
    }
    #endregion

    #region "Setup Players"

    [HarmonyPatch(typeof(BaseGameManager), "Update"), HarmonyPostfix]
    public static void BaseGameUpdate(BaseGameManager __instance)
    {
        Singleton<CoreGameManager>.Instance.sceneObject.skippable = false;
    }

    [HarmonyPatch(typeof(CoreGameManager), "PlayBegins"), HarmonyPrefix]
    public static bool PlayBegins()
    {
        rt = 0;
        started = true;
        return true;
    }

    [HarmonyPatch(typeof(HudManager), "Awake"), HarmonyPrefix]
    public static bool HudAwake(HudManager __instance)
    {
        __instance.gameObject.SetActive(__instance.hudNum == 0);
        return __instance.hudNum == 0;
    }

    [HarmonyPatch(typeof(EnvironmentController), "AssignPlayers"), HarmonyPrefix]
    public static bool AssignPlayers()
    {
        return CoreGameManager.Instance.GetPlayer(CoreGameManager.Instance.setPlayers - 1) != null;
    }

    [HarmonyPatch(typeof(BaseGameManager), "ApplyMap"), HarmonyPrefix]
    public static bool ApplyMap()
    {
        return CoreGameManager.Instance.GetPlayer(CoreGameManager.Instance.setPlayers - 1) != null;
    }

    public static DijkstraMap curGCMap;

    [HarmonyPatch(typeof(GameCamera), "Awake"), HarmonyPrefix]
    public static bool CamAwake(GameCamera __instance)
    {
        if (__instance.camNum != 0)
        {
            GameCamera.dijkstraMap = curGCMap;
            __instance.listenerTra.GetComponent<AudioListener>();
            __instance.listenerTra.GetComponent<VA_AudioListener>();
            __instance.canvasCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            __instance.camCom.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            __instance.billboardCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            __instance.overlayCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            __instance.mapCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            __instance.StopRendering(true);
            __instance.enabled = false;
        }
        else
        {
            curGCMap = GameCamera.dijkstraMap;
        }
        return __instance.camNum == 0;
    }
    #endregion

    #region "Player leave by force"

    [HarmonyPatch(typeof(CoreGameManager), "Quit"), HarmonyPrefix]
    public static bool Quit()
    {
        rt = 0;
        started = false;
        return true;
    }

    [HarmonyPatch(typeof(Baldi), "CaughtPlayer"), HarmonyPrefix]
    public static bool CaughtPlayer(PlayerManager player)
    {
        player.SetHidden(true);
        return player.playerNumber == 0;
    }

    public static bool isExit(Collider other, ColliderGroup instance)
    {
        //is player
        //  if (other.tag != "Player" || BaseGameManager.Instance.GetValue<bool>("allNotebooksFound"))
        {
            //  return false;
        }

        //is exit inside collider
        PlayerManager player = other.GetComponent<PlayerManager>();
        bool flag = false;
        foreach (var item in player.ec.elevators)
        {
            if (instance == item.InsideCollider)
            {
                flag = true;
            }
        }
        if (!flag)
        {
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ColliderGroup), "OnTriggerEnter"), HarmonyPrefix]
    public static bool ExitEnter(Collider other, ColliderGroup __instance)
    {
        if (isExit(other, __instance))
        {
            // is player 0
            return other.GetComponent<PlayerManager>().playerNumber == 0;
        }
        return true;
    }

    [HarmonyPatch(typeof(ColliderGroup), "OnTriggerExit"), HarmonyPrefix]
    public static bool ExitExit(Collider other, ColliderGroup __instance)
    {
        if (isExit(other, __instance))
        {
            // is player 0
            return other.GetComponent<PlayerManager>().playerNumber == 0;
        }

        return true;
    }

    #endregion

    #region "Sync: Input"

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
            __result = InternetStation.Get(Path.Combine($"Player_{playerIds[playerNum]}", id, onDown.ToString()))[0] != 0;
        }
        else
        {
            InternetStation.Set(Path.Combine($"Player_{playerIds[playerNum]}", id, onDown.ToString()), new byte[1] { (byte)(__result ? 1 : 0) });
        }
    }

    [HarmonyPatch(typeof(Player), "GetAxis", typeof(string)), HarmonyPostfix]
    public static void RewiredGetAxis(string actionName, ref float __result)
    {
        if (playerNum != 0)
        {
            __result = BitConverter.ToSingle(InternetStation.Get(Path.Combine($"Player_{playerIds[playerNum]}", actionName)), 0);
        }
        else
        {
            InternetStation.Set(Path.Combine($"Player_{playerIds[playerNum]}", actionName), BitConverter.GetBytes(__result));
        }
    }

    [HarmonyPatch(typeof(SteamInput), "GetAnalogActionData"), HarmonyPostfix]
    public static void SteamGetAnalogActionData(InputHandle_t inputHandle, InputAnalogActionHandle_t analogActionHandle, ref InputAnalogActionData_t __result)
    {
        Debug.LogError("Steam not ava!");
    }
    #endregion

    #region "Sync: Unity part"
    [HarmonyPatch(typeof(Time), "deltaTime", MethodType.Getter), HarmonyPostfix]
    public static void deltaTime(ref float __result)
    {
        __result = 0.02f;
    }

    public static float rt;
    [HarmonyPatch(typeof(UnityEngine.Random), "Range", typeof(float), typeof(float)), HarmonyPostfix]
    public static void RangeFloat(float minInclusive, float maxInclusive, ref float __result)
    {
        rt += 99;
        __result = Mathf.Lerp(minInclusive, maxInclusive, Mathf.Abs(Mathf.Cos(Mathf.Sin(rt))));
    }
    [HarmonyPatch(typeof(UnityEngine.Random), "Range", typeof(int), typeof(int)), HarmonyPostfix]
    public static void RangeInt(int minInclusive, int maxExclusive, ref int __result)
    {
        rt += 99;
        __result = (int)Mathf.Lerp(minInclusive, maxExclusive, Mathf.Abs(Mathf.Cos(Mathf.Sin(rt))));
    }
    #endregion

    #region "Sync Core"
    public static int playerNum;
    #endregion

    #region "More(>4) player(Experimental)"
    //[HarmonyPatch(typeof(CoreGameManager), "SpawnPlayers"), HarmonyPrefix]
    //public static bool Prefix1(CoreGameManager __instance)
    //{
    //    __instance.SetValue("players", new PlayerManager[__instance.setPlayers]);
    //    __instance.SetValue("huds", new HudManager[__instance.setPlayers]);
    //    __instance.SetValue("cameras", new GameCamera[__instance.setPlayers]);
    //    return false;
    //}

    //[HarmonyPatch(typeof(MathMachine), "ReInit"), HarmonyPrefix]
    //public static bool Prefix1(MathMachine __instance)
    //{
    //    __instance.SetValue("playerIsHolding", new bool[CoreGameManager.Instance.setPlayers]);
    //    __instance.SetValue("playerHolding", new int[CoreGameManager.Instance.setPlayers]);
    //    return false;
    //}
    #endregion
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

    public static void SetUp(string host, int port)
    {
        baseSocket = newSocket;
        baseSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
    }
    public static bool Connect(string host, int port)
    {
        try
        {
            tempSocket = newSocket;
            tempSocket.Connect(new IPEndPoint(IPAddress.Parse(host), port));
            availableSockets.Add(tempSocket);
            return tempSocket.Connected;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Try to connect player {host}:{port}! But it's failed. Exception: {e.ToString()}");
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
                socket.Send(ToBytes(new Message() { key = key, value = value });
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