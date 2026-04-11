using System;
using System.Collections.Generic;
using GameFrameX;
using GameFrameX.GlobalConfig.Runtime;
#if ENABLE_UI_FAIRYGUI
using GameFrameX.UI.FairyGUI.Runtime;
#endif
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Manager;
using Hotfix.Network;
#if ENABLE_UI_UGUI
using GameFrameX.UI.UGUI.Runtime;
#endif
using Hotfix.Proto;

namespace Hotfix.UI
{
    public partial class UIPlayerCreate
    {
        private ReqPlayerCreate req;
        private bool _isOfflineMode;
        private RespLogin _respLogin;
        private string _steamId;
        private string _steamTicket;

        public override void OnAwake()
        {
            UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Normal.Name);
            base.OnAwake();
        }

        public override void OnOpen(object userData)
        {
            req = new ReqPlayerCreate();
            base.OnOpen(userData);

            this.m_enter.onClick.Set(OnCreateButtonClick);

            if (userData is LoginFlowData flowData)
            {
                _isOfflineMode = flowData.IsOfflineMode;
                _respLogin = flowData.RespLogin;
                _steamId = flowData.SteamId;
                _steamTicket = flowData.SdkToken;
                if (!_isOfflineMode && flowData.RespLogin != null)
                {
                    req.Id = flowData.RespLogin.Id;
                }

                return;
            }

            RespLogin respLogin = userData as RespLogin;
            if (respLogin != null)
            {
                _respLogin = respLogin;
                req.Id = respLogin.Id;
            }
        }

        private async void OnCreateButtonClick()
        {
            if (m_UserName.text.IsNullOrWhiteSpace())
            {
                m_ErrorText.text = "角色名不能为空";
                return;
            }

            req.Name = m_UserName.text;

            if (_isOfflineMode)
            {
                var localPlayerInfo = new PlayerInfo
                {
                    Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Name = req.Name,
                    Level = 1,
                    Avatar = 0,
                    State = 0,
                    CurrentExp = 0
                };

                PlayerManager.Instance.PlayerInfo = localPlayerInfo;
                AccountManager.Instance.PlayerList = new List<PlayerInfo> { localPlayerInfo };

                await GameApp.UI.OpenFullScreenAsync<UIMain>(Utility.Asset.Path.GetUIPath(nameof(UIMain)), UIGroupConstants.Floor);
                GameApp.UI.CloseUIForm(this);
                return;
            }

            #region 创建角色

            var respPlayerCreate = await GameApp.WebProtoBuff.Post<RespPlayerCreate>(GameApiUrl.BuildGameApiUrl(nameof(ReqPlayerCreate)), req);

            if (respPlayerCreate.ErrorCode > 0)
            {
                Log.Error("登录失败，错误信息:" + respPlayerCreate.ErrorCode);
                return;
            }

            if (respPlayerCreate.PlayerInfo != null)
            {
                Log.Info("创建角色成功");
            }

            #endregion

            var autoPlayer = respPlayerCreate?.PlayerInfo;

            if (autoPlayer == null)
            {
                Log.Error("创建角色后未找到可登录的角色信息。");
                return;
            }

            AccountManager.Instance.PlayerList = new System.Collections.Generic.List<PlayerInfo> { autoPlayer };

            var loginResult = await GameServerLoginHelper.LoginAsync(
                autoPlayer,
                req.Id,
                _steamId,
                _steamTicket,
                _respLogin?.Token);

            if (!loginResult.IsSuccess)
            {
                Log.Error("创建角色后进入游戏失败。");
                return;
            }

            await GameApp.UI.OpenFullScreenAsync<UIMain>(Utility.Asset.Path.GetUIPath(nameof(UIMain)), UIGroupConstants.Floor);
            GameApp.UI.CloseUIForm(this);
        }
    }
}
