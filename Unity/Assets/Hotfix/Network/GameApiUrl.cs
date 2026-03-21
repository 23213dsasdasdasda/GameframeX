using System.Text;

namespace Hotfix.Network
{
    public static class GameApiUrl
    {
        // Development/testing endpoint.
        private const string DevelopmentBaseUrl = "http://47.113.221.3:28080";

        // Production endpoint. Prefer HTTPS + domain in online environment.
        private const string ProductionBaseUrl = "https://47.113.221.3:28080";

        // Development/testing TCP game server.
        private const string DevelopmentTcpHost = "47.113.221.3";
        private const int DevelopmentTcpPort = 29100;

        // Production TCP game server. Prefer domain in online environment.
        private const string ProductionTcpHost = "47.113.221.3";
        private const int ProductionTcpPort = 29100;

        public static string BaseUrl
        {
            get
            {
                var url = GetBaseUrlByEnvironment();
                return url.TrimEnd('/');
            }
        }

        public static string BuildGameApiUrl(string requestName)
        {
            return $"{BaseUrl}/game/api/{ToSnakeCase(requestName)}";
        }

        public static string BuildGameTcpUrl()
        {
            return $"tcp://{TcpHost}:{TcpPort}";
        }

        public static string TcpHost
        {
            get
            {
                return GetTcpHostByEnvironment();
            }
        }

        public static int TcpPort
        {
            get
            {
                return GetTcpPortByEnvironment();
            }
        }

        private static string GetBaseUrlByEnvironment()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return DevelopmentBaseUrl;
#else
            return ProductionBaseUrl;
#endif
        }

        private static string GetTcpHostByEnvironment()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return DevelopmentTcpHost;
#else
            return ProductionTcpHost;
#endif
        }

        private static int GetTcpPortByEnvironment()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return DevelopmentTcpPort;
#else
            return ProductionTcpPort;
#endif
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var sb = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        sb.Append('_');
                    }

                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
