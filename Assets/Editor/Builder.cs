using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Builder
{
    static string ProjectPath => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf ('/'));
    static string BuildBasePath => Path.Combine(ProjectPath, "Builds");

    [MenuItem("Build/StandaloneWindows64")]
    public static void BuildStandaloneWindows64()
    {
        Debug.Log("Build StandaloneWindows64");
        Build(BuildTarget.StandaloneWindows64, targetDirName: "StandaloneWindows64");
    }

    private static void Build(
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = "default")
    {
        // string[] scenes = { "Scenes/SampleScene" };
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        targetDirName ??= buildTarget.ToString();

        var locationPathName = Path.Combine(
            BuildBasePath,
            targetDirName,
            buildTarget switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => 
                    $"{PlayerSettings.productName}.exe",
                _ => PlayerSettings.productName
            }
        );

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = buildTarget,
            options = EditorUserBuildSettings.development
                ? options | BuildOptions.Development | BuildOptions.AllowDebugging
                : options
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                FileUtil.CopyFileOrDirectory(
                    Path.Combine(ProjectPath, "README.md"),
                    Path.Combine(BuildBasePath, targetDirName, "README.md"));
                break;
            case BuildResult.Failed:
                Debug.LogError("Build failed");
                foreach (var step in report.steps)
                    foreach (var message in step.messages)
                        if (message.type is LogType.Error or LogType.Exception)
                            Debug.LogError(message.content);
                break;
        }
    }
}
