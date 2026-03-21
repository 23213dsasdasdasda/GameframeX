using Hotfix.Proto;

namespace Hotfix.UI
{
    public sealed class LoginFlowData
    {
        public bool IsOfflineMode { get; set; }
        public RespLogin RespLogin { get; set; }
        public string SteamId { get; set; }
        public string SdkToken { get; set; }
    }
}
