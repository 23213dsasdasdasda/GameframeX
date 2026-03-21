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

using GameFrameX.DataBase.Abstractions;
using GameFrameX.Foundation.Utility;

namespace GameFrameX.Launcher.StartUp;

/// <summary>
/// 游戏服务器
/// </summary>
[StartUpTag(GlobalConst.GameServiceName)]
internal sealed class AppStartUpGame : AppStartUpBase
{
    public override async Task StartAsync()
    {
        string exitMessage = null;
        try
        {
            LogHelper.Info($"开始启动服务器{Setting.ServerType}");
            var hotfixPath = Directory.GetCurrentDirectory() + "/hotfix";
            if (!Directory.Exists(hotfixPath))
            {
                Directory.CreateDirectory(hotfixPath);
            }

            LogHelper.Debug("开始配置Actor限制逻辑...");
            ActorLimit.Init(ActorLimit.RuleType.None);
            LogHelper.Debug("配置Actor限制逻辑结束...");

            LogHelper.Debug("开始启动数据库服务...");
            var initResult = await GameDb.Init<MongoDbService>(new DbOptions { ConnectionString = Setting.DataBaseUrl, Name = Setting.DataBaseName, });
            if (initResult == false)
            {
                throw new InvalidOperationException("数据库服务启动失败");
            }

            LogHelper.DebugConsole("启动数据库服务 结束...");

            LogHelper.DebugConsole("注册组件开始...");
            await ComponentRegister.Init(typeof(AppsHandler).Assembly);
            LogHelper.DebugConsole("注册组件结束...");

            LogHelper.DebugConsole("开始加载热更新模块...");
            await HotfixManager.LoadHotfixModule(Setting);
            LogHelper.DebugConsole("加载热更新模块结束...");

            LogHelper.DebugConsole("进入游戏主循环...");
            GlobalSettings.LaunchTime = TimerHelper.GetUtcNow();
            GlobalSettings.IsAppRunning = true;
            LogHelper.Info($"服务器{Setting.ServerType}启动结束...");
            exitMessage = await AppExitToken;
        }
        catch (Exception e)
        {
            LogHelper.Info($"服务器执行异常，e:{e}");
            LogHelper.Fatal(e);
        }

        LogHelper.Info("退出服务器开始");
        await HotfixManager.Stop(exitMessage);
        LogHelper.Info("退出服务器成功");
    }

    protected override void Init()
    {
        if (Setting == null)
        {
            Setting = new AppSetting
            {
                ServerId = GlobalConst.GameServiceServerId,
                ServerType = GlobalConst.GameServiceName,
                IsEnableTcp = true,
                InnerPort = ushort.Parse(Environment.GetEnvironmentVariable("InnerPort") ?? "29100"),
                MetricsPort = ushort.Parse(Environment.GetEnvironmentVariable("APMPort") ?? "29090"),
                HttpPort = ushort.Parse(Environment.GetEnvironmentVariable("HttpPort") ?? "28080"),
                IsEnableHttp = true,
                WsPort = ushort.Parse(Environment.GetEnvironmentVariable("WsPort") ?? "29110"),
                MinModuleId = short.Parse(Environment.GetEnvironmentVariable("MinModuleId") ?? "10"),
                HttpIsDevelopment = true,
                MaxModuleId = short.Parse(Environment.GetEnvironmentVariable("MaxModuleId") ?? "9999"),

                DiscoveryCenterHost = Environment.GetEnvironmentVariable("DiscoveryCenterIp") ?? "127.0.0.1",
                TagName = "GameFrameX",
                DiscoveryCenterPort = ushort.Parse(Environment.GetEnvironmentVariable("DiscoveryCenterPort") ?? "21001"),

                DataBaseUrl = Environment.GetEnvironmentVariable("DataBaseUrl")
                              ?? "mongodb+srv://gameframex:f9v42aU9DVeFNfAF@gameframex.8taphic.mongodb.net/?retryWrites=true&w=majority",
                DataBaseName = Environment.GetEnvironmentVariable("DataBaseName") ?? "gameframex",
            };
        }
        else
        {
            Setting.ServerId = Setting.ServerId == 0 ? GlobalConst.GameServiceServerId : Setting.ServerId;
            Setting.ServerType = string.IsNullOrWhiteSpace(Setting.ServerType) ? GlobalConst.GameServiceName : Setting.ServerType;
            Setting.MinModuleId = Setting.MinModuleId == 0 ? short.Parse(Environment.GetEnvironmentVariable("MinModuleId") ?? "10") : Setting.MinModuleId;
            Setting.MaxModuleId = Setting.MaxModuleId == 0 ? short.Parse(Environment.GetEnvironmentVariable("MaxModuleId") ?? "9999") : Setting.MaxModuleId;
            Setting.InnerPort = Setting.InnerPort == 0 ? ushort.Parse(Environment.GetEnvironmentVariable("InnerPort") ?? "29100") : Setting.InnerPort;
            Setting.WsPort = Setting.WsPort == 0 ? ushort.Parse(Environment.GetEnvironmentVariable("WsPort") ?? "29110") : Setting.WsPort;
            Setting.HttpPort = Setting.HttpPort == 0 ? ushort.Parse(Environment.GetEnvironmentVariable("HttpPort") ?? "28080") : Setting.HttpPort;
            Setting.MetricsPort = Setting.MetricsPort == 0 ? ushort.Parse(Environment.GetEnvironmentVariable("APMPort") ?? "29090") : Setting.MetricsPort;
            Setting.DiscoveryCenterHost = string.IsNullOrWhiteSpace(Setting.DiscoveryCenterHost)
                ? Environment.GetEnvironmentVariable("DiscoveryCenterIp") ?? "127.0.0.1"
                : Setting.DiscoveryCenterHost;
            Setting.DiscoveryCenterPort = Setting.DiscoveryCenterPort == 0
                ? ushort.Parse(Environment.GetEnvironmentVariable("DiscoveryCenterPort") ?? "21001")
                : Setting.DiscoveryCenterPort;
            Setting.TagName = string.IsNullOrWhiteSpace(Setting.TagName) ? "GameFrameX" : Setting.TagName;
            Setting.DataBaseUrl = string.IsNullOrWhiteSpace(Setting.DataBaseUrl)
                ? Environment.GetEnvironmentVariable("DataBaseUrl")
                  ?? "mongodb+srv://gameframex:f9v42aU9DVeFNfAF@gameframex.8taphic.mongodb.net/?retryWrites=true&w=majority"
                : Setting.DataBaseUrl;
            Setting.DataBaseName = string.IsNullOrWhiteSpace(Setting.DataBaseName)
                ? Environment.GetEnvironmentVariable("DataBaseName") ?? "gameframex"
                : Setting.DataBaseName;
        }

        base.Init();
    }
}
