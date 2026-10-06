using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Editor
{
    public static class BuildSurvivalSceneTool
    {
        private const string GeneratedRootName = "PolyartSurvivalGenerated";
        private const string LegacyEnvironmentName = "Environment";
        private const string ChineseChessScenePath = "Assets/Bundles/Scenes/ChineseChess.unity";
        private const float GroundAlignRayStart = 120f;
        private const float GroundAlignDistance = 260f;
        private static readonly Rect WorldBounds = new Rect(-72f, -72f, 164f, 164f);
        private static readonly Rect SpawnClearArea = new Rect(-14f, -12f, 28f, 24f);
        private static readonly Rect[] ForestZones =
        {
            new Rect(-68f, -68f, 48f, 52f),
            new Rect(42f, -66f, 44f, 54f),
            new Rect(-70f, 24f, 54f, 60f),
            new Rect(34f, 28f, 52f, 56f),
            new Rect(-10f, 42f, 28f, 36f),
            new Rect(-4f, -62f, 34f, 28f),
        };

        private static readonly Rect[] GrassZones =
        {
            new Rect(-58f, -54f, 40f, 38f),
            new Rect(-6f, -48f, 30f, 28f),
            new Rect(34f, -56f, 34f, 34f),
            new Rect(-62f, 18f, 38f, 40f),
            new Rect(18f, 24f, 30f, 32f),
            new Rect(46f, 32f, 28f, 30f),
        };

        private static readonly string[] TerrainPrefabs =
        {
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Terrain/Prefab_Terrain_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Terrain/Prefab_Terrain_02.prefab",
        };

        private static readonly string[] TreePrefabs =
        {
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Trees/Prefab_TreeLarge_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Trees/Prefab_TreeLarge_02.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Trees/Prefab_TreeLarge_03.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Trees/Prefab_TreeLarge_04.prefab",
        };

        private static readonly string[] RockPrefabs =
        {
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RockFormation_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RockFormation_02.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RockFormation_03.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RockFormation_04.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RocksRound_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RocksRound_02.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Rocks/Prefab_RocksRound_03.prefab",
        };

        private static readonly string[] BushPrefabs =
        {
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Foliage/Prefab_Bush_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Foliage/Prefab_Bush_03.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Foliage/Prefab_Bush_04.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Foliage/Prefab_Bush_06.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Foliage/Prefab_Bush_08.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Foliage/Prefab_Bush_09.prefab",
        };

        private static readonly string[] GrassPrefabs =
        {
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Grass/Prefab_Grass_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Grass/Prefab_Grass_02.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Grass/Prefab_Grass_03.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Grass/Prefab_Grass_Group_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Grass/Prefab_Grass_Group_02.prefab",
        };

        private static readonly string[] PropPrefabs =
        {
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Tent.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Campfire_Lit.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Crate_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_TreeFallen.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_TreeStump_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_WoodSign_01.prefab",
            "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Barrel_01.prefab",
        };

        [MenuItem("Tools/Survival/Build Polyart Survival Scene")]
        public static void BuildPolyartSurvivalScene()
        {
            BuildScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Survival/Rebuild ChineseChess Survival Scene")]
        public static void RebuildChineseChessSurvivalScene()
        {
            var scene = EditorSceneManager.OpenScene(ChineseChessScenePath, OpenSceneMode.Single);
            BuildScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("No active loaded scene.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            RemoveExistingGeneratedContent();
            DisableLegacyEnvironment();

            var root = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create survival scene root");

            BuildTerrain(root.transform);
            BuildSpawnGround(root.transform);
            BuildLake(root.transform);
            BuildCamp(root.transform);
            BuildRockWalls(root.transform);
            ScatterTrees(root.transform);
            ScatterBushes(root.transform);
            ScatterGrass(root.transform);
            ScatterSurvivalProps(root.transform);
            ApplyAtmosphere();

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
            Debug.Log($"Polyart survival scene generated in {scene.path}.");
        }

        private static void RemoveExistingGeneratedContent()
        {
            var existing = GameObject.Find(GeneratedRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }

        private static void DisableLegacyEnvironment()
        {
            var legacy = GameObject.Find(LegacyEnvironmentName);
            if (legacy != null)
            {
                Undo.RecordObject(legacy, "Disable legacy environment");
                legacy.SetActive(false);
            }
        }

        private static void BuildTerrain(Transform parent)
        {
            var terrainA = InstantiatePrefab(TerrainPrefabs[0], parent, new Vector3(-18f, -2.5f, -8f), Quaternion.identity, Vector3.one * 3.2f);
            terrainA.name = "Terrain_Main";

            var terrainB = InstantiatePrefab(TerrainPrefabs[1], parent, new Vector3(44f, -2.5f, 30f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 3f);
            terrainB.name = "Terrain_North";
        }

        private static void BuildLake(Transform parent)
        {
            var lake = InstantiatePrefab(
                "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Water/Prefab_WaterLake.prefab",
                parent,
                new Vector3(46f, -1.8f, 46f),
                Quaternion.Euler(0f, 24f, 0f),
                Vector3.one * 2.2f);
            lake.name = "Water_Lake";
        }

        private static void BuildSpawnGround(Transform parent)
        {
            var spawnGround = new GameObject("SpawnGround");
            Undo.RegisterCreatedObjectUndo(spawnGround, "Create spawn ground");
            spawnGround.transform.SetParent(parent, false);
            spawnGround.transform.position = new Vector3(0f, -1.5f, 0f);

            var collider = spawnGround.AddComponent<BoxCollider>();
            collider.size = new Vector3(36f, 2f, 32f);
        }

        private static void BuildCamp(Transform parent)
        {
            var campRoot = new GameObject("CampSite");
            Undo.RegisterCreatedObjectUndo(campRoot, "Create campsite");
            campRoot.transform.SetParent(parent, false);
            campRoot.transform.position = new Vector3(-34f, -0.4f, -34f);

            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Tent.prefab",
                campRoot.transform, new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 145f, 0f), Vector3.one * 1.1f);
            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Campfire_Lit.prefab",
                campRoot.transform, new Vector3(2.6f, 0f, -0.8f), Quaternion.Euler(0f, 18f, 0f), Vector3.one * 1.1f);
            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Crate_01.prefab",
                campRoot.transform, new Vector3(-1.5f, 0f, 1.7f), Quaternion.Euler(0f, 25f, 0f), Vector3.one);
            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_Barrel_01.prefab",
                campRoot.transform, new Vector3(-0.4f, 0f, 2.3f), Quaternion.Euler(0f, -10f, 0f), Vector3.one);
            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_TreeStump_01.prefab",
                campRoot.transform, new Vector3(3.3f, 0f, 1.8f), Quaternion.identity, Vector3.one * 1.15f);
            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_WoodSign_01.prefab",
                campRoot.transform, new Vector3(-2.2f, 0f, -2f), Quaternion.Euler(0f, 30f, 0f), Vector3.one);
            InstantiatePrefab("Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/Props/Prefab_TreeFallen.prefab",
                campRoot.transform, new Vector3(7.5f, 0.2f, 4.8f), Quaternion.Euler(0f, 110f, 0f), Vector3.one * 1.15f);

            AlignChildrenToGround(campRoot.transform, parent);
        }

        private static void BuildRockWalls(Transform parent)
        {
            var positions = new[]
            {
                new Vector3(-68f, -1.2f, -54f),
                new Vector3(-60f, -1.2f, -12f),
                new Vector3(-66f, -1.2f, 34f),
                new Vector3(-36f, -1.2f, 70f),
                new Vector3(6f, -1.2f, 74f),
                new Vector3(44f, -1.2f, 68f),
                new Vector3(76f, -1.2f, 34f),
                new Vector3(74f, -1.2f, -8f),
                new Vector3(62f, -1.2f, -50f),
                new Vector3(18f, -1.2f, -66f),
                new Vector3(-24f, -1.2f, -64f),
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var prefabPath = RockPrefabs[i % RockPrefabs.Length];
                var rotation = Quaternion.Euler(0f, 35f * i, 0f);
                var scale = Vector3.one * (1.8f + (i % 3) * 0.35f);
                InstantiatePrefab(prefabPath, parent, positions[i], rotation, scale);
            }
        }

        private static void ScatterTrees(Transform parent)
        {
            var points = GenerateScatterPoints(ForestZones, 76, 8.5f, 98123);
            for (var i = 0; i < points.Count; i++)
            {
                if (InsideSpawnClearArea(points[i]))
                {
                    continue;
                }

                var prefabPath = TreePrefabs[i % TreePrefabs.Length];
                var position = CreatePlacement(points[i], -0.5f);
                var rotation = Quaternion.Euler(0f, (i * 47f) % 360f, 0f);
                var scale = Vector3.one * (1.6f + (i % 4) * 0.18f);
                var instance = InstantiatePrefab(prefabPath, parent, position, rotation, scale);
                AlignInstanceToGround(instance.transform, parent);
            }
        }

        private static void ScatterBushes(Transform parent)
        {
            var points = GenerateScatterPoints(ForestZones, 150, 3.8f, 22441);
            for (var i = 0; i < points.Count; i++)
            {
                if (InsideSpawnClearArea(points[i]))
                {
                    continue;
                }

                var prefabPath = BushPrefabs[i % BushPrefabs.Length];
                var position = CreatePlacement(points[i], -0.35f);
                var rotation = Quaternion.Euler(0f, (i * 31f) % 360f, 0f);
                var scale = Vector3.one * (0.9f + (i % 5) * 0.08f);
                var instance = InstantiatePrefab(prefabPath, parent, position, rotation, scale);
                AlignInstanceToGround(instance.transform, parent);
            }
        }

        private static void ScatterGrass(Transform parent)
        {
            var points = GenerateScatterPoints(GrassZones, 220, 2.2f, 55112);
            for (var i = 0; i < points.Count; i++)
            {
                if (InsideSpawnClearArea(points[i]))
                {
                    continue;
                }

                var prefabPath = GrassPrefabs[i % GrassPrefabs.Length];
                var position = CreatePlacement(points[i], -0.4f);
                var rotation = Quaternion.Euler(0f, (i * 19f) % 360f, 0f);
                var scale = Vector3.one * (0.85f + (i % 4) * 0.09f);
                var instance = InstantiatePrefab(prefabPath, parent, position, rotation, scale);
                AlignInstanceToGround(instance.transform, parent);
            }
        }

        private static void ScatterSurvivalProps(Transform parent)
        {
            var points = GenerateScatterPoints(new[]
            {
                new Rect(-60f, -58f, 34f, 34f),
                new Rect(44f, -52f, 24f, 24f),
                new Rect(-58f, 40f, 24f, 24f),
                new Rect(36f, 48f, 30f, 24f),
            }, 18, 9f, 77213);

            for (var i = 0; i < points.Count; i++)
            {
                var prefabPath = PropPrefabs[(i + 2) % PropPrefabs.Length];
                var position = CreatePlacement(points[i], -0.2f);
                var rotation = Quaternion.Euler(0f, (i * 57f) % 360f, 0f);
                var scale = Vector3.one * (0.95f + (i % 3) * 0.12f);
                var instance = InstantiatePrefab(prefabPath, parent, position, rotation, scale);
                AlignInstanceToGround(instance.transform, parent);
            }
        }

        private static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.19f, 0.30f, 0.24f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.42f, 0.34f);

            var directional = Object.FindObjectOfType<Light>();
            if (directional != null && directional.type == LightType.Directional)
            {
                Undo.RecordObject(directional, "Adjust directional light");
                directional.color = new Color(1f, 0.94f, 0.82f);
                directional.intensity = 0.85f;
                directional.transform.rotation = Quaternion.Euler(42f, 128f, 0f);
            }
        }

        private static bool InsideSpawnClearArea(Vector2 point)
        {
            return SpawnClearArea.Contains(point);
        }

        private static Vector3 CreatePlacement(Vector2 point, float y)
        {
            return new Vector3(
                Mathf.Clamp(point.x, WorldBounds.xMin, WorldBounds.xMax),
                y,
                Mathf.Clamp(point.y, WorldBounds.yMin, WorldBounds.yMax));
        }

        private static void AlignChildrenToGround(Transform root, Transform generatedRoot)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                AlignInstanceToGround(root.GetChild(i), generatedRoot);
            }
        }

        private static void AlignInstanceToGround(Transform instance, Transform generatedRoot)
        {
            if (instance == null)
            {
                return;
            }

            var bounds = CalculateWorldBounds(instance);
            var origin = new Vector3(bounds.center.x, GroundAlignRayStart, bounds.center.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, GroundAlignDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != null && (hit.transform == instance || hit.transform.IsChildOf(instance) || hit.transform == generatedRoot || hit.transform.IsChildOf(generatedRoot)))
                {
                    var delta = hit.point.y - bounds.min.y;
                    instance.position += new Vector3(0f, delta, 0f);
                }
            }
        }

        private static Bounds CalculateWorldBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return bounds;
            }

            return new Bounds(root.position, Vector3.one);
        }

        private static List<Vector2> GenerateScatterPoints(Rect[] areas, int count, float minDistance, int seed)
        {
            var result = new List<Vector2>();
            var random = new System.Random(seed);
            var attempts = 0;

            while (result.Count < count && attempts < count * 40)
            {
                attempts++;
                var area = areas[random.Next(areas.Length)];
                var point = new Vector2(
                    Mathf.Lerp(area.xMin, area.xMax, (float)random.NextDouble()),
                    Mathf.Lerp(area.yMin, area.yMax, (float)random.NextDouble()));

                var valid = true;
                for (var i = 0; i < result.Count; i++)
                {
                    if (Vector2.Distance(result[i], point) < minDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    result.Add(point);
                }
            }

            return result;
        }

        private static GameObject InstantiatePrefab(string assetPath, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                throw new System.InvalidOperationException($"Missing prefab at path: {assetPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException($"Failed to instantiate prefab: {assetPath}");
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            return instance;
        }
    }
}
