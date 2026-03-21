// See https://aka.ms/new-console-template for more information
using System.Reflection;

static void DumpType(Assembly asm, string fullName)
{
    var type = asm.GetType(fullName);
    if (type == null)
    {
        Console.WriteLine($"Type not found: {fullName}");
        return;
    }

    Console.WriteLine($"Type: {type.FullName}");
    Console.WriteLine($"Base: {type.BaseType}");
    Console.WriteLine($"IsAbstract: {type.IsAbstract}");
    Console.WriteLine($"GenericArgs: {string.Join(", ", type.GetGenericArguments().Select(t => t.Name))}");
    Console.WriteLine("Methods:");
    foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
    {
        var parameters = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.FullName ?? p.ParameterType.ToString()} {p.Name}"));
        var access = m.IsPublic ? "public" : m.IsFamily ? "protected" : m.IsPrivate ? "private" : m.IsAssembly ? "internal" : "other";
        var returnType = m.ReturnType.FullName ?? m.ReturnType.ToString();
        Console.WriteLine($"  {access} {returnType} {m.Name}({parameters})");
    }
    Console.WriteLine("Properties:");
    foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
    {
        Console.WriteLine($"  {p.PropertyType.Name} {p.Name} (CanRead:{p.CanRead}, CanWrite:{p.CanWrite})");
    }
    Console.WriteLine("Fields:");
    foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
    {
        Console.WriteLine($"  {f.FieldType.Name} {f.Name}");
    }
    Console.WriteLine();
}

var asmPath = @"E:\Unity Editor\GameFrameX\Server\bin\app_debug\GameFrameX.Core.dll";
var asm = Assembly.LoadFrom(asmPath);

DumpType(asm, "GameFrameX.Core.BaseHandler.Normal.BaseMessageHandler`1");
DumpType(asm, "GameFrameX.Core.BaseHandler.Normal.BaseComponentHandler`1");
DumpType(asm, "GameFrameX.Core.BaseHandler.Normal.BaseRpcComponentHandler`2");
DumpType(asm, "GameFrameX.Core.BaseHandler.Normal.BaseRpcMessageHandler`2");
DumpType(asm, "GameFrameX.Core.BaseHandler.GlobalComponentHandler`1");
DumpType(asm, "GameFrameX.Core.BaseHandler.GlobalRpcComponentHandler`3");
DumpType(asm, "GameFrameX.Core.BaseHandler.PlayerComponentHandler`2");
DumpType(asm, "GameFrameX.Core.BaseHandler.PlayerRpcComponentHandler`3");
