using System;
using Cysharp.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace Unity.Startup.Procedure
{
    public static class LauncherUIHandler
    {
        private static UILauncher _ui;
        private static bool _isClosing;
        private static CanvasGroup _canvasGroup;

        public static async UniTask StartAsync(float fadeInSeconds = 0.8f)
        {
            _ui = await GameApp.UI.OpenFullScreenAsync<UILauncher>("UI/UILauncher", UIGroupConstants.Loading);
            SetUiAlpha(0f);
            await FadeUiAlphaAsync(0f, 1f, fadeInSeconds);

            GameApp.Event.CheckSubscribe(AssetDownloadProgressUpdateEventArgs.EventId, SetProgressUpdate);
        }

        public static void Dispose()
        {
            GameApp.UI.CloseUIForm<UILauncher>();
            _ui = null;
            _isClosing = false;
            _canvasGroup = null;
        }

        public static async UniTask DisposeWithFadeAsync(float holdSeconds = 3f, float fadeSeconds = 1f)
        {
            if (_isClosing || _ui == null)
            {
                return;
            }

            _isClosing = true;

            if (holdSeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), ignoreTimeScale: true);
            }

            if (fadeSeconds > 0f)
            {
                await FadeUiAlphaAsync(1f, 0f, fadeSeconds);
            }

            Dispose();
        }

        private static async UniTask FadeUiAlphaAsync(float from, float to, float duration)
        {
            if (_ui == null)
            {
                return;
            }

            SetUiAlpha(from);
            if (duration <= 0f)
            {
                SetUiAlpha(to);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration && _ui != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetUiAlpha(Mathf.Lerp(from, to, t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (_ui != null)
            {
                SetUiAlpha(to);
            }
        }

        private static void SetUiAlpha(float alpha)
        {
#if ENABLE_UI_FAIRYGUI
            if (_ui != null && _ui.self != null)
            {
                _ui.self.alpha = alpha;
            }
#else
            if (_ui == null || _ui.gameObject == null)
            {
                return;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = _ui.gameObject.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _ui.gameObject.AddComponent<CanvasGroup>();
                }
            }

            _canvasGroup.alpha = alpha;
#endif
        }

        public static void SetTipText(string text)
        {
            _ui.m_TipText.text = text;
        }

        public static void SetProgressUpdateFinish()
        {
#if ENABLE_UI_FAIRYGUI
            _ui.m_IsDownload.SetSelectedIndex(0);
#endif
        }

        public static void SetProgressUpdate(object sender, GameEventArgs gameEventArgs)
        {
#if ENABLE_UI_FAIRYGUI
            _ui.m_IsDownload.SetSelectedIndex(1);
#endif
            var message = (AssetDownloadProgressUpdateEventArgs)gameEventArgs;
            float progress = message.CurrentDownloadSizeBytes / (message.TotalDownloadSizeBytes * 1f);
            string currentSizeMb = Utility.File.GetBytesSize(message.CurrentDownloadSizeBytes);
            string totalSizeMb = Utility.File.GetBytesSize(message.TotalDownloadSizeBytes);
            _ui.m_ProgressBar.value = progress * 100;
            _ui.m_TipText.text = $"Downloading {currentSizeMb}/{totalSizeMb}";
        }
    }
}
