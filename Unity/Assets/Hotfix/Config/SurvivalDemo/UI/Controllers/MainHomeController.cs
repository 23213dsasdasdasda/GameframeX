using FairyGUI;
using GameframeX.Config;
using UnityEngine;

namespace GameframeX.UI
{
    public class MainHomeController : MonoBehaviour
    {
        private GComponent _mainUI;
        private Controller _bottomTabCtrl;

        void Start()
        {
            _mainUI = UIManager.Instance.OpenUI("Main", "MainHomePage", UILayer.Bottom);

            _bottomTabCtrl = _mainUI.GetController("c1");
            _bottomTabCtrl.onChanged.Add(OnTabChanged);

            UpdatePlayerStats();

            _mainUI.GetChild("btn_rank").onClick.Add(() => UIManager.Instance.OpenUI("Social", "RankPanel"));
            _mainUI.GetChild("btn_pass").onClick.Add(() => UIManager.Instance.OpenUI("Activity", "BPPanel"));

            UIDispatcher.Instance.StartLoginSequence();
        }

        private void OnTabChanged()
        {
            string pageName = _bottomTabCtrl.selectedPage;
            Debug.Log($"切换到：{pageName}");
            _mainUI.GetChild("group_battle_modes").visible = pageName == "Battle";
        }

        public void UpdatePlayerStats()
        {
            _mainUI.GetChild("txt_gold").text = "9999";
            _mainUI.GetChild("txt_diamond").text = "100";
            _mainUI.GetChild("txt_strength").text = "20/20";
            _mainUI.GetChild("exp_bar").asProgress.value = 50;
        }

        void InitSubSystems(GComponent mainUI)
        {
            var battleComp = mainUI.GetChild("group_battle").asCom;
            gameObject.AddComponent<BattleController>().BindView(battleComp);
        }
    }
}
