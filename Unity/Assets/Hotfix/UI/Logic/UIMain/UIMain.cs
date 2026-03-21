using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using GameFrameX.UI.FairyGUI.Runtime;
#endif
#if ENABLE_UI_UGUI
using GameFrameX.UI.UGUI.Runtime;
#endif
using UnityEngine;
using Hotfix.Manager;
using Hotfix.Proto;

namespace Hotfix.UI
{
    public partial class UIMain
    {
        public override void OnAwake()
        {
            UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Normal.Name);
            base.OnAwake();
        }

        public override async void OnOpen(object userData)
        {
            base.OnOpen(userData);

            var playerInfo = PlayerManager.Instance?.PlayerInfo;
            if (playerInfo == null)
            {
                Log.Error("打开UIMain失败：PlayerManager.PlayerInfo 为空。");
                return;
            }

#if ENABLE_UI_UGUI
            if (m_player_icon == null || m_player_name == null || m_player_level == null)
            {
                Log.Error("打开UIMain失败：UGUI 节点绑定为空，请检查 UIMain 预制体与生成代码是否一致。");
                return;
            }

            var assetHandle = await GameApp.Asset.LoadAssetAsync<Sprite>(Utility.Asset.Path.GetCategoryFilePath("Sprites", $"avatar/{playerInfo.Avatar}"));
            m_player_icon.sprite = assetHandle.GetAssetObject<Sprite>();
#elif ENABLE_UI_FAIRYGUI
            if (m_player_icon == null || m_player_name == null || m_player_level == null)
            {
                Log.Error("打开UIMain失败：FairyGUI 节点绑定为空，请检查 UIMain 组件子节点名称。");
                return;
            }

            m_player_icon.icon = UIPackage.GetItemURL(FUIPackage.UICommonAvatar, playerInfo.Avatar.ToString());
#endif
            m_player_name.text = playerInfo.Name ?? string.Empty;
            m_player_level.text = "当前等级:" + playerInfo.Level;

            if (m_bag_button != null)
            {
                m_bag_button.onClick.Set(OnBagBtnClick);
            }

            if (m_user_info_button != null)
            {
                m_user_info_button.onClick.Set(OnUserInfoBtnClick);
            }

            if (m_not_rpc_button != null)
            {
                m_not_rpc_button.onClick.Set(OnnotrpBtnClickBtnClick);
            }
        }

        private void OnnotrpBtnClickBtnClick()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("当前为离线单机模式，无法请求服务器。");
                return;
            }

            ReqPlayerPet reqPlayerPet = new ReqPlayerPet();
            reqPlayerPet.PlayerId = PlayerManager.Instance.PlayerInfo.Id;
            GameApp.Network.GetNetworkChannel("network").Send(reqPlayerPet);
        }

        private async void OnUserInfoBtnClick()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("当前为离线单机模式，无法请求玩家信息。");
                return;
            }

            ReqPlayerInfo reqPlayerInfo = new ReqPlayerInfo();
            reqPlayerInfo.PlayerId = PlayerManager.Instance.PlayerInfo.Id;
            RespPlayerInfo respPlayerInfo = await GameApp.Network.GetNetworkChannel("network").Call<RespPlayerInfo>(reqPlayerInfo);
        }

        private async void OnBagBtnClick()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("当前为离线单机模式，背包联网功能不可用。");
                return;
            }

#if ENABLE_UI_UGUI
            ReqBagInfo reqBagInfo = new ReqBagInfo();
            var respBagInfo = await GameApp.Network.GetNetworkChannel("network").Call<RespBagInfo>(reqBagInfo);
            Log.Debug(respBagInfo);
#elif ENABLE_UI_FAIRYGUI
            await GameApp.UI.OpenAsync<UIBag>(UIGroupConstants.Window);
#endif
        }

    }
}
