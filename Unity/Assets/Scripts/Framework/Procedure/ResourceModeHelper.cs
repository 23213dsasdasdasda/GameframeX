using GameFrameX.Asset.Runtime;
using GameFrameX;
using GameFrameX.Runtime;
using YooAsset;

namespace Unity.Startup.Procedure
{
    public static class ResourceModeHelper
    {
        // Development default: player builds keep online networking but use built-in resources.
        public static bool UseBuiltInResourcesForOnlinePlay =>
#if UNITY_EDITOR
            false;
#else
            GameApp.Asset.GamePlayMode == EPlayMode.HostPlayMode;
#endif

        public static void ApplyBuiltInOnlinePlayMode()
        {
            if (!UseBuiltInResourcesForOnlinePlay)
            {
                return;
            }

            if (GameApp.Asset.GamePlayMode != EPlayMode.OfflinePlayMode)
            {
                GameApp.Asset.GamePlayMode = EPlayMode.OfflinePlayMode;
            }

            var assetManager = GameFrameworkEntry.GetModule<IAssetManager>();
            assetManager?.SetPlayMode(EPlayMode.OfflinePlayMode);
        }
    }
}
