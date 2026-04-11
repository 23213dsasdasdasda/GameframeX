using System.Collections.Generic;
using Hotfix.Proto;

namespace Hotfix.Manager
{
    public static class MatchBattleContext
    {
        private static List<MatchRoomPlayerInfo> _playerList = new List<MatchRoomPlayerInfo>();

        public static long RoomId { get; private set; }

        public static string SceneName { get; private set; }

        public static int SeatIndex { get; private set; } = -1;

        public static int Camp { get; private set; }

        public static IReadOnlyList<MatchRoomPlayerInfo> PlayerList => _playerList;

        public static bool HasContext => RoomId > 0 && !string.IsNullOrWhiteSpace(SceneName);

        public static void Apply(NotifyMatchGameStart message)
        {
            RoomId = message.RoomId;
            SceneName = message.SceneName;
            SeatIndex = message.SeatIndex;
            Camp = message.Camp;
            _playerList = message.PlayerList ?? new List<MatchRoomPlayerInfo>();
        }

        public static void Clear()
        {
            RoomId = 0;
            SceneName = null;
            SeatIndex = -1;
            Camp = 0;
            _playerList = new List<MatchRoomPlayerInfo>();
        }
    }
}
