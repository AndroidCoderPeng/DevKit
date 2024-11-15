using System.Collections.Generic;
using Newtonsoft.Json;

namespace DevKit.Configs
{
    public class ConfigCache
    {
        [JsonProperty("Apk")] public Apk Apk { get; set; }
        
        [JsonProperty("TcpClient")] public TcpClient TcpClient { get; set; }

        [JsonProperty("TcpServer")] public TcpServer TcpServer { get; set; }
    }

    public class Apk
    {
        [JsonProperty("Alias")] public string Alias { get; set; }

        [JsonProperty("RootFolder")] public string RootFolder { get; set; }

        [JsonProperty("KeyPath")] public string KeyPath { get; set; }

        [JsonProperty("Password")] public string Password { get; set; }
    }

    public class TcpClient
    {
        [JsonProperty("Extension")] public List<Extension> Extension { get; set; }

        [JsonProperty("RemoteAddress")] public string RemoteAddress { get; set; }

        [JsonProperty("RemotePort")] public long RemotePort { get; set; }

        [JsonProperty("SendHex")] public bool SendHex { get; set; }

        [JsonProperty("ShowHex")] public bool ShowHex { get; set; }
    }

    public class Extension
    {
        [JsonProperty("Order")] public long Order { get; set; }

        [JsonProperty("Command")] public string Command { get; set; }

        [JsonProperty("IsHex")] public bool IsHex { get; set; }

        [JsonProperty("Delay")] public long Delay { get; set; }
    }

    public class TcpServer
    {
    }
}