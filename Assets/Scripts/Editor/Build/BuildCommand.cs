using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project.Editor
{
    public static class BuildCommand
    {
        private const string BuildOutputArgument = "-buildOutput";

        [MenuItem("Build/Command Line/Build Windows 64")]
        public static void BuildWindows64()
        {
            Build(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64,
                "Windows",
                ".exe");
        }

        [MenuItem("Build/Command Line/Build Android")]
        public static void BuildAndroid()
        {
            Build(
                BuildTargetGroup.Android,
                BuildTarget.Android,
                "Android",
                EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk");
        }

        private static void Build(
            BuildTargetGroup targetGroup,
            BuildTarget target,
            string targetDirectory,
            string extension)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException(
                    "No enabled scenes were found in File > Build Profiles > Platforms > Scene List.");
            }

            if (!BuildPipeline.IsBuildTargetSupported(targetGroup, target))
            {
                throw new BuildFailedException(
                    $"Build support for {target} is not installed in this Unity Editor.");
            }

            if (EditorUserBuildSettings.activeBuildTarget != target &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target))
            {
                throw new BuildFailedException($"Unity could not switch to the {target} build target.");
            }

            string outputPath = GetCommandLineValue(BuildOutputArgument);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                string productName = SanitizeFileName(PlayerSettings.productName);
                outputPath = Path.Combine("Builds", targetDirectory, productName + extension);
            }

            outputPath = Path.GetFullPath(outputPath);
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new BuildFailedException($"Invalid build output path: {outputPath}");
            }

            Directory.CreateDirectory(outputDirectory);

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = targetGroup,
                options = GetBuildOptions(),
            };

            Debug.Log($"Building {target} to {outputPath} with {scenes.Length} enabled scene(s).");
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"{target} build failed with result {summary.result}, " +
                    $"{summary.totalErrors} error(s), and {summary.totalWarnings} warning(s).");
            }

            Debug.Log(
                $"{target} build succeeded: {summary.totalSize} bytes in " +
                $"{summary.totalTime.TotalSeconds:F1} seconds. Output: {outputPath}");
        }

        private static BuildOptions GetBuildOptions()
        {
            BuildOptions options = BuildOptions.None;

            if (HasCommandLineFlag("-developmentBuild"))
            {
                options |= BuildOptions.Development | BuildOptions.AllowDebugging;
            }

            if (HasCommandLineFlag("-connectProfiler"))
            {
                options |= BuildOptions.ConnectWithProfiler;
            }

            if (HasCommandLineFlag("-autoRunPlayer"))
            {
                options |= BuildOptions.AutoRunPlayer;
            }

            return options;
        }

        private static bool HasCommandLineFlag(string flag)
        {
            return Environment.GetCommandLineArgs()
                .Any(argument => string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetCommandLineValue(string key)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private static string SanitizeFileName(string value)
        {
            string sanitized = string.IsNullOrWhiteSpace(value) ? "NCKH VR" : value;
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidCharacter, '_');
            }

            return sanitized.Trim();
        }
    }
}
