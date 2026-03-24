using System.Collections.Generic;
public class RepoEntry
{
    public string Name { get; private set; }
    public string SteamID { get; private set; }
    public string Type { get; private set; }

    public RepoEntry(Dictionary<string, object> Data)
    {
        Name = (string)Data["Name"];
        SteamID = (string)Data["SteamID"];
        Type = (string)Data["Type"];
    }
}