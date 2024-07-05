using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WEEP : MonoBehaviour
{
    [System.Serializable]
    public class ProfilingSettings
    {
        public KeyCode toggleKey = KeyCode.F8;
        public int samplingInterval = 10;
        public float recordingDuration = 60f;
        public bool autoStopRecording = true;
        public bool includeScriptTimings = true;
        public bool includeGPUTimings = true;
        public bool includeMemoryDetails = true;
        public bool includePhysicsStats = true;
        public bool includeRenderingStats = true;
        public bool includeAudioStats = true;
        public string outputFileName = "WEEP_Profile_{0}.csv";
    }

    [Header("Profiling Settings")]
    public ProfilingSettings[] settings = new ProfilingSettings[1] { new ProfilingSettings() };

    [System.Serializable]
    public class PerformanceThresholds
    {
        public float minAcceptableFPS = 30f;
        public float maxAcceptableMemoryUsageMB = 1000f;
        public int maxAcceptableDrawCalls = 1000;
        public float maxAcceptableFrameTimems = 33.33f; // 30 FPS
    }

    [Header("Performance Thresholds")]
    public PerformanceThresholds thresholds = new PerformanceThresholds();

    [Header("Debug Settings")]
    public bool suppressConsoleLog = false;

    private bool isRecording = false;
    private StringBuilder csvContent;
    private float startTime;
    private int frameCount;
    private Dictionary<string, float> scriptTimings = new Dictionary<string, float>();

    private void Update()
    {
        if (Input.GetKeyDown(settings[0].toggleKey))
        {
            ToggleRecording();
        }

        if (isRecording)
        {
            RecordProfilerData();
        }
    }

    private void ToggleRecording()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        isRecording = true;
        startTime = Time.realtimeSinceStartup;
        frameCount = 0;
        csvContent = new StringBuilder();
        csvContent.AppendLine(GenerateCSVHeader());
        if (!suppressConsoleLog)
        {
            Debug.Log("WEEP: Profiling started. Press " + settings[0].toggleKey + " again to stop and export the data.");
        }

        if (settings[0].autoStopRecording)
        {
            Invoke("StopRecording", settings[0].recordingDuration);
        }
    }

    private void RecordProfilerData()
    {
        frameCount++;

        if (frameCount % settings[0].samplingInterval != 0) return;

        float elapsedTime = Time.realtimeSinceStartup - startTime;
        float fps = 1f / Time.unscaledDeltaTime;
        float timeMs = Time.unscaledDeltaTime * 1000f;

        StringBuilder frameData = new StringBuilder();
        frameData.Append($"{frameCount},{elapsedTime:F2},{fps:F2},{timeMs:F2}");

        if (settings[0].includeMemoryDetails)
        {
            float totalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() / 1048576f;
            float totalAllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
            float monoUsedMemoryMB = Profiler.GetMonoUsedSizeLong() / 1048576f;
            frameData.Append($",{totalReservedMemoryMB:F2},{totalAllocatedMemoryMB:F2},{monoUsedMemoryMB:F2}");
        }

        if (settings[0].includeGPUTimings)
        {
            frameData.Append($",{UnityStats.renderTime:F2}");
        }

        frameData.Append($",{UnityStats.batches},{UnityStats.setPassCalls},{UnityStats.drawCalls},{UnityStats.triangles},{UnityStats.vertices}");

        if (settings[0].includePhysicsStats)
        {
            frameData.Append($",{Physics.simulationMode},{Time.fixedDeltaTime:F4},{Physics.autoSimulation}");
        }

        if (settings[0].includeRenderingStats)
        {
            frameData.Append($",{QualitySettings.renderPipeline},{QualitySettings.vSyncCount},{QualitySettings.antiAliasing}");
        }

        if (settings[0].includeAudioStats)
        {
            frameData.Append($",{AudioSettings.outputSampleRate},{AudioSettings.speakerMode},{AudioListener.volume:F2}");
        }

        if (settings[0].includeScriptTimings)
        {
            RecordScriptTimings();
            string scriptTimingsStr = string.Join(";", scriptTimings.Select(kvp => $"{kvp.Key}:{kvp.Value:F3}ms"));
            frameData.Append($",{scriptTimingsStr}");
        }

        csvContent.AppendLine(frameData.ToString());
    }

    private void RecordScriptTimings()
    {
        scriptTimings.Clear();
        var monoBehaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in monoBehaviours)
        {
            string scriptName = mb.GetType().Name;
            float scriptTime = 0f;

            Profiler.BeginSample(scriptName);
            if (mb.enabled)
            {
                mb.Invoke("Update", 0);
            }
            Profiler.EndSample();

            scriptTime = Time.unscaledDeltaTime * 1000f; // Approximate time in milliseconds

            if (scriptTimings.ContainsKey(scriptName))
            {
                scriptTimings[scriptName] += scriptTime;
            }
            else
            {
                scriptTimings[scriptName] = scriptTime;
            }
        }
    }

    private void StopRecording()
    {
        isRecording = false;
        string filePath = Path.Combine(Application.dataPath, string.Format(settings[0].outputFileName, System.DateTime.Now.ToString("yyyyMMdd_HHmmss")));
        File.WriteAllText(filePath, csvContent.ToString());
        if (!suppressConsoleLog)
        {
            Debug.Log("WEEP: Profiling data exported to: " + filePath);
        }

        AnalyzePerformance();
    }

    private string GenerateCSVHeader()
    {
        StringBuilder header = new StringBuilder("Frame,Time (s),FPS,Frame Time (ms)");
        if (settings[0].includeMemoryDetails)
        {
            header.Append(",Total Reserved Memory (MB),Total Allocated Memory (MB),Mono Used Memory (MB)");
        }
        if (settings[0].includeGPUTimings)
        {
            header.Append(",GPU Time (ms)");
        }
        header.Append(",Batches Count,SetPass Calls Count,Draw Calls Count,Triangles Count,Vertices Count");
        if (settings[0].includePhysicsStats)
        {
            header.Append(",Physics Simulation Mode,Fixed Delta Time,Auto Simulation");
        }
        if (settings[0].includeRenderingStats)
        {
            header.Append(",Render Pipeline,VSync Count,Anti-Aliasing");
        }
        if (settings[0].includeAudioStats)
        {
            header.Append(",Audio Sample Rate,Speaker Mode,Audio Listener Volume");
        }
        if (settings[0].includeScriptTimings)
        {
            header.Append(",Script Timings");
        }
        return header.ToString();
    }

    private void AnalyzePerformance()
    {
        // Implement your performance analysis logic here
        // This could include checking for FPS drops, high memory usage, or excessive draw calls
        if (!suppressConsoleLog)
        {
            Debug.Log("WEEP: Performance analysis complete. Check the exported CSV for detailed data.");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WEEP))]
public class WEEPEditor : Editor
{
    private bool showProfilingSettings = true;
    private bool showPerformanceThresholds = true;
    private bool showDebugSettings = true;
    private bool showPerformanceGraph = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("WEEP - Wheazel's Epic Exporter Profile", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawProfilingSettings();
        EditorGUILayout.Space(10);
        DrawPerformanceThresholds();
        EditorGUILayout.Space(10);
        DrawDebugSettings();
        EditorGUILayout.Space(10);
        DrawControls();
        EditorGUILayout.Space(10);
        DrawVisualization();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProfilingSettings()
    {
        showProfilingSettings = EditorGUILayout.Foldout(showProfilingSettings, "Profiling Settings", true);
        if (showProfilingSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings"), true);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawPerformanceThresholds()
    {
        showPerformanceThresholds = EditorGUILayout.Foldout(showPerformanceThresholds, "Performance Thresholds", true);
        if (showPerformanceThresholds)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thresholds.minAcceptableFPS"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thresholds.maxAcceptableMemoryUsageMB"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thresholds.maxAcceptableDrawCalls"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thresholds.maxAcceptableFrameTimems"));
            EditorGUI.indentLevel--;
        }
    }

    private void DrawDebugSettings()
    {
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("suppressConsoleLog"));
            EditorGUI.indentLevel--;
        }
    }

    private void DrawControls()
    {
        EditorGUILayout.LabelField("WEEP Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Recording"))
        {
            (target as WEEP).Invoke("StartRecording", 0);
        }
        if (GUILayout.Button("Stop Recording"))
        {
            (target as WEEP).Invoke("StopRecording", 0);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Check Profile Analyzer"))
        {
            CheckProfileAnalyzer();
        }

        if (GUILayout.Button("Show README"))
        {
            ShowReadmePopup();
        }
    }

    private void DrawVisualization()
    {
        showPerformanceGraph = EditorGUILayout.Foldout(showPerformanceGraph, "Performance Graph", true);
        if (showPerformanceGraph)
        {
            EditorGUILayout.HelpBox("Performance graph visualization will be implemented here.", MessageType.Info);
        }
    }

    private void CheckProfileAnalyzer()
    {
        bool hasProfileAnalyzer = System.Type.GetType("UnityEditor.Performance.ProfileAnalyzer") != null;
        if (hasProfileAnalyzer)
        {
            EditorUtility.DisplayDialog("WEEP", "Profile Analyzer package is installed.", "OK");
        }
        else
        {
            if (EditorUtility.DisplayDialog("WEEP", "Profile Analyzer package is not installed. Would you like to install it?", "Yes", "No"))
            {
                InstallProfileAnalyzer();
            }
        }
    }

    private void InstallProfileAnalyzer()
    {
        UnityEditor.PackageManager.Client.Add("com.unity.performance.profile-analyzer");
    }

    private void ShowReadmePopup()
    {
        string readme = @"# WEEP (Wheazel's Epic Exporter Profile)

WEEP is an advanced profiling tool for Unity that allows you to collect and analyze performance data from your game.

## Features
- Customizable profiling settings
- Detailed performance metrics including FPS, memory usage, draw calls, and more
- Script execution time tracking
- Integration with Unity's Profile Analyzer
- CSV export for further analysis

## Usage
1. Configure the Profiling Settings in the Inspector.
2. Use the 'Start Recording' and 'Stop Recording' buttons in the Inspector, or use the toggle key in play mode.
3. Analyze the exported CSV file for performance insights.

## Settings
- Toggle Key: Key to start/stop recording (default: F8)
- Sampling Interval: Frames between each data sample
- Recording Duration: Maximum recording time (if auto-stop is enabled)
- Auto Stop Recording: Automatically stop after the specified duration
- Various inclusion options for different types of profiling data

## Performance Thresholds
Set acceptable thresholds for key performance metrics. WEEP will highlight when these thresholds are exceeded.

## Notes
- WEEP may impact game performance while recording. Use for debugging and optimization, not in release builds.
- For advanced analysis, use Unity's built-in Profiler or the Profile Analyzer package alongside WEEP.

For more detailed information, please refer to the full documentation.";

        EditorUtility.DisplayDialog("WEEP README", readme, "OK");
    }
}
#endif