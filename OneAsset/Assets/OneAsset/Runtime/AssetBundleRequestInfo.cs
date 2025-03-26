// using System;
// using System.Threading;
// using UnityEngine;
//
// namespace OneAsset.Runtime
// {
//     public class AssetBundleRequestInfo : IDisposable
//     {
//         public string Path { get; private set; }
//
//         public bool IsSync => AsyncRequest == null;
//
//         // Sync Fields
//         private AssetBundle _syncResult;
//
//         private Action<AssetBundle> _syncCallback;
//
//         // Async Fields
//         public AssetBundleCreateRequest AsyncRequest { get; }
//         public CancellationTokenSource CancellationTokenSource { get; }
//
//         public AssetBundleRequestInfo(string path, Action<AssetBundle> callback, AssetBundle result)
//         {
//             Path = path;
//             _syncCallback = callback;
//             _syncResult = result;
//         }
//
//         public AssetBundleRequestInfo(string path, AssetBundleCreateRequest request, CancellationTokenSource cancellationTokenSource)
//         {
//             Path = path;
//             AsyncRequest = request;
//             CancellationTokenSource = cancellationTokenSource;
//         }
//
//         public void InvokeSyncCallback()
//         {
//             _syncCallback?.Invoke(_syncResult);
//             Dispose();
//         }
//
//         public void InvokeAsyncCallback()
//         {
//             Dispose();
//         }
//
//         public void Dispose()
//         {
//             CancellationTokenSource?.Dispose();
//             _syncCallback = null;
//             _syncResult = null;
//         }
//     }
// }