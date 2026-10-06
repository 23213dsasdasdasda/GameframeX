using GameFrameX.Network.Runtime;
using GameFrameX.Runtime;
using Hotfix.Proto;
using UnityEngine;

namespace Hotfix.Manager
{
    // The previous implementation depended on a missing third-person network/KCC stack.
    // Keep the integration surface minimal until that chain is restored intentionally.
    public sealed class BattleSyncManager : GameFrameworkSingleton<BattleSyncManager>, IMessageHandler
    {
        public BattleSyncManager()
        {
            Register();
        }

        public void Register()
        {
            ProtoMessageHandler.Add(this);
        }

        [MessageHandler(typeof(NotifyBattleSnapshot), nameof(NotifyBattleSnapshotHandler))]
        private void NotifyBattleSnapshotHandler(NotifyBattleSnapshot message)
        {
        }

        public void AttachDriver(GameObject host)
        {
            if (host == null || host.GetComponent<BattleSyncDriver>() != null)
            {
                return;
            }

            host.AddComponent<BattleSyncDriver>();
        }

        public void Reset()
        {
        }

        public void BindLocalPlayer(GameObject player)
        {
        }

        public void BindRemotePlayer(long playerId, GameObject player)
        {
        }

        public void Tick()
        {
        }
    }

    public sealed class BattleSyncDriver : MonoBehaviour
    {
        private void Update()
        {
            BattleSyncManager.Instance.Tick();
        }
    }
}
