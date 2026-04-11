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
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hotfix.Events;
using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using Hotfix.Proto;

namespace Hotfix.Manager
{
    public sealed class MatchManager : GameFrameworkSingleton<MatchManager>, IMessageHandler
    {
        public MatchManager()
        {
            Register();
        }

        public long CurrentRoomId { get; private set; }

        public List<MatchRoomPlayerInfo> CurrentRoomPlayers { get; private set; } = new List<MatchRoomPlayerInfo>();

        public bool IsMatching { get; private set; }

        public bool IsReady { get; private set; }

        [MessageHandler(typeof(NotifyMatchSuccess), nameof(NotifyMatchSuccessHandler))]
        private void NotifyMatchSuccessHandler(NotifyMatchSuccess message)
        {
            CurrentRoomId = message.RoomId;
            CurrentRoomPlayers = message.PlayerList ?? new List<MatchRoomPlayerInfo>();
            IsMatching = false;
            UpdateReadyState();
            Log.Info($"match success, roomId={CurrentRoomId}, playerCount={CurrentRoomPlayers.Count}");
            FireMatchChangedEvent();
        }

        [MessageHandler(typeof(NotifyMatchRoomChanged), nameof(NotifyMatchRoomChangedHandler))]
        private void NotifyMatchRoomChangedHandler(NotifyMatchRoomChanged message)
        {
            CurrentRoomId = message.RoomId;
            CurrentRoomPlayers = message.PlayerList ?? new List<MatchRoomPlayerInfo>();
            UpdateReadyState();
            FireMatchChangedEvent();
        }

        public async UniTask<RespMatchStart> RequestStartMatch()
        {
            var response = await GameApp.Network.GetNetworkChannel("network").Call<RespMatchStart>(new ReqMatchStart());
            IsMatching = response != null && response.ErrorCode == 0 && response.IsMatching;
            if (response != null && response.RoomId > 0)
            {
                CurrentRoomId = response.RoomId;
            }

            FireMatchChangedEvent();

            return response;
        }

        public async UniTask<RespMatchCancel> RequestCancelMatch()
        {
            var response = await GameApp.Network.GetNetworkChannel("network").Call<RespMatchCancel>(new ReqMatchCancel());
            if (response != null && response.ErrorCode == 0 && response.IsCancelled)
            {
                IsMatching = false;
            }

            FireMatchChangedEvent();

            return response;
        }

        public async UniTask<RespMatchRoomReady> RequestRoomReady(bool isReady)
        {
            var response = await GameApp.Network.GetNetworkChannel("network").Call<RespMatchRoomReady>(new ReqMatchRoomReady
            {
                IsReady = isReady
            });
            if (response != null && response.ErrorCode == 0)
            {
                CurrentRoomId = response.RoomId;
                CurrentRoomPlayers = response.PlayerList ?? new List<MatchRoomPlayerInfo>();
                UpdateReadyState();
            }

            FireMatchChangedEvent();

            return response;
        }

        public void Register()
        {
            ProtoMessageHandler.Add(this);
        }

        private void UpdateReadyState()
        {
            var localPlayerId = PlayerManager.Instance?.PlayerInfo?.Id ?? 0;
            IsReady = false;
            if (localPlayerId <= 0 || CurrentRoomPlayers == null)
            {
                return;
            }

            for (var i = 0; i < CurrentRoomPlayers.Count; i++)
            {
                if (CurrentRoomPlayers[i].PlayerId == localPlayerId)
                {
                    IsReady = CurrentRoomPlayers[i].IsReady;
                    return;
                }
            }
        }

        private void FireMatchChangedEvent()
        {
            GameApp.Event.Fire(this, MatchChangedEventArgs.Create());
        }
    }
}
