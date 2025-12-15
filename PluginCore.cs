using System;
using System.Net;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;

[BepInPlugin("imystman12.baldifull.studentplus", "BB+ Plugin Student Plus", "1.0")]
public class PluginCore : BaseUnityPlugin
{
    public static PluginCore instance;
    IPEndPoint point = new IPEndPoint(IPAddress.Any, 443);
    public void Awake()
    {
        new Harmony("imystman12.baldifull.studentplus").PatchAll();
        instance = this;

        InternetStation.SetUp(
            IPFromString(
                Config.Bind(
                    new ConfigDefinition("Self IP isn't your local device IP! The defualt value isn't available too!", "SelfIP"),
                 point.ToString()
                    ).Value
                    ));

        PlayerList playerListExample = new PlayerList()
        {
            playerIPs = new string[3]
                {
                       point.ToString(),
                          point.ToString(),
                             point.ToString()
                }
        };

        PlayerList playerList =
            JsonConvert.DeserializeObject<PlayerList>(
            Config.Bind<string>(
            new ConfigDefinition("The defualt value isn't available too! Max player count(including yourself) must less than 5! IPs in the list shouldn't be same!",
            "Player List"),
            JsonConvert.SerializeObject(playerListExample)
            ).Value);

        while (true)
        {
            InternetStation.Check();
        }
    }

    public IPEndPoint IPFromString(string ip)
    {
        string[] splited = ip.Split(':');
        if (splited.Length != 2)
        {
            return default;
        }
        return new IPEndPoint(IPAddress.Parse(splited[0]), int.Parse(splited[1]));
    }

    [Serializable]
    public class PlayerList
    {
        public string[] playerIPs;
    }
}
