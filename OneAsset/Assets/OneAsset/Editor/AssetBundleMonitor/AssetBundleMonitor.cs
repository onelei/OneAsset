using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using OneAsset.Runtime.Monitor;
using RuntimeAssetBundleMonitor = OneAsset.Runtime.Monitor.AssetBundleMonitor;

namespace OneAsset.Editor.AssetBundleMonitor
{
    /// <summary>
    /// AssetBundle load monitor (Editor only)
    /// Subscribes to AssetBundleLoadMonitor events and collects runtime data
    /// </summary>
    public static class AssetBundleMonitor
    {
        // EditorPrefs key name, used to restore state after domain reload
        private const string PrefKeyShouldRecord = "AssetBundleMonitor_ShouldRecord";

        // Recording flag: set to true when Start is clicked in Editor, determines whether to record at runtime based on this flag
        private static bool _shouldRecord = false;

        // Session data
        private static MonitorSessionData _currentSession;

        private static readonly Dictionary<string, AssetBundleRecord> LoadingBundles =
            new Dictionary<string, AssetBundleRecord>();

        private static readonly Dictionary<string, int> BundleReferenceCounts = new Dictionary<string, int>();
 
        private static int _lastFrameIndex = -1;
        private static int _currentFrameLoadedCount = 0;
        private static int _currentFrameUnloadedCount = 0;

        public static MonitorSessionData CurrentSession => _currentSession;
        public static bool IsRecording => _shouldRecord;

        /// <summary>
        /// Automatically subscribe to events during editor initialization (handles domain reload)
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // Restore recording state from EditorPrefs (solves state loss issue after domain reload)
            _shouldRecord = EditorPrefs.GetBool(PrefKeyShouldRecord, false);
            if (_shouldRecord)
            {
                StartRecording();
            }

            RuntimeAssetBundleMonitor.OnBundleLoadStart += OnBundleLoadStart;
            RuntimeAssetBundleMonitor.OnBundleLoadSuccess += OnBundleLoadSuccess;
            RuntimeAssetBundleMonitor.OnBundleLoadFailed += OnBundleLoadFailed;
            RuntimeAssetBundleMonitor.OnBundleUnload += OnBundleUnload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!_shouldRecord || !Application.isPlaying || _currentSession == null) 
                return;

            var currentFrame = Time.frameCount;
            if (currentFrame != _lastFrameIndex)
            {
                // Record data for the frame that just finished (or the current state if it's the first frame)
                if (_lastFrameIndex != -1)
                {
                    var frameData = new ProfilerFrameData
                    {
                        frameIndex = _lastFrameIndex,
                        totalBundleCount = BundleReferenceCounts.Count,
                        loadedCount = _currentFrameLoadedCount,
                        unloadedCount = _currentFrameUnloadedCount
                    };
                    _currentSession.profilerData.Add(frameData);
                    
                    // Keep only last 400 frames
                    if (_currentSession.profilerData.Count > 400)
                    {
                        _currentSession.profilerData.RemoveAt(0);
                    }
                    
                    _currentFrameLoadedCount = 0;
                    _currentFrameUnloadedCount = 0;
                }
                _lastFrameIndex = currentFrame;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && _shouldRecord)
            {
                RuntimeAssetBundleMonitor.OnBundleLoadStart -= OnBundleLoadStart;
                RuntimeAssetBundleMonitor.OnBundleLoadSuccess -= OnBundleLoadSuccess;
                RuntimeAssetBundleMonitor.OnBundleLoadFailed -= OnBundleLoadFailed;
                RuntimeAssetBundleMonitor.OnBundleUnload -= OnBundleUnload;

                StopRecording();
                Debug.Log($"[AssetBundle Monitor] Stopped recording, total records: {_currentSession.records.Count}");
            }
        }
 
        /// <summary>
        /// Start recording (only sets the flag, actual recording starts at runtime)
        /// </summary>
        public static void StartRecording()
        {
            _shouldRecord = true;
            EditorPrefs.SetBool(PrefKeyShouldRecord, true); // Save to EditorPrefs

            // If already in play mode, start recording immediately
            if (EditorApplication.isPlaying)
            {
                CreateSession();
            }
            else
            {
                Debug.Log("[AssetBundle Monitor] Marked for recording, will start when entering Play Mode");
            }
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public static void StopRecording()
        {
            if (!_shouldRecord)
            {
                Debug.LogWarning("[AssetBundle Monitor] Not recording");
                return;
            }

            _shouldRecord = false;
            EditorPrefs.SetBool(PrefKeyShouldRecord, false); // Save to EditorPrefs

            if (_currentSession != null)
            {
                _currentSession.sessionEndTime = DateTime.Now;
                _currentSession.isRecording = false;
                Debug.Log($"[AssetBundle Monitor] Stopped recording, total records: {_currentSession.records.Count}");
            }
        }

        private static void CreateSession()
        {
            if (_currentSession != null)
                return;
            _currentSession = new MonitorSessionData
            {
                sessionStartTime = DateTime.Now,
                isRecording = true
            };
            LoadingBundles.Clear();
            BundleReferenceCounts.Clear();
            
            _lastFrameIndex = Time.frameCount;
            _currentFrameLoadedCount = 0;
            _currentFrameUnloadedCount = 0;

            Debug.Log("[AssetBundle Monitor] Started recording immediately");
        }

        /// <summary>
        /// Clear session data
        /// </summary>
        public static void ClearSession()
        {
            _shouldRecord = false;
            EditorPrefs.SetBool(PrefKeyShouldRecord, false); // Clear EditorPrefs
            _currentSession = null;
            LoadingBundles.Clear();
            BundleReferenceCounts.Clear();
        }

        private static void OnBundleLoadStart(BundleLoadEventArgs args)
        {
            if (!_shouldRecord)
                return;
            CreateSession();
            var record = new AssetBundleRecord
            {
                bundleName = args.BundleName,
                packageName = args.PackageName,
                sceneName = SceneManager.GetActiveScene().name,
                assetAddress = args.AssetAddress,
                assetPath = args.AssetPath,
                loadStartTime = args.StartTime,
                isAsync = args.IsAsync,
                loadType = args.IsAsync ? "Async" : "Sync",
                frameIndex = Time.frameCount,
                dependencies = args.Dependencies != null ? new List<string>(args.Dependencies) : new List<string>()
            };

            if (!BundleReferenceCounts.ContainsKey(args.BundleName))
                BundleReferenceCounts[args.BundleName] = 0;
            BundleReferenceCounts[args.BundleName]++;
            record.referenceCount = BundleReferenceCounts[args.BundleName];

            LoadingBundles[args.BundleName] = record;
        }

        private static void OnBundleLoadSuccess(BundleLoadEventArgs args)
        {
            if (!_shouldRecord)
                return;
            CreateSession();
            if (LoadingBundles.TryGetValue(args.BundleName, out var record))
            {
                record.loadEndTime = args.EndTime;
                record.loadDuration = (record.loadEndTime - record.loadStartTime).TotalMilliseconds;
                record.loadSuccess = true;
                record.bundleSize = args.BundleSize;

                _currentSession.records.Add(record);
                LoadingBundles.Remove(args.BundleName);
                
                _currentFrameLoadedCount++;
            }
        }

        private static void OnBundleLoadFailed(BundleLoadEventArgs args)
        {
            // Only record if in runtime and recording is flagged
            if (!_shouldRecord)
                return;
            CreateSession();
            if (LoadingBundles.TryGetValue(args.BundleName, out var record))
            {
                record.loadEndTime = args.EndTime;
                record.loadDuration = (record.loadEndTime - record.loadStartTime).TotalMilliseconds;
                record.loadSuccess = false;
                record.errorMessage = args.ErrorMessage;

                _currentSession.records.Add(record);
                LoadingBundles.Remove(args.BundleName);
            }
        }

        private static void OnBundleUnload(string bundleName)
        {
            if (BundleReferenceCounts.ContainsKey(bundleName))
            {
                BundleReferenceCounts[bundleName]--;
                if (BundleReferenceCounts[bundleName] <= 0)
                    BundleReferenceCounts.Remove(bundleName);
                
                if (_shouldRecord)
                    _currentFrameUnloadedCount++;
            }
        }

        public static int GetBundleReferenceCount(string bundleName)
        {
            return BundleReferenceCounts.TryGetValue(bundleName, out var count) ? count : 0;
        }

        public static List<AssetBundleRecord> GetAllRecords()
        {
            return _currentSession?.records ?? new List<AssetBundleRecord>();
        }

        public static void SaveSession(string path)
        {
            if (_currentSession == null) return;
            try
            {
                string json = JsonUtility.ToJson(_currentSession, true);
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"[AssetBundle Monitor] Session saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetBundle Monitor] Failed to save session: {e.Message}");
            }
        }

        public static void LoadSession(string path)
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                _currentSession = JsonUtility.FromJson<MonitorSessionData>(json);
                _shouldRecord = false; // Stop recording when loading external data
                EditorPrefs.SetBool(PrefKeyShouldRecord, false);
                Debug.Log($"[AssetBundle Monitor] Session loaded from {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetBundle Monitor] Failed to load session: {e.Message}");
            }
        }
    }
}
