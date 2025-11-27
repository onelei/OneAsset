using System;
using System.Collections.Generic;

namespace OneAsset.Runtime.Monitor
{
    /// <summary>
    /// Bundle load event data
    /// </summary>
    public class BundleLoadEventArgs
    {
        public string BundleName { get; set; }
        public string PackageName { get; set; }
        public string AssetAddress { get; set; }
        public string AssetPath { get; set; }
        public List<string> Dependencies { get; set; }
        public bool IsAsync { get; set; }
        public long BundleSize { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    /// <summary>
    /// AssetBundle load record
    /// </summary>
    [Serializable]
    public class AssetBundleRecord
    {
        public string bundleName;
        public string packageName;
        public string sceneName;
        public string assetAddress;
        public string assetPath;
        public DateTime loadStartTime;
        public DateTime loadEndTime;
        public double loadDuration; // milliseconds
        public bool loadSuccess;
        public string errorMessage;
        public int referenceCount;
        public List<string> dependencies = new List<string>();
        public long bundleSize; // bytes
        public bool isAsync;
        public string loadType;
        public int frameIndex;

        public string GetLoadDurationReadable()
        {
            if (loadDuration < 1000)
                return $"{loadDuration:F2}ms";
            else
                return $"{loadDuration / 1000.0:F2}s";
        }

        public string GetBundleSizeReadable()
        {
            if (bundleSize < 1024)
                return $"{bundleSize}B";
            else if (bundleSize < 1024 * 1024)
                return $"{bundleSize / 1024.0:F2}KB";
            else
                return $"{bundleSize / (1024.0 * 1024.0):F2}MB";
        }
    }

    /// <summary>
    /// Profiler frame data
    /// </summary>
    [Serializable]
    public class ProfilerFrameData
    {
        public int frameIndex;
        public int totalBundleCount;
        public int loadedCount;
        public int unloadedCount;
    }

    /// <summary>
    /// Monitor session data
    /// </summary>
    [Serializable]
    public class MonitorSessionData
    {
        public DateTime sessionStartTime;
        public DateTime sessionEndTime;
        public bool isRecording;
        public List<AssetBundleRecord> records = new List<AssetBundleRecord>();
        public List<ProfilerFrameData> profilerData = new List<ProfilerFrameData>();

        public double GetSessionDuration()
        {
            if (isRecording)
                return (DateTime.Now - sessionStartTime).TotalSeconds;
            return (sessionEndTime - sessionStartTime).TotalSeconds;
        }

        public int GetSuccessCount()
        {
            int count = 0;
            foreach (var record in records)
            {
                if (record.loadSuccess)
                    count++;
            }

            return count;
        }

        public int GetFailedCount()
        {
            return records.Count - GetSuccessCount();
        }

        public long GetTotalLoadedSize()
        {
            long total = 0;
            foreach (var record in records)
            {
                if (record.loadSuccess)
                    total += record.bundleSize;
            }

            return total;
        }
    }

    /// <summary>
    /// Bundle loading monitor - responsible for tracking and reporting bundle load events
    /// Separates monitoring concerns from loading logic
    /// </summary>
    public static class AssetBundleMonitor
    {
        // Events for monitoring
        public static event Action<BundleLoadEventArgs> OnBundleLoadStart;
        public static event Action<BundleLoadEventArgs> OnBundleLoadSuccess;
        public static event Action<BundleLoadEventArgs> OnBundleLoadFailed;
        public static event Action<string> OnBundleUnload;

        /// <summary>
        /// Record the start of a bundle load operation
        /// </summary>
        public static void RecordLoadStart(string bundleName, string packageName, string assetAddress,
            string assetPath, List<string> dependencies, bool isAsync)
        {
            var args = new BundleLoadEventArgs
            {
                BundleName = bundleName,
                PackageName = packageName,
                AssetAddress = assetAddress,
                AssetPath = assetPath,
                Dependencies = dependencies,
                IsAsync = isAsync,
                StartTime = DateTime.Now
            };
            OnBundleLoadStart?.Invoke(args);
        }

        /// <summary>
        /// Record a successful bundle load
        /// </summary>
        public static void RecordLoadSuccess(string bundleName, string packageName, string assetAddress,
            string assetPath, List<string> dependencies, bool isAsync, DateTime startTime, string bundlePath)
        {
            // Record file size
            long bundleSize = 0;
            try
            {
                var fileInfo = new System.IO.FileInfo(bundlePath);
                bundleSize = fileInfo.Length;
            }
            catch
            {
                // Ignore file info errors
            }

            var args = new BundleLoadEventArgs
            {
                BundleName = bundleName,
                PackageName = packageName,
                AssetAddress = assetAddress,
                AssetPath = assetPath,
                Dependencies = dependencies,
                IsAsync = isAsync,
                StartTime = startTime,
                EndTime = DateTime.Now,
                BundleSize = bundleSize
            };
            OnBundleLoadSuccess?.Invoke(args);
        }

        /// <summary>
        /// Record a failed bundle load
        /// </summary>
        public static void RecordLoadFailed(string bundleName, string packageName, string assetAddress,
            string assetPath, List<string> dependencies, bool isAsync, DateTime startTime, string errorMessage)
        {
            var args = new BundleLoadEventArgs
            {
                BundleName = bundleName,
                PackageName = packageName,
                AssetAddress = assetAddress,
                AssetPath = assetPath,
                Dependencies = dependencies,
                IsAsync = isAsync,
                StartTime = startTime,
                EndTime = DateTime.Now,
                ErrorMessage = errorMessage
            };
            OnBundleLoadFailed?.Invoke(args);
        }

        /// <summary>
        /// Record a bundle unload operation
        /// </summary>
        public static void RecordUnload(string bundleName)
        {
            OnBundleUnload?.Invoke(bundleName);
        }
    }
}