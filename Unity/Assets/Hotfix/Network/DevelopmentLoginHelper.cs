using System;
using UnityEngine;

namespace Hotfix.Network
{
    public static class DevelopmentLoginHelper
    {
        public static bool Enabled => !Application.isEditor;

        public static (string accountId, string displayName, string sdkToken) CreateIdentity()
        {
            var company = string.IsNullOrWhiteSpace(Application.companyName) ? "dev" : Application.companyName;
            var product = string.IsNullOrWhiteSpace(Application.productName) ? "player" : Application.productName;
            var accountId = $"dev_{company}_{product}_{Environment.UserName}".Replace(' ', '_');
            var displayName = $"Dev-{Environment.UserName}";
            var sdkToken = $"dev-token-{company}-{product}-{Environment.UserName}".Replace(' ', '_');
            return (accountId, displayName, sdkToken);
        }
    }
}
