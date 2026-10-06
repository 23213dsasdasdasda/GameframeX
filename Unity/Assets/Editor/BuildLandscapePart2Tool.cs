using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFrameX.EditorTools
{
    public static class BuildLandscapePart2Tool
    {
        private const string ChineseChessScenePath = "Assets/Bundles/Scenes/ChineseChess.unity";
        private const string GeneratedRootName = "LandscapePart2_HighGrid";
        private const string ForestLakeTerrainRootName = "ForestLakeImportedTerrain";
        private const string ForestLakeTerrainDataPath = "Assets/ForestLake/Terrain/Terrain_0_0_806e08b3-b276-4e1a-a5a5-d3b1e76ca436.asset";
        private const int TerrainGroupingId = 24027;

        private static readonly TerrainTileDefinition[] HighTiles =
        {
            new("High_24", "Assets/Landscapes Part 2/Maps/High_24/high_24.asset", 0, 0),
            new("High_25", "Assets/Landscapes Part 2/Maps/High_25/high_25.asset", 1, 0),
            new("High_26", "Assets/Landscapes Part 2/Maps/High_26/high_26.asset", 0, 1),
            new("High_27", "Assets/Landscapes Part 2/Maps/High_27/high_27.asset", 1, 1),
        };

        [MenuItem("Tools/Landscapes/Build High 24-27 Grid In Active Scene")]
        public static void BuildHighGridInActiveScene()
        {
            BuildHighGrid(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Landscapes/Rebuild ChineseChess With High 24-27 Grid")]
        public static void RebuildChineseChessWithHighGrid()
        {
            var scene = EditorSceneManager.OpenScene(ChineseChessScenePath, OpenSceneMode.Single);
            BuildHighGrid(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Landscapes/Import ForestLake Demo Terrain To ChineseChess")]
        public static void ImportForestLakeDemoTerrainToChineseChess()
        {
            var scene = EditorSceneManager.OpenScene(ChineseChessScenePath, OpenSceneMode.Single);
            ImportForestLakeTerrain(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildHighGrid(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("No active loaded scene.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            RemoveExistingRoot(GeneratedRootName);

            var root = new GameObject(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create landscape grid root");
            SceneManager.MoveGameObjectToScene(root, scene);

            var instances = new Terrain[2, 2];
            var tileSize = Vector3.zero;

            for (var i = 0; i < HighTiles.Length; i++)
            {
                var definition = HighTiles[i];
                var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(definition.TerrainDataPath);
                if (terrainData == null)
                {
                    Debug.LogError($"Missing TerrainData: {definition.TerrainDataPath}");
                    continue;
                }

                if (tileSize == Vector3.zero)
                {
                    tileSize = terrainData.size;
                }

                var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                Undo.RegisterCreatedObjectUndo(terrainObject, $"Create {definition.Name}");
                SceneManager.MoveGameObjectToScene(terrainObject, scene);
                terrainObject.name = definition.Name;
                terrainObject.transform.SetParent(root.transform, false);
                terrainObject.transform.position = new Vector3(
                    definition.GridX * tileSize.x,
                    0f,
                    definition.GridY * tileSize.z);

                var terrain = terrainObject.GetComponent<Terrain>();
                terrain.groupingID = TerrainGroupingId;
                terrain.allowAutoConnect = true;
                terrain.drawInstanced = true;

                instances[definition.GridX, definition.GridY] = terrain;
            }

            ConnectNeighbors(instances);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
            Debug.Log($"Built {GeneratedRootName} in {scene.path}.");
        }

        private static void ImportForestLakeTerrain(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("No active loaded scene.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            RemoveExistingRoot(ForestLakeTerrainRootName);
            RemoveAllTerrainObjects();

            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(ForestLakeTerrainDataPath);
            if (terrainData == null)
            {
                Debug.LogError($"Missing TerrainData: {ForestLakeTerrainDataPath}");
                return;
            }

            var terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            Undo.RegisterCreatedObjectUndo(terrainObject, "Import ForestLake terrain");
            SceneManager.MoveGameObjectToScene(terrainObject, scene);
            terrainObject.name = ForestLakeTerrainRootName;
            terrainObject.transform.position = Vector3.zero;
            terrainObject.transform.rotation = Quaternion.identity;
            terrainObject.transform.localScale = Vector3.one;

            var terrain = terrainObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.allowAutoConnect = true;
                terrain.groupingID = 31001;
                terrain.drawInstanced = true;
                terrain.drawTreesAndFoliage = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = terrainObject;
            Debug.Log($"Imported ForestLake terrain into {scene.path}.");
        }

        private static void ConnectNeighbors(Terrain[,] instances)
        {
            for (var x = 0; x < instances.GetLength(0); x++)
            {
                for (var y = 0; y < instances.GetLength(1); y++)
                {
                    var terrain = instances[x, y];
                    if (terrain == null)
                    {
                        continue;
                    }

                    var left = x > 0 ? instances[x - 1, y] : null;
                    var right = x < instances.GetLength(0) - 1 ? instances[x + 1, y] : null;
                    var top = y < instances.GetLength(1) - 1 ? instances[x, y + 1] : null;
                    var bottom = y > 0 ? instances[x, y - 1] : null;

                    terrain.SetNeighbors(left, top, right, bottom);
                }
            }
        }

        private static void RemoveExistingRoot(string rootName)
        {
            var existing = GameObject.Find(rootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }

        private static void RemoveAllTerrainObjects()
        {
            var terrains = Object.FindObjectsOfType<Terrain>();
            for (var i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(terrain.gameObject);
            }
        }

        private readonly struct TerrainTileDefinition
        {
            public TerrainTileDefinition(string name, string terrainDataPath, int gridX, int gridY)
            {
                Name = name;
                TerrainDataPath = terrainDataPath;
                GridX = gridX;
                GridY = gridY;
            }

            public string Name { get; }
            public string TerrainDataPath { get; }
            public int GridX { get; }
            public int GridY { get; }
        }
    }
}
