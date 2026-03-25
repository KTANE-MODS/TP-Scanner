using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.UI.Navigation;


public class Scanner : MonoBehaviour {

	[SerializeField]
	private string modulePath; //The path where all the modules are installed located
    [SerializeField]
    private string url; //Repo path
    [SerializeField]
    private string csvPath; //path to save the csv

    List<RepoEntry> mods = new List<RepoEntry>();



    //For each folder, verify the module
    class KtaneData
    {
        public List<Dictionary<string, object>> KtaneModules { get; set; }
    }

    bool successJSON = false;

    void Awake()
    {
        StartCoroutine(Scan());
    }

    IEnumerator Scan()
    {
        yield return LoadJSON();
        if (successJSON)
        {
            //check if player is missing any modules on their device
            RepoEntry.ParentPath = modulePath;
            List<RepoEntry> installedModules = mods.Where(mod => Directory.Exists(mod.DirectoryPath)).ToList();
            List<RepoEntry> missingModules = mods.Except(installedModules).ToList();
            Log($"Installed modules: {installedModules.Select(m => m.Name).Join(", ")}");
            Log($"Modules not installed from the repo: {missingModules.Select(m => m.Name).Join(", ")}");

            //Check with modules have TP Support
            List<RepoEntry> modsWithTPSupport = CheckForTPSupport(installedModules);
            List<RepoEntry> modsWithoutTPSupport = installedModules.Except(modsWithTPSupport).ToList();
            Log($"Modules with TP Support: {modsWithTPSupport.Select(m => m.Name).Join(", ")}");
            Log($"Modules without TP Support: {modsWithoutTPSupport.Select(m => m.Name).Join(", ")}");

            //Check whichm oudles have autosolvers
            List<RepoEntry> modsWithAutoSolvers = CheckForAutosolves(modsWithTPSupport);
            List<RepoEntry> tpSupportedModsWithoutAutoSolvers = modsWithTPSupport.Except(modsWithAutoSolvers).ToList();
            Log($"Modules with autosolvers: {modsWithAutoSolvers.Select(m => m.Name).Join(", ")}");
            Log($"Modules without autosolvers: {tpSupportedModsWithoutAutoSolvers.Select(m => m.Name).Join(", ")}");


            //write the csv and save it
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Name, Has TP Support, Has Auto Solver");


            foreach (RepoEntry mod in modsWithAutoSolvers)
            {
                csv.AppendLine($"{EscapeCsv(mod.Name)}, TRUE, TRUE");
            }

            foreach (RepoEntry mod in tpSupportedModsWithoutAutoSolvers)
            {
                csv.AppendLine($"{EscapeCsv(mod.Name)}, TRUE, FALSE");
            }

            foreach (RepoEntry mod in modsWithoutTPSupport)
            {
                csv.AppendLine($"{EscapeCsv(mod.Name)}, FALSE, FALSE");
            }

            foreach (RepoEntry mod in missingModules)
            {
                csv.AppendLine($"{EscapeCsv(mod.Name)}, UNKNOWN, UNKNOWN");
            }

            if (Application.isEditor) 
            { 
                string path = Path.Combine(Application.dataPath, "output.csv");
                File.WriteAllText(path, csv.ToString());
            }

        }
    }

    //Load the JSON
    IEnumerator LoadJSON()
    {
        if(successJSON)
        {
            Log("Connection to repo already established, skipping loading...");
            yield break;
        }
        Log($"Connecting to the repo with url \"{url}\"...");
        WWW fetch = new WWW(url);
        yield return fetch;
        if (fetch.error == null)
        {
            Log("Connection successful.");
            mods = ProcessJson(fetch.text).Where(x => x.SteamID != null && (x.Type == "Regular" || x.Type == "Needy")).OrderBy(m => m.Name).ToList();
            successJSON = true;
        }
        else
        {
            Log("Connection failed.");
            successJSON = false;
        }
    }

    //Converts JsON into C# objects
    List<RepoEntry> ProcessJson(string fetched)
    {
        KtaneData Deserialized = JsonConvert.DeserializeObject<KtaneData>(fetched);
        List<RepoEntry> RepoEntries = new List<RepoEntry>();
        foreach (var item in Deserialized.KtaneModules)
            RepoEntries.Add(new RepoEntry(item));
        return RepoEntries;
    }

    List<RepoEntry> CheckForTPSupport(List<RepoEntry> mods)
    {
        return mods.Where(mod => DllHasFunction(mod.GetDLLPaths(), "ProcessTwitchCommand")).ToList();
    }

    List<RepoEntry> CheckForAutosolves(List<RepoEntry> mods)
    {
        Log("Checking for autosolves...");
        return mods.Where(mod => DllHasFunction(mod.GetDLLPaths(), "TwitchHandleForcedSolve")).ToList();
    }

    bool DllHasFunction(string[] dllFilePaths, string functionName)
    {
        foreach (string dll in dllFilePaths)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);

                var validTypes = assembly.GetTypes()
                    .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t));

                return validTypes.Any(t =>
                    t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     .Any(m => m.Name ==  functionName)
                );
            }
            catch
            {
                Log($"Failed to load: {dll}", LogType.Warning);
            }
        }

        return false;
    }
    void Log(string str, LogType logType = LogType.Log)
    {
        switch (logType)
        {
            case LogType.Warning:
                Debug.LogWarning($"[TP Scanner] {str}");
                break;
            default:
                Debug.Log($"[TP Scanner] {str}");
            break;
        }
    }

    string EscapeCsv(string field)
    {
        if (field.Contains(",") || field.Contains("\n"))
        {
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }
}
