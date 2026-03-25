using System.Collections.Generic;
using System.IO;
public class RepoEntry
{
    public string Name { get; private set; }
    public string SteamID { get; private set; }
    public string Type { get; private set; }


    public static string ParentPath;
    public string DirectoryPath { get { return ParentPath + SteamID; } }

    private string[] dllPaths;

    public RepoEntry(Dictionary<string, object> Data)
    {
        Name = (string)Data["Name"];
        SteamID = (string)Data["SteamID"];
        Type = (string)Data["Type"];
    }

    public string[] GetDLLPaths()
    {
        if (dllPaths != null)
        { 
            return dllPaths;
        }

        //Get all the dlls within the directory (included sub directories)
        dllPaths = Directory.GetFiles(DirectoryPath, "*.dll", SearchOption.AllDirectories);
        return dllPaths;
    }
}