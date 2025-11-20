using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Data
{
    /// <summary>
    /// Build report data
    /// </summary>
    [Serializable]
    public class BuildReportData
    {
        /// <summary>
        /// Summary information (contains all build information)
        /// </summary>
        public SummaryInfo summary = new SummaryInfo();
        
        /// <summary>
        /// Bundle information list
        /// </summary>
        public List<BundleReportInfo> bundles = new List<BundleReportInfo>();
        
        /// <summary>
        /// Save report to file
        /// </summary>
        public void SaveReport(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonUtility.ToJson(this, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"[BuildReport] Build report saved: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildReport] Failed to save build report: {e.Message}");
            }
        }

        /// <summary>
        /// Load report from file
        /// </summary>
        public static BuildReportData LoadReport(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[BuildReport] Report file does not exist: {filePath}");
                    return null;
                }

                var json = File.ReadAllText(filePath);
                var report = JsonUtility.FromJson<BuildReportData>(json);
                Debug.Log($"[BuildReport] Build report loaded: {filePath}");
                return report;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildReport] Failed to load build report: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Bundle report information
    /// </summary>
    [Serializable]
    public class BundleReportInfo
    {
        /// <summary>
        /// Bundle name
        /// </summary>
        public string bundleName;
        
        /// <summary>
        /// Bundle hash
        /// </summary>
        public string bundleHash;
        
        /// <summary>
        /// Bundle file size (in bytes)
        /// </summary>
        public long bundleSize;
        
        /// <summary>
        /// Bundle file size (readable format)
        /// </summary>
        public string bundleSizeReadable;
        
        /// <summary>
        /// Bundle dependencies list
        /// </summary>
        public List<string> dependencies = new List<string>();
        
        /// <summary>
        /// Assets contained in the bundle
        /// </summary>
        public List<AssetReportInfo> assets = new List<AssetReportInfo>();
        
        /// <summary>
        /// Asset count
        /// </summary>
        public int assetCount;
    }

    /// <summary>
    /// Asset report information
    /// </summary>
    [Serializable]
    public class AssetReportInfo
    {
        /// <summary>
        /// Asset address
        /// </summary>
        public string address;
        
        /// <summary>
        /// Asset path
        /// </summary>
        public string assetPath;
        
        /// <summary>
        /// Asset GUID
        /// </summary>
        public string assetGuid;
        
        /// <summary>
        /// Asset tags
        /// </summary>
        public List<string> assetTags = new List<string>();
    }

    /// <summary>
    /// Summary information (contains all build-related information)
    /// </summary>
    [Serializable]
    public class SummaryInfo
    {
        /// <summary>
        /// Report version
        /// </summary>
        public string reportVersion = "1.0.0";
        
        /// <summary>
        /// Package name
        /// </summary>
        public string packageName;
        
        /// <summary>
        /// Build time
        /// </summary>
        public string buildTime;
        
        /// <summary>
        /// Build duration (in seconds)
        /// </summary>
        public double buildDuration;
        
        /// <summary>
        /// Build target platform
        /// </summary>
        public string buildTarget;
        
        /// <summary>
        /// Build mode
        /// </summary>
        public string buildMode;
        
        /// <summary>
        /// Compress mode
        /// </summary>
        public string compressMode;
        
        /// <summary>
        /// Encrypt rule
        /// </summary>
        public string encryptRule;
        
        /// <summary>
        /// Output path
        /// </summary>
        public string outputPath;
        
        /// <summary>
        /// Build status
        /// </summary>
        public string buildStatus = "Success";
        
        /// <summary>
        /// Total bundle count
        /// </summary>
        public int totalBundleCount;
        
        /// <summary>
        /// Total asset count
        /// </summary>
        public int totalAssetCount;
        
        /// <summary>
        /// Total file size (in bytes)
        /// </summary>
        public long totalSize;
        
        /// <summary>
        /// Total file size (readable format)
        /// </summary>
        public string totalSizeReadable;
        
        /// <summary>
        /// Largest bundle name
        /// </summary>
        public string largestBundle;
        
        /// <summary>
        /// Largest bundle size (in bytes)
        /// </summary>
        public long largestBundleSize;
        
        /// <summary>
        /// Largest bundle size (readable format)
        /// </summary>
        public string largestBundleSizeReadable;
    }
}

