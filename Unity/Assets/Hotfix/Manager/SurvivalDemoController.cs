﻿﻿using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameframeX.Config;
using GameFrameX.Runtime;
using Hotfix.Config;
using Hotfix.Gameplay.Combat;
using TbMonsterConfig = Hotfix.Config.Tables.TbMonsterConfig;
using TbMonsterSkillConfig = Hotfix.Config.Tables.TbMonsterSkillConfig;
using TbMonsterSpawnWaveConfig = Hotfix.Config.Tables.TbMonsterSpawnWaveConfig;
using TbBulletVisualConfig = Hotfix.Config.Tables.survival.TbBulletVisualConfig;
using OptionConfig = Hotfix.Config.Tables.survival.OptionConfig;
using TbRoleConfig = Hotfix.Config.Tables.survival.TbRoleConfig;
using TbSoundEventConfig = Hotfix.Config.Tables.survival.TbSoundEventConfig;
using TbWeaponDropModelConfig = Hotfix.Config.Tables.survival.TbWeaponDropModelConfig;
using TbSurvivalWeapon = Hotfix.Config.Tables.survival.Tbweapon;
using Hotfix.Gameplay.Enemy;
using Hotfix.Gameplay.Player;
using Hotfix.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hotfix.Manager
{
    public sealed class SurvivalDemoController : MonoBehaviour
    {
        private GameObject spawnpoint;
        private const bool EnableEnemyHitDebugLog = true;

        private readonly List<EnemyData> _enemies = new List<EnemyData>();
        private readonly List<PickupData> _pickups = new List<PickupData>();
        private readonly List<int> _weaponIds = new List<int>();
        private readonly List<Transform> _enemySpawnPoints = new List<Transform>();
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, AudioClip> _audioClipCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<Renderer, Color> _playerBaseColors = new Dictionary<Renderer, Color>();
        private readonly Dictionary<int, float> _soundCooldowns = new Dictionary<int, float>();
        private readonly BattleLevelUpOptionService _levelUpOptionService = new BattleLevelUpOptionService();

        private GameObject _runtimeRoot;
        private GameObject _worldRoot;
        private GameObject _sceneLightingRoot;
        private GameObject _bootstrapCameraRoot;
        private GameObject _player;
        private PlayerController _playerEntity;
        private CharacterController _playerController;
        private Renderer[] _playerRenderers = System.Array.Empty<Renderer>();
        private Transform _weaponPivot;
        private AudioSource _weaponAudioSource;
        private Camera _camera;

        private Text _hudText;
        private Text _crosshairText;
        private Text _gameOverText;
        private BattleLevelUpOptionPopup _levelUpOptionPopup;

        private int _currentWeaponId;
        private int _currentRoleId = 101;
        private int _kills;
        private int _pendingLevelUpSelections;
        private int _waveIndex;
        private int _waveSpawnedCount;
        private float _health;
        private float _maxHealth = DefaultPlayerHealth;
        private float _poisonTimer;
        private float _poisonDamagePerSecond;
        private float _elapsedTime;
        private float _nextShotTime;
        private float _verticalVelocity;
        private Vector3 _externalVelocity;
        private float _waveStateTimer;
        private float _currentWaveElapsedTime;
        private readonly List<WaveSpawnRuntimeState> _waveSpawnStates = new List<WaveSpawnRuntimeState>();
        private Dictionary<int, WaveConfig> _cachedWaveConfigs;
        private int _cachedWaveTableCount = -1;

        private bool _isGameOver;
        private bool _isOptionSelectionActive;
        private bool _isPreparingWave;
        private bool _isDemoReady;
        private float _weaponDamageBonusRate;
        private float _weaponCooldownReduceRate;
        private float _aoeDamageBonusRate;
        private float _pickupDropBonusRate;
        private float _moveSpeedBonusRate;
        private float _lifeStealRate;
        private float _killExplosionChanceRate;
        private float _shotgunSpreadReduceRate;

        private const float ArenaSize = 48f;
        private const float PlayerMoveSpeed = 4.5f;
        private const float Gravity = -24f;
        private const float EnemyGravity = -24f;
        private const float ExternalVelocityDamping = 7f;
        private const float PickupRadius = 1.8f;
        private const float DefaultPlayerHealth = 100f;
        private static readonly Vector3 DemoCameraInitialPosition = new Vector3(-23f, 14.6999998f, 10.6000004f);
        private static readonly Quaternion DemoCameraInitialRotation = new Quaternion(0.435619652f, 0.00392750837f, -0.00190074591f, 0.900120258f);
        private TbSurvivalWeapon SurvivalWeaponTable => GameApp.Config.GetConfig<TbSurvivalWeapon>();
        private TbWeapon LegacyWeaponTable => GameApp.Config.GetConfig<TbWeapon>();
        private TbMonsterConfig MonsterTable => GameApp.Config.GetConfig<TbMonsterConfig>();
        private TbMonsterSkillConfig MonsterSkillTable => GameApp.Config.GetConfig<TbMonsterSkillConfig>();
        private TbMonsterSpawnWaveConfig MonsterSpawnWaveTable => GameApp.Config.GetConfig<TbMonsterSpawnWaveConfig>();
        private TbBulletVisualConfig BulletVisualTable => GameApp.Config.GetConfig<TbBulletVisualConfig>();
        private TbRoleConfig RoleTable => GameApp.Config.GetConfig<TbRoleConfig>();
        private TbSoundEventConfig SoundEventTable => GameApp.Config.GetConfig<TbSoundEventConfig>();
        private TbWeaponDropModelConfig WeaponDropModelTable => GameApp.Config.GetConfig<TbWeaponDropModelConfig>();

        private void Start()
        {
            EnsureBootstrapCamera();
            TryBuildDemo();
        }

        private void Update()
        {
            if (!_isDemoReady)
            {
                EnsureBootstrapCamera();
                TryBuildDemo();
                return;
            }

            if (_player == null)
            {
                return;
            }

            if (_isOptionSelectionActive)
            {
                RefreshHud();
                return;
            }

            if (_isGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    BuildDemo();
                }

                return;
            }

            var deltaTime = Time.deltaTime;
            _elapsedTime += deltaTime;

            UpdatePlayer(deltaTime);
            UpdateEnemies(deltaTime);
            UpdatePickups(deltaTime);
            UpdateWave(deltaTime);
            UpdateCamera();
            RefreshHud();
        }

        private void BuildDemo()
        {
            DestroyBootstrapCamera();
            ClearChildren();
            _runtimeRoot = new GameObject("RuntimeRoot");
            _runtimeRoot.transform.SetParent(transform, false);
            _enemies.Clear();
            _pickups.Clear();
            _weaponIds.Clear();
            _enemySpawnPoints.Clear();

            foreach (var weapon in GetAllWeapons())
            {
                _weaponIds.Add(weapon.Id);
            }

            _weaponIds.Sort();
            _currentWeaponId = _weaponIds.Count > 0 ? _weaponIds[0] : 101;

            _kills = 0;
            _pendingLevelUpSelections = 0;
            _waveIndex = 1;
            _waveSpawnedCount = 0;
            _health = DefaultPlayerHealth;
            _maxHealth = DefaultPlayerHealth;
            _poisonTimer = 0f;
            _poisonDamagePerSecond = 0f;
            _elapsedTime = 0f;
            _nextShotTime = 0f;
            _verticalVelocity = 0f;
            _externalVelocity = Vector3.zero;
            _isGameOver = false;
            _currentWaveElapsedTime = 0f;
            _waveSpawnStates.Clear();
            _weaponDamageBonusRate = 0f;
            _weaponCooldownReduceRate = 0f;
            _aoeDamageBonusRate = 0f;
            _pickupDropBonusRate = 0f;
            _moveSpeedBonusRate = 0f;
            _lifeStealRate = 0f;
            _killExplosionChanceRate = 0f;
            _shotgunSpreadReduceRate = 0f;
            _isOptionSelectionActive = false;
            _levelUpOptionService.Reset();
            _levelUpOptionPopup?.Close();

            var firstWave = GetWaveConfig(_waveIndex);
            _isPreparingWave = true;
            _waveStateTimer = firstWave?.PrepareTime ?? 5f;
            InitializeWaveRuntime(firstWave);

            if (!ShouldSkipGeneratedWorld())
            {
                BuildWorld();
            }
            else
            {
                EnsureSceneLighting();
            }
            BuildPlayer();
            if (_player == null || _playerController == null)
            {
                Log.Error("[Player] build failed, abort demo initialization.");
                return;
            }
            BuildCamera();
            BuildHud();
            RefreshHud();
            _isDemoReady = true;
        }

        private void TryBuildDemo()
        {
            var table = SurvivalWeaponTable;
            var monsterTable = MonsterTable;
            var skillTable = MonsterSkillTable;
            var waveTable = MonsterSpawnWaveTable;
            var bulletVisualTable = BulletVisualTable;
            var roleTable = RoleTable;
            var hasWeaponTable = table != null && table.Count > 0;
            if (hasWeaponTable &&
                roleTable != null && roleTable.Count > 0 &&
                monsterTable != null && monsterTable.Count > 0 &&
                skillTable != null && skillTable.Count > 0 &&
                waveTable != null && waveTable.Count > 0)
            {
                BuildDemo();
                return;
            }

            if (Time.frameCount % 120 == 0)
            {
                Log.Warning($"SurvivalDemoController waiting for config tables. survivalWeaponCount={table?.Count ?? -1}, roleCount={roleTable?.Count ?? -1}, monsterCount={monsterTable?.Count ?? -1}, skillCount={skillTable?.Count ?? -1}, waveCount={waveTable?.Count ?? -1}, bulletVisualCount={bulletVisualTable?.Count ?? -1}");
            }
        }

        private void ClearChildren()
        {
            if (_runtimeRoot != null)
            {
                Destroy(_runtimeRoot);
                _runtimeRoot = null;
            }
        }

        private void BuildWorld()
        {
            _worldRoot = new GameObject("World");
            _worldRoot.transform.SetParent(_runtimeRoot.transform, false);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(_worldRoot.transform, false);
            ground.transform.position = new Vector3(0.38319999f, 0f, 0.188099995f);
            ground.transform.localScale = new Vector3(9.63893032f, 1f, 9.31704712f);
            SetColor(ground, new Color32(56, 76, 58, 255));

            BuildBoundary(new Vector3(0f, 1.5f, ArenaSize), new Vector3(ArenaSize * 2f, 3f, 1f));
            BuildBoundary(new Vector3(0f, 1.5f, -ArenaSize), new Vector3(ArenaSize * 2f, 3f, 1f));
            BuildBoundary(new Vector3(ArenaSize, 1.5f, 0f), new Vector3(1f, 3f, ArenaSize * 2f));
            BuildBoundary(new Vector3(-ArenaSize, 1.5f, 0f), new Vector3(1f, 3f, ArenaSize * 2f));

            for (var i = 0; i < 14; i++)
            {
                var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cover.name = $"Cover_{i}";
                cover.transform.SetParent(_worldRoot.transform, false);
                cover.transform.position = new Vector3(Random.Range(-30f, 30f), 0.75f, Random.Range(-30f, 30f));
                cover.transform.localScale = new Vector3(Random.Range(2f, 5f), Random.Range(1f, 2.5f), Random.Range(2f, 5f));
                SetColor(cover, new Color32(88, 94, 101, 255));
            }

            var lightObject = new GameObject("DemoLight");
            lightObject.transform.SetParent(_worldRoot.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.60f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.72f, 0.76f, 0.71f);
            RenderSettings.fogDensity = 0.008f;
        }

        private static bool ShouldSkipGeneratedWorld()
        {
            var scene = SceneManager.GetActiveScene();
            return scene.IsValid() &&
                   !string.IsNullOrEmpty(scene.path) &&
                   scene.path.IndexOf("Chinese", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void BuildBoundary(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Boundary";
            wall.transform.SetParent(_worldRoot.transform, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            SetColor(wall, new Color32(66, 78, 72, 255));
        }

        private void BuildPlayer()
        {
            var roleConfig = GetCurrentRoleConfig();
            if (roleConfig == null || string.IsNullOrWhiteSpace(roleConfig.PrefabPath))
            {
                Log.Error($"[Player] role config missing or prefab path empty roleId={_currentRoleId}");
                return;
            }

            var playerPrefab = LoadPrefabAtPath(roleConfig.PrefabPath);
            if (playerPrefab == null)
            {
                Log.Error($"[Player] role prefab load failed roleId={roleConfig.RoleId} path={roleConfig.PrefabPath}");
                return;
            }

            _player = Instantiate(playerPrefab);
            _player.name = $"Player_{roleConfig.RoleName}";
            _player.transform.SetParent(_runtimeRoot.transform, false);
            _player.transform.position = GetInitialPlayerPosition();
            Log.Info($"[Player] spawned position={_player.transform.position}");

            _playerController = EnsurePlayerController(_player);
            _playerEntity = _player.GetComponent<PlayerController>();
            if (_playerEntity == null)
            {
                _playerEntity = _player.AddComponent<PlayerController>();
            }
            _playerEntity.InitializeRuntime(DefaultPlayerHealth, PlayerMoveSpeed, Gravity, ExternalVelocityDamping, 18f);
            _playerRenderers = _player.GetComponentsInChildren<Renderer>(true);
            _playerBaseColors.Clear();
            foreach (var renderer in _playerRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                _playerBaseColors[renderer] = renderer.material.color;
            }

            _weaponPivot = new GameObject("WeaponPivot").transform;
            _weaponPivot.SetParent(_player.transform, false);
            _weaponPivot.localPosition = new Vector3(0.4f, 1.15f, 0.45f);
            BuildWeaponAudioSource();

            ApplyWeaponVisual();
        }

        private Hotfix.Config.Tables.survival.RoleConfig GetCurrentRoleConfig()
        {
            var table = RoleTable;
            if (table == null || table.Count <= 0)
            {
                return null;
            }

            return table.Get(_currentRoleId) ?? table.FirstOrDefault;
        }

        private void BuildWeaponAudioSource()
        {
            if (_player == null)
            {
                _weaponAudioSource = null;
                return;
            }

            _weaponAudioSource = _player.GetComponent<AudioSource>();
            if (_weaponAudioSource == null)
            {
                _weaponAudioSource = _player.AddComponent<AudioSource>();
            }

            _weaponAudioSource.playOnAwake = false;
            _weaponAudioSource.loop = false;
            _weaponAudioSource.spatialBlend = 1f;
            _weaponAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _weaponAudioSource.dopplerLevel = 0f;
            _weaponAudioSource.minDistance = 1.5f;
            _weaponAudioSource.maxDistance = 18f;
        }

        private Vector3 GetInitialPlayerPosition()
        {
            if (spawnpoint == null)
            {
                spawnpoint = FindSceneObjectWithTag("sp point") ??
                             FindSceneObject("PlayerSpawnPoint") ??
                             FindSceneObject("PlaySpawnPoint") ??
                             FindSceneObject("SpawnPoint");
            }

            var spawnPoint = spawnpoint;
            if (spawnPoint != null)
            {
                Log.Info($"[SpawnPoint] resolved name={spawnPoint.name} active={spawnPoint.activeInHierarchy} position={spawnPoint.transform.position}");
                return spawnPoint.transform.position;
            }

            Log.Warning("[SpawnPoint] scene spawn point not found, fallback to ground center.");

            var groundObject = FindSceneGroundObject();
            if (groundObject != null)
            {
                var collider = groundObject.GetComponent<Collider>() ?? groundObject.GetComponentInChildren<Collider>();
                if (collider != null)
                {
                    var bounds = collider.bounds;
                    return new Vector3(bounds.center.x, bounds.max.y + 1.1f, bounds.center.z);
                }

                return groundObject.transform.position + Vector3.up * 1.1f;
            }

            return new Vector3(0f, 1.1f, 0f);
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform != null &&
                    transform.gameObject.scene.IsValid() &&
                    string.Equals(transform.name, objectName, System.StringComparison.Ordinal))
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindSceneObjectWithTag(string tag)
        {
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform == null)
                {
                    continue;
                }

                var gameObject = transform.gameObject;
                if (!gameObject.scene.IsValid())
                {
                    continue;
                }

                if (string.Equals(gameObject.tag, tag, System.StringComparison.Ordinal))
                {
                    return gameObject;
                }
            }

            return null;
        }

        private static List<Transform> FindSceneObjectsWithTag(string tag)
        {
            var results = new List<Transform>();
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform == null)
                {
                    continue;
                }

                var gameObject = transform.gameObject;
                if (!gameObject.scene.IsValid())
                {
                    continue;
                }

                if (string.Equals(gameObject.tag, tag, System.StringComparison.Ordinal))
                {
                    results.Add(transform);
                }
            }

            return results;
        }

        private static GameObject FindSceneGroundObject()
        {
            return FindSceneObject("Terrain_SimpleMesh") ??
                   FindSceneObject("Ground") ??
                   FindSceneObject("Terrain") ??
                   FindSceneObject("Terrain_lot") ??
                   FindSceneObject("Map") ??
                   FindSceneObject("Environment");
        }

        private static Vector3 ProjectPointToGround(Vector3 point, float heightOffset = 0f)
        {
            var groundObject = FindSceneGroundObject();
            var collider = groundObject != null
                ? groundObject.GetComponent<Collider>() ?? groundObject.GetComponentInChildren<Collider>()
                : null;

            if (collider != null)
            {
                var bounds = collider.bounds;
                var rayOrigin = new Vector3(point.x, bounds.max.y + 200f, point.z);
                if (collider.Raycast(new Ray(rayOrigin, Vector3.down), out var hit, bounds.size.y + 400f))
                {
                    return hit.point + Vector3.up * heightOffset;
                }
            }

            var physicsOrigin = point + Vector3.up * 200f;
            if (Physics.Raycast(physicsOrigin, Vector3.down, out var physicsHit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return physicsHit.point + Vector3.up * heightOffset;
            }

            point.y = heightOffset;
            return point;
        }

        private void BuildCamera()
        {
            LogCameraStates("BuildCamera.before");
            foreach (var existingCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existingCamera == null)
                {
                    continue;
                }

                if (ShouldKeepCameraEnabled(existingCamera))
                {
                    continue;
                }

                existingCamera.enabled = false;
                var listener = existingCamera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }

            var cameraObject = new GameObject("DemoCamera");
            cameraObject.transform.SetParent(_runtimeRoot.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            _camera.enabled = true;
            _camera.fieldOfView = 63f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 300f;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.depth = -1f;
            _camera.cullingMask = GetGameplayCullingMask();
            _camera.targetTexture = null;
            _camera.transform.position = DemoCameraInitialPosition;
            _camera.transform.rotation = DemoCameraInitialRotation;
            Log.Info($"[Camera] created DemoCamera scene={SceneManager.GetActiveScene().name} uiLayer={GetUiLayer()} mask={_camera.cullingMask} depth={_camera.depth}");
            LogCameraStates("BuildCamera.after");
        }

        private void EnsureBootstrapCamera()
        {
            if (_isDemoReady)
            {
                return;
            }

            if (_bootstrapCameraRoot != null)
            {
                return;
            }

            foreach (var existingCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existingCamera == null)
                {
                    continue;
                }

                if (ShouldKeepCameraEnabled(existingCamera))
                {
                    continue;
                }

                existingCamera.enabled = false;
                var listener = existingCamera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }

            _bootstrapCameraRoot = new GameObject("BootstrapDemoCamera");
            var bootstrapCamera = _bootstrapCameraRoot.AddComponent<Camera>();
            bootstrapCamera.fieldOfView = 63f;
            bootstrapCamera.nearClipPlane = 0.1f;
            bootstrapCamera.farClipPlane = 300f;
            bootstrapCamera.clearFlags = CameraClearFlags.Skybox;
            bootstrapCamera.depth = -1f;
            bootstrapCamera.cullingMask = GetGameplayCullingMask();
            _bootstrapCameraRoot.AddComponent<AudioListener>();

            var anchor = FindSceneObjectWithTag("sp point") ??
                         FindSceneObject("PlayerSpawnPoint") ??
                         FindSceneObject("PlaySpawnPoint") ??
                         FindSceneObject("SpawnPoint");
            var anchorPosition = anchor != null ? anchor.transform.position : new Vector3(0f, 1f, 0f);
            _bootstrapCameraRoot.transform.position = anchorPosition + new Vector3(0f, 24f, -15f);
            _bootstrapCameraRoot.transform.LookAt(anchorPosition + Vector3.up * 1.25f);
            Log.Info($"[Camera] bootstrap created pos={_bootstrapCameraRoot.transform.position} lookAt={anchorPosition} uiLayer={GetUiLayer()} mask={bootstrapCamera.cullingMask} depth={bootstrapCamera.depth}");
        }

        private static int GetGameplayCullingMask()
        {
            var uiLayer = GetUiLayer();
            if (uiLayer < 0)
            {
                return ~0;
            }

            return ~LayerMask.GetMask("UI");
        }

        private static int GetUiLayer()
        {
            return LayerMask.NameToLayer("UI");
        }

        private static bool ShouldKeepCameraEnabled(Camera camera)
        {
            if (camera == null)
            {
                return false;
            }

            if (string.Equals(camera.name, "UGUICamera", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(camera.name, "StageCamra", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(camera.name, "StageCamera", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var uiLayer = GetUiLayer();
            if (uiLayer < 0)
            {
                return false;
            }

            return (camera.cullingMask & (1 << uiLayer)) != 0;
        }

        private void DestroyBootstrapCamera()
        {
            if (_bootstrapCameraRoot == null)
            {
                return;
            }

            Destroy(_bootstrapCameraRoot);
            _bootstrapCameraRoot = null;
        }

        private void EnsureSceneLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            if (RenderSettings.ambientLight.maxColorComponent < 0.2f)
            {
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.60f);
            }

            var hasEnabledLight = false;
            foreach (var sceneLight in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sceneLight != null && sceneLight.enabled && sceneLight.gameObject.activeInHierarchy)
                {
                    hasEnabledLight = true;
                    break;
                }
            }

            if (hasEnabledLight)
            {
                return;
            }

            _sceneLightingRoot = new GameObject("SceneLightingFallback");
            _sceneLightingRoot.transform.SetParent(_runtimeRoot.transform, false);
            _sceneLightingRoot.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            var light = _sceneLightingRoot.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("Hud");
            canvasObject.transform.SetParent(_runtimeRoot.transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            _hudText = CreateText("HudText", canvasObject.transform, 28, TextAnchor.UpperLeft);
            _hudText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _hudText.rectTransform.anchorMax = new Vector2(0f, 1f);
            _hudText.rectTransform.pivot = new Vector2(0f, 1f);
            _hudText.rectTransform.anchoredPosition = new Vector2(42f, -36f);
            _hudText.rectTransform.sizeDelta = new Vector2(960f, 320f);

            var tipText = CreateText("TipText", canvasObject.transform, 24, TextAnchor.LowerLeft);
            tipText.text = "WASD Move  Mouse Aim  Left Click Fire  Auto Pickup  R Restart";
            tipText.color = new Color32(233, 235, 221, 255);
            tipText.rectTransform.anchorMin = new Vector2(0f, 0f);
            tipText.rectTransform.anchorMax = new Vector2(0f, 0f);
            tipText.rectTransform.pivot = new Vector2(0f, 0f);
            tipText.rectTransform.anchoredPosition = new Vector2(42f, 32f);
            tipText.rectTransform.sizeDelta = new Vector2(1200f, 60f);

            _crosshairText = CreateText("Crosshair", canvasObject.transform, 40, TextAnchor.MiddleCenter);
            _crosshairText.text = "+";
            _crosshairText.color = new Color32(255, 243, 192, 255);
            _crosshairText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _crosshairText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _crosshairText.rectTransform.anchoredPosition = Vector2.zero;
            _crosshairText.rectTransform.sizeDelta = new Vector2(60f, 60f);

            _gameOverText = CreateText("GameOver", canvasObject.transform, 44, TextAnchor.MiddleCenter);
            _gameOverText.color = new Color32(255, 122, 102, 255);
            _gameOverText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _gameOverText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _gameOverText.rectTransform.anchoredPosition = new Vector2(0f, 140f);
            _gameOverText.rectTransform.sizeDelta = new Vector2(1100f, 240f);
            _gameOverText.text = string.Empty;
        }

        private void UpdatePlayer(float deltaTime)
        {
            if (_playerEntity == null || _playerController == null)
            {
                return;
            }

            var moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            var forward = _camera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            var right = _camera.transform.right;
            right.y = 0f;
            right.Normalize();

            var moveDirection = (forward * moveInput.z + right * moveInput.x) * (1f + _moveSpeedBonusRate);

            if (_poisonTimer > 0f)
            {
                _poisonTimer -= deltaTime;
                _health = Mathf.Max(0f, _health - _poisonDamagePerSecond * deltaTime);
                SetPlayerTint(new Color32(118, 205, 112, 255));
                if (_health <= 0f)
                {
                    _playerEntity.OnDie();
                    HandleGameOver();
                    return;
                }
            }

            var aimPoint = GetAimPoint();
            var aimDirection = aimPoint - _player.transform.position;
            aimDirection.y = 0f;
            var firePressed = Input.GetMouseButton(0);
            _playerEntity.SetCommand(new Vector2(moveInput.x, moveInput.z), aimDirection, firePressed, false, false);
            _playerEntity.TickLocomotion(deltaTime, moveDirection, aimDirection);

            if (firePressed && Time.time >= _nextShotTime)
            {
                _playerEntity.NotifyAttack();
                FireWeapon(aimPoint);
            }

            _externalVelocity = _playerEntity.ExternalVelocity;
            _verticalVelocity = _playerEntity.VerticalVelocity;
            _playerEntity.ClearTransientCommandFlags();
        }

        private Vector3 GetAimPoint()
        {
            if (_camera == null)
            {
                return _player.transform.position + _player.transform.forward * 10f;
            }

            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var enter))
            {
                return ray.GetPoint(enter);
            }

            return _player.transform.position + _player.transform.forward * 10f;
        }

        private void FireWeapon(Vector3 aimPoint)
        {
            var weapon = GetWeapon(_currentWeaponId);
            if (weapon == null)
            {
                return;
            }

            var adjustedFireInterval = Mathf.Max(0.01f, weapon.FireInterval * (1f - _weaponCooldownReduceRate));
            _nextShotTime = Time.time + adjustedFireInterval;
            PlayWeaponFireSound(weapon, _weaponAudioSource);

            var range = Mathf.Clamp(weapon.LifeTime * 18f, 8f, 60f);
            var spreadAngle = GetWeaponSpreadAngle(weapon) * Mathf.Max(0.05f, 1f - _shotgunSpreadReduceRate);
            var bulletVisual = GetBulletVisual(weapon.BulletVisualId);
            Log.Info($"[BulletVisual] fire weapon={weapon.Id} name={weapon.Name} bulletVisualId={weapon.BulletVisualId} visualNull={bulletVisual == null}");
            var fireOrigin = ResolveFireOrigin(bulletVisual);
            var resolvedAimPoint = aimPoint;
            resolvedAimPoint.y = fireOrigin.y;

            var baseDirection = (resolvedAimPoint - fireOrigin).normalized;
            if (baseDirection.sqrMagnitude < 0.001f)
            {
                baseDirection = _player.transform.forward;
            }

            for (var i = 0; i < Mathf.Max(1, weapon.BulletCount); i++)
            {
                var yawOffset = Random.Range(-spreadAngle, spreadAngle);
                var direction = Quaternion.Euler(
                    0f,
                    yawOffset,
                    0f) * baseDirection;
                SpawnBullet(weapon, fireOrigin, direction.normalized, bulletVisual);
            }
        }

        private void SpawnBullet(WeaponConfig weapon, Vector3 fireOrigin, Vector3 direction, Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual)
        {
            SpawnFxAtPath(GetMuzzleFxPath(bulletVisual), fireOrigin, Quaternion.LookRotation(direction, Vector3.up), bulletVisual?.ImpactLifeTime ?? 1f, bulletVisual?.FxScale ?? 1f);

            var bulletObject = CreateBulletObject(weapon, bulletVisual, direction);
            if (bulletObject == null)
            {
                Log.Error($"[BulletVisual] abort spawn weapon={weapon?.Id} visualId={bulletVisual?.BulletVisualId ?? 0} tracerPath={bulletVisual?.TracerFxPath}");
                return;
            }

            var projectileRotationOffset = bulletObject.transform.rotation;
            var projectileFacing = Quaternion.LookRotation(direction, Vector3.up);
            bulletObject.name = $"Bullet_{weapon.Name}";
            bulletObject.transform.SetParent(_runtimeRoot.transform, false);
            bulletObject.transform.position = fireOrigin + direction * 0.9f;
            bulletObject.transform.rotation = projectileFacing * projectileRotationOffset;
            Log.Info($"[BulletRotation] weapon={weapon?.Id} dir={direction} prefabOffset={projectileRotationOffset.eulerAngles} final={bulletObject.transform.rotation.eulerAngles}");

            var sphereCollider = bulletObject.GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = bulletObject.AddComponent<SphereCollider>();
            }

            sphereCollider.isTrigger = true;
            sphereCollider.radius = weapon.ExplosionRadius > 0f ? 0.16f : 0.08f;
            sphereCollider.center = Vector3.zero;

            var rigidbody = bulletObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = bulletObject.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            EnsureProjectileAudioSource(bulletObject);

            var renderers = bulletObject.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                var tint = GetWeaponColor(weapon.Id);
                foreach (var currentRenderer in renderers)
                {
                    if (currentRenderer == null || currentRenderer.sharedMaterial == null || !currentRenderer.sharedMaterial.HasProperty("_Color"))
                    {
                        continue;
                    }

                    currentRenderer.material.color = tint;
                }
            }

            var bullet = bulletObject.AddComponent<Bullet>();
            bullet.Init(weapon, direction, projectileRotationOffset, (foreign, hitPoint) => OnBulletHit(foreign, hitPoint, weapon, bulletVisual));
        }

        private void OnBulletHit(Collider foreign, Vector3 hitPoint, WeaponConfig weapon, Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual)
        {
            if (EnableEnemyHitDebugLog)
            {
                Debug.Log($"[EnemyHit] bullet hit callback collider={foreign?.name} root={foreign?.transform.root.name} point={hitPoint}");
            }

            var marker = foreign.GetComponentInParent<EnemyMarker>();
            if (marker == null || marker.Owner == null)
            {
                SpawnFxAtPath(GetImpactGroundFxPath(bulletVisual), hitPoint, Quaternion.identity, bulletVisual?.ImpactLifeTime ?? 1f, bulletVisual?.FxScale ?? 1f);
                if (EnableEnemyHitDebugLog)
                {
                    Debug.LogWarning($"[EnemyHit] marker missing for collider={foreign?.name} root={foreign?.transform.root.name}");
                }
                return;
            }

            SpawnFxAtPath(GetImpactEnemyFxPath(bulletVisual), hitPoint, Quaternion.identity, bulletVisual?.ImpactLifeTime ?? 1f, bulletVisual?.FxScale ?? 1f);
            if (EnableEnemyHitDebugLog)
            {
                Debug.Log($"[EnemyHit] marker found enemy={marker.Owner.Name} hp={marker.Owner.Health:0.##}");
            }

            ApplyWeaponDamage(marker.Owner, hitPoint, weapon);
        }

        private void ApplyWeaponDamage(EnemyData enemy, Vector3 hitPoint, WeaponConfig weapon)
        {
            if (weapon.ExplosionRadius > 0f)
            {
                for (var i = _enemies.Count - 1; i >= 0; i--)
                {
                    var target = _enemies[i];
                    if (target.Root == null)
                    {
                        continue;
                    }

                    var distance = Vector3.Distance(target.Root.transform.position, hitPoint);
                    if (distance > weapon.ExplosionRadius)
                    {
                        continue;
                    }

                    var falloff = 1f - Mathf.Clamp01(distance / weapon.ExplosionRadius);
                    DamageEnemy(target, GetAdjustedDamage(weapon, weapon.Damage * Mathf.Max(0.25f, falloff)));
                }

                return;
            }

            DamageEnemy(enemy, GetAdjustedDamage(weapon, weapon.Damage));
        }

        private void DamageEnemy(EnemyData enemy, float damage)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            var previousHealth = enemy.Health;
            if (enemy.Gameplay != null)
            {
                var result = DamageResolver.Resolve(new DamageRequest(null, enemy.Gameplay.Combat, damage));
                enemy.Gameplay.ApplyDamageResult(result);
                enemy.Health = enemy.Gameplay.Hp;
            }
            else
            {
                enemy.Health -= damage;
            }

            SetEnemyTint(enemy, new Color32(255, 72, 72, 255));
            enemy.HitFlashTimer = 0.12f;
            if (enemy.Health > 0f)
            {
                enemy.Gameplay?.NotifyHit();
                ApplyLifeSteal(previousHealth - enemy.Health);
            }

            if (enemy.Health <= 0f)
            {
                ApplyLifeSteal(previousHealth);
                KillEnemy(enemy);
            }
        }

        private void UpdateEnemies(float deltaTime)
        {
            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (enemy.Root == null)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }

                var gameplay = enemy.Gameplay;
                if (gameplay == null)
                {
                    continue;
                }

                gameplay.SetTarget(_player.transform);
                gameplay.TickBehaviour(deltaTime);
                enemy.Health = gameplay.Hp;
                enemy.IsDead = gameplay.IsDead;

                if (gameplay.TryAttack())
                {
                    var playerResult = DamageResolver.Resolve(new DamageRequest(
                        gameplay.Combat,
                        _playerEntity?.Combat,
                        gameplay.AttackDamage,
                        gameplay.PoisonDps,
                        gameplay.PoisonDuration,
                        gameplay.KnockbackForce,
                        gameplay.CurrentMoveDirection));
                    _playerEntity?.ApplyDamageResult(playerResult);
                    _health = _playerEntity?.Hp ?? Mathf.Max(0f, _health - gameplay.AttackDamage);
                    SetPlayerTint(gameplay.PoisonDps > 0f
                        ? new Color32(118, 205, 112, 255)
                        : new Color32(255, 110, 110, 255));

                    if (playerResult.AppliedPoison)
                    {
                        _poisonDamagePerSecond = Mathf.Max(_poisonDamagePerSecond, playerResult.PoisonDps);
                        _poisonTimer = Mathf.Max(_poisonTimer, playerResult.PoisonDuration);
                    }

                    if (playerResult.HasKnockback)
                    {
                        if (_playerEntity != null)
                        {
                            _playerEntity.AddExternalVelocity(playerResult.KnockbackDirection * playerResult.KnockbackForce);
                            _externalVelocity = _playerEntity.ExternalVelocity;
                        }
                        else
                        {
                            _externalVelocity += playerResult.KnockbackDirection * playerResult.KnockbackForce;
                        }
                    }

                    if (_health <= 0f)
                    {
                        _health = 0f;
                        HandleGameOver();
                        return;
                    }
                }

                if (enemy.HitFlashTimer > 0f)
                {
                    enemy.HitFlashTimer -= deltaTime;
                }
                else
                {
                    RestoreEnemyTint(enemy, deltaTime);
                }
            }

            if (!_isGameOver)
            {
                RestorePlayerTint(deltaTime);
            }
        }

        private void UpdatePickups(float deltaTime)
        {
            for (var i = _pickups.Count - 1; i >= 0; i--)
            {
                var pickup = _pickups[i];
                if (pickup.Root == null)
                {
                    _pickups.RemoveAt(i);
                    continue;
                }

                pickup.LifeTime -= deltaTime;
                pickup.Root.transform.Rotate(0f, 80f * deltaTime, 0f, Space.World);
                var pickupPosition = pickup.BasePosition;
                pickupPosition.y += Mathf.Sin((_elapsedTime + pickup.BobPhase) * 3f) * 0.25f;
                pickup.Root.transform.position = pickupPosition;

                if (pickup.LifeTime <= 0f)
                {
                    Destroy(pickup.Root);
                    _pickups.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(_player.transform.position, pickup.Root.transform.position) > PickupRadius)
                {
                    continue;
                }

                _currentWeaponId = pickup.WeaponId;
                ApplyWeaponVisual();
                Destroy(pickup.Root);
                _pickups.RemoveAt(i);
            }
        }

        private void UpdateWave(float deltaTime)
        {
            var currentWave = GetWaveConfig(_waveIndex);
            if (currentWave == null)
            {
                return;
            }

            if (_isPreparingWave)
            {
                _waveStateTimer -= deltaTime;
                if (_waveStateTimer <= 0f)
                {
                    _isPreparingWave = false;
                    _currentWaveElapsedTime = 0f;
                    _waveSpawnedCount = 0;
                    InitializeWaveRuntime(currentWave);
                }

                return;
            }

            _currentWaveElapsedTime += deltaTime;
            for (var spawnGuard = 0; spawnGuard < 8; spawnGuard++)
            {
                var spawnState = GetNextEligibleSpawnState();
                if (spawnState == null)
                {
                    break;
                }

                SpawnEnemy(spawnState.MonsterId);
                _waveSpawnedCount++;
                spawnState.RemainingCount--;
                spawnState.NextSpawnTime = Mathf.Max(spawnState.NextSpawnTime, _currentWaveElapsedTime) +
                                          Mathf.Max(0.05f, spawnState.SpawnInterval);
            }

            if (_waveSpawnedCount >= currentWave.TotalCount && _enemies.Count == 0)
            {
                _waveIndex++;
                var nextWave = GetWaveConfig(_waveIndex);
                if (nextWave != null)
                {
                    _isPreparingWave = true;
                    _waveStateTimer = nextWave.PrepareTime;
                    _waveSpawnedCount = 0;
                    _currentWaveElapsedTime = 0f;
                    InitializeWaveRuntime(nextWave);
                }
            }
        }

        private void InitializeWaveRuntime(WaveConfig wave)
        {
            _waveSpawnStates.Clear();
            if (wave?.SpawnEntries == null)
            {
                return;
            }

            foreach (var entry in wave.SpawnEntries)
            {
                _waveSpawnStates.Add(new WaveSpawnRuntimeState
                {
                    EntryId = entry.Id,
                    MonsterId = entry.MonsterId,
                    RemainingCount = Mathf.Max(0, entry.SpawnCount),
                    StartTime = Mathf.Max(0f, entry.StartTime),
                    SpawnInterval = Mathf.Max(0.05f, entry.SpawnInterval),
                    NextSpawnTime = Mathf.Max(0f, entry.StartTime),
                    SpawnWeight = Mathf.Max(1, entry.SpawnWeight)
                });
            }
        }

        private WaveSpawnRuntimeState GetNextEligibleSpawnState()
        {
            var eligibleStates = _waveSpawnStates
                .Where(state => state.RemainingCount > 0 && _currentWaveElapsedTime >= state.StartTime && _currentWaveElapsedTime >= state.NextSpawnTime)
                .ToList();
            if (eligibleStates.Count == 0)
            {
                return null;
            }

            var totalWeight = eligibleStates.Sum(state => Mathf.Max(1, state.SpawnWeight));
            var roll = Random.Range(0, totalWeight);
            foreach (var state in eligibleStates)
            {
                roll -= Mathf.Max(1, state.SpawnWeight);
                if (roll < 0)
                {
                    return state;
                }
            }

            return eligibleStates[eligibleStates.Count - 1];
        }

        private void SpawnEnemy(int monsterId)
        {
            var config = GetMonster(monsterId);
            if (config == null)
            {
                return;
            }

            var skill = GetMonsterSkill(config.SkillConfigId);
            if (skill == null)
            {
                return;
            }

            var spawnPosition = Vector3.zero;
            var hasFixedSpawnPoint = TryGetEnemySpawnPoint(out spawnPosition);
            if (!hasFixedSpawnPoint)
            {
                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var radius = Random.Range(26f, 38f);
                spawnPosition = _player.transform.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (!ShouldSkipGeneratedWorld())
                {
                    spawnPosition.x = Mathf.Clamp(spawnPosition.x, -ArenaSize + 2f, ArenaSize - 2f);
                    spawnPosition.z = Mathf.Clamp(spawnPosition.z, -ArenaSize + 2f, ArenaSize - 2f);
                    spawnPosition.y = 1f;
                }
                else
                {
                    spawnPosition = ProjectPointToGround(spawnPosition, 0.05f);
                }
            }

            var enemyObject = CreateEnemyVisual(config, out var baseColor);
            enemyObject.transform.SetParent(_runtimeRoot.transform, false);
            enemyObject.transform.position = spawnPosition;
            var enemyMovementController = EnsureEnemyController(enemyObject, config);
            var enemyGameplay = enemyObject.GetComponent<EnemyController>();
            if (enemyGameplay == null)
            {
                enemyGameplay = enemyObject.AddComponent<EnemyController>();
            }
            enemyGameplay.InitializeRuntime(
                config.MaxHp,
                config.MoveSpeed,
                config.AttackRange,
                config.AttackDamage,
                config.AttackInterval,
                skill.AiType,
                skill.PreferredRange,
                skill.PoisonDps,
                skill.PoisonDuration,
                skill.DashTriggerRange,
                skill.DashCooldown,
                skill.DashDuration,
                skill.DashMultiplier,
                skill.KnockbackForce,
                EnemyGravity);

            var enemy = new EnemyData
            {
                Root = enemyObject,
                Controller = enemyMovementController,
                Gameplay = enemyGameplay,
                Renderers = enemyObject.GetComponentsInChildren<Renderer>(true),
                ConfigId = config.Id,
                Name = config.Name,
                BaseColor = baseColor,
                MaxHealth = config.MaxHp,
                Health = config.MaxHp
            };

            var marker = enemyObject.AddComponent<EnemyMarker>();
            marker.Owner = enemy;
            enemyGameplay.PlayAnimation("Aggro");
            if (EnableEnemyHitDebugLog)
            {
                var colliders = enemyObject.GetComponentsInChildren<Collider>(true);
                Debug.Log($"[EnemySpawn] name={enemy.Name} root={enemyObject.name} colliders={colliders.Length} animator={(enemyGameplay.Anim != null)}");
                foreach (var collider in colliders)
                {
                    Debug.Log($"[EnemySpawn] collider={collider.name} trigger={collider.isTrigger} center={collider.bounds.center} size={collider.bounds.size}");
                }
            }
            _enemies.Add(enemy);
        }

        private bool TryGetEnemySpawnPoint(out Vector3 spawnPosition)
        {
            if (_enemySpawnPoints.Count == 0)
            {
                _enemySpawnPoints.AddRange(FindSceneObjectsWithTag("en point"));
                if (_enemySpawnPoints.Count > 0)
                {
                    Log.Info($"[EnemySpawnPoint] resolved count={_enemySpawnPoints.Count}");
                }
            }

            if (_enemySpawnPoints.Count == 0)
            {
                spawnPosition = Vector3.zero;
                return false;
            }

            var validPoints = _enemySpawnPoints.Where(point => point != null).ToList();
            if (validPoints.Count == 0)
            {
                spawnPosition = Vector3.zero;
                return false;
            }

            var distantPoints = validPoints
                .Where(point => _player == null || Vector3.Distance(point.position, _player.transform.position) >= 12f)
                .ToList();
            var candidates = distantPoints.Count > 0 ? distantPoints : validPoints;
            var selectedPoint = candidates[Random.Range(0, candidates.Count)];
            spawnPosition = selectedPoint.position;
            return true;
        }

        private GameObject CreateEnemyVisual(MonsterConfig config, out Color baseColor)
        {
            var prefab = LoadEnemyPrefab(config.PrefabPath);
            if (prefab != null)
            {
                baseColor = GetEnemyBaseColor(config.AiType);
                var instance = Instantiate(prefab);
                instance.name = config.Name;
                instance.transform.localScale = Vector3.one * Mathf.Max(0.1f, config.BodyScale);
                BindEnemyAnimatorController(instance, config);
                EnsureEnemyCollider(instance, config);
                return instance;
            }

            PrimitiveType primitiveType;
            Vector3 scale;
            switch (config.AiType)
            {
                case "Tank":
                    primitiveType = PrimitiveType.Cube;
                    scale = new Vector3(2.3f, 2.3f, 2.3f);
                    baseColor = GetEnemyBaseColor(config.AiType);
                    break;
                case "Ranged":
                    primitiveType = PrimitiveType.Cylinder;
                    scale = new Vector3(0.9f, 1.2f, 0.9f);
                    baseColor = GetEnemyBaseColor(config.AiType);
                    break;
                case "Runner":
                    primitiveType = PrimitiveType.Capsule;
                    scale = new Vector3(0.8f, 0.95f, 0.8f);
                    baseColor = GetEnemyBaseColor(config.AiType);
                    break;
                default:
                    primitiveType = PrimitiveType.Capsule;
                    scale = new Vector3(0.9f, 1f, 0.9f);
                    baseColor = GetEnemyBaseColor(config.AiType);
                    break;
            }

            var enemyObject = GameObject.CreatePrimitive(primitiveType);
            enemyObject.name = config.Name;
            enemyObject.transform.localScale = scale * Mathf.Max(0.1f, config.BodyScale);
            SetColor(enemyObject, baseColor);
            return enemyObject;
        }

        private void KillEnemy(EnemyData enemy, bool allowDrop = true)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            enemy.IsDead = true;
            enemy.Gameplay?.OnDie();
            _kills++;
            GrantBattleExperience(GetMonster(enemy.ConfigId)?.ExpAward ?? 0f);
            if (enemy.Root != null)
            {
                TryTriggerKillExplosion(enemy.Root.transform.position);
            }
            _enemies.Remove(enemy);

            if (enemy.Root == null)
            {
                return;
            }

            foreach (var collider in enemy.Root.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            var marker = enemy.Root.GetComponent<EnemyMarker>();
            if (marker != null)
            {
                marker.enabled = false;
                marker.Owner = null;
            }

            if (allowDrop)
            {
                MaybeSpawnPickup(enemy.Root.transform.position);
            }

            if (enemy.Gameplay?.Anim != null)
            {
                Destroy(enemy.Root, 2f);
                return;
            }

            Destroy(enemy.Root);
        }

        private void MaybeSpawnPickup(Vector3 position)
        {
            var dropChance = Mathf.Clamp01(0.28f + _pickupDropBonusRate);
            if (_weaponIds.Count <= 1 || Random.value > dropChance)
            {
                return;
            }

            var nextWeaponId = _weaponIds[Random.Range(1, _weaponIds.Count)];
            var weapon = GetWeapon(nextWeaponId);
            if (weapon == null)
            {
                return;
            }

            var pickupObject = CreatePickupObject(weapon, position);
            if (pickupObject == null)
            {
                Log.Warning($"[Pickup] spawn skipped, drop model missing weapon={weapon.Id} name={weapon.Name}");
                return;
            }

            _pickups.Add(new PickupData
            {
                Root = pickupObject,
                WeaponId = nextWeaponId,
                LifeTime = 20f,
                BasePosition = pickupObject.transform.position,
                BobPhase = Random.Range(0f, 10f)
            });
        }

        private void UpdateCamera()
        {
            if (_camera == null || _player == null)
            {
                if (Time.frameCount % 60 == 0)
                {
                    Log.Warning($"[Camera] update skipped cameraNull={_camera == null} playerNull={_player == null}");
                }
                return;
            }

            if (!_camera.enabled || !_camera.gameObject.activeInHierarchy)
            {
                if (Time.frameCount % 60 == 0)
                {
                    Log.Warning($"[Camera] target invalid name={_camera.name} active={_camera.gameObject.activeInHierarchy} enabled={_camera.enabled}");
                    LogCameraStates("UpdateCamera.invalid");
                }
                return;
            }

            var targetPosition = _player.transform.position + new Vector3(0f, 12f, -11f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPosition, 14f * Time.deltaTime);
            _camera.transform.LookAt(_player.transform.position + Vector3.up * 1.25f);

            if (Time.frameCount % 60 == 0)
            {
                var forward = _camera.transform.forward;
                var hitText = "none";
                if (Physics.Raycast(_camera.transform.position, forward, out var hit, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    hitText = $"{hit.collider.name}@{hit.point}";
                }

                Log.Info($"[Camera] tick name={_camera.name} pos={_camera.transform.position} rot={_camera.transform.rotation.eulerAngles} clear={_camera.clearFlags} mask={_camera.cullingMask} targetTexture={(_camera.targetTexture != null ? _camera.targetTexture.name : "null")} playerPos={_player.transform.position} forwardHit={hitText}");
            }
        }

        private static void LogCameraStates(string stage)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cameras == null || cameras.Length == 0)
            {
                Log.Warning($"[Camera] {stage} no cameras found");
                return;
            }

            var states = string.Join(" | ", cameras.Select(camera =>
                $"{camera.name}(active={camera.gameObject.activeInHierarchy},enabled={camera.enabled},tag={camera.tag},pos={camera.transform.position})"));
            Log.Info($"[Camera] {stage} count={cameras.Length} {states}");
        }

        private void HandleGameOver()
        {
            _isGameOver = true;
            _gameOverText.text = $"Game Over\nTime {_elapsedTime:0.0}s  Kills {_kills}\nPress R to restart";
            _crosshairText.text = "X";
        }

        private void ApplyWeaponVisual()
        {
            var weapon = GetWeapon(_currentWeaponId);
            if (weapon != null)
            {
                _playerEntity?.SetWeaponType(weapon.Type);
            }
        }

        private void RefreshHud()
        {
            var weapon = GetWeapon(_currentWeaponId);
            var currentWave = GetWaveConfig(_waveIndex);
            var waveLabel = currentWave == null ? "Completed" : $"{currentWave.WaveIndex} - {currentWave.WaveName}";
            var stateLabel = _isPreparingWave
                ? $"Prepare {_waveStateTimer:0.0}s"
                : $"{_waveSpawnedCount}/{currentWave?.TotalCount ?? 0}";

            _hudText.text =
                $"Weapon: {weapon?.Name ?? "None"}\n" +
                $"Health: {_health:0} / {_maxHealth:0}\n" +
                $"Level: {_levelUpOptionService.CurrentLevel}  Exp: {_levelUpOptionService.CurrentExp:0}/{_levelUpOptionService.NextLevelExp:0}\n" +
                $"Poison: {Mathf.Max(0f, _poisonTimer):0.0}s\n" +
                $"Kills: {_kills}\n" +
                $"Time: {_elapsedTime:0.0}s\n" +
                $"Wave: {waveLabel}\n" +
                $"Wave State: {stateLabel}\n" +
                $"Enemies: {_enemies.Count}\n" +
                $"Drops: {_pickups.Count}\n" +
                $"Pending Level Up: {_pendingLevelUpSelections}";

            if (!_isGameOver)
            {
                _gameOverText.text = string.Empty;
                _crosshairText.text = "+";
            }
        }

        private static float GetWeaponSpreadAngle(WeaponConfig weapon)
        {
            if (weapon.BulletCount >= 8)
            {
                return 7f;
            }

            if (weapon.FireInterval <= 0.05f)
            {
                return 4f;
            }

            return weapon.BulletCount > 1 ? 2f : 0.6f;
        }

        private float GetAdjustedDamage(WeaponConfig weapon, float baseDamage)
        {
            var damage = baseDamage * (1f + _weaponDamageBonusRate);
            if (weapon != null && weapon.ExplosionRadius > 0f)
            {
                damage *= 1f + _aoeDamageBonusRate;
            }

            return damage;
        }

        private void ApplyLifeSteal(float damageAmount)
        {
            if (_lifeStealRate <= 0f || damageAmount <= 0f || _health <= 0f)
            {
                return;
            }

            _health = Mathf.Min(_maxHealth, _health + damageAmount * _lifeStealRate);
        }

        private void GrantBattleExperience(float expAward)
        {
            var levelUpCount = _levelUpOptionService.AddExperience(expAward);
            Log.Info($"[LevelUpFlow] Grant exp={expAward:0.##} currentLevel={_levelUpOptionService.CurrentLevel} currentExp={_levelUpOptionService.CurrentExp:0.##}/{_levelUpOptionService.NextLevelExp:0.##} levelUpCount={levelUpCount}");
            if (levelUpCount <= 0)
            {
                return;
            }

            _pendingLevelUpSelections += levelUpCount;
            Log.Info($"[LevelUpFlow] Pending selections={_pendingLevelUpSelections}");
            ShowPendingLevelUpOptionsAsync().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid ShowPendingLevelUpOptionsAsync()
        {
            Log.Info($"[LevelUpFlow] ShowPendingLevelUpOptionsAsync enter active={_isOptionSelectionActive} pending={_pendingLevelUpSelections} isGameOver={_isGameOver}");
            if (_isOptionSelectionActive || _pendingLevelUpSelections <= 0 || _isGameOver)
            {
                return;
            }

            _isOptionSelectionActive = true;
            while (_pendingLevelUpSelections > 0 && !_isGameOver)
            {
                var options = _levelUpOptionService.RollOptions(3);
                Log.Info($"[LevelUpFlow] Rolled option count={options.Count} ids={string.Join(",", options.Select(x => x?.OptionId ?? 0))}");
                if (options.Count == 0)
                {
                    Log.Warning("level up option pool is empty.");
                    _pendingLevelUpSelections--;
                    continue;
                }

                _levelUpOptionPopup ??= new BattleLevelUpOptionPopup();
                Log.Info("[LevelUpFlow] Await popup selection.");
                var selectedOption = await _levelUpOptionPopup.ShowAsync(options);
                if (selectedOption == null)
                {
                    Log.Warning("[LevelUpFlow] Popup returned null selection.");
                    break;
                }

                _levelUpOptionService.RecordSelection(selectedOption);
                ApplySelectedOption(selectedOption);
                _pendingLevelUpSelections--;
                Log.Info($"[LevelUpFlow] Selection applied optionId={selectedOption.OptionId} pending={_pendingLevelUpSelections}");
            }

            _isOptionSelectionActive = false;
            Log.Info("[LevelUpFlow] ShowPendingLevelUpOptionsAsync exit.");
        }

        private void ApplySelectedOption(OptionConfig option)
        {
            if (option == null)
            {
                return;
            }

            var value = option.Param / 100f;
            switch (option.OptionGroupId)
            {
                case 1:
                    _weaponDamageBonusRate += value;
                    break;
                case 4:
                {
                    _maxHealth *= 1f + value;
                    _health = _maxHealth;
                    if (_playerEntity != null)
                    {
                        _playerEntity.InitializeStats(_maxHealth, _playerEntity.MoveSpeed > 0f ? _playerEntity.MoveSpeed : PlayerMoveSpeed);
                    }

                    break;
                }
                case 5:
                    _weaponCooldownReduceRate = Mathf.Clamp(_weaponCooldownReduceRate + value, 0f, 0.85f);
                    break;
                case 6:
                    _aoeDamageBonusRate += value;
                    break;
                case 7:
                case 20:
                    _pickupDropBonusRate = Mathf.Clamp01(_pickupDropBonusRate + value);
                    break;
                case 8:
                    _moveSpeedBonusRate += value;
                    break;
                case 17:
                    _lifeStealRate = Mathf.Clamp01(_lifeStealRate + value);
                    break;
                case 18:
                    _killExplosionChanceRate = Mathf.Clamp01(_killExplosionChanceRate + value);
                    break;
                case 19:
                    _shotgunSpreadReduceRate = Mathf.Clamp(_shotgunSpreadReduceRate + value, 0f, 0.95f);
                    break;
                default:
                    Log.Warning($"option group not supported locally yet. optionId={option.OptionId} groupId={option.OptionGroupId}");
                    break;
            }

            Log.Info($"[LevelUpOption] selected optionId={option.OptionId} groupId={option.OptionGroupId} name={option.OptionName} param={option.Param}");
        }

        private void TryTriggerKillExplosion(Vector3 center)
        {
            if (_killExplosionChanceRate <= 0f || Random.value > _killExplosionChanceRate)
            {
                return;
            }

            var radius = 3.5f + _aoeDamageBonusRate * 4f;
            var damage = 30f * (1f + _aoeDamageBonusRate);
            var targets = _enemies.ToList();
            foreach (var target in targets)
            {
                if (target == null || target.IsDead || target.Root == null)
                {
                    continue;
                }

                if ((target.Root.transform.position - center).sqrMagnitude > radius * radius)
                {
                    continue;
                }

                DamageEnemy(target, damage);
            }
        }

        private List<WeaponConfig> GetAllWeapons()
        {
            var table = SurvivalWeaponTable;
            if (table != null && table.Count > 0)
            {
                var weapons = table.ToList().Select(ConvertWeapon).ToList();
                Log.Info($"[WeaponConfig] use survival luban table count={weapons.Count} ids={string.Join(",", weapons.Select(x => $"{x.Id}:{x.BulletVisualId}"))}");
                return weapons;
            }

            Log.Warning("[WeaponConfig] survival weapon table not ready.");
            return new List<WeaponConfig>();
        }

        private MonsterConfig GetMonster(int monsterId)
        {
            var table = MonsterTable;
            if (table != null && table.Count > 0)
            {
                var monster = table.Get(monsterId);
                if (monster != null)
                {
                    return ConvertMonster(monster);
                }
            }

            return null;
        }

        private MonsterSkillConfig GetMonsterSkill(int skillConfigId)
        {
            var table = MonsterSkillTable;
            if (table != null && table.Count > 0)
            {
                var skill = table.Get(skillConfigId);
                if (skill != null)
                {
                    return ConvertMonsterSkill(skill);
                }
            }

            return null;
        }

        private WaveConfig GetWaveConfig(int waveIndex)
        {
            var table = MonsterSpawnWaveTable;
            if (table != null && table.Count > 0)
            {
                if (_cachedWaveConfigs == null || _cachedWaveTableCount != table.Count)
                {
                    _cachedWaveConfigs = table.ToList()
                        .GroupBy(row => row.WaveIndex)
                        .ToDictionary(
                            group => group.Key,
                            group => new WaveConfig
                            {
                                WaveIndex = group.Key,
                                WaveName = group.First().WaveName,
                                PrepareTime = group.First().PrepareTime,
                                Description = group.First().Description,
                                SpawnEntries = group
                                    .OrderBy(row => row.StartTime)
                                    .ThenBy(row => row.Id)
                                    .Select(ConvertWaveSpawnEntry)
                                    .ToList()
                            });
                    _cachedWaveTableCount = table.Count;
                }

                return _cachedWaveConfigs.TryGetValue(waveIndex, out var wave) ? wave : null;
            }

            return null;
        }

        private WeaponConfig GetWeapon(int weaponId)
        {
            var table = SurvivalWeaponTable;
            if (table != null && table.Count > 0)
            {
                var weapon = table.Get(weaponId);
                return weapon == null ? null : ConvertWeapon(weapon);
            }

            Log.Warning($"[WeaponConfig] resolve weapon failed, survival weapon table not ready. id={weaponId}");
            return null;
        }

        private Hotfix.Config.Tables.survival.WeaponDropModelConfig GetWeaponDropModel(int weaponId)
        {
            var table = WeaponDropModelTable;
            if (table == null || table.Count <= 0 || weaponId <= 0)
            {
                return null;
            }

            return table.Get(weaponId);
        }

        private Hotfix.Config.Tables.survival.SoundEventConfig GetSoundEvent(string category, string eventKey, string variantKey)
        {
            var table = SoundEventTable;
            if (table == null || table.Count <= 0)
            {
                Log.Warning($"[SoundEvent] table not ready category={category} eventKey={eventKey} variant={variantKey}");
                return null;
            }

            var match = table.Find(config =>
                string.Equals(config.Category, category, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.EventKey, eventKey, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.VariantKey, variantKey, System.StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }

            var fallback = table.Find(config =>
                string.Equals(config.Category, category, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.EventKey, eventKey, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.VariantKey, "common", System.StringComparison.OrdinalIgnoreCase));
            if (fallback == null)
            {
                Log.Warning($"[SoundEvent] config missing category={category} eventKey={eventKey} variant={variantKey} tableCount={table.Count}");
            }

            return fallback;
        }

        private Hotfix.Config.Tables.survival.BulletVisualConfig GetBulletVisual(int bulletVisualId)
        {
            var table = BulletVisualTable;
            if (table == null || table.Count <= 0 || bulletVisualId <= 0)
            {
                return null;
            }

            return table.Get(bulletVisualId);
        }

        private GameObject CreatePickupObject(WeaponConfig weapon, Vector3 position)
        {
            var dropModel = GetWeaponDropModel(weapon.Id);
            if (dropModel == null)
            {
                Log.Warning($"[Pickup] drop model config missing weapon={weapon.Id} name={weapon.Name}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(dropModel.PrefabPath))
            {
                Log.Warning($"[Pickup] drop model prefab path empty weapon={weapon.Id} name={weapon.Name}");
                return null;
            }

            var prefab = LoadPrefabAtPath(dropModel.PrefabPath);
            if (prefab == null)
            {
                Log.Warning($"[Pickup] drop model prefab missing weapon={weapon.Id} path={dropModel.PrefabPath}");
                return null;
            }

            var pickupObject = Instantiate(prefab);
            pickupObject.name = $"Pickup_{weapon.Name}";
            pickupObject.transform.SetParent(_runtimeRoot.transform, false);

            var groundedPosition = ShouldSkipGeneratedWorld()
                ? ProjectPointToGround(position, dropModel.HeightOffset)
                : new Vector3(position.x, dropModel.HeightOffset + 0.5f, position.z);
            pickupObject.transform.position = groundedPosition;
            pickupObject.transform.rotation = Quaternion.Euler(0f, dropModel.RotationY, 0f);
            pickupObject.transform.localScale *= Mathf.Max(0.01f, dropModel.DisplayScale);

            return pickupObject;
        }

        private void PlayWeaponFireSound(WeaponConfig weapon, AudioSource audioSource)
        {
            if (weapon == null || audioSource == null)
            {
                Log.Warning($"[SoundEvent] weapon fire skipped weaponNull={weapon == null} audioNull={audioSource == null}");
                return;
            }

            var soundEvent = GetSoundEvent("WeaponFire", "weapon_fire", weapon.Id.ToString());
            if (soundEvent != null)
            {
                Log.Info($"[SoundEvent] weapon fire resolved weaponId={weapon.Id} soundEventId={soundEvent.SoundEventId} clipPath={soundEvent.ClipPath}");
            }
            PlaySoundEvent(audioSource, soundEvent, 0f);
        }

        private void PlaySoundEvent(AudioSource audioSource, Hotfix.Config.Tables.survival.SoundEventConfig soundEvent, float? spatialBlendOverride = null)
        {
            if (audioSource == null || soundEvent == null || string.IsNullOrWhiteSpace(soundEvent.ClipPath))
            {
                Log.Warning($"[SoundEvent] play skipped audioNull={audioSource == null} soundNull={soundEvent == null} clipEmpty={string.IsNullOrWhiteSpace(soundEvent?.ClipPath)}");
                return;
            }

            if (_soundCooldowns.TryGetValue(soundEvent.SoundEventId, out var nextPlayTime) && Time.time < nextPlayTime)
            {
                return;
            }

            var clip = LoadAudioClipAtPath(soundEvent.ClipPath);
            if (clip == null)
            {
                Log.Warning($"[SoundEvent] clip missing id={soundEvent.SoundEventId} path={soundEvent.ClipPath}");
                return;
            }

            audioSource.pitch = Mathf.Max(0.01f, soundEvent.Pitch);
            audioSource.spatialBlend = spatialBlendOverride.HasValue
                ? Mathf.Clamp01(spatialBlendOverride.Value)
                : Mathf.Clamp01(soundEvent.SpatialBlend);
            audioSource.maxDistance = Mathf.Max(0.1f, soundEvent.MaxDistance);
            audioSource.minDistance = Mathf.Min(2f, audioSource.maxDistance);
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.PlayOneShot(clip, Mathf.Clamp01(soundEvent.Volume));
            Log.Info($"[SoundEvent] play success id={soundEvent.SoundEventId} clip={clip.name} target={audioSource.gameObject.name}");

            _soundCooldowns[soundEvent.SoundEventId] = Time.time + Mathf.Max(0f, soundEvent.MinInterval);
        }

        private AudioClip LoadAudioClipAtPath(string clipPath)
        {
            if (string.IsNullOrWhiteSpace(clipPath))
            {
                return null;
            }

            if (_audioClipCache.TryGetValue(clipPath, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            var handle = GameApp.Asset.LoadAssetSync<AudioClip>(clipPath);
            var clip = handle?.GetAssetObject<AudioClip>();
#if UNITY_EDITOR
            if (clip == null)
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            }
#endif
            if (clip != null)
            {
                _audioClipCache[clipPath] = clip;
            }

            return clip;
        }

        private static AudioSource EnsureProjectileAudioSource(GameObject bulletObject)
        {
            if (bulletObject == null)
            {
                return null;
            }

            var audioSource = bulletObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = bulletObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 18f;
            audioSource.hideFlags = HideFlags.None;
            return audioSource;
        }

        private static CharacterController EnsurePlayerController(GameObject playerObject)
        {
            var controller = playerObject.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = playerObject.AddComponent<CharacterController>();
            }

            var renderers = playerObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                controller.height = 2f;
                controller.radius = 0.45f;
                controller.center = new Vector3(0f, 1f, 0f);
                controller.stepOffset = 0.3f;
                controller.skinWidth = 0.04f;
                controller.minMoveDistance = 0f;
                controller.slopeLimit = 50f;
                return controller;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var rootPosition = playerObject.transform.position;
            var height = Mathf.Clamp(bounds.size.y * 1.05f, 1.6f, 2.4f);
            var radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.6f, 0.28f, 0.5f);
            var centerY = Mathf.Clamp(bounds.center.y - rootPosition.y, height * 0.5f, height * 0.65f);

            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, centerY, 0f);
            controller.stepOffset = Mathf.Min(0.35f, height * 0.18f);
            controller.skinWidth = Mathf.Max(0.02f, radius * 0.1f);
            controller.minMoveDistance = 0f;
            controller.slopeLimit = 50f;
            return controller;
        }

        private Vector3 ResolveFireOrigin(Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual)
        {
            if (_player == null)
            {
                return Vector3.zero;
            }

            if (_weaponPivot != null && bulletVisual != null && !string.IsNullOrWhiteSpace(bulletVisual.MuzzleSocketName))
            {
                var socket = FindChildRecursive(_weaponPivot, bulletVisual.MuzzleSocketName);
                if (socket != null)
                {
                    return socket.position;
                }
            }

            return _player.transform.position + Vector3.up * 1.15f;
        }

        private static MonsterConfig ConvertMonster(Hotfix.Config.Tables.MonsterConfig monster)
        {
            return new MonsterConfig
            {
                Id = monster.Id,
                Name = monster.Name,
                SkillConfigId = monster.SkillConfigId,
                MaxHp = monster.MaxHp,
                MoveSpeed = monster.MoveSpeed,
                AttackRange = monster.AttackRange,
                AttackDamage = monster.AttackDamage,
                AttackInterval = monster.AttackInterval,
                ExpAward = monster.ExpAward,
                AiType = monster.AiType,
                PrefabName = monster.PrefabName,
                PrefabPath = monster.PrefabPath,
                BodyScale = monster.BodyScale,
                ColliderRadius = monster.ColliderRadius,
                ColliderHeight = monster.ColliderHeight,
                Notes = monster.Notes
            };
        }

        private static MonsterSkillConfig ConvertMonsterSkill(Hotfix.Config.Tables.MonsterSkillConfig skill)
        {
            return new MonsterSkillConfig
            {
                Id = skill.Id,
                Name = skill.Name,
                AiType = skill.AiType,
                PreferredRange = skill.PreferredRange,
                PoisonDps = skill.PoisonDps,
                PoisonDuration = skill.PoisonDuration,
                DashTriggerRange = skill.DashTriggerRange,
                DashCooldown = skill.DashCooldown,
                DashDuration = skill.DashDuration,
                DashMultiplier = skill.DashMultiplier,
                KnockbackForce = skill.KnockbackForce,
                Notes = skill.Notes
            };
        }

        private static WaveSpawnEntryConfig ConvertWaveSpawnEntry(Hotfix.Config.Tables.MonsterSpawnWaveConfig row)
        {
            return new WaveSpawnEntryConfig
            {
                Id = row.Id,
                MonsterId = row.MonsterId,
                SpawnCount = row.SpawnCount,
                StartTime = row.StartTime,
                SpawnInterval = row.SpawnInterval,
                SpawnWeight = row.SpawnWeight
            };
        }

        private static WeaponConfig ConvertWeapon(Weapon weapon)
        {
            var bulletCount = 1;
            var lifeTime = 2f;
            var destroyOnHit = true;
            var explosionRadius = 0f;

            switch (weapon.Type)
            {
                case EWeaponType.Shotgun:
                    bulletCount = 8;
                    lifeTime = 0.8f;
                    break;
                case EWeaponType.Laser:
                    lifeTime = 0.12f;
                    destroyOnHit = false;
                    break;
                case EWeaponType.Grenade:
                    lifeTime = 5f;
                    explosionRadius = weapon.IsAoe ? 3f : 0f;
                    break;
                default:
                    lifeTime = Mathf.Clamp(36f / Mathf.Max(weapon.BulletSpeed, 0.01f), 0.6f, 4f);
                    break;
            }

            return new WeaponConfig
            {
                Id = weapon.Id,
                Name = weapon.Name,
                Type = weapon.Type,
                Damage = weapon.Damage,
                BulletSpeed = weapon.BulletSpeed,
                FireInterval = weapon.FireInterval,
                FireRate = weapon.FireInterval > 0f ? 1f / weapon.FireInterval : 0f,
                LifeTime = lifeTime,
                DestroyOnHit = destroyOnHit,
                BulletCount = bulletCount,
                ExplosionRadius = explosionRadius,
                BulletVisualId = weapon.BulletVisualId,
                Description = weapon.Type.ToString()
            };
        }

        private static WeaponConfig ConvertWeapon(Hotfix.Config.Tables.survival.weapon weapon)
        {
            var bulletCount = 1;
            var lifeTime = 2f;
            var destroyOnHit = true;
            var explosionRadius = 0f;

            switch (weapon.Type)
            {
                case EWeaponType.Shotgun:
                    bulletCount = 8;
                    lifeTime = 0.8f;
                    break;
                case EWeaponType.Laser:
                    lifeTime = 0.12f;
                    destroyOnHit = false;
                    break;
                case EWeaponType.Grenade:
                    lifeTime = 5f;
                    explosionRadius = weapon.IsAoe ? 3f : 0f;
                    break;
                default:
                    lifeTime = Mathf.Clamp(36f / Mathf.Max(weapon.BulletSpeed, 0.01f), 0.6f, 4f);
                    break;
            }

            return new WeaponConfig
            {
                Id = weapon.Id,
                Name = weapon.Name,
                Type = weapon.Type,
                Damage = weapon.Damage,
                BulletSpeed = weapon.BulletSpeed,
                FireInterval = weapon.FireInterval,
                FireRate = weapon.FireInterval > 0f ? 1f / weapon.FireInterval : 0f,
                LifeTime = lifeTime,
                DestroyOnHit = destroyOnHit,
                BulletCount = bulletCount,
                ExplosionRadius = explosionRadius,
                BulletVisualId = weapon.BulletVisualId,
                Description = weapon.Type.ToString()
            };
        }

        private static string GetMuzzleFxPath(Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual)
        {
            if (bulletVisual == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(bulletVisual.MuzzleFxPath) ? bulletVisual.MuzzleFxPath : bulletVisual.ImpactFxPath;
        }

        private static string GetImpactEnemyFxPath(Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual)
        {
            if (bulletVisual == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(bulletVisual.ImpactEnemyFxPath))
            {
                return bulletVisual.ImpactEnemyFxPath;
            }

            return bulletVisual.ImpactFxPath;
        }

        private static string GetImpactGroundFxPath(Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual)
        {
            if (bulletVisual == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(bulletVisual.ImpactGroundFxPath))
            {
                return bulletVisual.ImpactGroundFxPath;
            }

            return bulletVisual.ImpactFxPath;
        }

        private void SpawnFxAtPath(string prefabPath, Vector3 position, Quaternion rotation, float lifeTime, float scale)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return;
            }

            var prefab = LoadPrefabAtPath(prefabPath);
            if (prefab == null)
            {
                Log.Warning($"[BulletVisual] fx prefab missing path={prefabPath}");
                return;
            }

            var fx = Instantiate(prefab);
            fx.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : null, false);
            fx.transform.position = position;
            fx.transform.rotation = rotation;
            fx.transform.localScale *= Mathf.Max(0.01f, scale);
            Destroy(fx, Mathf.Max(0.1f, lifeTime));
        }

        private GameObject CreateBulletObject(WeaponConfig weapon, Hotfix.Config.Tables.survival.BulletVisualConfig bulletVisual, Vector3 direction)
        {
            if (bulletVisual != null)
            {
                if (string.IsNullOrWhiteSpace(bulletVisual.TracerFxPath))
                {
                    Log.Error($"[BulletVisual] tracer path empty weapon={weapon?.Id} visualId={bulletVisual.BulletVisualId}");
                    return null;
                }

                var prefab = LoadPrefabAtPath(bulletVisual.TracerFxPath);
                if (prefab != null)
                {
                    var projectile = Instantiate(prefab);
                    projectile.name = $"{bulletVisual.VisualName}_Projectile";
                    projectile.transform.localScale *= Mathf.Max(0.01f, bulletVisual.FxScale);
                    Log.Info($"[BulletVisual] projectile prefab applied weapon={weapon?.Id} visualId={bulletVisual.BulletVisualId} path={bulletVisual.TracerFxPath}");
                    return projectile;
                }

                Log.Error($"[BulletVisual] projectile prefab missing weapon={weapon?.Id} visualId={bulletVisual.BulletVisualId} path={bulletVisual.TracerFxPath}");
                return null;
            }

            var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.transform.localScale = weapon.ExplosionRadius > 0f
                ? new Vector3(0.45f, 0.45f, 0.45f)
                : new Vector3(0.22f, 0.22f, 0.22f);
            return fallback;
        }

        private GameObject LoadPrefabAtPath(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return null;
            }

            if (_prefabCache.TryGetValue(prefabPath, out var cachedPrefab) && cachedPrefab != null)
            {
                return cachedPrefab;
            }

            var handle = GameApp.Asset.LoadAssetSync<GameObject>(prefabPath);
            var prefab = handle?.GetAssetObject<GameObject>();
            if (prefab == null)
            {
                Log.Warning($"[BulletVisual] load prefab sync failed path={prefabPath}");
            }
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
#endif
            if (prefab != null)
            {
                _prefabCache[prefabPath] = prefab;
            }

            return prefab;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                var nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Color GetEnemyBaseColor(string aiType)
        {
            switch (aiType)
            {
                case "Tank": return new Color32(108, 82, 68, 255);
                case "Ranged": return new Color32(115, 180, 96, 255);
                case "Runner": return new Color32(224, 141, 84, 255);
                default: return new Color32(184, 66, 66, 255);
            }
        }

        private static void SetEnemyTint(EnemyData enemy, Color color)
        {
            if (enemy.Renderers == null)
            {
                return;
            }

            foreach (var renderer in enemy.Renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    if (material != null && material.HasProperty("_Color"))
                    {
                        material.color = color;
                    }
                }
            }
        }

        private static void RestoreEnemyTint(EnemyData enemy, float deltaTime)
        {
            if (enemy.Renderers == null)
            {
                return;
            }

            foreach (var renderer in enemy.Renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    if (material != null && material.HasProperty("_Color"))
                    {
                        material.color = Color.Lerp(material.color, enemy.BaseColor, 10f * deltaTime);
                    }
                }
            }
        }

        private static void EnsureEnemyCollider(GameObject enemyObject, MonsterConfig config)
        {
            var existingColliders = enemyObject.GetComponentsInChildren<Collider>(true);
            if (existingColliders != null && existingColliders.Length > 0)
            {
                if (EnableEnemyHitDebugLog)
                {
                    Debug.Log($"[EnemyCollider] existing colliders kept on {enemyObject.name}: {existingColliders.Length}");
                }
                return;
            }

            var renderers = enemyObject.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                var fallbackCollider = enemyObject.AddComponent<CapsuleCollider>();
                fallbackCollider.radius = Mathf.Max(0.2f, config.ColliderRadius);
                fallbackCollider.height = Mathf.Max(fallbackCollider.radius * 2f, config.ColliderHeight);
                fallbackCollider.center = new Vector3(0f, fallbackCollider.height * 0.5f, 0f);
                if (EnableEnemyHitDebugLog)
                {
                    Debug.Log($"[EnemyCollider] fallback collider added to {enemyObject.name} center={fallbackCollider.center} radius={fallbackCollider.radius:0.###} height={fallbackCollider.height:0.###}");
                }
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var localCenter = enemyObject.transform.InverseTransformPoint(bounds.center);
            var localTop = enemyObject.transform.InverseTransformPoint(bounds.center + Vector3.up * bounds.extents.y);
            var localBottom = enemyObject.transform.InverseTransformPoint(bounds.center - Vector3.up * bounds.extents.y);
            var localHeight = Mathf.Abs(localTop.y - localBottom.y);
            var localRadius = Mathf.Max(bounds.extents.x, bounds.extents.z) / Mathf.Max(0.0001f, enemyObject.transform.lossyScale.x);

            var collider = enemyObject.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.center = localCenter;
            collider.radius = Mathf.Max(0.2f, Mathf.Max(config.ColliderRadius, localRadius * 0.45f));
            collider.height = Mathf.Max(collider.radius * 2f, Mathf.Max(config.ColliderHeight, localHeight * 0.92f));
            if (EnableEnemyHitDebugLog)
            {
                Debug.Log($"[EnemyCollider] generated collider for {enemyObject.name} center={collider.center} radius={collider.radius:0.###} height={collider.height:0.###}");
            }
        }

        private static CharacterController EnsureEnemyController(GameObject enemyObject, MonsterConfig config)
        {
            var controller = enemyObject.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = enemyObject.AddComponent<CharacterController>();
            }

            var radius = Mathf.Max(0.2f, config.ColliderRadius);
            var height = Mathf.Max(radius * 2f, config.ColliderHeight);
            controller.radius = radius;
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            controller.stepOffset = Mathf.Min(0.4f, height * 0.3f);
            controller.skinWidth = Mathf.Max(0.02f, radius * 0.1f);
            controller.minMoveDistance = 0f;
            controller.slopeLimit = 50f;
            return controller;
        }

        private static GameObject LoadEnemyPrefab(string prefabPath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#else
            return null;
#endif
        }

        private static void BindEnemyAnimatorController(GameObject enemyObject, MonsterConfig config)
        {
            var animator = enemyObject.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController != null)
            {
                return;
            }

#if UNITY_EDITOR
            var controllerPath = config.PrefabPath != null && config.PrefabPath.Contains("Zombie Girl")
                ? "Assets/Zombie Slaughter/Zombie Girl - Animation Demo.controller"
                : "Assets/Zombie Slaughter/Zombie Guy - Animation Demo.controller";
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
            }
#endif
        }

        private static Color GetWeaponColor(int weaponId)
        {
            switch (weaponId)
            {
                case 101: return new Color32(243, 214, 104, 255);
                case 102: return new Color32(236, 137, 86, 255);
                case 103: return new Color32(255, 125, 69, 255);
                case 104: return new Color32(104, 210, 255, 255);
                case 105: return new Color32(167, 255, 122, 255);
                case 106: return new Color32(255, 88, 88, 255);
                default: return new Color32(210, 210, 210, 255);
            }
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color32(246, 244, 237, 255);
            return text;
        }

        private static void SetColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.material.color = color;
        }

        private void SetPlayerTint(Color color)
        {
            foreach (var renderer in _playerRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.material.color = color;
            }
        }

        private void RestorePlayerTint(float deltaTime)
        {
            foreach (var renderer in _playerRenderers)
            {
                if (renderer == null || !_playerBaseColors.TryGetValue(renderer, out var baseColor))
                {
                    continue;
                }

                renderer.material.color = Color.Lerp(renderer.material.color, baseColor, 12f * deltaTime);
            }
        }

        private void OnDestroy()
        {
            _levelUpOptionPopup?.Dispose();
            _levelUpOptionPopup = null;
        }

        private sealed class EnemyData
        {
            public GameObject Root;
            public CharacterController Controller;
            public EnemyController Gameplay;
            public Renderer[] Renderers;
            public bool IsDead;
            public int ConfigId;
            public string Name;
            public Color BaseColor;
            public float MaxHealth;
            public float Health;
            public float HitFlashTimer;
        }

        private sealed class WaveSpawnRuntimeState
        {
            public int EntryId;
            public int MonsterId;
            public int RemainingCount;
            public float StartTime;
            public float SpawnInterval;
            public float NextSpawnTime;
            public int SpawnWeight;
        }

        private sealed class PickupData
        {
            public GameObject Root;
            public int WeaponId;
            public float LifeTime;
            public Vector3 BasePosition;
            public float BobPhase;
        }

        private sealed class EnemyMarker : MonoBehaviour
        {
            public EnemyData Owner;
        }
    }
}
