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
using System.Text.Json;

namespace GameFrameX.Launcher.StartUp;

/// <summary>
/// 游戏服务器
/// </summary>
[StartUpTag(GlobalConst.GameServiceName)]
internal sealed class AppStartUpGame : AppStartUpBase
{
    private const string DefaultDataBaseUrl = "mongodb+srv://gameframex:f9v42aU9DVeFNfAF@gameframex.8taphic.mongodb.net/?retryWrites=true&w=majority";
    private const string DefaultDataBaseName = "gameframex";

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
            LogHelper.Info($"[Mongo] init start. db={Setting.DataBaseName}, url={Setting.DataBaseUrl}");
            var initResult = await GameDb.Init<MongoDbService>(new DbOptions { ConnectionString = Setting.DataBaseUrl, Name = Setting.DataBaseName, });
            LogHelper.Info($"[Mongo] init result={initResult}");
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

        GlobalSettings.IsAppRunning = false;
        LogHelper.Info("退出服务器开始");
        await HotfixManager.Stop(exitMessage);
        LogHelper.Info("退出服务器成功");
    }

    protected override void Init()
    {
        var fileConfig = LoadFileConfig();

        if (Setting == null)
        {
            Setting = new AppSetting
            {
                ServerId = GetInt("ServerId", fileConfig.ServerId, GlobalConst.GameServiceServerId),
                ServerType = GetString("ServerType", fileConfig.ServerType, GlobalConst.GameServiceName),
                ServerName = GetString("ServerName", fileConfig.ServerName, GetString("ServerType", fileConfig.ServerType, GlobalConst.GameServiceName)),
                TagName = GetString("TagName", fileConfig.TagName, "GameFrameX"),
                InnerHost = GetString("InnerHost", fileConfig.InnerHost, "0.0.0.0"),
                InnerPort = GetUShort("InnerPort", fileConfig.InnerPort, 29100),
                OuterHost = GetString("OuterHost", fileConfig.OuterHost, "0.0.0.0"),
                OuterPort = GetUShort("OuterPort", fileConfig.OuterPort, 29010),
                HttpUrl = GetString("HttpUrl", fileConfig.HttpUrl, "/game/api/"),
                IsEnableHttp = GetBool("IsEnableHttp", fileConfig.IsEnableHttp, true),
                HttpPort = GetUShort("HttpPort", fileConfig.HttpPort, 28080),
                HttpsPort = GetUShort("HttpsPort", fileConfig.HttpsPort, 0),
                MetricsPort = GetUShort("APMPort", fileConfig.MetricsPort, 29090),
                IsEnableTcp = GetBool("IsEnableTcp", fileConfig.IsEnableTcp, true),
                IsEnableWebSocket = GetBool("IsEnableWebSocket", fileConfig.IsEnableWebSocket, false),
                WsPort = GetUShort("WsPort", fileConfig.WsPort, 29110),
                MinModuleId = GetShort("MinModuleId", fileConfig.MinModuleId, 10),
                MaxModuleId = GetShort("MaxModuleId", fileConfig.MaxModuleId, 9999),
                DiscoveryCenterHost = GetString("DiscoveryCenterIp", fileConfig.DiscoveryCenterHost, "127.0.0.1"),
                DiscoveryCenterPort = GetUShort("DiscoveryCenterPort", fileConfig.DiscoveryCenterPort, 21001),
                DataBaseUrl = GetString("DataBaseUrl", fileConfig.DataBaseUrl, DefaultDataBaseUrl),
                DataBaseName = GetString("DataBaseName", fileConfig.DataBaseName, DefaultDataBaseName),
                IsDebug = GetBool("IsDebug", fileConfig.IsDebug, false),
                HttpIsDevelopment = true,
            };
        }
        else
        {
            Setting.ServerId = GetInt("ServerId", fileConfig.ServerId, Setting.ServerId == 0 ? GlobalConst.GameServiceServerId : Setting.ServerId);
            Setting.ServerType = GetString("ServerType", fileConfig.ServerType, string.IsNullOrWhiteSpace(Setting.ServerType) ? GlobalConst.GameServiceName : Setting.ServerType);
            Setting.ServerName = GetString("ServerName", fileConfig.ServerName, string.IsNullOrWhiteSpace(Setting.ServerName) ? Setting.ServerType : Setting.ServerName);
            Setting.TagName = GetString("TagName", fileConfig.TagName, string.IsNullOrWhiteSpace(Setting.TagName) ? "GameFrameX" : Setting.TagName);
            Setting.InnerHost = GetString("InnerHost", fileConfig.InnerHost, string.IsNullOrWhiteSpace(Setting.InnerHost) ? "0.0.0.0" : Setting.InnerHost);
            Setting.InnerPort = GetUShort("InnerPort", fileConfig.InnerPort, Setting.InnerPort == 0 ? (ushort)29100 : Setting.InnerPort);
            Setting.OuterHost = GetString("OuterHost", fileConfig.OuterHost, string.IsNullOrWhiteSpace(Setting.OuterHost) ? "0.0.0.0" : Setting.OuterHost);
            Setting.OuterPort = GetUShort("OuterPort", fileConfig.OuterPort, Setting.OuterPort == 0 ? (ushort)29010 : Setting.OuterPort);
            Setting.HttpUrl = GetString("HttpUrl", fileConfig.HttpUrl, string.IsNullOrWhiteSpace(Setting.HttpUrl) ? "/game/api/" : Setting.HttpUrl);
            Setting.IsEnableHttp = GetBool("IsEnableHttp", fileConfig.IsEnableHttp, Setting.IsEnableHttp);
            Setting.HttpPort = GetUShort("HttpPort", fileConfig.HttpPort, Setting.HttpPort == 0 ? (ushort)28080 : Setting.HttpPort);
            Setting.HttpsPort = GetUShort("HttpsPort", fileConfig.HttpsPort, Setting.HttpsPort);
            Setting.MetricsPort = GetUShort("APMPort", fileConfig.MetricsPort, Setting.MetricsPort == 0 ? (ushort)29090 : Setting.MetricsPort);
            Setting.IsEnableTcp = GetBool("IsEnableTcp", fileConfig.IsEnableTcp, Setting.IsEnableTcp);
            Setting.IsEnableWebSocket = GetBool("IsEnableWebSocket", fileConfig.IsEnableWebSocket, Setting.IsEnableWebSocket);
            Setting.WsPort = GetUShort("WsPort", fileConfig.WsPort, Setting.WsPort == 0 ? (ushort)29110 : Setting.WsPort);
            Setting.MinModuleId = GetShort("MinModuleId", fileConfig.MinModuleId, Setting.MinModuleId == 0 ? (short)10 : Setting.MinModuleId);
            Setting.MaxModuleId = GetShort("MaxModuleId", fileConfig.MaxModuleId, Setting.MaxModuleId == 0 ? (short)9999 : Setting.MaxModuleId);
            Setting.DiscoveryCenterHost = GetString("DiscoveryCenterIp", fileConfig.DiscoveryCenterHost, string.IsNullOrWhiteSpace(Setting.DiscoveryCenterHost) ? "127.0.0.1" : Setting.DiscoveryCenterHost);
            Setting.DiscoveryCenterPort = GetUShort("DiscoveryCenterPort", fileConfig.DiscoveryCenterPort, Setting.DiscoveryCenterPort == 0 ? (ushort)21001 : Setting.DiscoveryCenterPort);
            Setting.DataBaseUrl = GetString("DataBaseUrl", fileConfig.DataBaseUrl, string.IsNullOrWhiteSpace(Setting.DataBaseUrl) ? DefaultDataBaseUrl : Setting.DataBaseUrl);
            Setting.DataBaseName = GetString("DataBaseName", fileConfig.DataBaseName, string.IsNullOrWhiteSpace(Setting.DataBaseName) ? DefaultDataBaseName : Setting.DataBaseName);
            Setting.IsDebug = GetBool("IsDebug", fileConfig.IsDebug, Setting.IsDebug);
        }

        base.Init();
    }

    private static AppConfigFile LoadFileConfig()
    {
        foreach (var path in GetConfigCandidatePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var configs = JsonSerializer.Deserialize<List<AppConfigFile>>(json);
                var config = configs?.FirstOrDefault();
                if (config != null)
                {
                    LogHelper.Info($"加载配置文件成功: {path}");
                    return config;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error($"加载配置文件失败: {path}, error={e.Message}");
            }
        }

        return new AppConfigFile();
    }

    private static IEnumerable<string> GetConfigCandidatePaths()
    {
        var baseDirectory = AppContext.BaseDirectory;
        yield return Path.Combine(baseDirectory, "app_config.json");
        yield return Path.Combine(baseDirectory, "Configs", "app_config.json");
        yield return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "Configs", "app_config.json"));
        yield return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "Configs", "app_config.json"));
        yield return Path.Combine(Directory.GetCurrentDirectory(), "app_config.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Configs", "app_config.json");
    }

    private static string GetString(string envKey, string fileValue, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(envKey) ?? fileValue ?? defaultValue;
    }

    private static bool GetBool(string envKey, bool? fileValue, bool defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(envKey);
        return bool.TryParse(env, out var result) ? result : fileValue ?? defaultValue;
    }

    private static ushort GetUShort(string envKey, ushort? fileValue, ushort defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(envKey);
        return ushort.TryParse(env, out var result) ? result : fileValue ?? defaultValue;
    }

    private static short GetShort(string envKey, short? fileValue, short defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(envKey);
        return short.TryParse(env, out var result) ? result : fileValue ?? defaultValue;
    }

    private static long GetLong(string envKey, long? fileValue, long defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(envKey);
        return long.TryParse(env, out var result) ? result : fileValue ?? defaultValue;
    }

    private static int GetInt(string envKey, long? fileValue, int defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(envKey);
        if (int.TryParse(env, out var result))
        {
            return result;
        }

        if (fileValue.HasValue && fileValue.Value >= int.MinValue && fileValue.Value <= int.MaxValue)
        {
            return (int)fileValue.Value;
        }

        return defaultValue;
    }

    private sealed class AppConfigFile
    {
        public string ServerType { get; set; }
        public long? ServerId { get; set; }
        public string ServerName { get; set; }
        public string TagName { get; set; }
        public string InnerHost { get; set; }
        public ushort? InnerPort { get; set; }
        public string OuterHost { get; set; }
        public ushort? OuterPort { get; set; }
        public string HttpUrl { get; set; }
        public bool? IsEnableHttp { get; set; }
        public ushort? HttpPort { get; set; }
        public ushort? HttpsPort { get; set; }
        public ushort? MetricsPort { get; set; }
        public bool? IsEnableTcp { get; set; }
        public bool? IsEnableWebSocket { get; set; }
        public ushort? WsPort { get; set; }
        public short? MinModuleId { get; set; }
        public short? MaxModuleId { get; set; }
        public string DiscoveryCenterHost { get; set; }
        public ushort? DiscoveryCenterPort { get; set; }
        public string DataBaseUrl { get; set; }
        public string DataBaseName { get; set; }
        public bool? IsDebug { get; set; }
    }
}
