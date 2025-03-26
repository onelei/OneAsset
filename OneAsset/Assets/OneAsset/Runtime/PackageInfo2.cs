using System.Collections.Generic;

namespace OneAsset.Runtime.Manifest
{
    
        // private Dictionary<string, AssetBundleRequestInfo> _activeRequests =
        //     new Dictionary<string, AssetBundleRequestInfo>();
        //
        // private HashSet<string> _loadingPaths = new HashSet<string>();
        //
        // public void Update()
        // {
        //     // 处理同步加载的完成回调
        //     var completedKeys = ListPool<string>.Get();
        //     foreach (var kvp in _activeRequests)
        //     {
        //         var requestInfo = kvp.Value;
        //
        //         if (requestInfo.IsSync)
        //         {
        //             completedKeys.Add(kvp.Key);
        //             requestInfo.InvokeSyncCallback();
        //         }
        //         else if (requestInfo.AsyncRequest.isDone)
        //         {
        //             completedKeys.Add(kvp.Key);
        //             requestInfo.InvokeAsyncCallback();
        //         }
        //     }
        //
        //     foreach (var key in completedKeys)
        //     {
        //         _activeRequests.Remove(key);
        //         _loadingPaths.Remove(key);
        //     }
        //
        //     ListPool<string>.Release(completedKeys);
        // }
        //
        // #region Sync Loading
        //
        // public void LoadSync(string path, Action<AssetBundle> onComplete)
        // {
        //     if (_loadingPaths.Contains(path))
        //     {
        //         return;
        //     }
        //
        //     _loadingPaths.Add(path);
        //     var requestInfo = new AssetBundleRequestInfo(path, onComplete, LoadSyncInternal(path));
        //     _activeRequests[path] = requestInfo;
        // }
        //
        // private AssetBundle LoadSyncInternal(string path)
        // {
        //     var assetBundle = AssetBundle.LoadFromFile(path);
        //     return assetBundle;
        // }
        //
        // #endregion
        //
        // #region Async Loading
        //
        // public async UniTask<AssetBundle> LoadAsync(string path, CancellationToken cancellationToken = default)
        // {
        //     if (_activeRequests.TryGetValue(path, out var existingRequest))
        //     {
        //         return await existingRequest.AsyncRequest.WithCancellation(cancellationToken);
        //     }
        //
        //     var request = AssetBundle.LoadFromFileAsync(path);
        //     var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        //     var requestInfo = new AssetBundleRequestInfo(path, request, cts);
        //     _activeRequests[path] = requestInfo;
        //     _loadingPaths.Add(path);
        //
        //     try
        //     {
        //         await request.WithCancellation(cts.Token);
        //         return request.assetBundle;
        //     }
        //     finally
        //     {
        //         _activeRequests.Remove(path);
        //         _loadingPaths.Remove(path);
        //         cts.Dispose();
        //     }
        // }
        //
        // #endregion
        //
        // #region Cancel Methods
        //
        // public void CancelLoad(string path)
        // {
        //     if (!_activeRequests.TryGetValue(path, out var requestInfo)) return;
        //     if (!requestInfo.IsSync)
        //     {
        //         requestInfo.CancellationTokenSource.Cancel();
        //     }
        //
        //     _activeRequests.Remove(path);
        //     _loadingPaths.Remove(path);
        //     requestInfo.Dispose();
        // }
        //
        // public void CancelAllLoads()
        // {
        //     foreach (var requestInfo in _activeRequests.Values)
        //     {
        //         requestInfo.Dispose();
        //     }
        //
        //     _activeRequests.Clear();
        //     _loadingPaths.Clear();
        // }
        //
        // #endregion
    
}