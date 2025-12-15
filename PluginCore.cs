using BepInEx;
using HarmonyLib;

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
