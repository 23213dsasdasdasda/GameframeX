using System.Collections.Generic;
using GameFrameX.Runtime;
using Hotfix.Config;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hotfix.Gameplay.Player
{
    public sealed class PlayerAnimationDriver
    {
        private struct AnimationSet
        {
            public string IdlePath;
            public string RunPath;
            public string AttackPath;
            public string HitPath;
            public string DeathPath;
        }

        private static readonly Dictionary<string, AnimationClip> ClipCache = new Dictionary<string, AnimationClip>();

        private readonly Animator _animator;
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _mixer;

        private AnimationClipPlayable _basePlayable;
        private AnimationClipPlayable _actionPlayable;
        private AnimationSet _animationSet;
        private string _currentBasePath;
        private bool _actionActive;
        private bool _lockedToDeath;

        public PlayerAnimationDriver(Animator animator)
        {
            _animator = animator;
            if (_animator == null)
            {
                return;
            }

            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _graph = PlayableGraph.Create($"PlayerAnimationDriver_{animator.gameObject.name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2, true);

            var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            output.SetSourcePlayable(_mixer);
            _mixer.SetInputWeight(0, 1f);
            _mixer.SetInputWeight(1, 0f);
            _graph.Play();
        }

        public void Dispose()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        public void SetWeaponType(EWeaponType weaponType)
        {
            _animationSet = ResolveAnimationSet(weaponType);
            _currentBasePath = string.Empty;
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

            if (_actionPlayable.GetTime() + deltaTime >= clip.length)
            {
                StopAction();
            }
        }

        public void PlayIdle()
        {
            if (_lockedToDeath)
            {
                return;
            }

            if (_actionActive)
            {
                StopAction();
            }

            PlayBase(_animationSet.IdlePath);
        }

        public void PlayMove()
        {
            if (_lockedToDeath)
            {
                return;
            }

            if (_actionActive)
            {
                StopAction();
            }

            PlayBase(_animationSet.RunPath);
        }

        public void PlayAttack()
        {
            if (_lockedToDeath)
            {
                return;
            }

            PlayAction(_animationSet.AttackPath);
        }

        public void PlayHit()
        {
            if (_lockedToDeath)
            {
                return;
            }

            PlayAction(_animationSet.HitPath);
        }

        public void PlayDeath()
        {
            _lockedToDeath = true;
            StopAction();
            PlayBase(_animationSet.DeathPath, true);
        }

        private void PlayBase(string clipPath, bool force = false)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(clipPath))
            {
                return;
            }

            if (!force && _currentBasePath == clipPath)
            {
                return;
            }

            var clip = LoadClip(clipPath);
            if (clip == null)
            {
                return;
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
            _currentBasePath = clipPath;
        }

        private void PlayAction(string clipPath)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(clipPath))
            {
                return;
            }

            var clip = LoadClip(clipPath);
            if (clip == null)
            {
                return;
            }

            StopAction();

            _actionPlayable = AnimationClipPlayable.Create(_graph, clip);
            _actionPlayable.SetApplyFootIK(false);
            _actionPlayable.SetTime(0d);
            _graph.Connect(_actionPlayable, 0, _mixer, 1);
            _mixer.SetInputWeight(0, 0f);
            _mixer.SetInputWeight(1, 1f);
            _actionActive = true;
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
        }

        private static AnimationClip LoadClip(string clipPath)
        {
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
                Log.Warning($"[PlayerAnimation] clip missing path={clipPath}");
                return null;
            }

            ClipCache[clipPath] = clip;
            return clip;
        }

        private static AnimationSet ResolveAnimationSet(EWeaponType weaponType)
        {
            switch (weaponType)
            {
                case EWeaponType.Laser:
                    return new AnimationSet
                    {
                        IdlePath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Machinegun/machinegun_combat_idle.FBX",
                        RunPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Machinegun/movement/machinegun_combat_walk.FBX",
                        AttackPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Machinegun/machinegun_combat_shoot_loop.FBX",
                        HitPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Machinegun/machinegun_take_damage.FBX",
                        DeathPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Machinegun/machinegun_death_A.FBX"
                    };
                case EWeaponType.Grenade:
                    return new AnimationSet
                    {
                        IdlePath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/RocketLauncher/rocketlauncher_combat_idle.FBX",
                        RunPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/RocketLauncher/movement/rocketlauncher_combat_walk.FBX",
                        AttackPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/RocketLauncher/rocketlauncher_combat_shoot.FBX",
                        HitPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/RocketLauncher/rocketlauncher_take_damage.FBX",
                        DeathPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/RocketLauncher/rocketlauncher_death_A.FBX"
                    };
                case EWeaponType.Shotgun:
                    return new AnimationSet
                    {
                        IdlePath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_combat_idle.FBX",
                        RunPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/movement/infantry_combat_walk.FBX",
                        AttackPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_combat_shoot_shotgun.FBX",
                        HitPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_take_damage.FBX",
                        DeathPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_death_A.FBX"
                    };
                default:
                    return new AnimationSet
                    {
                        IdlePath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_combat_idle.FBX",
                        RunPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/movement/infantry_combat_walk.FBX",
                        AttackPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_combat_shoot.FBX",
                        HitPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_take_damage.FBX",
                        DeathPath = "Assets/Toon_Soldiers/ToonSoldiers_militias/animation/Infantry/infantry_death_A.FBX"
                    };
            }
        }
    }
}
