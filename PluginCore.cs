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
    public void Awake()
    {
        instance = this;

        IPEndPoint iP = IPFromString(
                Config.Bind(
                    new ConfigDefinition("Self IP is not your local device IP! The defualt value is not available too!", "Self IP"),
               new IPEndPoint(IPAddress.Any, 443).ToString()
                    ).Value
                    );

        Main.offline = InternetStation.SetUp(iP);

        if (Main.offline)
        {
            new Harmony("imystman12.baldifull.studentplus").PatchAll();
        }

        Main.AddPlayerIP(iP);

        PlayerList playerListExample = new PlayerList()
        {
            playerIPs = new string[3]
                {
                      new IPEndPoint(IPAddress.Broadcast, 443).ToString(),
                       new IPEndPoint(IPAddress.Loopback, 443).ToString(),
                             new IPEndPoint(IPAddress.IPv6Loopback, 443).ToString()
                }
        };

        PlayerList playerList =
            JsonConvert.DeserializeObject<PlayerList>(
            Config.Bind<string>(
            new ConfigDefinition("The defualt value is not available too! Max player count(including yourself) must less than 5! IPs in the list should not be same!",
            "Player List"),
            JsonConvert.SerializeObject(playerListExample)
            ).Value);

        foreach (var item in playerList.playerIPs)
        {
            Main.AddPlayerIP(IPFromString(item));
        }

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
