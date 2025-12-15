using HarmonyLib;
using UnityEngine;
using static UnityEngine.Random;
[HarmonyPatch]
public static class UnityBaseSync
{
    public static float rt;
    [HarmonyPatch(typeof(CoreGameManager), "PlayBegins"), HarmonyPrefix]
    public static bool PlayBegins()
    {
        rt = 0;
        return true;
    }

    [HarmonyPatch(typeof(CoreGameManager), "Quit"), HarmonyPrefix]
    public static bool Quit()
    {
        rt = 0;
        return true;
    }

    [HarmonyPatch(typeof(Time), "deltaTime", MethodType.Getter), HarmonyPostfix]
    public static void deltaTime(ref float __result)
    {
        __result = 0.02f;
    }

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
}