using System;
using System.Collections.Generic;
using System.Net;
using GameFrameX.Event.Runtime;
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using GameFrameX.UI.FairyGUI.Runtime;
#endif
#if ENABLE_UI_UGUI
using GameFrameX.UI.UGUI.Runtime;
#endif
using Hotfix.Manager;
using Hotfix.Network;
using Hotfix.Proto;
using UnityEngine;

namespace Hotfix.UI
{
    public partial class UIPlayerList
    {
        public override void OnAwake()
        {
            UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Normal.Name);
            base.OnAwake();
        }

        List<PlayerInfo> playerList = new List<PlayerInfo>();

        private static INetworkChannel networkChannel;

        public override async void OnOpen(object userData)
        {
            base.OnOpen(userData);
#if ENABLE_UI_FAIRYGUI
            this.m_login_button.onClick.Set(OnLoginButtonClick);
            this.m_player_list.itemRenderer = ItemRenderer;
            this.m_player_list.onClickItem.Set(OnPlayerListItemClick);
#elif ENABLE_UI_UGUI
            m_right_Panel.gameObject.SetActive(false);
            m_right_Panel_login_button.onClick.Set(OnLoginButtonClick);
#endif
            playerList = AccountManager.Instance.PlayerList;
#if ENABLE_UI_UGUI
            var uiPlayerListItemAssetHandle = await GameApp.Asset.LoadAssetAsync<GameObject>(Utility.Asset.Path.GetUIPath($"{nameof(UILogin)}/{nameof(UIPlayerListItem)}"));
            foreach (var playerInfo in playerList)
            {
                var item = uiPlayerListItemAssetHandle.InstantiateSync(m_left_Panel_ScrollView_Viewport_Content);
                var uiPlayerListItem = item.GetComponent<UIPlayerListItem>();
                uiPlayerListItem.m_level_text.text = "当前等级:" + playerInfo.Level.ToString();
                uiPlayerListItem.m_name_text.text = playerInfo.Name;
                uiPlayerListItem.m_click_Button.onClick.Set(OnPlayerListItemClick, playerInfo);
                var assetHandle = await GameApp.Asset.LoadAssetAsync<Sprite>(Utility.Asset.Path.GetCategoryFilePath("Sprites", $"avatar/{PlayerManager.Instance.PlayerInfo.Avatar}"));
                uiPlayerListItem.m_icon.sprite = assetHandle.GetAssetObject<Sprite>();
            }
#endif
#if ENABLE_UI_FAIRYGUI
            this.m_player_list.DataList = new List<object>(playerList);
            this.m_player_list.numItems = playerList.Count;
#endif
        }

        private void OnLoginButtonClick()
        {
            Log.Error("[TRACE] UIPlayerList.OnLoginButtonClick");
            if (networkChannel != null && networkChannel.Connected)
            {
                Log.Error("[TRACE] UIPlayerList reuse connected channel -> Login()");
                Login();
                return;
            }

            if (networkChannel != null && GameApp.Network.HasNetworkChannel("network") && !networkChannel.Connected)
            {
                GameApp.Network.DestroyNetworkChannel("network");
            }

            networkChannel = GameApp.Network.CreateNetworkChannel("network", new DefaultNetworkChannelHelper());
            // 注册心跳消息
            DefaultPacketHeartBeatHandler packetSendHeaderHandler = new DefaultPacketHeartBeatHandler();
            networkChannel.RegisterHeartBeatHandler(packetSendHeaderHandler);
            var tcpUrl = GameApiUrl.BuildGameTcpUrl();
            Log.Info($"Connect game server: {tcpUrl}");
            Log.Error($"[TRACE] UIPlayerList connect tcpUrl={tcpUrl}");
            networkChannel.Connect(new Uri(tcpUrl));
            GameApp.Event.CheckSubscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnected);
            GameApp.Event.CheckSubscribe(NetworkClosedEventArgs.EventId, OnNetworkClosed);
        }

        private async void Login()
        {
            Log.Error($"[TRACE] UIPlayerList.Login send ReqPlayerLogin, selectedId={m_SelectedPlayerInfo?.Id}");
            ReqPlayerLogin reqPlayerLogin = new ReqPlayerLogin();
            reqPlayerLogin.UserName = m_SelectedPlayerInfo.Id.ToString();
            var respPlayerLogin = await GameApp.Network.GetNetworkChannel("network").Call<RespPlayerLogin>(reqPlayerLogin);
            Log.Error($"[TRACE] UIPlayerList.Login recv RespPlayerLogin, errorCode={respPlayerLogin?.ErrorCode}");
            PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;
            await GameApp.UI.OpenFullScreenAsync<UIMain>(Utility.Asset.Path.GetUIPath(nameof(UIMain)), UIGroupConstants.Floor);
            GameApp.UI.CloseUIForm(this);
            await BagManager.Instance.RequestGetBagInfo();
        }

        PlayerInfo m_SelectedPlayerInfo;
#if ENABLE_UI_FAIRYGUI
        private void OnPlayerListItemClick(EventContext context)
        {
            if ((context.data as GComponent).dataSource is PlayerInfo playerInfo)
            {
                m_SelectedPlayerInfo = playerInfo;

                this.m_selected_icon.icon = UIPackage.GetItemURL(FUIPackage.UICommonAvatar, playerInfo.Avatar.ToString());
                this.m_selected_name.text = playerInfo.Name;
                this.m_selected_level.text = "当前等级:" + playerInfo.Level;
                this.m_IsSelected.SetSelectedIndex(1);
            }
        }
#elif ENABLE_UI_UGUI
        private async void OnPlayerListItemClick(object userData)
        {
            m_SelectedPlayerInfo = (PlayerInfo)userData;
            var assetHandle = await GameApp.Asset.LoadAssetAsync<Sprite>(Utility.Asset.Path.GetCategoryFilePath("Sprites", $"avatar/{PlayerManager.Instance.PlayerInfo.Avatar}"));

            m_right_Panel_selected_icon.sprite = assetHandle.GetAssetObject<Sprite>();
            m_right_Panel_selected_name.text = m_SelectedPlayerInfo.Name;
            m_right_Panel_selected_level.text = "当前等级:" + m_SelectedPlayerInfo.Level;
            m_right_Panel.gameObject.SetActive(true);
        }
#endif
#if ENABLE_UI_FAIRYGUI
        private void ItemRenderer(int index, GObject item)
        {
            var playerInfo = playerList[index];
            var uiPlayerListItem = UIPlayerListItem.GetFormPool(item);
            uiPlayerListItem.m_level_text.text = "当前等级:" + playerInfo.Level.ToString();
            uiPlayerListItem.m_name_text.text = playerInfo.Name;
            uiPlayerListItem.m_icon.icon = UIPackage.GetItemURL(FUIPackage.UICommonAvatar, playerInfo.Avatar.ToString());
            item.data = playerInfo;
        }
#endif
        private static void OnNetworkClosed(object sender, GameEventArgs e)
        {
            Log.Info(nameof(OnNetworkClosed));
        }

        private void OnNetworkConnected(object sender, GameEventArgs e)
        {
            Log.Error("[TRACE] UIPlayerList.OnNetworkConnected -> Login()");
            Login();
            Log.Info(nameof(OnNetworkConnected));
        }
    }
}
