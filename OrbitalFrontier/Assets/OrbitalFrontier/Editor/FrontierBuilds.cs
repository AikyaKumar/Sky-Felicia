using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OrbitalFrontier.Editor
{
    public static class FrontierBuilds
    {
        const string Expedition = "Assets/OrbitalFrontier/Scenes/Expedition.unity";
        const string Desk = "Assets/OrbitalFrontier/Scenes/MissionDesk.unity";
        const string Hangar = "Assets/OrbitalFrontier/Scenes/Hangar.unity";
        const string Flight = "Assets/OrbitalFrontier/Scenes/LunarOrbit.unity";

        [MenuItem("Orbital Frontier/Build/Windows x64 (LAN + Solo)")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/OrbitalFrontier.exe");

        [MenuItem("Orbital Frontier/Build/Linux x64 (LAN + Solo)")]
        public static void Linux() => Build(BuildTarget.StandaloneLinux64, "Builds/Linux/OrbitalFrontier.x86_64");

        [MenuItem("Orbital Frontier/Build/Unity Web (Solo)")]
        public static void Web() => Build(BuildTarget.WebGL, "Builds/UnityWeb");

        static void Build(BuildTarget target, string path)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException("Install build support for " + target + " in Unity Hub.");
            bool web = target == BuildTarget.WebGL;
            var oldCompression = PlayerSettings.WebGL.compressionFormat;
            var oldGraphics = PlayerSettings.GetGraphicsAPIs(target);
            bool oldAutomatic = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
            try
            {
                if (web)
                    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                if (target == BuildTarget.StandaloneWindows64)
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                    PlayerSettings.SetGraphicsAPIs(target, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { Expedition, Desk, Hangar, Flight }, locationPathName = path, target = target,
                    options = BuildOptions.StrictMode
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Build finished with " + report.summary.result + ". Check the Unity console.");
                Debug.Log("Orbital Frontier build ready at " + path);
            }
            finally
            {
                if (web) PlayerSettings.WebGL.compressionFormat = oldCompression;
                if (target == BuildTarget.StandaloneWindows64)
                {
                    PlayerSettings.SetGraphicsAPIs(target, oldGraphics);
                    PlayerSettings.SetUseDefaultGraphicsAPIs(target, oldAutomatic);
                }
            }
        }
    }
}
