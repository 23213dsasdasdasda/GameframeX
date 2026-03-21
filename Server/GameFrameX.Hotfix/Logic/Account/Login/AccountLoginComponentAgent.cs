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

using System;
using System.Linq;
using GameFrameX.Apps.Account.Login.Component;
using GameFrameX.Apps.Account.Login.Entity;
using GameFrameX.Apps.Player.Player.Entity;
using GameFrameX.DataBase;
using GameFrameX.Hotfix.Logic.Player.Login;
using GameFrameX.Monitor.Account;
using GameFrameX.Monitor.Player;

namespace GameFrameX.Hotfix.Logic.Account.Login;

public class AccountLoginComponentAgent : StateComponentAgent<LoginComponent, LoginState>
{
    private static void AppendTrace(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
        File.AppendAllText("/app/logs/login_trace.log", line);
    }

    public async Task OnLogin(INetWorkChannel workChannel, ReqLogin reqLogin)
    {
        ArgumentNullException.ThrowIfNull(workChannel);
        ArgumentNullException.ThrowIfNull(reqLogin);

        LogHelper.Info($"[OnLogin-Begin] reqUniqueId={reqLogin.UniqueId} user={reqLogin.UserName} id={reqLogin.Id} sdkType={reqLogin.SdkType}");
        AppendTrace($"[OnLogin-Begin] reqUniqueId={reqLogin.UniqueId} user={reqLogin.UserName} id={reqLogin.Id} sdkType={reqLogin.SdkType}");

        var respLogin = new RespLogin
        {
            UniqueId = reqLogin.UniqueId,
        };

        var accountName = ResolveAccountName(reqLogin);
        if (string.IsNullOrWhiteSpace(accountName))
        {
            respLogin.Code = 1;
            LogHelper.Info($"[OnLogin-RespLogin] respMsg=19660811 respUniqueId={respLogin.UniqueId} code={respLogin.Code} id={respLogin.Id} reason=empty_account");
            AppendTrace($"[OnLogin-RespLogin] respMsg=19660811 respUniqueId={respLogin.UniqueId} code={respLogin.Code} id={respLogin.Id} reason=empty_account");
            await workChannel.WriteAsync(respLogin);
            return;
        }

        LoginState loginState = null;
        try
        {
            reqLogin.UserName = accountName;
            loginState = await OnLoginInternal(reqLogin);
            if (loginState == null)
            {
                var accountId = ActorIdGenerator.GetUniqueId();
                loginState = await Register(accountId, reqLogin);
            }

            var createTime = loginState.CreateTime > 0 ? loginState.CreateTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            respLogin.Code = 0;
            respLogin.Id = loginState.Id;
            respLogin.RoleName = string.IsNullOrWhiteSpace(loginState.NickName) ? reqLogin.UserName : loginState.NickName;
            respLogin.Level = loginState.Level == 0 ? 1u : loginState.Level;
            respLogin.CreateTime = createTime;
            respLogin.Token = $"{loginState.Id}_{Guid.NewGuid():N}";
            respLogin.ServerTime = DateTime.UtcNow.ToString("O");

            LogHelper.Info($"[OnLogin-RespLogin] respMsg=19660811 respUniqueId={respLogin.UniqueId} code={respLogin.Code} id={respLogin.Id} role={respLogin.RoleName}");
            AppendTrace($"[OnLogin-RespLogin] respMsg=19660811 respUniqueId={respLogin.UniqueId} code={respLogin.Code} id={respLogin.Id} role={respLogin.RoleName}");
            await workChannel.WriteAsync(respLogin);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[LoginException] User:{reqLogin.UserName} Err:{ex}");
            respLogin.Code = 500;
            LogHelper.Info($"[OnLogin-RespLogin] respMsg=19660811 respUniqueId={respLogin.UniqueId} code={respLogin.Code} id={respLogin.Id} reason=exception");
            AppendTrace($"[LoginException] User:{reqLogin.UserName} Err:{ex}");
            AppendTrace($"[OnLogin-RespLogin] respMsg=19660811 respUniqueId={respLogin.UniqueId} code={respLogin.Code} id={respLogin.Id} reason=exception");
            await workChannel.WriteAsync(respLogin);
            return;
        }

        try
        {
            await AutoPlayerLogin(workChannel, reqLogin, loginState);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[AutoPlayerLoginException] User:{reqLogin.UserName} Err:{ex}");
        }
    }

    private static async Task<LoginState> OnLoginInternal(ReqLogin reqLogin)
    {
        MetricsAccountHelper.LoginCounterOptions.Inc();
        if (string.IsNullOrWhiteSpace(reqLogin.Password))
        {
            return await GameDb.FindAsync<LoginState>(m => m.UserName == reqLogin.UserName, false);
        }

        return await GameDb.FindAsync<LoginState>(m => m.UserName == reqLogin.UserName && m.Password == reqLogin.Password, false);
    }

    private static async Task<LoginState> Register(long accountId, ReqLogin reqLogin)
    {
        MetricsAccountHelper.RegisterCounterOptions.Inc();
        var loginState = new LoginState
        {
            Id = accountId,
            UserName = reqLogin.UserName,
            Password = reqLogin.Password ?? string.Empty,
            NickName = reqLogin.UserName,
            Level = 1,
            State = 0,
        };
        await GameDb.AddOrUpdateAsync(loginState);
        return loginState;
    }

    private static async Task AutoPlayerLogin(INetWorkChannel workChannel, ReqLogin reqLogin, LoginState loginState)
    {
        var playerState = await GetOrCreateSinglePlayer(loginState, reqLogin.UserName);
        if (playerState == null)
        {
            return;
        }

        workChannel.SetData(GlobalConst.ActorIdKey, playerState.Id);

        var response = new RespPlayerLogin();
        var playerComponentAgent = await ActorManager.GetComponentAgent<PlayerComponentAgent>(playerState.Id);
        await playerComponentAgent.OnPlayerLogin(workChannel, playerState, response);
        await workChannel.WriteAsync(response);
    }

    private static async Task<PlayerState> GetOrCreateSinglePlayer(LoginState loginState, string fallbackUserName)
    {
        var playerList = await GameDb.FindListAsync<PlayerState>(m => m.AccountId == loginState.Id);
        var playerState = playerList?
            .OrderBy(m => m.CreateTime)
            .FirstOrDefault() ?? playerList?.FirstOrDefault();

        if (playerState != null)
        {
            return playerState;
        }

        var resolvedUserName = string.IsNullOrWhiteSpace(loginState.UserName) ? fallbackUserName : loginState.UserName;
        var roleName = string.IsNullOrWhiteSpace(loginState.NickName) ? resolvedUserName : loginState.NickName;
        playerState = new PlayerState
        {
            Id = ActorIdGenerator.GetActorId(GlobalConst.ActorTypePlayer),
            AccountId = loginState.Id,
            UserName = resolvedUserName,
            RoleName = roleName,
            Name = roleName,
            Level = 1,
            State = 0,
            Avatar = 0,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        MetricsPlayerHelper.CreateCounterOptions.Inc();
        await GameDb.SaveOneAsync(playerState);
        return playerState;
    }

    private static string ResolveAccountName(ReqLogin reqLogin)
    {
        if (!string.IsNullOrWhiteSpace(reqLogin.UserName))
        {
            return reqLogin.UserName.Trim();
        }

        if (reqLogin.Id != 0)
        {
            return reqLogin.Id.ToString();
        }

        if (!string.IsNullOrWhiteSpace(reqLogin.Device))
        {
            return reqLogin.Device.Trim();
        }

        if (!string.IsNullOrWhiteSpace(reqLogin.SdkToken))
        {
            return reqLogin.SdkToken.Trim();
        }

        return null;
    }
}
