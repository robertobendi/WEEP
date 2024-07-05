# WEEP (Wheazel's Epic Exporter Profile)

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
