using System.Collections.Generic;
using GameFrameX.Runtime;
using Hotfix.Config;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hotfix.Gameplay.Enemy
{
    public sealed class EnemyAnimationDriver
    {
        private static readonly Dictionary<string, AnimationClip> ClipCache = new Dictionary<string, AnimationClip>();

        private readonly Animator _animator;
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _mixer;

        private AnimationClipPlayable _basePlayable;
        private AnimationClipPlayable _actionPlayable;
        private string _variantPrefix;
        private string _currentBaseState;
        private string _currentActionState;
        private bool _actionActive;
        private bool _lockedToDeath;

        public EnemyAnimationDriver(Animator animator)
        {
            _animator = animator;
            if (_animator == null)
            {
                return;
            }

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _graph = PlayableGraph.Create($"EnemyAnimationDriver_{animator.gameObject.name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2, true);

            var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            output.SetSourcePlayable(_mixer);
            _mixer.SetInputWeight(0, 1f);
            _mixer.SetInputWeight(1, 0f);
            _graph.Play();
        }

        public void ConfigureVariant(string variantPrefix)
        {
            _variantPrefix = string.IsNullOrWhiteSpace(variantPrefix) ? "Zombie Guy" : variantPrefix;
            _currentBaseState = string.Empty;
            _currentActionState = string.Empty;
            _lockedToDeath = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_actionActive || !_actionPlayable.IsValid())
            {
                return;
            }

            var clip = _actionPlayable.GetAnimationClip();
            if (clip == null)
            {
                StopAction();
                return;
            }

            if (_actionPlayable.GetTime() + deltaTime < clip.length)
            {
                return;
            }

            if (_lockedToDeath)
            {
                _actionPlayable.SetTime(clip.length);
                _actionPlayable.SetSpeed(0d);
                return;
            }

            StopAction();
        }

        public bool PlayState(string stateName)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            if (IsLocomotionState(stateName))
            {
                if (_lockedToDeath)
                {
                    return true;
                }

                if (_actionActive)
                {
                    StopAction();
                }

                return PlayBase(stateName);
            }

            if (stateName == "Death")
            {
                _lockedToDeath = true;
            }

            return PlayAction(stateName);
        }

        public void Dispose()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private bool PlayBase(string stateName)
        {
            if (_currentBaseState == stateName)
            {
                return true;
            }

            var clip = LoadClip(stateName);
            if (clip == null)
            {
                return false;
            }

            if (_basePlayable.IsValid())
            {
                _graph.Disconnect(_mixer, 0);
                _basePlayable.Destroy();
            }

            _basePlayable = AnimationClipPlayable.Create(_graph, clip);
            _basePlayable.SetApplyFootIK(false);
            _basePlayable.SetDuration(double.MaxValue);
            _basePlayable.SetTime(0d);
            _graph.Connect(_basePlayable, 0, _mixer, 0);
            _mixer.SetInputWeight(0, 1f);
            _currentBaseState = stateName;
            return true;
        }

        private bool PlayAction(string stateName)
        {
            if (_currentActionState == stateName && _actionActive)
            {
                return true;
            }

            var clip = LoadClip(stateName);
            if (clip == null)
            {
                return false;
            }

            StopAction();

            _actionPlayable = AnimationClipPlayable.Create(_graph, clip);
            _actionPlayable.SetApplyFootIK(false);
            _actionPlayable.SetTime(0d);
            _actionPlayable.SetSpeed(1d);
            _graph.Connect(_actionPlayable, 0, _mixer, 1);
            _mixer.SetInputWeight(0, 0f);
            _mixer.SetInputWeight(1, 1f);
            _actionActive = true;
            _currentActionState = stateName;
            return true;
        }

        private void StopAction()
        {
            if (_actionPlayable.IsValid())
            {
                _graph.Disconnect(_mixer, 1);
                _actionPlayable.Destroy();
            }

            _mixer.SetInputWeight(1, 0f);
            _mixer.SetInputWeight(0, 1f);
            _actionActive = false;
            _currentActionState = string.Empty;
        }

        private AnimationClip LoadClip(string stateName)
        {
            var clipPath = ResolveClipPath(stateName);
            if (string.IsNullOrWhiteSpace(clipPath))
            {
                return null;
            }

            if (ClipCache.TryGetValue(clipPath, out var cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            var handle = GameApp.Asset.LoadAssetSync<AnimationClip>(clipPath);
            var clip = handle?.GetAssetObject<AnimationClip>();
#if UNITY_EDITOR
            if (clip == null)
            {
                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            }
#endif
            if (clip == null)
            {
                Log.Warning($"[EnemyAnimation] clip missing state={stateName} path={clipPath}");
                return null;
            }

            ClipCache[clipPath] = clip;
            return clip;
        }

        private string ResolveClipPath(string stateName)
        {
            if (string.IsNullOrWhiteSpace(_variantPrefix))
            {
                _variantPrefix = "Zombie Guy";
            }

            return $"Assets/Zombie Slaughter/Animation/{_variantPrefix}@{stateName}.FBX";
        }

        private static bool IsLocomotionState(string stateName)
        {
            return stateName == "Idle" ||
                   stateName == "Walk" ||
                   stateName == "Walk Aggro" ||
                   stateName == "Walk Back" ||
                   stateName == "Run";
        }
    }
}
