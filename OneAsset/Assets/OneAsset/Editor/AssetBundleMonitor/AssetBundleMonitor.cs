using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using OneAsset.Runtime.Loader;

namespace OneAsset.Editor.AssetBundleMonitor
{
    /// <summary>
    /// AssetBundle load monitor (Editor only)
    /// Subscribes to AssetBundleLoader events to monitor bundle loading
    /// </summary>
    public static class AssetBundleMonitor
    {
        // EditorPrefs key name, used to restore state after domain reload
        private const string PrefKeyShouldRecord = "AssetBundleMonitor_ShouldRecord";

        // Recording flag: set to true when Start is clicked in Editor, determines whether to record at runtime based on this flag
        private static bool _shouldRecord = false;

        // Session data
        private static MonitorSessionData _currentSession;

        private static readonly Dictionary<string, AssetBundleRecord> _loadingBundles =
            new Dictionary<string, AssetBundleRecord>();

        private static readonly Dictionary<string, int> _bundleReferenceCounts = new Dictionary<string, int>();

        // Flag to check if events are already subscribed to prevent duplicate subscriptions
        private static bool _isSubscribed = false;

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

            AssetBundleLoader.OnBundleLoadStart += OnBundleLoadStart;
            AssetBundleLoader.OnBundleLoadSuccess += OnBundleLoadSuccess;
            AssetBundleLoader.OnBundleLoadFailed += OnBundleLoadFailed;
            AssetBundleLoader.OnBundleUnload += OnBundleUnload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!_shouldRecord || !Application.isPlaying || _currentSession == null) 
                return;

            int currentFrame = Time.frameCount;
            if (currentFrame != _lastFrameIndex)
            {
                // Record data for the frame that just finished (or the current state if it's the first frame)
                if (_lastFrameIndex != -1)
                {
                    var frameData = new ProfilerFrameData
                    {
                        frameIndex = _lastFrameIndex,
                        totalBundleCount = _bundleReferenceCounts.Count,
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
                AssetBundleLoader.OnBundleLoadStart -= OnBundleLoadStart;
                AssetBundleLoader.OnBundleLoadSuccess -= OnBundleLoadSuccess;
                AssetBundleLoader.OnBundleLoadFailed -= OnBundleLoadFailed;
                AssetBundleLoader.OnBundleUnload -= OnBundleUnload;

                StopRecording();
                Debug.Log($"[AssetBundle Monitor] Stopped recording, total records: {_currentSession.records.Count}");
            }
        }

        /// <summary>
        /// Ensure events are subscribed (prevents subscription loss due to domain reload)
        /// </summary>
        private static void EnsureSubscribed()
        {
            if (_isSubscribed)
                return;

            AssetBundleLoader.OnBundleLoadStart += OnBundleLoadStart;
            AssetBundleLoader.OnBundleLoadSuccess += OnBundleLoadSuccess;
            AssetBundleLoader.OnBundleLoadFailed += OnBundleLoadFailed;
            AssetBundleLoader.OnBundleUnload += OnBundleUnload;

            _isSubscribed = true;
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
            _loadingBundles.Clear();
            _bundleReferenceCounts.Clear();
            
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
            _loadingBundles.Clear();
            _bundleReferenceCounts.Clear();
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

            if (!_bundleReferenceCounts.ContainsKey(args.BundleName))
                _bundleReferenceCounts[args.BundleName] = 0;
            _bundleReferenceCounts[args.BundleName]++;
            record.referenceCount = _bundleReferenceCounts[args.BundleName];

            _loadingBundles[args.BundleName] = record;
        }

        private static void OnBundleLoadSuccess(BundleLoadEventArgs args)
        {
            if (!_shouldRecord)
                return;
            CreateSession();
            if (_loadingBundles.TryGetValue(args.BundleName, out var record))
            {
                record.loadEndTime = args.EndTime;
                record.loadDuration = (record.loadEndTime - record.loadStartTime).TotalMilliseconds;
                record.loadSuccess = true;
                record.bundleSize = args.BundleSize;

                _currentSession.records.Add(record);
                _loadingBundles.Remove(args.BundleName);
                
                _currentFrameLoadedCount++;
            }
        }

        private static void OnBundleLoadFailed(BundleLoadEventArgs args)
        {
            // Only record if in runtime and recording is flagged
            if (!_shouldRecord)
                return;
            CreateSession();
            if (_loadingBundles.TryGetValue(args.BundleName, out var record))
            {
                record.loadEndTime = args.EndTime;
                record.loadDuration = (record.loadEndTime - record.loadStartTime).TotalMilliseconds;
                record.loadSuccess = false;
                record.errorMessage = args.ErrorMessage;

                _currentSession.records.Add(record);
                _loadingBundles.Remove(args.BundleName);
            }
        }

        private static void OnBundleUnload(string bundleName)
        {
            if (_bundleReferenceCounts.ContainsKey(bundleName))
            {
                _bundleReferenceCounts[bundleName]--;
                if (_bundleReferenceCounts[bundleName] <= 0)
                    _bundleReferenceCounts.Remove(bundleName);
                
                if (_shouldRecord)
                    _currentFrameUnloadedCount++;
            }
        }

        public static int GetBundleReferenceCount(string bundleName)
        {
            return _bundleReferenceCounts.TryGetValue(bundleName, out var count) ? count : 0;
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
