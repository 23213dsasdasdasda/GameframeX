using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFrameX.EditorTools
{
    public static class CleanChineseChessSceneTool
    {
        private const string ChineseChessScenePath = "Assets/Bundles/Scenes/ChineseChess.unity";

        [MenuItem("Tools/Scene/Clean ChineseChess Missing References")]
        public static void CleanChineseChessMissingReferences()
        {
            var scene = EditorSceneManager.OpenScene(ChineseChessScenePath, OpenSceneMode.Single);
            CleanScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CleanScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("ChineseChess scene is not loaded.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            var missingPrefabRoots = new HashSet<GameObject>();
            var missingScriptCount = 0;

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                CollectMissingPrefabRoots(roots[i], missingPrefabRoots);
            }

            foreach (var root in missingPrefabRoots)
            {
                if (root == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(root);
            }

            roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    missingScriptCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[j].gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Cleaned scene {scene.path}. Removed missing prefab roots: {missingPrefabRoots.Count}, removed missing scripts: {missingScriptCount}");
        }

        private static void CollectMissingPrefabRoots(GameObject root, HashSet<GameObject> missingPrefabRoots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var gameObject = transforms[i].gameObject;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                {
                    continue;
                }

                if (PrefabUtility.IsPrefabAssetMissing(gameObject))
                {
                    missingPrefabRoots.Add(gameObject);
                }
            }
        }
    }
}
