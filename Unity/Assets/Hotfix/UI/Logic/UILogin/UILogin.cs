﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using GameFrameX;
using GameFrameX.Event.Runtime;
using GameFrameX.GlobalConfig.Runtime;
#if ENABLE_UI_FAIRYGUI
using GameFrameX.UI.FairyGUI.Runtime;
#endif
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
#if ENABLE_UI_UGUI
using GameFrameX.UI.UGUI.Runtime;
#endif
using Hotfix.Manager;
using Hotfix.Network;
using Hotfix.Proto;
using UnityEngine;
using System.Linq;

namespace Hotfix.UI
{
    public partial class UILogin
    {
        private bool _isLoggingIn;
        private string _steamId;
        private string _steamTicket;

        public override void OnAwake()
        {
            UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Normal.Name);
            base.OnAwake();
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ConfigureAutoLoginView();
            StartSteamAutoLogin().Forget();
        }
        /// <summary>
        /// 通过反射检查 AOT 层的 SteamManager 是否初始化成功
        /// </summary>
        private bool CheckSteamManagerInitialized()
        {
            try
            {
                // 1. 获取包含 SteamManager 的程序集（通常是 Unity.Startup 或 Assembly-CSharp）
                var aotAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Unity.Startup"); // 根据你之前的截图，应该是这个

                if (aotAssembly == null) return false;

                // 2. 找到 SteamManager 类型
                var type = aotAssembly.GetType("SteamManager");
                if (type == null) return false;

                // 3. 获取静态属性 Initialized
                var prop = type.GetProperty("Initialized", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop == null) return false;

                // 4. 返回其布尔值
                return (bool)prop.GetValue(null);
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 统一处理进入离线/单机模式的逻辑
        /// </summary>
        private async void HandleOfflineMode()
        {
            // 1. 设置框架状态为离线
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.IsOfflineMode = true;
            }

            m_ErrorText.text = "Steam 离线模式，正在进入单机环境...";
            Log.Warning("未获取到有效 Steam 登录信息，系统已切换至离线单机模式。");

            // 2. 打开角色创建界面（传入离线标识）
            await GameApp.UI.OpenFullScreenAsync<UIPlayerCreate>(
                Utility.Asset.Path.GetUIPath(nameof(UILogin)),
                new LoginFlowData { IsOfflineMode = true, RespLogin = null });

            // 3. 关闭当前登录界面
            GameApp.UI.CloseUIForm(this);
        }
        
        private async UniTaskVoid StartSteamAutoLogin()
        {
            m_ErrorText.text = "正在检测 Steam 登录状态...";
    
            try 
            {
                // --- 新增：起跑线等待逻辑 ---
                // 这一步是核心！通过反射检查 AOT 层的 SteamManager 是否 Ready
                float timeout = 3f; // 最多等3秒
                while (!CheckSteamManagerInitialized() && timeout > 0)
                {
                    await UniTask.Yield(); // 每一帧检查一次
                    timeout -= Time.deltaTime;
                }

                // --- 修正：只调用一次，并获取结果 ---
                var (ok, steamId, steamName) = await TryGetSteamLoginInfoWithRetryAsync();
        
                if (!ok)
                {
                    HandleOfflineMode();
                    return;
                }

                // 成功后的逻辑
                AccountManager.Instance.IsOfflineMode = false;
                m_UserName.text = steamName;
                m_Password.text = string.Empty;
                m_ErrorText.text = "正在使用 Steam 自动登录...";
                _steamId = steamId;
                SteamAuthProvider.TryGetAuthSessionTicket(out _steamTicket);
                if (string.IsNullOrWhiteSpace(_steamTicket))
                {
                    Log.Warning("Steam session ticket is empty; server may reject ReqPlayerLogin.");
                }
                else
                {
                    Log.Warning($"Steam session ticket length: {_steamTicket.Length}");
                }
                Login(steamId, steamName);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Steam 自动登录过程崩溃: {ex}");
                HandleOfflineMode();
            }
        }

        private async void Login(string steamId, string steamName)
        {
            if (_isLoggingIn)
            {
                return;
            }

            _isLoggingIn = true;
            _steamId = steamId;
            if (string.IsNullOrWhiteSpace(_steamTicket))
            {
                SteamAuthProvider.TryGetAuthSessionTicket(out _steamTicket);
                if (string.IsNullOrWhiteSpace(_steamTicket))
                {
                    Log.Warning("Steam session ticket is empty; server may reject ReqPlayerLogin.");
                }
                else
                {
                    Log.Warning($"Steam session ticket length: {_steamTicket.Length}");
                }
            }
            try
            {
                #region 账号登录

                var req = new ReqLogin
                {
                    SdkType = 1,
                    SdkToken = _steamTicket,
                    UserName = steamId,
                    Password = string.Empty,
                    Device = SystemInfo.deviceUniqueIdentifier
                };
                req.Platform = "steam";

                RespLogin respLogin;
                try
                {
                    respLogin = await GameServerLoginHelper.LoginAccountAsync(req);
                }
                catch (Exception e)
                {
                    m_ErrorText.text = "登录请求失败，请稍后重试";
                    Log.Error($"登录请求异常(TCP)。error={e}");
                    return;
                }

                if (respLogin == null)
                {
                    m_ErrorText.text = "登录失败，服务器无响应";
                    Log.Error("登录失败，响应为空(TCP)。");
                    return;
                }

                if (respLogin.ErrorCode > 0)
                {
                    Log.Error("登录失败，错误信息:" + respLogin.ErrorCode);
                    m_ErrorText.text = $"登录失败，错误码:{respLogin.ErrorCode}";
                    return;
                }

                #endregion

                #region 获取角色列表

                ReqPlayerList reqPlayerList = new ReqPlayerList();

                reqPlayerList.Id = respLogin.Id;
                var playerListUrl = GameApiUrl.BuildGameApiUrl(nameof(ReqPlayerList));
                RespPlayerList respPlayerList;
                try
                {
                    respPlayerList = await GameApp.WebProtoBuff.Post<RespPlayerList>(playerListUrl, reqPlayerList);
                }
                catch (TimeoutException e)
                {
                    m_ErrorText.text = "获取角色列表超时，请稍后重试";
                    Log.Error($"获取角色列表超时。url={playerListUrl}, error={e.Message}");
                    return;
                }
                catch (WebException e)
                {
                    m_ErrorText.text = "获取角色列表失败(网关/服务异常)，请稍后重试";
                    Log.Error($"获取角色列表异常。url={playerListUrl}, {BuildWebExceptionDetail(e)}");
                    return;
                }
                catch (Exception e)
                {
                    m_ErrorText.text = "获取角色列表失败，请稍后重试";
                    Log.Error($"获取角色列表异常。url={playerListUrl}, error={e}");
                    return;
                }

                if (respPlayerList == null)
                {
                    m_ErrorText.text = "获取角色列表失败，服务器无响应";
                    Log.Error($"获取角色列表失败，响应为空。url={playerListUrl}");
                    return;
                }

                if (respPlayerList.ErrorCode > 0)
                {
                    Log.Error("登录失败，错误信息:" + respPlayerList.ErrorCode);
                    m_ErrorText.text = $"获取角色列表失败，错误码:{respPlayerList.ErrorCode}";
                    return;
                }

                var playerList = respPlayerList.PlayerList ?? new List<PlayerInfo>();
                var singlePlayerList = playerList.Count > 0
                    ? new List<PlayerInfo> { playerList[0] }
                    : new List<PlayerInfo>();
                AccountManager.Instance.PlayerList = singlePlayerList;

                #endregion

                if (singlePlayerList.Count > 0)
                {
                    var autoPlayer = singlePlayerList[0];
                    m_ErrorText.text = "正在进入游戏...";

                    var loginResult = await GameServerLoginHelper.LoginAsync(
                        autoPlayer,
                        respLogin.Id,
                        _steamId,
                        _steamTicket,
                        respLogin.Token);

                    if (!loginResult.IsSuccess)
                    {
                        var errCode = loginResult.Response?.ErrorCode ?? -1;
                        var errText = loginResult.Error ?? errCode.ToString();
                        m_ErrorText.text = $"进入游戏失败({errText})，请稍后重试";
                        return;
                    }

                    await GameApp.UI.OpenFullScreenAsync<UIMain>(Utility.Asset.Path.GetUIPath(nameof(UIMain)), UIGroupConstants.Floor);
                    GameApp.UI.CloseUIForm(this);
                    return;
                }

                m_ErrorText.text = "未找到角色，正在创建角色...";
                await GameApp.UI.OpenFullScreenAsync<UIPlayerCreate>(
                    Utility.Asset.Path.GetUIPath(nameof(UILogin)),
                    new LoginFlowData { IsOfflineMode = false, RespLogin = respLogin, SteamId = _steamId, SdkToken = _steamTicket });

                GameApp.UI.CloseUIForm(this);
            }
            finally
            {
                _isLoggingIn = false;
            }
        }

        private static async UniTask<(bool ok, string steamId, string steamName)> TryGetSteamLoginInfoWithRetryAsync()
        {
            const int maxAttempts = 10;
            for (var i = 0; i < maxAttempts; i++)
            {
                if (TryGetSteamLoginInfo(out var steamId, out var steamName))
                {
                    return (true, steamId, steamName);
                }

                await UniTask.Delay(300);
            }

            return (false, null, null);
        }

        private static bool TryGetSteamLoginInfo(out string steamId, out string steamName)
        {
            steamId = null;
            steamName = null;

            if (TryGetSteamworksNetLoginInfo(out steamId, out steamName))
            {
                return true;
            }

            if (TryGetFacepunchLoginInfo(out steamId, out steamName))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetSteamworksNetLoginInfo(out string steamId, out string steamName)
        {
            steamId = string.Empty;
            steamName = string.Empty;

            try
            {
                // 1. 获取 Steamworks 程序集 (UPM 安装后的标准名称)
                var steamAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "com.rlabrecque.steamworks.net");

                if (steamAssembly == null) return false;

                // 2. 直接反射官方的 SteamUser 类，而不是自定义的 Manager
                var steamUserType = steamAssembly.GetType("Steamworks.SteamUser");
                var steamFriendsType = steamAssembly.GetType("Steamworks.SteamFriends");

                if (steamUserType == null || steamFriendsType == null) return false;

                // 3. 获取 GetSteamID 方法
                var getSteamIdMethod = steamUserType.GetMethod("GetSteamID", BindingFlags.Public | BindingFlags.Static);
                var getPersonaNameMethod = steamFriendsType.GetMethod("GetPersonaName", BindingFlags.Public | BindingFlags.Static);

                if (getSteamIdMethod == null) return false;

                // 4. 执行调用
                var steamIdObj = getSteamIdMethod.Invoke(null, null); // 返回的是 CSteamID 结构体
                if (steamIdObj != null)
                {
                    steamId = steamIdObj.ToString(); // CSteamID 已重写 ToString
                    steamName = getPersonaNameMethod?.Invoke(null, null)?.ToString() ?? "Unknown";
                    return !string.IsNullOrEmpty(steamId);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Steam反射失败]: {ex.Message}");
            }
            return false;
        }

        private static bool TryGetFacepunchLoginInfo(out string steamId, out string steamName)
        {
            steamId = null;
            steamName = null;

            var steamClientType = FindType("Steamworks.SteamClient");
            if (steamClientType == null)
            {
                return false;
            }

            var isValidProp = steamClientType.GetProperty("IsValid", BindingFlags.Public | BindingFlags.Static);
            if (isValidProp != null)
            {
                var isValidValue = isValidProp.GetValue(null);
                if (isValidValue is bool b && !b)
                {
                    return false;
                }
            }

            var steamIdProp = steamClientType.GetProperty("SteamId", BindingFlags.Public | BindingFlags.Static);
            if (steamIdProp == null)
            {
                return false;
            }

            steamId = NormalizeSteamId(steamIdProp.GetValue(null));
            if (steamId.IsNullOrWhiteSpace())
            {
                return false;
            }

            var nameProp = steamClientType.GetProperty("Name", BindingFlags.Public | BindingFlags.Static);
            if (nameProp != null)
            {
                steamName = nameProp.GetValue(null) as string;
            }

            return true;
        }

        private static Type FindType(string fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string NormalizeSteamId(object rawSteamId)
        {
            if (rawSteamId == null)
            {
                return null;
            }

            var type = rawSteamId.GetType();
            var innerIdProp = type.GetProperty("m_SteamID", BindingFlags.Public | BindingFlags.Instance);
            if (innerIdProp != null)
            {
                var innerValue = innerIdProp.GetValue(rawSteamId);
                return innerValue?.ToString();
            }

            var value = rawSteamId.ToString();
            return value == "0" ? null : value;
        }

        private static string BuildWebExceptionDetail(WebException exception)
        {
            if (!(exception.Response is HttpWebResponse response))
            {
                return $"webStatus={exception.Status}, error={exception.Message}";
            }

            var body = "<empty>";
            try
            {
                using (var stream = response.GetResponseStream())
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            body = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception readException)
            {
                body = $"<read body failed: {readException.Message}>";
            }

            if (body.Length > 2048)
            {
                body = body.Substring(0, 2048) + "...(truncated)";
            }

            return $"status={(int)response.StatusCode}({response.StatusCode}), webStatus={exception.Status}, contentType={response.ContentType}, body={body}";
        }

        private void ConfigureAutoLoginView()
        {
#if ENABLE_UI_UGUI
            if (m_UserName != null) m_UserName.gameObject.SetActive(false);
            if (m_Password != null) m_Password.gameObject.SetActive(false);
            if (m_enter != null) m_enter.gameObject.SetActive(false);
#elif ENABLE_UI_FAIRYGUI
            if (m_UserName != null) m_UserName.visible = false;
            if (m_Password != null) m_Password.visible = false;
            if (m_enter != null) m_enter.visible = false;
#endif
        }
    }
}
