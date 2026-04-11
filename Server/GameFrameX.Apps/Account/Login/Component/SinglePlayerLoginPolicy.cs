using System.Linq;
using GameFrameX.Apps.Player.Player.Entity;

namespace GameFrameX.Apps.Account.Login.Component;

public static class SinglePlayerLoginPolicy
{
    public static async Task<PlayerState> GetPrimaryPlayerByAccountIdAsync(long accountId)
    {
        if (accountId <= 0)
        {
            return null;
        }

        var playerList = await GameDb.FindListAsync<PlayerState>(m => m.AccountId == accountId);
        return SelectPrimaryPlayer(playerList);
    }

    public static List<PlayerState> BuildSinglePlayerList(PlayerState playerState)
    {
        return playerState == null ? new List<PlayerState>() : new List<PlayerState> { playerState };
    }

    private static PlayerState SelectPrimaryPlayer(List<PlayerState> playerList)
    {
        if (playerList == null || playerList.Count == 0)
        {
            return null;
        }

        return playerList
            .OrderBy(m => m.CreateTime > 0 ? m.CreateTime : long.MaxValue)
            .ThenBy(m => m.Id)
            .FirstOrDefault();
    }
}
