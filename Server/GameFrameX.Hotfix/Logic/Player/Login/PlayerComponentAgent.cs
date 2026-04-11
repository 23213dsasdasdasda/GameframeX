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
using GameFrameX.Apps.Common.Session;
using GameFrameX.Apps.Player.Player.Component;
using GameFrameX.Apps.Player.Player.Entity;
using GameFrameX.Hotfix.Logic.Server;

namespace GameFrameX.Hotfix.Logic.Player.Login;

public class PlayerComponentAgent : StateComponentAgent<PlayerComponent, PlayerState>
{
    public async Task OnLogout()
    {
        var serverComponent = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        await serverComponent.CancelMatch(ActorId);
        //移除在线玩家
        await serverComponent.RemoveOnlineRole(ActorId);
        //下线后会被自动回收
        SetAutoRecycle(true);
        QuartzTimer.Remove(ScheduleIdSet);
    }

    /// <summary>
    /// 使用角色ID登录
    /// </summary>
    /// <param name="workChannel"></param>
    /// <param name="playerState"></param>
    /// <param name="reqLoginUniqueId"></param>
    /// <param name="response"></param>
    public async Task OnPlayerLogin(INetWorkChannel workChannel, PlayerState playerState, RespPlayerLogin response)
    {
        // 更新连接会话数据
        SessionManager.UpdateSession(workChannel.GameAppSession.SessionID, playerState.Id, playerState.Id.ToString());
        response.Code = playerState.State;
        response.CreateTime = playerState.CreateTime;
        response.PlayerInfo = new PlayerInfo
        {
            Id = playerState.Id,
            Name = playerState.Name,
            Level = playerState.Level,
            State = playerState.State,
            Avatar = playerState.Avatar,
        };

        //加入在线玩家
        var serverComp = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        await serverComp.AddOnlineRole(ActorId);
    }

    public async Task OnPlayerInfo(INetWorkChannel workChannel, ReqPlayerInfo request, RespPlayerInfo response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        var playerState = await OwnerComponent.OnPlayerInfo(request.PlayerId);
        if (playerState == null)
        {
            response.ErrorCode = (int)OperationStatusCode.NotFound;
            return;
        }

        response.PlayerInfo = new PlayerInfo
        {
            Id = playerState.Id,
            Name = string.IsNullOrWhiteSpace(playerState.Name) ? playerState.RoleName : playerState.Name,
            Level = playerState.Level == 0 ? 1u : playerState.Level,
            State = playerState.State,
            Avatar = playerState.Avatar,
        };
    }

    public async Task OnPlayerPet(INetWorkChannel workChannel, ReqPlayerPet request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await OwnerComponent.OnPlayerPet(request.PlayerId);
    }

    public async Task OnMatchStart(RespMatchStart response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var serverComp = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        var result = await serverComp.StartMatch(ActorId);
        CopyMatchStartResult(result, response);
    }

    public async Task OnMatchCancel(RespMatchCancel response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var serverComp = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        var result = await serverComp.CancelMatch(ActorId);
        response.IsCancelled = result.IsCancelled;
        response.QueueCount = result.QueueCount;
        response.ErrorCode = result.ErrorCode;
    }

    public async Task OnMatchRoomReady(ReqMatchRoomReady request, RespMatchRoomReady response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);
        var serverComp = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        var result = await serverComp.SetRoomReady(ActorId, request.IsReady);
        response.RoomId = result.RoomId;
        response.IsReady = result.IsReady;
        response.ErrorCode = result.ErrorCode;
        response.PlayerList = result.PlayerList ?? new List<MatchRoomPlayerInfo>();
    }

    private static void CopyMatchStartResult(RespMatchStart source, RespMatchStart target)
    {
        target.IsMatching = source.IsMatching;
        target.QueueCount = source.QueueCount;
        target.RoomId = source.RoomId;
        target.ErrorCode = source.ErrorCode;
    }
    
    
}
