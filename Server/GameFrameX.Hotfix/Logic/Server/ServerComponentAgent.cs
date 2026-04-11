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
using GameFrameX.Apps.Player.Player.Entity;
using GameFrameX.Apps.Server.Component;
using GameFrameX.Apps.Server.Entity;
using GameFrameX.Core.Abstractions.Attribute;
using GameFrameX.Core.Abstractions.Events;
using GameFrameX.Core.Timer.Handler;
using GameFrameX.Foundation.Utility;
using GameFrameX.Hotfix.Logic.Player.Login;

namespace GameFrameX.Hotfix.Logic.Server;

public class ServerComponentAgent : StateComponentAgent<ServerComponent, ServerState>
{
    public override async Task<bool> Active()
    {
        var isContinue = await base.Active();
        if (isContinue)
        {
            // 跨天定时器
            WithCronExpression<CrossDayTimeHandler>("0 0 0 * * ? *");
            if (State.FirstStartTime == default)
            {
                State.FirstStartTime = TimerHelper.UnixTimeSeconds();
                await OwnerComponent.WriteStateAsync();
            }
        }

        return isContinue;
    }

    [Service]
    [Discard]
    public virtual ValueTask AddOnlineRole(long roleId)
    {
        OwnerComponent.OnlineSet.Add(roleId);
        return ValueTask.CompletedTask;
    }

    [Service]
    [Discard]
    public virtual ValueTask RemoveOnlineRole(long roleId)
    {
        OwnerComponent.OnlineSet.Remove(roleId);
        return ValueTask.CompletedTask;
    }

    public static async Task OnlineRoleForeach(Action<PlayerComponentAgent> func)
    {
        var serverComp = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        serverComp.Tell(async () =>
        {
            foreach (var roleId in serverComp.OwnerComponent.OnlineSet)
            {
                var roleComp = await ActorManager.GetComponentAgent<PlayerComponentAgent>(roleId);
                roleComp.Tell(() => func(roleComp));
            }
        });
    }


    private Task TestDelayTimer()
    {
        LogHelper.Debug("ServerCompAgent.TestDelayTimer.延时3秒执行.执行一次");
        return Task.CompletedTask;
    }

    private Task TestScheduleTimer()
    {
        LogHelper.Debug("ServerCompAgent.TestSchedueTimer.延时1秒执行.每隔10秒执行");
        //
        // var states = await GameDb.FindListAsync<LoginState>(m => m.Id != 0);
        // LOGGER.Debug(states);
        //
        // var s1 = await GameDb.CountAsync<LoginState>(m => m.Id == 563517512475926528);
        // LOGGER.Debug(s1);
        // var s = await GameDb.FindAsync<LoginState>(m => m.UserName != null);
        // LOGGER.Debug(s);
        // LoginState loginState = new LoginState()
        // {
        //     Id = TimeHelper.UnixTimeSeconds(), CreateId = TimeHelper.UnixTimeSeconds(),
        //     UserName = "Save"
        // };
        // loginState.AfterLoadFromDB(true);
        // var savedState = await GameDb.UpdateAsync<LoginState>(loginState);
        // LOGGER.Debug(savedState);
        // await Task.Delay(TimeSpan.FromSeconds(1));
        // var count = await GameDb.DeleteAsync(savedState);
        // LOGGER.Debug(count);
        return Task.CompletedTask;
    }


    /// <summary>
    /// 获取服务器开服启动时间
    /// </summary>
    /// <returns></returns>
    [Service]
    [ThreadSafe]
    public virtual long FirstStartTime()
    {
        return State.FirstStartTime;
    }

    [Service]
    [ThreadSafe]
    public virtual Task<int> GetWorldLevel()
    {
        return Task.FromResult(State.WorldLevel);
    }

    [Service]
    public virtual Task<bool> IsOnline(long roleId)
    {
        foreach (var id in OwnerComponent.OnlineSet)
        {
            if (id == roleId)
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    [Service]
    public virtual async Task<RespMatchStart> StartMatch(long playerId)
    {
        var response = new RespMatchStart();
        if (playerId <= 0)
        {
            response.ErrorCode = (int)OperationStatusCode.Unprocessable;
            return response;
        }

        if (OwnerComponent.PlayerRoomMap.TryGetValue(playerId, out var roomId))
        {
            response.RoomId = roomId;
            response.ErrorCode = (int)OperationStatusCode.HasExist;
            return response;
        }

        if (!OwnerComponent.MatchQueue.Contains(playerId))
        {
            OwnerComponent.MatchQueue.Add(playerId);
        }

        response.IsMatching = true;
        response.QueueCount = OwnerComponent.MatchQueue.Count;
        if (ShouldUseDebugBotMatch(playerId))
        {
            var botRoom = await CreateDebugBotRoomAsync(playerId);
            if (botRoom != null)
            {
                response.RoomId = botRoom.RoomId;
                response.IsMatching = false;
                response.QueueCount = OwnerComponent.MatchQueue.Count;
                await NotifyMatchSuccessAsync(botRoom);
                return response;
            }
        }

        await TryMatchAsync();
        if (OwnerComponent.PlayerRoomMap.TryGetValue(playerId, out var matchedRoomId))
        {
            response.RoomId = matchedRoomId;
            response.IsMatching = false;
        }

        return response;
    }

    [Service]
    public virtual Task<RespMatchCancel> CancelMatch(long playerId)
    {
        var response = new RespMatchCancel();
        response.IsCancelled = OwnerComponent.MatchQueue.Remove(playerId);
        response.QueueCount = OwnerComponent.MatchQueue.Count;
        return Task.FromResult(response);
    }

    [Service]
    public virtual Task<RespMatchInfo> GetMatchInfo(long playerId)
    {
        var response = new RespMatchInfo
        {
            IsMatching = OwnerComponent.MatchQueue.Contains(playerId),
            QueueCount = OwnerComponent.MatchQueue.Count
        };

        if (!OwnerComponent.PlayerRoomMap.TryGetValue(playerId, out var roomId))
        {
            return Task.FromResult(response);
        }

        if (!OwnerComponent.MatchRoomMap.TryGetValue(roomId, out var room))
        {
            OwnerComponent.PlayerRoomMap.Remove(playerId);
            return Task.FromResult(response);
        }

        response.RoomId = room.RoomId;
        response.PlayerList = BuildMatchRoomPlayerInfoList(room);
        var player = room.PlayerList.FirstOrDefault(m => m.PlayerId == playerId);
        response.IsReady = player?.IsReady ?? false;
        response.IsMatching = false;
        return Task.FromResult(response);
    }

    [Service]
    public virtual async Task<RespMatchRoomLeave> LeaveRoom(long playerId)
    {
        var response = new RespMatchRoomLeave();
        if (OwnerComponent.MatchQueue.Remove(playerId))
        {
            response.IsLeft = true;
        }

        if (!OwnerComponent.PlayerRoomMap.TryGetValue(playerId, out var roomId))
        {
            response.QueueCount = OwnerComponent.MatchQueue.Count;
            return response;
        }

        OwnerComponent.PlayerRoomMap.Remove(playerId);
        if (!OwnerComponent.MatchRoomMap.TryGetValue(roomId, out var room))
        {
            response.IsLeft = true;
            response.QueueCount = OwnerComponent.MatchQueue.Count;
            return response;
        }

        OwnerComponent.MatchRoomMap.Remove(roomId);
        room.PlayerList.RemoveAll(m => m.PlayerId == playerId);
        foreach (var player in room.PlayerList)
        {
            OwnerComponent.PlayerRoomMap.Remove(player.PlayerId);
            player.IsReady = false;
        }

        response.IsLeft = true;
        response.QueueCount = OwnerComponent.MatchQueue.Count;
        await NotifyRoomDismissedAsync(room);
        return response;
    }

    [Service]
    public virtual Task<RespMatchRoomReady> SetRoomReady(long playerId, bool isReady)
    {
        var response = new RespMatchRoomReady
        {
            IsReady = isReady
        };

        if (!OwnerComponent.PlayerRoomMap.TryGetValue(playerId, out var roomId))
        {
            response.ErrorCode = (int)OperationStatusCode.NotFound;
            return Task.FromResult(response);
        }

        if (!OwnerComponent.MatchRoomMap.TryGetValue(roomId, out var room))
        {
            response.ErrorCode = (int)OperationStatusCode.NotFound;
            return Task.FromResult(response);
        }

        var player = room.PlayerList.FirstOrDefault(m => m.PlayerId == playerId);
        if (player == null)
        {
            response.ErrorCode = (int)OperationStatusCode.NotFound;
            return Task.FromResult(response);
        }

        player.IsReady = isReady;
        response.RoomId = room.RoomId;
        response.PlayerList = BuildMatchRoomPlayerInfoList(room);
        _ = NotifyMatchRoomChangedAsync(room);
        if (room.PlayerList.Count >= 2 && room.PlayerList.All(m => m.IsReady))
        {
            _ = NotifyMatchGameStartAsync(room);
        }

        return Task.FromResult(response);
    }

    /*******************演示代码**************************/
    [Service]
    [ThreadSafe]
    public virtual int DoSomething0()
    {
        return State.WorldLevel;
    }

    [Discard]
    [ThreadSafe]
    protected virtual Task DoSomething1()
    {
        return Task.CompletedTask;
    }

    [ThreadSafe]
    protected void DoSomething2()
    {
    }

    [Discard]
    [TimeOut(12000)]
    protected virtual Task DoSomething3()
    {
        return Task.CompletedTask;
    }

    private class DelayTimer : TimerHandler<ServerComponentAgent>
    {
        protected override Task HandleTimer(ServerComponentAgent agent, GameEventArgs gameEventArgs)
        {
            return agent.TestDelayTimer();
        }
    }

    private class ScheduleTimer : TimerHandler<ServerComponentAgent>
    {
        protected override Task HandleTimer(ServerComponentAgent agent, GameEventArgs gameEventArgs)
        {
            return agent.TestScheduleTimer();
        }
    }

    /// <summary>
    /// 跨天定时器调用
    /// </summary>
    private class CrossDayTimeHandler : TimerHandler<ServerComponentAgent>
    {
        protected override async Task HandleTimer(ServerComponentAgent agent, GameEventArgs gameEventArgs)
        {
            LogHelper.Debug($"ServerCompAgent.CrossDayTimeHandler.跨天定时器执行{TimerHelper.CurrentDateTimeWithUtcFormat()}");
            await ActorManager.RoleCrossDay(1);
            await ActorManager.CrossDay(1, GlobalConst.ActorTypeServer);
        }
    }
    /*******************演示代码**************************/

    private async Task TryMatchAsync()
    {
        while (OwnerComponent.MatchQueue.Count >= 2)
        {
            var playerId1 = TakeNextMatchPlayer();
            var playerId2 = TakeNextMatchPlayer();
            if (playerId1 == 0 || playerId2 == 0)
            {
                return;
            }

            var room = await CreateRoomAsync(playerId1, playerId2);
            if (room == null)
            {
                continue;
            }

            await NotifyMatchSuccessAsync(room);
        }
    }

    private long TakeNextMatchPlayer()
    {
        while (OwnerComponent.MatchQueue.Count > 0)
        {
            var playerId = OwnerComponent.MatchQueue[0];
            OwnerComponent.MatchQueue.RemoveAt(0);
            if (playerId == 0 || OwnerComponent.PlayerRoomMap.ContainsKey(playerId))
            {
                continue;
            }

            var session = SessionManager.GetByRoleId(playerId);
            if (session == null)
            {
                continue;
            }

            return playerId;
        }

        return 0;
    }

    private async Task<MatchRoomRuntime> CreateDebugBotRoomAsync(long playerId)
    {
        if (!OwnerComponent.MatchQueue.Remove(playerId))
        {
            return null;
        }

        var player = await LoadMatchPlayerAsync(playerId);
        if (player == null)
        {
            return null;
        }

        var room = new MatchRoomRuntime
        {
            RoomId = ActorIdGenerator.GetUniqueId(),
            CreateTime = TimerHelper.UnixTimeSeconds(),
            PlayerList = new List<MatchRoomPlayerRuntime>
            {
                player,
                CreateDebugBotPlayer()
            }
        };

        OwnerComponent.MatchRoomMap[room.RoomId] = room;
        OwnerComponent.PlayerRoomMap[playerId] = room.RoomId;
        return room;
    }

    private async Task<MatchRoomRuntime> CreateRoomAsync(long playerId1, long playerId2)
    {
        var player1 = await LoadMatchPlayerAsync(playerId1);
        var player2 = await LoadMatchPlayerAsync(playerId2);
        if (player1 == null || player2 == null)
        {
            return null;
        }

        var room = new MatchRoomRuntime
        {
            RoomId = ActorIdGenerator.GetUniqueId(),
            CreateTime = TimerHelper.UnixTimeSeconds(),
            PlayerList = new List<MatchRoomPlayerRuntime> { player1, player2 }
        };

        OwnerComponent.MatchRoomMap[room.RoomId] = room;
        OwnerComponent.PlayerRoomMap[playerId1] = room.RoomId;
        OwnerComponent.PlayerRoomMap[playerId2] = room.RoomId;
        return room;
    }

    private static async Task<MatchRoomPlayerRuntime> LoadMatchPlayerAsync(long playerId)
    {
        var playerState = await GameDb.FindAsync<PlayerState>(m => m.Id == playerId, false);
        if (playerState == null)
        {
            return null;
        }

        return new MatchRoomPlayerRuntime
        {
            PlayerId = playerState.Id,
            Name = string.IsNullOrWhiteSpace(playerState.Name) ? playerState.RoleName : playerState.Name,
            Level = playerState.Level == 0 ? 1u : playerState.Level,
            Avatar = playerState.Avatar,
            IsReady = false
        };
    }

    private static MatchRoomPlayerRuntime CreateDebugBotPlayer()
    {
        return new MatchRoomPlayerRuntime
        {
            PlayerId = -1,
            Name = "Training Bot",
            Level = 1,
            Avatar = 1,
            IsReady = true
        };
    }

    private static bool ShouldUseDebugBotMatch(long playerId)
    {
        if (playerId <= 0)
        {
            return false;
        }

        var matchDebugBot = Environment.GetEnvironmentVariable("MATCH_DEBUG_BOT");
        if (string.Equals(matchDebugBot, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(matchDebugBot, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var isDebug = Environment.GetEnvironmentVariable("IsDebug");
        return string.Equals(isDebug, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(isDebug, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task NotifyMatchSuccessAsync(MatchRoomRuntime room)
    {
        var notify = new NotifyMatchSuccess
        {
            RoomId = room.RoomId,
            PlayerList = BuildMatchRoomPlayerInfoList(room)
        };

        foreach (var player in room.PlayerList)
        {
            var session = SessionManager.GetByRoleId(player.PlayerId);
            if (session != null)
            {
                await session.WriteAsync(notify);
            }
        }
    }

    private static async Task NotifyMatchRoomChangedAsync(MatchRoomRuntime room)
    {
        var notify = new NotifyMatchRoomChanged
        {
            RoomId = room.RoomId,
            PlayerList = BuildMatchRoomPlayerInfoList(room)
        };

        foreach (var player in room.PlayerList)
        {
            var session = SessionManager.GetByRoleId(player.PlayerId);
            if (session != null)
            {
                await session.WriteAsync(notify);
            }
        }
    }

    private static async Task NotifyRoomDismissedAsync(MatchRoomRuntime room)
    {
        var notify = new NotifyMatchRoomChanged
        {
            RoomId = 0,
            PlayerList = new List<MatchRoomPlayerInfo>()
        };

        foreach (var player in room.PlayerList)
        {
            var session = SessionManager.GetByRoleId(player.PlayerId);
            if (session != null)
            {
                await session.WriteAsync(notify);
            }
        }
    }

    private static async Task NotifyMatchGameStartAsync(MatchRoomRuntime room)
    {
        var playerList = BuildMatchRoomPlayerInfoList(room);
        for (var i = 0; i < room.PlayerList.Count; i++)
        {
            var player = room.PlayerList[i];
            if (player.PlayerId <= 0)
            {
                continue;
            }

            var notify = new NotifyMatchGameStart
            {
                RoomId = room.RoomId,
                SceneName = GetMatchSceneName(),
                SeatIndex = i,
                Camp = i == 0 ? 1 : 2,
                PlayerList = playerList
            };

            var session = SessionManager.GetByRoleId(player.PlayerId);
            if (session != null)
            {
                await session.WriteAsync(notify);
            }
        }
    }

    private static string GetMatchSceneName()
    {
        var sceneName = Environment.GetEnvironmentVariable("MATCH_GAME_SCENE");
        return string.IsNullOrWhiteSpace(sceneName) ? "ChineseChess" : sceneName;
    }

    private static List<MatchRoomPlayerInfo> BuildMatchRoomPlayerInfoList(MatchRoomRuntime room)
    {
        return room.PlayerList.Select(m => new MatchRoomPlayerInfo
        {
            PlayerId = m.PlayerId,
            Name = m.Name,
            Level = m.Level,
            Avatar = m.Avatar,
            IsReady = m.IsReady
        }).ToList();
    }
}
