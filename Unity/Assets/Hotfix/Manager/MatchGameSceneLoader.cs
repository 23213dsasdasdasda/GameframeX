using System;
using Cysharp.Threading.Tasks;
using GameFrameX.Runtime;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Hotfix.Manager
{
    public static class MatchGameSceneLoader
    {
        private const string DefaultPackageName = "DefaultPackage";

        public static async UniTask<bool> LoadAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Log.Error("match scene name is empty.");
                return false;
            }

            try
            {
                if (await LoadByYooAssetAsync(sceneName))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Log.Warning($"load scene by yooasset failed : {sceneName}, exception : {exception}");
            }

            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (operation == null)
                {
                    Log.Error($"unity scene load operation is null : {sceneName}");
                    return false;
                }

                while (!operation.isDone)
                {
                    await UniTask.Yield();
                }

                Log.Info($"match scene loaded by unity scene manager : {sceneName}");
                return true;
            }
            catch (Exception exception)
            {
                Log.Error($"load scene by unity failed : {sceneName}, exception : {exception}");
                return false;
            }
        }

        private static async UniTask<bool> LoadByYooAssetAsync(string sceneName)
        {
            var package = YooAssets.GetPackage(DefaultPackageName);
            if (package == null)
            {
                Log.Warning($"yooasset package not found : {DefaultPackageName}");
                return false;
            }

            if (!package.CheckLocationValid(sceneName))
            {
                Log.Warning($"scene location is not collected by yooasset : {sceneName}");
                return false;
            }

            var handle = package.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Warning($"yooasset load scene failed : {sceneName}, error : {handle.LastError}");
                return false;
            }

            Log.Info($"match scene loaded by yooasset : {sceneName}");
            return true;
        }
    }
}
