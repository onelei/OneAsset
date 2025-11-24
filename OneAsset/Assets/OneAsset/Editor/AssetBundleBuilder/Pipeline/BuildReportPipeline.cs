using System;
using System.Diagnostics;
using System.IO;
using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Runtime;
using OneAsset.Runtime.Manifest;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    /// <summary>
    /// Build report pipeline for generating and saving build reports
    /// </summary>
    public class BuildReportPipeline : IPipeline
    {
        private Stopwatch _stopwatch;
        
        public void Run(PipelineData pipelineData)
        {
            // Start timing
            if (_stopwatch == null)
            {
                _stopwatch = new Stopwatch();
            }
            _stopwatch.Start();
            
            var builderPackage = pipelineData.AssetBundleBuilderPackage;
            if (builderPackage == null)
            {
                OneAssetLogger.LogWarning("[BuildReport] AssetBundleBuilderPackage is null, skip build report.");
                return;
            }

            var manifest = pipelineData.Manifest;
            if (manifest == null)
            {
                OneAssetLogger.LogWarning("[BuildReport] AssetBundleManifest is null, skip build report.");
                return;
            }

            var customManifest = pipelineData.CustomVirtualManifest;
            if (customManifest == null)
            {
                OneAssetLogger.LogWarning("[BuildReport] CustomVirtualManifest is null, skip build report.");
                return;
            }

            try
            {
                // Create build report
                var buildReport = CreateBuildReport(builderPackage, manifest, customManifest);
                
                // Save report
                SaveBuildReport(buildReport, builderPackage);
                
                // Log summary information
                LogBuildReportSummary(buildReport);
            }
            catch (Exception e)
            {
                OneAssetLogger.LogError($"[BuildReport] Generate build report failed: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// Create build report
        /// </summary>
        private BuildReportData CreateBuildReport(AssetBundleBuilderPackage builderPackage, 
            UnityEngine.AssetBundleManifest manifest, VirtualManifest customManifest)
        {
            var buildReport = new BuildReportData();
            
            // Fill build information in summary
            buildReport.summary.packageName = builderPackage.packageName;
            buildReport.summary.buildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            buildReport.summary.buildDuration = _stopwatch.Elapsed.TotalSeconds;
            buildReport.summary.buildTarget = builderPackage.GetBuildTarget().ToString();
            buildReport.summary.buildMode = builderPackage.buildMode.ToString();
            buildReport.summary.compressMode = builderPackage.compressMode.ToString();
            buildReport.summary.encryptRule = builderPackage.encryptRule;
            buildReport.summary.outputPath = builderPackage.GetOriginOutputPath();

            var outputPath = builderPackage.GetOriginOutputPath();
            long totalSize = 0;
            long largestSize = 0;
            string largestBundle = string.Empty;

            // Iterate through all bundles
            foreach (var package in customManifest.packages)
            {
                if (package.name != builderPackage.packageName)
                    continue;

                foreach (var group in package.groups)
                {
                    foreach (var bundleInfo in group.bundles)
                    {
                        var bundleName = bundleInfo.name;
                        var bundlePath = Path.Combine(outputPath, bundleName);

                        // Get bundle file size
                        long bundleSize = 0;
                        if (File.Exists(bundlePath))
                        {
                            var fileInfo = new FileInfo(bundlePath);
                            bundleSize = fileInfo.Length;
                        }

                        // Create bundle report information
                        var bundleReport = new BundleReportInfo
                        {
                            bundleName = bundleName,
                            bundleHash = bundleInfo.hash,
                            bundleSize = bundleSize,
                            bundleSizeReadable = GetReadableFileSize(bundleSize),
                            assetCount = bundleInfo.assets.Count
                        };

                        // Add dependencies
                        bundleReport.dependencies.AddRange(bundleInfo.depends);

                        // Add asset information
                        foreach (var assetInfo in bundleInfo.assets)
                        {
                            var assetReport = new AssetReportInfo
                            {
                                address = assetInfo.address,
                                assetPath = assetInfo.assetPath,
                                assetGuid = assetInfo.assetGuid,
                                assetTags = assetInfo.assetTags ?? new System.Collections.Generic.List<string>()
                            };
                            bundleReport.assets.Add(assetReport);
                        }

                        buildReport.bundles.Add(bundleReport);

                        // Update statistics
                        totalSize += bundleSize;
                        if (bundleSize > largestSize)
                        {
                            largestSize = bundleSize;
                            largestBundle = bundleName;
                        }
                    }
                }
            }

            // Fill summary information
            buildReport.summary.totalBundleCount = buildReport.bundles.Count;
            buildReport.summary.totalAssetCount = GetTotalAssetCount(buildReport);
            buildReport.summary.totalSize = totalSize;
            buildReport.summary.totalSizeReadable = GetReadableFileSize(totalSize);
            buildReport.summary.largestBundle = largestBundle;
            buildReport.summary.largestBundleSize = largestSize;
            buildReport.summary.largestBundleSizeReadable = GetReadableFileSize(largestSize);
            buildReport.summary.buildStatus = "Success";

            return buildReport;
        }

        /// <summary>
        /// Save build report
        /// </summary>
        private void SaveBuildReport(BuildReportData buildReport, AssetBundleBuilderPackage builderPackage)
        {
            var outputPath = builderPackage.GetOriginOutputPath();
            var reportFileName = $"BuildReport_{buildReport.summary.packageName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var reportPath = Path.Combine(outputPath, reportFileName);

            buildReport.SaveReport(reportPath);

            var finalOutputPath = builderPackage.GetFinalOutputPath();
            buildReport.SaveReport(Path.Combine(finalOutputPath, reportFileName));
            
            // Also save a copy of the latest report (for quick access)
            buildReport.SaveReport(Path.Combine(finalOutputPath, "BuildReport_Latest.json"));
        }

        /// <summary>
        /// Log build report summary to console
        /// </summary>
        private void LogBuildReportSummary(BuildReportData buildReport)
        {
            var summary = buildReport.summary;
            OneAssetLogger.Log("========== Build Report Summary ==========");
            OneAssetLogger.Log($"Package Name: {summary.packageName}");
            OneAssetLogger.Log($"Build Time: {summary.buildTime}");
            OneAssetLogger.Log($"Build Duration: {summary.buildDuration:F2} seconds");
            OneAssetLogger.Log($"Build Target: {summary.buildTarget}");
            OneAssetLogger.Log($"Build Mode: {summary.buildMode}");
            OneAssetLogger.Log($"Compress Mode: {summary.compressMode}");
            OneAssetLogger.Log($"Encrypt Rule: {summary.encryptRule}");
            OneAssetLogger.Log($"------------------------------------------");
            OneAssetLogger.Log($"Total Bundles: {summary.totalBundleCount}");
            OneAssetLogger.Log($"Total Assets: {summary.totalAssetCount}");
            OneAssetLogger.Log($"Total Size: {summary.totalSizeReadable} ({summary.totalSize} bytes)");
            OneAssetLogger.Log($"Largest Bundle: {summary.largestBundle} ({summary.largestBundleSizeReadable})");
            OneAssetLogger.Log($"Build Status: {summary.buildStatus}");
            OneAssetLogger.Log("==========================================");
        }

        /// <summary>
        /// Get total asset count
        /// </summary>
        private int GetTotalAssetCount(BuildReportData buildReport)
        {
            int count = 0;
            foreach (var bundle in buildReport.bundles)
            {
                count += bundle.assetCount;
            }
            return count;
        }

        /// <summary>
        /// Get readable file size
        /// </summary>
        private string GetReadableFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}

