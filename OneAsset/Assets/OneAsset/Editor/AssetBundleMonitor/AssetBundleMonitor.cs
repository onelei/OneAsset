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
        // EditorPrefs 键名，用于在域重载后恢复状态
        private const string PrefKeyShouldRecord = "AssetBundleMonitor_ShouldRecord";

        // 录制标记：在编辑器下点击 Start 设置为 true，运行时根据此标记决定是否记录
        private static bool _shouldRecord = false;

        // 会话数据
        private static MonitorSessionData _currentSession;

        private static readonly Dictionary<string, AssetBundleRecord> _loadingBundles =
            new Dictionary<string, AssetBundleRecord>();

        private static readonly Dictionary<string, int> _bundleReferenceCounts = new Dictionary<string, int>();

        // 标记是否已经订阅事件，防止重复订阅
        private static bool _isSubscribed = false;

        public static MonitorSessionData CurrentSession => _currentSession;
        public static bool IsRecording => _shouldRecord;

        /// <summary>
        /// 编辑器初始化时自动订阅事件（处理域重载）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // 从 EditorPrefs 恢复录制状态（解决域重载后状态丢失问题）
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
        /// 确保已订阅事件（防止域重载导致订阅丢失）
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
        /// 开始录制（仅设置标记，实际录制在运行时开始）
        /// </summary>
        public static void StartRecording()
        {
            _shouldRecord = true;
            EditorPrefs.SetBool(PrefKeyShouldRecord, true); // 保存到 EditorPrefs

            // 如果已经在运行模式，立即开始录制
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
        /// 停止录制
        /// </summary>
        public static void StopRecording()
        {
            if (!_shouldRecord)
            {
                Debug.LogWarning("[AssetBundle Monitor] Not recording");
                return;
            }

            _shouldRecord = false;
            EditorPrefs.SetBool(PrefKeyShouldRecord, false); // 保存到 EditorPrefs

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
            Debug.Log("[AssetBundle Monitor] Started recording immediately");
        }

        /// <summary>
        /// 清空会话数据
        /// </summary>
        public static void ClearSession()
        {
            _shouldRecord = false;
            EditorPrefs.SetBool(PrefKeyShouldRecord, false); // 清除 EditorPrefs
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
            }
        }

        private static void OnBundleLoadFailed(BundleLoadEventArgs args)
        {
            // 只有在运行时且标记了录制才记录
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
    }
}