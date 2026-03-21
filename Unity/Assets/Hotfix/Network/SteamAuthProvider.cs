using System;
using System.Linq;
using System.Reflection;

namespace Hotfix.Network
{
    public static class SteamAuthProvider
    {
        public static bool TryGetAuthSessionTicket(out string ticketBase64)
        {
            ticketBase64 = null;

            try
            {
                var steamAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "com.rlabrecque.steamworks.net");
                if (steamAssembly == null)
                {
                    return false;
                }

                var steamUserType = steamAssembly.GetType("Steamworks.SteamUser");
                if (steamUserType == null)
                {
                    return false;
                }

                // Steamworks.NET signature changed in newer versions to include SteamNetworkingIdentity by ref.
                var methods = steamUserType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "GetAuthSessionTicket")
                    .ToArray();

                MethodInfo method = null;
                bool hasIdentity = false;
                Type identityType = null;

                foreach (var m in methods)
                {
                    var ps = m.GetParameters();
                    if (ps.Length == 3 &&
                        ps[0].ParameterType == typeof(byte[]) &&
                        ps[1].ParameterType == typeof(int) &&
                        ps[2].ParameterType == typeof(uint).MakeByRefType())
                    {
                        method = m;
                        hasIdentity = false;
                        break;
                    }

                    if (ps.Length == 4 &&
                        ps[0].ParameterType == typeof(byte[]) &&
                        ps[1].ParameterType == typeof(int) &&
                        ps[2].ParameterType == typeof(uint).MakeByRefType() &&
                        ps[3].ParameterType.IsByRef)
                    {
                        method = m;
                        hasIdentity = true;
                        identityType = ps[3].ParameterType.GetElementType();
                        break;
                    }
                }

                if (method == null)
                {
                    return false;
                }

                var buffer = new byte[1024];
                object[] args;
                if (hasIdentity && identityType != null)
                {
                    var identity = Activator.CreateInstance(identityType);
                    args = new object[] { buffer, buffer.Length, 0u, identity };
                }
                else
                {
                    args = new object[] { buffer, buffer.Length, 0u };
                }
                method.Invoke(null, args);
                var ticketSize = (uint)args[2];
                if (ticketSize == 0)
                {
                    return false;
                }

                ticketBase64 = Convert.ToBase64String(buffer, 0, (int)ticketSize);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
