using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class Unexpectations
{
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
}