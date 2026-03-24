using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour {

	[SerializeField]
	private string modulePath; //The path where all the modules are installed located
    [SerializeField]
    private string url; //Repo path

    List<RepoEntry> mods = new List<RepoEntry>();

    //For each folder, verify the module
    class KtaneData
    {
        public List<Dictionary<string, object>> KtaneModules { get; set; }
    }

    bool successJSON = false;

    void Awake()
    {
        StartCoroutine(ScanTP());
    }

    IEnumerator ScanTP()
    {
        yield return LoadJSON();
        if (successJSON)
        {
            ScanForTP();
        }
    }

    //Load the JSON
    IEnumerator LoadJSON()
    {
        if(successJSON)
        {
            Log("Connection already established, skipping loading...");
        }
        Log("Connecting to the repo...");
        WWW fetch = new WWW(url);
        yield return fetch;
        if (fetch.error == null)
        {
            Log("Connection successful.");
            mods = ProcessJson(fetch.text).Where(x => x.SteamID != null && (x.Type == "Regular" || x.Type == "Needy")).ToList();
            successJSON = true;
        }
        else
        {
            Log("Connection failed.");
            successJSON = true;
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

    void ScanForTP()
    {
        //for each module, check if the player has it installed
        List<RepoEntry> installedModules = mods.Where(mod => Directory.Exists(modulePath + mod.SteamID)).ToList();
        Log($"Installed modules: {installedModules.Select(m => m.Name).Join(", ")}");

        //Figure out which modules are not installed
        List<RepoEntry> missingModules = mods.Except(installedModules).ToList();
        Log($"Modules not isntalled from the repo: {missingModules.Select(m => m.Name).Join(", ")}");
    }

    private void Log(string str)
    {
        Debug.Log($"[TP Scanner] {str}");
    }
}
