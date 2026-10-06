using GameFrameX.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hotfix.Manager
{
    public static class SurvivalMatchBattleRuntime
    {
        private static bool _installed;
        private static GameObject _root;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            _installed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryBuild(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBuild(scene);
        }

        private static void TryBuild(Scene scene)
        {
            if (!MatchBattleContext.HasContext || !string.Equals(scene.name, MatchBattleContext.SceneName))
            {
                DestroyRoot();
                return;
            }

            DestroyRoot();

            _root = new GameObject("SurvivalMatchBattleRuntime");
            SceneManager.MoveGameObjectToScene(_root, scene);
            _root.AddComponent<SurvivalDemoController>();
            BattleSyncManager.Instance.AttachDriver(_root);

            Log.Info($"survival match runtime started. scene={scene.name}, roomId={MatchBattleContext.RoomId}");
        }

        private static void DestroyRoot()
        {
            if (_root == null)
            {
                return;
            }

            Object.Destroy(_root);
            _root = null;
        }
    }
}
