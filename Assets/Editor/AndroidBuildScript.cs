using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuildScript
{
    private const string OutputFileName = "codexDlablo.apk";
    private const string AndroidApplicationId = "com.xyzhi.codexdlablo";

    public static void BuildReleaseApk()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var outputPath = Path.Combine(projectRoot, OutputFileName);
        var scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                throw new System.Exception("No enabled scenes found in EditorBuildSettings.");
            }

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidApplicationId);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception("Android build failed: " + report.summary.result);
            }

        UnityEngine.Debug.Log("Android APK built: " + outputPath);
    }

    private static string[] GetEnabledScenes()
    {
        var enabledScenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                enabledScenes.Add(scene.path);
            }
        }

        return enabledScenes.ToArray();
    }
}
