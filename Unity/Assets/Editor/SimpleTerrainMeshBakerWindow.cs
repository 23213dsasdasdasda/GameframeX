using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameFrameX.EditorTools
{
    public sealed class SimpleTerrainMeshBakerWindow : EditorWindow
    {
        private const int MinQuadsPerAxis = 8;
        private const int MaxQuadsPerAxis = 512;

        private Terrain _terrain;
        private int _quadsPerAxis = 128;
        private bool _createMeshCollider = true;
        private bool _disableSourceTerrain = true;
        private bool _createDedicatedMaterial = true;
        private Material _overrideMaterial;

        [MenuItem("Tools/Terrain/Bake Selected Terrain To Simple Mesh")]
        public static void Open()
        {
            var window = GetWindow<SimpleTerrainMeshBakerWindow>("Terrain To Simple Mesh");
            window.minSize = new Vector2(420f, 260f);
            window.TryAssignSelectedTerrain();
        }

        private void OnEnable()
        {
            TryAssignSelectedTerrain();
        }

        private void OnSelectionChange()
        {
            if (_terrain == null)
            {
                TryAssignSelectedTerrain();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "把选中的 Terrain 采样成一个简化 Mesh。这个工具会生成 Mesh 资产，并在场景里创建一个带 MeshRenderer/MeshFilter 的替代物体。",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _terrain = (Terrain)EditorGUILayout.ObjectField("Source Terrain", _terrain, typeof(Terrain), true);
                _quadsPerAxis = EditorGUILayout.IntSlider("Quads Per Axis", _quadsPerAxis, MinQuadsPerAxis, MaxQuadsPerAxis);
                _createMeshCollider = EditorGUILayout.Toggle("Create MeshCollider", _createMeshCollider);
                _disableSourceTerrain = EditorGUILayout.Toggle("Disable Source Terrain", _disableSourceTerrain);
                _createDedicatedMaterial = EditorGUILayout.Toggle("Create Dedicated Material", _createDedicatedMaterial);
                _overrideMaterial = (Material)EditorGUILayout.ObjectField("Override Material", _overrideMaterial, typeof(Material), false);
            }

            if (_terrain != null && _terrain.terrainData != null)
            {
                var terrainSize = _terrain.terrainData.size;
                var heightResolution = _terrain.terrainData.heightmapResolution;
                EditorGUILayout.HelpBox(
                    $"Terrain Size: {terrainSize.x:0.##}, {terrainSize.y:0.##}, {terrainSize.z:0.##}\nHeightmap Resolution: {heightResolution}",
                    MessageType.None);
            }

            using (new EditorGUI.DisabledScope(_terrain == null || _terrain.terrainData == null))
            {
                if (GUILayout.Button("Bake Simple Mesh", GUILayout.Height(32f)))
                {
                    Bake();
                }
            }
        }

        private void TryAssignSelectedTerrain()
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            _terrain = Selection.activeGameObject.GetComponent<Terrain>();
            if (_terrain == null)
            {
                _terrain = Selection.activeGameObject.GetComponentInChildren<Terrain>();
            }
        }

        private void Bake()
        {
            if (_terrain == null || _terrain.terrainData == null)
            {
                EditorUtility.DisplayDialog("Terrain To Simple Mesh", "请选择一个有效的 Terrain。", "OK");
                return;
            }

            var defaultDirectory = GetDefaultSaveDirectory(_terrain);
            var meshPath = EditorUtility.SaveFilePanelInProject(
                "Save Baked Terrain Mesh",
                $"{_terrain.name}_SimpleMesh",
                "asset",
                "请选择生成 mesh 的保存位置。",
                defaultDirectory);

            if (string.IsNullOrEmpty(meshPath))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            var mesh = BuildMesh(_terrain.terrainData, _quadsPerAxis);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var material = CreateMaterialAssetIfNeeded(meshPath, _terrain);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var meshObject = new GameObject($"{_terrain.name}_SimpleMesh");
            Undo.RegisterCreatedObjectUndo(meshObject, "Create baked terrain mesh");

            meshObject.transform.SetParent(_terrain.transform.parent, false);
            meshObject.transform.position = _terrain.transform.position;
            meshObject.transform.rotation = _terrain.transform.rotation;
            meshObject.transform.localScale = _terrain.transform.localScale;
            meshObject.isStatic = true;

            var filter = meshObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            if (_createMeshCollider)
            {
                var collider = meshObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            if (_disableSourceTerrain)
            {
                Undo.RecordObject(_terrain.gameObject, "Disable source terrain");
                _terrain.gameObject.SetActive(false);
            }

            Selection.activeGameObject = meshObject;
            EditorSceneManager.MarkSceneDirty(meshObject.scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Baked terrain '{_terrain.name}' to mesh '{meshPath}'. Quads per axis: {_quadsPerAxis}.");
        }

        private static Mesh BuildMesh(TerrainData terrainData, int quadsPerAxis)
        {
            var clampedQuads = Mathf.Clamp(quadsPerAxis, MinQuadsPerAxis, MaxQuadsPerAxis);
            var verticesPerAxis = clampedQuads + 1;
            var vertexCount = verticesPerAxis * verticesPerAxis;

            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[clampedQuads * clampedQuads * 6];
            var size = terrainData.size;

            for (var z = 0; z < verticesPerAxis; z++)
            {
                var v = z / (float)clampedQuads;
                for (var x = 0; x < verticesPerAxis; x++)
                {
                    var u = x / (float)clampedQuads;
                    var index = z * verticesPerAxis + x;
                    var height = terrainData.GetInterpolatedHeight(u, v);

                    vertices[index] = new Vector3(u * size.x, height, v * size.z);
                    uvs[index] = new Vector2(u, v);
                }
            }

            var triangleIndex = 0;
            for (var z = 0; z < clampedQuads; z++)
            {
                for (var x = 0; x < clampedQuads; x++)
                {
                    var topLeft = z * verticesPerAxis + x;
                    var bottomLeft = (z + 1) * verticesPerAxis + x;
                    var topRight = topLeft + 1;
                    var bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;

                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            var mesh = new Mesh
            {
                name = $"{terrainData.name}_SimpleMesh"
            };

            if (vertexCount > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Material CreateMaterialAssetIfNeeded(string meshPath, Terrain terrain)
        {
            var sourceMaterial = ResolveSourceMaterial(terrain);
            if (!_createDedicatedMaterial)
            {
                return sourceMaterial;
            }

            var directory = Path.GetDirectoryName(meshPath)?.Replace("\\", "/") ?? "Assets";
            var materialPath = $"{directory}/{terrain.name}_SimpleMesh.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null)
            {
                CopyMaterialProperties(sourceMaterial, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var created = new Material(sourceMaterial)
            {
                name = $"{terrain.name}_SimpleMesh"
            };
            AssetDatabase.CreateAsset(created, materialPath);
            return created;
        }

        private Material ResolveSourceMaterial(Terrain terrain)
        {
            if (_overrideMaterial != null)
            {
                return _overrideMaterial;
            }

            if (terrain.materialTemplate != null)
            {
                return terrain.materialTemplate;
            }

            var material = new Material(Shader.Find("Standard"))
            {
                name = $"{terrain.name}_SimpleMeshMaterial"
            };

            var terrainLayers = terrain.terrainData.terrainLayers;
            if (terrainLayers != null && terrainLayers.Length > 0 && terrainLayers[0] != null && terrainLayers[0].diffuseTexture != null)
            {
                material.mainTexture = terrainLayers[0].diffuseTexture;
                material.mainTextureScale = new Vector2(
                    Mathf.Max(1f, terrain.terrainData.size.x / 15f),
                    Mathf.Max(1f, terrain.terrainData.size.z / 15f));
            }

            return material;
        }

        private static void CopyMaterialProperties(Material source, Material target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.shader = source.shader;
            target.CopyPropertiesFromMaterial(source);
        }

        private static string GetDefaultSaveDirectory(Terrain terrain)
        {
            var terrainPath = AssetDatabase.GetAssetPath(terrain.terrainData);
            if (string.IsNullOrEmpty(terrainPath))
            {
                return "Assets";
            }

            var directory = Path.GetDirectoryName(terrainPath);
            return string.IsNullOrEmpty(directory) ? "Assets" : directory.Replace("\\", "/");
        }
    }
}
