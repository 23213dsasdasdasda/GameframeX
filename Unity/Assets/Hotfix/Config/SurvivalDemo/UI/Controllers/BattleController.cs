using FairyGUI;
using UnityEngine;
using GameframeX.Config;

namespace GameframeX.UI
{
    public class BattleController : MonoBehaviour
    {
        private GComponent _view;
        private GButton _btnSingle;
        private GButton _btnTeam;

        public void BindView(GComponent view)
        {
            _view = view;
            _btnSingle = _view.GetChild("btn_single").asButton;
            _btnTeam = _view.GetChild("btn_team").asButton;

            _btnSingle.onClick.Add(() => OnClickEnter(1)); // 1为单人模式
            _btnTeam.onClick.Add(() => OnClickEnter(2));   // 2为组队模式
        }

        private void OnClickEnter(int mode)
        {
            // 1. 先判断体力 (假设当前体力存在某个全局变量里)
            if (PlayerGlobalConfig.CurrentStrength < 5)
            {
                NoticeDispatcher.DispatchNotify("StrengthNotEnough");
                return;
            }

            // 2. 逻辑分发
            Debug.Log($"进入模式: {mode}, 准备加载波次: {GameConfigBus.WaveLib.Get(1).WaveName}");
            
            // 3. 实际切换场景或通知服务端
            // SceneManager.LoadScene("BattleMap");
        }
    }
}