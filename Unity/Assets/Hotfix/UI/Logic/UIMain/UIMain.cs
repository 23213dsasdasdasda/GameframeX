// ==========================================================================================
//  GameFrameX 组织及其衍生项目的版权、商标、专利及其他相关权利
//  GameFrameX organization and its derivative projects' copyrights, trademarks, patents, and related rights
//  均受中华人民共和国及相关国际法律法规保护。
//  are protected by the laws of the People's Republic of China and relevant international regulations.
//  
//  使用本项目须严格遵守相应法律法规及开源许可证之规定。
//  Usage of this project must strictly comply with applicable laws, regulations, and open-source licenses.
//  
//  本项目采用 MIT 许可证与 Apache License 2.0 双许可证分发，
//  This project is dual-licensed under the MIT License and Apache License 2.0,
//  完整许可证文本请参见源代码根目录下的 LICENSE 文件。
//  please refer to the LICENSE file in the root directory of the source code for the full license text.
//  
//  禁止利用本项目实施任何危害国家安全、破坏社会秩序、
//  It is prohibited to use this project to engage in any activities that endanger national security, disrupt social order,
//  侵犯他人合法权益等法律法规所禁止的行为！
//  or infringe upon the legitimate rights and interests of others, as prohibited by laws and regulations!
//  因基于本项目二次开发所产生的一切法律纠纷与责任，
//  Any legal disputes and liabilities arising from secondary development based on this project
//  本项目组织与贡献者概不承担。
//  shall be borne solely by the developer; the project organization and contributors assume no responsibility.
//  
//  GitHub 仓库：https://github.com/GameFrameX
//  GitHub Repository: https://github.com/GameFrameX
//  Gitee  仓库：https://gitee.com/GameFrameX
//  Gitee Repository:  https://gitee.com/GameFrameX
//  官方文档：https://gameframex.doc.alianblank.com/
//  Official Documentation: https://gameframex.doc.alianblank.com/
// ==========================================================================================
﻿using System.Text;
using Cysharp.Threading.Tasks;
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
using Hotfix.Events;

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
                Log.Error("open UIMain failed: PlayerManager.PlayerInfo is null.");
                return;
            }

#if ENABLE_UI_UGUI
            if (m_player_icon == null || m_player_name == null || m_player_level == null)
            {
                Log.Error("open UIMain failed: UGUI binding is null.");
                return;
            }

            var assetHandle = await GameApp.Asset.LoadAssetAsync<Sprite>(Utility.Asset.Path.GetCategoryFilePath("Sprites", $"avatar/{playerInfo.Avatar}"));
            m_player_icon.sprite = assetHandle.GetAssetObject<Sprite>();
#elif ENABLE_UI_FAIRYGUI
            if (m_player_icon == null || m_player_name == null || m_player_level == null)
            {
                Log.Error("open UIMain failed: FairyGUI binding is null.");
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

#if ENABLE_UI_FAIRYGUI
            if (m_user_info_button != null)
            {
                m_user_info_button.onClick.Set(OnMatchButtonClick);
            }

            if (m_not_rpc_button != null)
            {
                m_not_rpc_button.onClick.Set(OnReadyButtonClick);
            }

            GameApp.Event.CheckSubscribe(MatchChangedEventArgs.EventId, OnMatchChanged);
#endif

            RefreshMatchView();
        }

        private void OnMatchChanged(object sender, GameFrameX.Event.Runtime.GameEventArgs e)
        {
            RefreshMatchView();
        }

        private void OnnotrpBtnClickBtnClick()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("offline mode does not support pet request.");
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
                Log.Warning("offline mode does not support player info request.");
                return;
            }

            ReqPlayerInfo reqPlayerInfo = new ReqPlayerInfo();
            reqPlayerInfo.PlayerId = PlayerManager.Instance.PlayerInfo.Id;
            _ = await GameApp.Network.GetNetworkChannel("network").Call<RespPlayerInfo>(reqPlayerInfo);
        }

        private async void OnBagBtnClick()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("offline mode does not support online bag features.");
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

#if ENABLE_UI_FAIRYGUI
        private void OnMatchButtonClick()
        {
            ToggleMatchAsync().Forget();
        }

        private void OnReadyButtonClick()
        {
            ToggleRoomReadyAsync().Forget();
        }

        private async UniTaskVoid ToggleMatchAsync()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("offline mode does not support matchmaking.");
                return;
            }

            if (MatchManager.Instance.CurrentRoomId > 0)
            {
                Log.Info("already in a match room.");
                RefreshMatchView();
                return;
            }

            if (!MatchManager.Instance.IsMatching)
            {
                var response = await MatchManager.Instance.RequestStartMatch();
                if (response == null || response.ErrorCode != 0)
                {
                    Log.Error($"start match failed, errorCode={response?.ErrorCode ?? -1}");
                    return;
                }
            }
            else
            {
                var response = await MatchManager.Instance.RequestCancelMatch();
                if (response == null || response.ErrorCode != 0)
                {
                    Log.Error($"cancel match failed, errorCode={response?.ErrorCode ?? -1}");
                    return;
                }
            }

            RefreshMatchView();
        }

        private async UniTaskVoid ToggleRoomReadyAsync()
        {
            if (AccountManager.Instance.IsOfflineMode)
            {
                Log.Warning("offline mode does not support room ready.");
                return;
            }

            if (MatchManager.Instance.CurrentRoomId <= 0)
            {
                Log.Warning("not in a match room.");
                RefreshMatchView();
                return;
            }

            var response = await MatchManager.Instance.RequestRoomReady(!MatchManager.Instance.IsReady);
            if (response == null || response.ErrorCode != 0)
            {
                Log.Error($"room ready failed, errorCode={response?.ErrorCode ?? -1}");
                return;
            }

            RefreshMatchView();
        }
#endif

        private void RefreshMatchView()
        {
#if ENABLE_UI_FAIRYGUI
            if (m_user_info_button != null)
            {
                m_user_info_button.title = MatchManager.Instance.IsMatching ? "取消匹配" : "开始匹配";
            }

            if (m_not_rpc_button != null)
            {
                m_not_rpc_button.title = MatchManager.Instance.CurrentRoomId > 0
                    ? (MatchManager.Instance.IsReady ? "取消准备" : "准备")
                    : "未入房";
            }

            if (m_player_rank != null)
            {
                m_player_rank.text = MatchManager.Instance.CurrentRoomId > 0
                    ? $"房间:{MatchManager.Instance.CurrentRoomId}"
                    : (MatchManager.Instance.IsMatching ? "匹配中..." : "大厅中");
            }

            if (m_player_fjlb != null)
            {
                var sb = new StringBuilder();
                var roomPlayers = MatchManager.Instance.CurrentRoomPlayers;
                if (roomPlayers == null || roomPlayers.Count == 0)
                {
                    sb.Append("房间玩家: 无");
                }
                else
                {
                    sb.Append("房间玩家:");
                    for (var i = 0; i < roomPlayers.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(" | ");
                        }

                        var player = roomPlayers[i];
                        sb.Append(player.Name);
                        sb.Append(player.IsReady ? "(已准备)" : "(未准备)");
                    }
                }

                m_player_fjlb.text = sb.ToString();
            }
#endif
        }
    }
}
