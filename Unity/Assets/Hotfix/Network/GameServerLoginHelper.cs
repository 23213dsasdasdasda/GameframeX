using System;

using Cysharp.Threading.Tasks;
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using GameFrameX.Event.Runtime;
using Hotfix.Manager;
using Hotfix.Proto;
using UnityEngine;

namespace Hotfix.Network
{
    public static class GameServerLoginHelper
    {
        private const string ChannelName = "network";
        private static INetworkChannel _sNetworkChannel;
        private static bool _eventsSubscribed;

        public sealed class LoginResult
        {
            public RespPlayerLogin Response { get; private set; }
            public string Error { get; private set; }

            public bool IsSuccess => Response != null && Response.ErrorCode == 0;

            public static LoginResult FromError(string error)
            {
                return new LoginResult { Error = error };
            }

            public static LoginResult FromResponse(RespPlayerLogin response)
            {
                return new LoginResult { Response = response };
            }
        }

        public static async UniTask<LoginResult> LoginAsync(
            PlayerInfo playerInfo,
            long accountId,
            string steamId,
            string sdkToken = null,
            string fallbackSdkToken = null,
            string platform = "steam",
            int sdkType = 1,
            float connectTimeoutSeconds = 5f)
        {
            if (playerInfo == null)
            {
                Log.Error("LoginAsync failed: playerInfo is null.");
                return LoginResult.FromError("playerInfo is null");
            }

            if (playerInfo.Id <= 0)
            {
                Log.Error($"LoginAsync failed: invalid playerId={playerInfo.Id}.");
                return LoginResult.FromError($"invalid playerId={playerInfo.Id}");
            }

            var channel = GetOrCreateChannel();
            if (channel == null)
            {
                Log.Error("LoginAsync failed: network channel is null.");
                return LoginResult.FromError("network channel is null");
            }

            if (!channel.Connected)
            {
                var connected = await WaitForConnectedAsync(channel, connectTimeoutSeconds);
                if (!connected)
                {
                    Log.Error($"LoginAsync failed: connect timeout ({connectTimeoutSeconds}s).");
                    return LoginResult.FromError($"connect timeout ({connectTimeoutSeconds}s)");
                }
            }

            await UniTask.Yield();

            var userName = string.IsNullOrWhiteSpace(steamId) ? playerInfo.Id.ToString() : steamId;
            var requests = new System.Collections.Generic.List<(string tag, ReqPlayerLogin req)>();

            // Steam ticket with playerId.
            if (!string.IsNullOrWhiteSpace(sdkToken))
            {
                requests.Add(("steam_player", new ReqPlayerLogin
                {
                    Id = playerInfo.Id,
                    UserName = userName,
                    Platform = platform,
                    SdkType = sdkType,
                    SdkToken = sdkToken,
                    Device = SystemInfo.deviceUniqueIdentifier,
                    DeviceModel = SystemInfo.deviceModel,
                    Version = Application.version
                }));

                if (accountId > 0)
                {
                    requests.Add(("steam_account", new ReqPlayerLogin
                    {
                        Id = accountId,
                        UserName = userName,
                        Platform = platform,
                        SdkType = sdkType,
                        SdkToken = sdkToken,
                        Device = SystemInfo.deviceUniqueIdentifier,
                        DeviceModel = SystemInfo.deviceModel,
                        Version = Application.version
                    }));
                }
            }

            // Fallback token (usually from HTTP login).
            if (!string.IsNullOrWhiteSpace(fallbackSdkToken))
            {
                if (accountId > 0)
                {
                    requests.Add(("fallback_account", new ReqPlayerLogin
                    {
                        Id = accountId,
                        UserName = userName,
                        Platform = platform,
                        SdkType = 0,
                        SdkToken = fallbackSdkToken,
                        Device = SystemInfo.deviceUniqueIdentifier,
                        DeviceModel = SystemInfo.deviceModel,
                        Version = Application.version
                    }));
                }

                requests.Add(("fallback_player", new ReqPlayerLogin
                {
                    Id = playerInfo.Id,
                    UserName = userName,
                    Platform = platform,
                    SdkType = 0,
                    SdkToken = fallbackSdkToken,
                    Device = SystemInfo.deviceUniqueIdentifier,
                    DeviceModel = SystemInfo.deviceModel,
                    Version = Application.version
                }));
            }

            // Legacy payload (original UIPlayerList path).
            requests.Add(("legacy", new ReqPlayerLogin
            {
                UserName = playerInfo.Id.ToString()
            }));

            LoginResult lastResult = null;
            foreach (var entry in requests)
            {
                var result = await SendLoginAsync(channel, entry.req, entry.tag);
                lastResult = result;

                if (result.IsSuccess)
                {
                    return result;
                }

                // If server responded with an error code, stop and surface it.
                if (result.Response != null)
                {
                    return result;
                }
            }

            return lastResult ?? LoginResult.FromError("rpc timeout");
        }

        public static async UniTask<RespLogin> LoginAccountAsync(ReqLogin request, float connectTimeoutSeconds = 5f)
        {
            if (request == null)
            {
                Log.Error("LoginAccountAsync failed: request is null.");
                return null;
            }

            var channel = GetOrCreateChannel();
            if (channel == null)
            {
                Log.Error("LoginAccountAsync failed: network channel is null.");
                return null;
            }

            if (!channel.Connected)
            {
                var connected = await WaitForConnectedAsync(channel, connectTimeoutSeconds);
                if (!connected)
                {
                    Log.Error($"LoginAccountAsync failed: connect timeout ({connectTimeoutSeconds}s).");
                    return null;
                }
            }

            await UniTask.Yield();

            try
            {
                const int reqLoginMsgId = ((300 << 16) + 10);
                Debug.LogError($"ReqLogin msgId={reqLoginMsgId} uniqueId={request.UniqueId} user={request.UserName}");
                Log.Error($"[TRACE] ReqLogin send: {FormatReqLogin(request)}");

                var respLogin = await channel.Call<RespLogin>(request);
                Debug.LogError($"TCP receive type={respLogin?.GetType().FullName ?? "null"}");
                if (respLogin == null)
                {
                    Log.Error("LoginAccountAsync failed: respLogin is null.");
                    return null;
                }

                Log.Error($"[TRACE] RespLogin recv: {FormatRespLogin(respLogin)}");

                if (respLogin.ErrorCode > 0)
                {
                    Log.Error($"LoginAccountAsync failed: errorCode={respLogin.ErrorCode}, user={request.UserName}.");
                }

                return respLogin;
            }
            catch (TimeoutException)
            {
                Log.Error("LoginAccountAsync timeout: no response from server.");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"LoginAccountAsync exception: {ex}");
                return null;
            }
        }

        private static INetworkChannel GetOrCreateChannel()
        {
            EnsureEventSubscribed();

            if (_sNetworkChannel != null && _sNetworkChannel.Connected)
            {
                return _sNetworkChannel;
            }

            if (GameApp.Network.HasNetworkChannel(ChannelName))
            {
                _sNetworkChannel = GameApp.Network.GetNetworkChannel(ChannelName);
            }

            if (_sNetworkChannel != null && _sNetworkChannel.Connected)
            {
                return _sNetworkChannel;
            }

            if (_sNetworkChannel != null && GameApp.Network.HasNetworkChannel(ChannelName) && !_sNetworkChannel.Connected)
            {
                GameApp.Network.DestroyNetworkChannel(ChannelName);
                _sNetworkChannel = null;
            }

            _sNetworkChannel = GameApp.Network.CreateNetworkChannel(ChannelName, new DefaultNetworkChannelHelper());
            _sNetworkChannel.RegisterHeartBeatHandler(new DefaultPacketHeartBeatHandler());
            _sNetworkChannel.SetRPCStartHandler(OnRpcStart);
            _sNetworkChannel.SetRPCEndHandler(OnRpcEnd);
            _sNetworkChannel.SetRPCErrorHandler(OnRpcTimeoutOrError);
            _sNetworkChannel.SetRPCErrorCodeHandler(OnRpcErrorCode);

            var tcpUrl = GameApiUrl.BuildGameTcpUrl();
            Log.Info($"Connect game server: {tcpUrl}");
            _sNetworkChannel.Connect(new Uri(tcpUrl));

            return _sNetworkChannel;
        }

        private static async UniTask<bool> WaitForConnectedAsync(INetworkChannel channel, float timeoutSeconds)
        {
            var timeout = Mathf.Max(0f, timeoutSeconds);
            while (!channel.Connected && timeout > 0f)
            {
                await UniTask.Yield();
                timeout -= Time.deltaTime;
            }

            return channel.Connected;
        }

        private static async UniTask<LoginResult> SendLoginAsync(INetworkChannel channel, ReqPlayerLogin request, string tag)
        {
            try
            {
                Log.Error($"[TRACE] ReqPlayerLogin send [{tag}]: {FormatReqPlayerLogin(request)}");

                var respPlayerLogin = await channel.Call<RespPlayerLogin>(request);

                if (respPlayerLogin == null)
                {
                    Log.Error($"LoginAsync failed [{tag}]: respPlayerLogin is null.");
                    return LoginResult.FromError("respPlayerLogin is null");
                }

                Log.Error($"[TRACE] RespPlayerLogin recv [{tag}]: {FormatRespPlayerLogin(respPlayerLogin)}");

                if (respPlayerLogin.ErrorCode > 0)
                {
                    Log.Error($"LoginAsync failed [{tag}]: errorCode={respPlayerLogin.ErrorCode}, playerId={request.UserName}.");
                    return LoginResult.FromResponse(respPlayerLogin);
                }

                PlayerManager.Instance.PlayerInfo = respPlayerLogin.PlayerInfo;
                return LoginResult.FromResponse(respPlayerLogin);
            }
            catch (TimeoutException)
            {
                Log.Error($"LoginAsync timeout [{tag}]: no response from server.");
                return LoginResult.FromError("rpc timeout");
            }
            catch (Exception ex)
            {
                Log.Error($"LoginAsync exception [{tag}]: {ex}");
                return LoginResult.FromError(ex.Message);
            }
        }

        private static void EnsureEventSubscribed()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            _eventsSubscribed = true;
            GameApp.Event.CheckSubscribe(NetworkConnectedEventArgs.EventId, OnNetworkConnected);
            GameApp.Event.CheckSubscribe(NetworkClosedEventArgs.EventId, OnNetworkClosed);
            GameApp.Event.CheckSubscribe(NetworkErrorEventArgs.EventId, OnNetworkError);
        }

        private static void OnNetworkConnected(object sender, GameEventArgs e)
        {
            if (e is NetworkConnectedEventArgs ne && ne.NetworkChannel != null && ne.NetworkChannel.Name == ChannelName)
            {
                Log.Error($"Network connected: {ne.NetworkChannel.Name}");
            }
        }

        private static void OnNetworkClosed(object sender, GameEventArgs e)
        {
            if (e is NetworkClosedEventArgs ne && ne.NetworkChannel != null && ne.NetworkChannel.Name == ChannelName)
            {
                Log.Error($"Network closed: {ne.NetworkChannel.Name}, reason={ne.Reason}, code={ne.ErrorCode}");
            }
        }

        private static void OnNetworkError(object sender, GameEventArgs e)
        {
            if (e is NetworkErrorEventArgs ne && ne.NetworkChannel != null && ne.NetworkChannel.Name == ChannelName)
            {
                Log.Error($"Network error: {ne.NetworkChannel.Name}, code={ne.ErrorCode}, msg={ne.ErrorMessage}");
            }
        }

        private static void OnRpcStart(object sender, MessageObject message)
        {
            Debug.LogError($"RPC start type={message?.GetType().FullName ?? "null"} uniqueId={message?.UniqueId ?? 0}");
        }

        private static void OnRpcEnd(object sender, MessageObject message)
        {
            Debug.LogError($"RPC end type={message?.GetType().FullName ?? "null"} uniqueId={message?.UniqueId ?? 0}");
        }

        private static void OnRpcTimeoutOrError(object sender, MessageObject message)
        {
            Debug.LogError($"RPC timeout/error reqType={message?.GetType().FullName ?? "null"} uniqueId={message?.UniqueId ?? 0}");
        }

        private static void OnRpcErrorCode(object sender, MessageObject message)
        {
            var response = message as IResponseMessage;
            Debug.LogError($"RPC errorCode respType={message?.GetType().FullName ?? "null"} uniqueId={message?.UniqueId ?? 0} errorCode={response?.ErrorCode ?? -1}");
        }

        private static string FormatReqLogin(ReqLogin req)
        {
            if (req == null)
            {
                return "<null>";
            }

            return $"Id={req.Id}, UserName={req.UserName}, Platform={req.Platform}, SdkType={req.SdkType}, " +
                   $"SdkToken={MaskToken(req.SdkToken)}, Device={req.Device}, DeviceModel={req.DeviceModel}, Version={req.Version}";
        }

        private static string FormatRespLogin(RespLogin resp)
        {
            if (resp == null)
            {
                return "<null>";
            }

            return $"Id={resp.Id}, RoleName={resp.RoleName}, Code={resp.Code}, Level={resp.Level}, " +
                   $"CreateTime={resp.CreateTime}, Token={MaskToken(resp.Token)}, ServerTime={resp.ServerTime}, ErrorCode={resp.ErrorCode}";
        }

        private static string FormatReqPlayerLogin(ReqPlayerLogin req)
        {
            if (req == null)
            {
                return "<null>";
            }

            return $"Id={req.Id}, UserName={req.UserName}, Platform={req.Platform}, SdkType={req.SdkType}, " +
                   $"SdkToken={MaskToken(req.SdkToken)}, Device={req.Device}, DeviceModel={req.DeviceModel}, Version={req.Version}";
        }

        private static string FormatRespPlayerLogin(RespPlayerLogin resp)
        {
            if (resp == null)
            {
                return "<null>";
            }

            var player = resp.PlayerInfo;
            var playerText = player == null
                ? "<null>"
                : $"Id={player.Id}, Name={player.Name}, Level={player.Level}, State={player.State}, Avatar={player.Avatar}, CurrentExp={player.CurrentExp}";

            return $"Code={resp.Code}, CreateTime={resp.CreateTime}, PlayerInfo=({playerText}), ErrorCode={resp.ErrorCode}";
        }

        private static string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "<empty>";
            }

            const int keepPrefixLength = 8;
            const int keepSuffixLength = 4;
            if (token.Length <= keepPrefixLength + keepSuffixLength)
            {
                return $"{token}(len={token.Length})";
            }

            var prefix = token.Substring(0, keepPrefixLength);
            var suffix = token.Substring(token.Length - keepSuffixLength, keepSuffixLength);
            return $"{prefix}...{suffix}(len={token.Length})";
        }
    }
}
