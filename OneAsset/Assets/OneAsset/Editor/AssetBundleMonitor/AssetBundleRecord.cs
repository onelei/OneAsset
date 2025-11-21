using System;
using System.Collections.Generic;

namespace OneAsset.Editor.AssetBundleMonitor
{
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
    /// Monitor session data
    /// </summary>
    [Serializable]
    public class MonitorSessionData
    {
        public DateTime sessionStartTime;
        public DateTime sessionEndTime;
        public bool isRecording;
        public List<AssetBundleRecord> records = new List<AssetBundleRecord>();
        
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
}

