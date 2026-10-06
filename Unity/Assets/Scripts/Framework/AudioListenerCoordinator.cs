using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Startup.Framework
{
    [DefaultExecutionOrder(-10000)]
    public sealed class AudioListenerCoordinator : MonoBehaviour
    {
        private static AudioListenerCoordinator _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                _instance.NormalizeListeners();
                return;
            }

            var root = new GameObject(nameof(AudioListenerCoordinator));
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<AudioListenerCoordinator>();
            _instance.NormalizeListeners();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void LateUpdate()
        {
            NormalizeListeners();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            NormalizeListeners();
        }

        private void NormalizeListeners()
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AudioListener selected = null;
            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null || !listener.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (selected == null && listener.enabled)
                {
                    selected = listener;
                }
            }

            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.gameObject.activeInHierarchy)
            {
                var mainListener = mainCamera.GetComponent<AudioListener>();
                if (mainListener != null)
                {
                    selected = mainListener;
                }
            }

            if (selected == null)
            {
                for (var i = 0; i < listeners.Length; i++)
                {
                    var listener = listeners[i];
                    if (listener != null && listener.gameObject.activeInHierarchy)
                    {
                        selected = listener;
                        break;
                    }
                }
            }

            if (selected == null)
            {
                var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (var i = 0; i < cameras.Length; i++)
                {
                    var camera = cameras[i];
                    if (camera == null || !camera.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    selected = camera.GetComponent<AudioListener>();
                    if (selected == null)
                    {
                        selected = camera.gameObject.AddComponent<AudioListener>();
                    }

                    break;
                }
            }

            if (selected == null)
            {
                return;
            }

            if (!selected.enabled)
            {
                selected.enabled = true;
            }

            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null || listener == selected)
                {
                    continue;
                }

                if (listener.enabled)
                {
                    listener.enabled = false;
                }
            }
        }
    }
}
