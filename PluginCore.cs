using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Rewired;
using Steamworks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.Rendering.Universal;
using UnityInterface;
using System.Threading.Tasks;

[BepInPlugin("imystman12.baldifull.studentplus", "BB+ Plugin Student Plus", "1.0")]
public class PluginCore : BaseUnityPlugin, INode
{
    public P2PNode thisNode = new P2PNode();
    public IPAddress address => IPAddress.Parse("127.0.0.99");

    public static PluginCore instance;
    public void Awake()
    {
        new Harmony("imystman12.baldifull.studentplus").PatchAll();

        for (int i = 0; i < 4; i++)
        {
            Main.inputed[i] = new List<int>();
            Main.inputedBut[i] = new List<Input_But>();
            Main.inputedAxis[i] = new List<Input_Axis>();
        }

        instance = this;
        thisNode.showLog = true;
        thisNode.Initialize(address, this);
    }



    public void SendMessages(int toPlayerId, Message msg)
    {
        thisNode.Send(toPlayerId, ToBytes(msg));
    }

    public static byte[] ToBytes<T>(T nm)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(nm));
    }

    public static T ToMessage<T>(byte[] nm)
    {
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(nm));
    }

    public bool ShouldListening(int id, Socket node)
    {
        return true;
    }

    public void OnReceive(int id, Socket node, byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            Debug.LogWarning("You haven't receive any messages yet!");
            return;
        }
        Message msg = ToMessage<Message>(bytes);
        switch (msg.type)
        {
            case MessageType.Tst:
                Debug.LogWarning($"Received message from player{msg.playerId}!");
                break;
            case MessageType.EnterMatch:
                Main.AddPlayer(msg);
                break;
            case MessageType.Input_But:
                Main.AddInput(msg);
                break;
            case MessageType.Input_Axis:
                Main.AddInput(msg);
                break;
        }
    }
}

[HarmonyPatch]
public static class Main
{
    //bugs:audio listener,locker
    #region "Find Players"

    static bool started;
    static string roomName;
    public static List<int> playerIds = new List<int>();
    public static Stopwatch watch;
    [HarmonyPatch(typeof(GameLoader), "LoadLevel"), HarmonyPrefix]
    public static bool LoadLevel(SceneObject sceneObject)
    {
        watch = new Stopwatch();
        watch.Start();
        playerIds.Clear();
        playerIds.Add(PluginCore.instance.thisNode.id);
        roomName = sceneObject.name + " " + PlayerFileManager.Instance.lifeMode.ToString() + " " + PlayerFileManager.Instance.inventoryChallenge.ToString() + " " + PlayerFileManager.Instance.mapChallenge.ToString() + " " + PlayerFileManager.Instance.timeLimitChallenge.ToString();
        Debug.LogWarning("Joining room: " + roomName);
        Message msg = new Message()
        {
            playerId = playerIds[0],
            type = MessageType.EnterMatch,
            context = roomName
        };
        PluginCore.instance.thisNode.SendGlobal(PluginCore.ToBytes(msg));
        return true;
    }

    public static void AddPlayer(Message msg)
    {
        if (!started && msg.context == roomName && playerIds.Count < 4)
        {
            watch.Restart();
            playerIds.Add(msg.playerId);
        }
    }

    [HarmonyPatch(typeof(ElevatorScreen), "Update"), HarmonyPrefix]
    public static bool ElvUpdate(ElevatorScreen __instance)
    {
        if (started)
        {
            return true;
        }
        bool flag = playerIds.Count < 4 && watch.ElapsedMilliseconds < 50000;
        if (!flag)
        {
            watch.Stop();
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
        if (other.tag != "Player" || BaseGameManager.Instance.GetValue<bool>("allNotebooksFound"))
        {
            return false;
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
            if (inputed[playerNum].Count > 0 && inputed[playerNum][0] == 0 && inputedBut[playerNum].Count > 0)
            {
                Input_But imb = inputedBut[playerNum][0];
                if (imb.id == id && imb.onDown == onDown)
                {
                    __result = imb.result;
                }
                inputedBut[playerNum].RemoveAt(0);
            }
            else
            {
                __result = false;
            }
        }
        else
        {
            SendThing(new Input_But() { id = id, onDown = onDown, result = __result });
        }
    }

    [HarmonyPatch(typeof(Player), "GetAxis", typeof(string)), HarmonyPostfix]
    public static void RewiredGetAxis(string actionName, ref float __result)
    {
        if (playerNum != 0)
        {
            if (inputed[playerNum].Count > 0 && inputed[playerNum][0] == 0 && inputedAxis[playerNum].Count > 0)
            {
                Input_Axis imb = inputedAxis[playerNum][0];
                if (imb.id == actionName)
                {
                    __result = imb.result;
                }
                inputedAxis[playerNum].RemoveAt(0);
            }
            else
            {
                __result = 0;
            }
        }
        else
        {
            SendThing(new Input_Axis() { id = actionName, result = __result });
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

    public static void AddInput(Message msg)
    {
        if (playerIds.Contains(msg.playerId))
        {
            for (int i = 1; i < playerIds.Count; i++)
            {
                if (msg.playerId == playerIds[i])
                {
                    switch (msg.type)
                    {
                        case MessageType.Input_But:
                            inputedBut[i].Add(JsonConvert.DeserializeObject<Input_But>(msg.context));
                            break;
                        case MessageType.Input_Axis:
                            inputedAxis[i].Add(JsonConvert.DeserializeObject<Input_Axis>(msg.context));
                            break;
                    }
                }
            }
        }
    }

    public static void SendThing<T>(T thing)
    {
        Message msg = new Message()
        {
            playerId = PluginCore.instance.thisNode.id,
            context = JsonConvert.SerializeObject(thing)
        };
        if (typeof(T) == typeof(Input_But))
        {
            msg.type = MessageType.Input_But;
        }
        else if (typeof(T) == typeof(Input_Axis))
        {
            msg.type = MessageType.Input_Axis;
        }
        for (int i = 1; i < playerIds.Count; i++)
        {
            PluginCore.instance.SendMessages(playerIds[i], msg);
        }
    }

    //0 But,1 Axis
    public static List<int>[] inputed = new List<int>[4];
    public static List<Input_But>[] inputedBut = new List<Input_But>[4];
    public static List<Input_Axis>[] inputedAxis = new List<Input_Axis>[4];
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
[Serializable]
public struct Input_But
{
    public bool result, onDown;
    public string id;
}
[Serializable]
public struct Input_Axis
{
    public float result;
    public string id;
}
[Serializable]
public enum MessageType
{
    Tst,
    EnterMatch,
    Input_But,
    Input_Axis
}
[Serializable]
public struct Message
{
    public MessageType type;
    public int playerId;
    public string note;
    public string context;
}