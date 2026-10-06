using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameFrameX.EditorTools
{
    public static class ReplaceBattlePlayerWithCitizenTool
    {
        private const string BattlePlayerPrefabPath = "Assets/Resources/Battle/PlayerArmature.prefab";
        private const string StarterPlayerPrefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        private const string CitizenPrefabPath = "Assets/Citizens PRO/People Prefabs/Male/Summer/business01_m_highpoly.prefab";
        private const string CameraTargetName = "PlayerCameraRoot";

        [MenuItem("Tools/Citizens PRO/Rebuild Battle Player From business01")]
        public static void RebuildBattlePlayerFromBusiness01()
        {
            var starterRoot = PrefabUtility.LoadPrefabContents(StarterPlayerPrefabPath);
            var citizenRoot = PrefabUtility.LoadPrefabContents(CitizenPrefabPath);
            if (starterRoot == null || citizenRoot == null)
            {
                Debug.LogError("Failed to load starter player or citizen prefab.");
                if (starterRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(starterRoot);
                }

                if (citizenRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(citizenRoot);
                }

                return;
            }

            try
            {
                RebuildPlayerPrefab(starterRoot, citizenRoot);
                PrefabUtility.SaveAsPrefabAsset(citizenRoot, BattlePlayerPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Rebuilt battle player prefab from {CitizenPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(starterRoot);
                PrefabUtility.UnloadPrefabContents(citizenRoot);
            }
        }

        private static void RebuildPlayerPrefab(GameObject starterRoot, GameObject citizenRoot)
        {
            citizenRoot.name = "PlayerArmature";
            citizenRoot.tag = "Player";
            citizenRoot.layer = starterRoot.layer;

            var citizenTransform = citizenRoot.transform;
            citizenTransform.localPosition = Vector3.zero;
            citizenTransform.localRotation = Quaternion.identity;
            citizenTransform.localScale = Vector3.one;

            var starterAnimator = starterRoot.GetComponent<Animator>();
            var starterCharacterController = starterRoot.GetComponent<CharacterController>();
            var starterThirdPersonController = FindComponentByScriptGuid(starterRoot, "26e54e5a728a9234ab24fcf1460ed8a2");
            var starterPlayerPush = FindComponentByScriptGuid(starterRoot, "909d917d73a63f940ac158d02e936645");
            var starterInputs = FindComponentByScriptGuid(starterRoot, "e087ecce43ebbff45a1b360637807d93");
            var starterPlayerInput = FindComponentByScriptGuid(starterRoot, "62899f850307741f2a39c98a8b639597");
            var starterCameraTarget = FindChild(starterRoot.transform, CameraTargetName);

            var citizenAnimator = citizenRoot.GetComponent<Animator>();
            if (citizenAnimator == null || starterAnimator == null || starterCharacterController == null || starterThirdPersonController == null || starterInputs == null || starterPlayerInput == null)
            {
                throw new System.InvalidOperationException("Starter or citizen prefab is missing required components.");
            }

            var cameraTarget = EnsureCameraTarget(citizenRoot.transform, starterCameraTarget);

            citizenAnimator.runtimeAnimatorController = starterAnimator.runtimeAnimatorController;
            citizenAnimator.applyRootMotion = false;
            citizenAnimator.cullingMode = starterAnimator.cullingMode;
            citizenAnimator.updateMode = starterAnimator.updateMode;

            var characterController = EnsureComponent<CharacterController>(citizenRoot);
            CopySerialized(starterCharacterController, characterController);

            var thirdPersonController = EnsureComponentByType(citizenRoot, starterThirdPersonController.GetType());
            CopySerialized(starterThirdPersonController, thirdPersonController);
            SetObjectReference(thirdPersonController, "CinemachineCameraTarget", cameraTarget);

            if (starterPlayerPush != null)
            {
                var playerPush = EnsureComponentByType(citizenRoot, starterPlayerPush.GetType());
                CopySerialized(starterPlayerPush, playerPush);
            }

            var inputs = EnsureComponentByType(citizenRoot, starterInputs.GetType());
            CopySerialized(starterInputs, inputs);

            var playerInput = EnsureComponentByType(citizenRoot, starterPlayerInput.GetType());
            CopySerialized(starterPlayerInput, playerInput);
            RetargetPlayerInputEvents(playerInput, thirdPersonController);
        }

        private static Transform EnsureCameraTarget(Transform root, Transform starterCameraTarget)
        {
            var existing = FindChild(root, CameraTargetName);
            if (existing != null)
            {
                return existing;
            }

            var target = new GameObject(CameraTargetName).transform;
            target.SetParent(root, false);
            if (starterCameraTarget != null)
            {
                target.localPosition = starterCameraTarget.localPosition;
                target.localRotation = starterCameraTarget.localRotation;
                target.localScale = starterCameraTarget.localScale;
            }
            else
            {
                target.localPosition = new Vector3(0f, 1.375f, 0f);
                target.localRotation = Quaternion.identity;
                target.localScale = Vector3.one;
            }

            if (HasTag("CinemachineTarget"))
            {
                target.tag = "CinemachineTarget";
            }

            return target;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static Component EnsureComponentByType(GameObject gameObject, System.Type type)
        {
            var component = gameObject.GetComponent(type);
            return component != null ? component : gameObject.AddComponent(type);
        }

        private static void CopySerialized(Object source, Object destination)
        {
            EditorUtility.CopySerialized(source, destination);
        }

        private static void SetObjectReference(Component component, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RetargetPlayerInputEvents(Component playerInput, Component target)
        {
            var serializedObject = new SerializedObject(playerInput);
            var actionEvents = serializedObject.FindProperty("m_ActionEvents");
            if (actionEvents == null || !actionEvents.isArray)
            {
                return;
            }

            for (var i = 0; i < actionEvents.arraySize; i++)
            {
                var actionEvent = actionEvents.GetArrayElementAtIndex(i);
                var calls = actionEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
                if (calls == null || !calls.isArray)
                {
                    continue;
                }

                for (var j = 0; j < calls.arraySize; j++)
                {
                    var call = calls.GetArrayElementAtIndex(j);
                    var targetProperty = call.FindPropertyRelative("m_Target");
                    if (targetProperty != null)
                    {
                        targetProperty.objectReferenceValue = target;
                    }
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasTag(string tagName)
        {
            var tags = InternalEditorUtility.tags;
            for (var i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tagName)
                {
                    return true;
                }
            }

            return false;
        }

        private static Component FindComponentByScriptGuid(GameObject root, string scriptGuid)
        {
            var behaviours = root.GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                var monoScript = MonoScript.FromMonoBehaviour(behaviour);
                if (monoScript != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(monoScript)) == scriptGuid)
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == childName)
                {
                    return transforms[i];
                }
            }

            return null;
        }
    }
}
