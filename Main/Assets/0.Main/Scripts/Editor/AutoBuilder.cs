#if UNITY_EDITOR
using System.IO;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoBuilder
{
    static readonly string rootBuildFolder = "../../AutomatedBuilds";
    static readonly string scenesDir = "Assets/0.Main/Scenes/";


    private static string[] serverScenesNames = new string[]
    {
         "Server.unity",
         "Client_GameScene.unity"
    };

    private static readonly string[] clientScenesNames = new string[]
    {
        
        "Client_WaitingRoom.unity",
        "Client_GameScene.unity"
    };

  
    [MenuItem("Builds/Batch Build")]
    public static void BuildServerAndClient()
    {

        CreateBuild(serverScenesNames, scenesDir, "Server");
        CreateBuild(clientScenesNames, scenesDir, "Client");

        Debug.Log("[AutoBuilder] Batch process complete! Check the 'AutomatedBuilds' folder one level above the GitHub Repo directory.");
    }

    public static void CreateBuild(string[] sceneNames, string scenesDir, string buildName)
    {

        string[] scenesToBuild = new string[sceneNames.Length];

        for (int i = 0; i < scenesToBuild.Length; i++)
        {
            scenesToBuild[i] = scenesDir + sceneNames[i];
        }

        string targetFolder = Path.Combine(rootBuildFolder, $"{buildName}_Build");
        Directory.CreateDirectory(targetFolder);

        // Full path including executable name
        string exePath = Path.Combine(targetFolder, $"{buildName}.exe");
        Debug.Log($"[AutoBuilder] Building {buildName}...");

        //Configure build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenesToBuild,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        // Set the window title name in PlayerSettings dynamically
        PlayerSettings.productName = $"NetworksGame_{buildName}";


        //Trigger build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AutoBuilder] Successfully created: {exePath} ({summary.totalSize / (1024 * 1024)} MB)");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[AutoBuilder] Build {buildName} failed with {summary.totalErrors} errors.");
        }
    }
}
#endif