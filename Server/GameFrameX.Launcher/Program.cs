using GameFrameX.NetWork.Abstractions;
using System.Reflection;
using System.Runtime.Loader;

namespace GameFrameX.Launcher;

internal static class Program
{
    private static readonly Action MarkProcessAsKilled = CreateKillAction();

    private static async Task Main(string[] args)
    {
#if DEBUG
        LogOptions.Default.IsConsole = true;
#endif
        RegisterGracefulExitGuards();
        await GameApp.Entry(args, () =>
        {
            CacheStateTypeManager.Init();
            MessageProtoHelper.Init(typeof(MessageProtoHandler).Assembly);
        }, LogAction);
    }

    private static void RegisterGracefulExitGuards()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => MarkProcessAsKilled();
        AssemblyLoadContext.Default.Unloading += _ => MarkProcessAsKilled();
    }

    private static Action CreateKillAction()
    {
        var appExitHandlerType = typeof(GameApp).Assembly.GetType("GameFrameX.StartUp.AppExitHandler");
        var killMethod = appExitHandlerType?.GetMethod("Kill", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return () => killMethod?.Invoke(null, null);
    }

    private static void LogAction(LogOptions options)
    {
        // 发布之后控制台不输出
#if DEBUG
        options.IsConsole = true;
#else
        options.IsConsole = false;
#endif
        options.IsConsole = true;
    }
}
