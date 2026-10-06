using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFrameX.Runtime;
using Hotfix.Config.Tables.survival;

namespace Hotfix.UI
{
    internal sealed class BattleLevelUpOptionPopup : IDisposable
    {
        public async UniTask<OptionConfig> ShowAsync(IReadOnlyList<OptionConfig> options)
        {
            Log.Info($"[LevelUpPopup] Forward show to UI3Pick1 count={options?.Count ?? 0}");
            return await UI3Pick1.ShowSelectionAsync(options);
        }

        public void Close()
        {
            Log.Info("[LevelUpPopup] Close forwarded to UI3Pick1.");
            GameApp.UI.CloseUIForm<UI3Pick1>();
        }

        public void Dispose()
        {
            Log.Info("[LevelUpPopup] Dispose.");
        }
    }
}
