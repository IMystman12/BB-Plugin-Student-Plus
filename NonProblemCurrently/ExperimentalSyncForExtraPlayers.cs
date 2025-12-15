using System;
using HarmonyLib;

[HarmonyPatch, Obsolete("For Player.Count > 4 Only! Not available yet! Because we don't know mystman12 puts how many of them(Cause player count limited) into this game!")]
internal static class ExperimentalSyncForExtraPlayers
{
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
}