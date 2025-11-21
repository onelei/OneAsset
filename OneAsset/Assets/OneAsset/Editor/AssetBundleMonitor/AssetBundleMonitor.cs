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
    public class AssetBundleMonitor
    {
        private static AssetBundleMonitor _instance;
        public static AssetBundleMonitor Instance => _instance ?? (_instance = new AssetBundleMonitor());

        public MonitorSessionData CurrentSession { get; private set; }
        public bool IsRecording => CurrentSession != null && CurrentSession.isRecording;
        
        private readonly Dictionary<string, AssetBundleRecord> _loadingBundles = new Dictionary<string, AssetBundleRecord>();
        private readonly Dictionary<string, int> _bundleReferenceCounts = new Dictionary<string, int>();
        
        // 标记是否已经订阅事件，防止重复订阅
        private static bool _isSubscribed = false;
        
        private AssetBundleMonitor()
        {
            EnsureSubscribed();
        }
        
        ~AssetBundleMonitor()
        {
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// 编辑器初始化时自动订阅事件（处理域重载）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // 域重载后重新订阅事件
            EnsureSubscribed();
            
            // 监听 Play Mode 状态变化，确保在进入/退出 Play Mode 时事件订阅依然有效
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 在进入 Play Mode 后重新确保订阅
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                EnsureSubscribed();
            }
        }
        
        /// <summary>
        /// 确保已订阅事件（防止域重载导致订阅丢失）
        /// </summary>
        private static void EnsureSubscribed()
        {
            if (_isSubscribed)
                return;
                
            AssetBundleLoader.OnBundleLoadStart += OnBundleLoadStartStatic;
            AssetBundleLoader.OnBundleLoadSuccess += OnBundleLoadSuccessStatic;
            AssetBundleLoader.OnBundleLoadFailed += OnBundleLoadFailedStatic;
            AssetBundleLoader.OnBundleUnload += OnBundleUnloadStatic;
            
            _isSubscribed = true;
        }
        
        private static void UnsubscribeFromEvents()
        {
            if (!_isSubscribed)
                return;
                
            AssetBundleLoader.OnBundleLoadStart -= OnBundleLoadStartStatic;
            AssetBundleLoader.OnBundleLoadSuccess -= OnBundleLoadSuccessStatic;
            AssetBundleLoader.OnBundleLoadFailed -= OnBundleLoadFailedStatic;
            AssetBundleLoader.OnBundleUnload -= OnBundleUnloadStatic;
            
            _isSubscribed = false;
        }
        
        public void StartRecording()
        {
            if (IsRecording)
            {
                Debug.LogWarning("[AssetBundle Monitor] Already recording");
                return;
            }
            
            CurrentSession = new MonitorSessionData
            {
                sessionStartTime = DateTime.Now,
                isRecording = true
            };
            
            _loadingBundles.Clear();
            _bundleReferenceCounts.Clear();
            
            Debug.Log("[AssetBundle Monitor] Started recording");
        }
        
        public void StopRecording()
        {
            if (!IsRecording)
            {
                Debug.LogWarning("[AssetBundle Monitor] Not recording");
                return;
            }
            
            CurrentSession.sessionEndTime = DateTime.Now;
            CurrentSession.isRecording = false;
            
            Debug.Log($"[AssetBundle Monitor] Stopped recording, total records: {CurrentSession.records.Count}");
        }
        
        public void ClearSession()
        {
            CurrentSession = null;
            _loadingBundles.Clear();
            _bundleReferenceCounts.Clear();
        }
        
        /// <summary>
        /// 静态回调：Bundle 开始加载（解决域重载问题）
        /// </summary>
        private static void OnBundleLoadStartStatic(BundleLoadEventArgs args)
        {
            Instance.OnBundleLoadStart(args);
        }
        
        /// <summary>
        /// 静态回调：Bundle 加载成功（解决域重载问题）
        /// </summary>
        private static void OnBundleLoadSuccessStatic(BundleLoadEventArgs args)
        {
            Instance.OnBundleLoadSuccess(args);
        }
        
        /// <summary>
        /// 静态回调：Bundle 加载失败（解决域重载问题）
        /// </summary>
        private static void OnBundleLoadFailedStatic(BundleLoadEventArgs args)
        {
            Instance.OnBundleLoadFailed(args);
        }
        
        /// <summary>
        /// 静态回调：Bundle 卸载（解决域重载问题）
        /// </summary>
        private static void OnBundleUnloadStatic(string bundleName)
        {
            Instance.OnBundleUnload(bundleName);
        }
        
        private void OnBundleLoadStart(BundleLoadEventArgs args)
        {
            if (!IsRecording)
                return;
            
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
        
        private void OnBundleLoadSuccess(BundleLoadEventArgs args)
        {
            if (!IsRecording)
                return;
            
            if (_loadingBundles.TryGetValue(args.BundleName, out var record))
            {
                record.loadEndTime = args.EndTime;
                record.loadDuration = (record.loadEndTime - record.loadStartTime).TotalMilliseconds;
                record.loadSuccess = true;
                record.bundleSize = args.BundleSize;
                
                CurrentSession.records.Add(record);
                _loadingBundles.Remove(args.BundleName);
            }
        }
        
        private void OnBundleLoadFailed(BundleLoadEventArgs args)
        {
            if (!IsRecording)
                return;
            
            if (_loadingBundles.TryGetValue(args.BundleName, out var record))
            {
                record.loadEndTime = args.EndTime;
                record.loadDuration = (record.loadEndTime - record.loadStartTime).TotalMilliseconds;
                record.loadSuccess = false;
                record.errorMessage = args.ErrorMessage;
                
                CurrentSession.records.Add(record);
                _loadingBundles.Remove(args.BundleName);
            }
        }
        
        private void OnBundleUnload(string bundleName)
        {
            if (_bundleReferenceCounts.ContainsKey(bundleName))
            {
                _bundleReferenceCounts[bundleName]--;
                if (_bundleReferenceCounts[bundleName] <= 0)
                    _bundleReferenceCounts.Remove(bundleName);
            }
        }
        
        public int GetBundleReferenceCount(string bundleName)
        {
            return _bundleReferenceCounts.TryGetValue(bundleName, out var count) ? count : 0;
        }
        
        public List<AssetBundleRecord> GetAllRecords()
        {
            return CurrentSession?.records ?? new List<AssetBundleRecord>();
        }
    }
}

