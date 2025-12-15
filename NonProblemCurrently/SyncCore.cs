using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[HarmonyPatch]
public static class PlayerManagement
{
    [HarmonyPatch(typeof(BaseGameManager), "Update"), HarmonyPostfix]
    public static void BaseGameUpdate(BaseGameManager __instance)
    {
        Singleton<CoreGameManager>.Instance.sceneObject.skippable = false;
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
}
