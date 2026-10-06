#if ENABLE_UI_FAIRYGUI
using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using Hotfix.Config.Tables.survival;
using UnityEngine;

namespace Hotfix.UI
{
    public sealed partial class UI3Pick1
    {
        private const float HighlightPadding = 8f;
        private const int HighlightLineSize = 4;

        private sealed class SelectionRequest
        {
            public IReadOnlyList<OptionConfig> Options;
            public UniTaskCompletionSource<OptionConfig> CompletionSource;
        }

        private static SelectionRequest _pendingRequest;

        public static async UniTask<OptionConfig> ShowSelectionAsync(IReadOnlyList<OptionConfig> options)
        {
            if (options == null || options.Count == 0)
            {
                Log.Warning("[UI3Pick1] ShowSelectionAsync skipped because options are empty.");
                return null;
            }

            var tcs = new UniTaskCompletionSource<OptionConfig>();
            _pendingRequest = new SelectionRequest
            {
                Options = options,
                CompletionSource = tcs
            };

            Log.Info($"[UI3Pick1] Open request count={options.Count} ids={string.Join(",", GetOptionIds(options))}");
            await GameApp.UI.OpenAsync<UI3Pick1>(UIGroupConstants.Window);
            return await tcs.Task;
        }

        public override void OnAwake()
        {
            UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Window.Name);
            base.OnAwake();
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            BindStaticEvents();
            ApplyLayout();
            ApplyPendingRequest();
        }

        private void BindStaticEvents()
        {
            m_option_action_btn1?.onClick.Set(() => SelectAt(0));
            m_option_action_btn2?.onClick.Set(() => SelectAt(1));
            m_option_action_btn3?.onClick.Set(() => SelectAt(2));
        }

        private void ApplyPendingRequest()
        {
            var request = _pendingRequest;
            if (request == null)
            {
                Log.Warning("[UI3Pick1] ApplyPendingRequest skipped because request is null.");
                return;
            }

            var titles = new[] { m_option_title1, m_option_title2, m_option_title3 };
            var descriptions = new[] { m_option_desc1, m_option_desc2, m_option_desc3 };
            var buttons = new[] { m_option_action_btn1, m_option_action_btn2, m_option_action_btn3 };
            var icons = new[] { m_option_btn1, m_option_btn2, m_option_btn3 };
            var highlightBorders = new[]
            {
                GetOrCreateHighlightBorder("option_highlight_1"),
                GetOrCreateHighlightBorder("option_highlight_2"),
                GetOrCreateHighlightBorder("option_highlight_3")
            };

            for (var i = 0; i < 3; i++)
            {
                var option = i < request.Options.Count ? request.Options[i] : null;
                var hasOption = option != null;
                titles[i].visible = hasOption;
                descriptions[i].visible = hasOption;
                buttons[i].visible = hasOption;
                icons[i].visible = hasOption;
                highlightBorders[i].visible = hasOption;

                if (!hasOption)
                {
                    continue;
                }

                titles[i].text = option.OptionName;
                descriptions[i].text = string.IsNullOrWhiteSpace(option.Desc) ? option.Effect ?? string.Empty : option.Desc;
                ApplyIcon(icons[i], option.Icon);
                ApplyQualityHighlight(highlightBorders[i], buttons[i], option.QualityTier);
                Log.Info($"[UI3Pick1] Bind slot={i + 1} optionId={option.OptionId} groupId={option.OptionGroupId} name={option.OptionName} icon={option.Icon}");
            }
        }

        private void ApplyLayout()
        {
            if (self == null || GRoot.inst == null)
            {
                return;
            }

            var designWidth = self.width;
            var designHeight = self.height;
            var rootWidth = GRoot.inst.width;
            var rootHeight = GRoot.inst.height;
            var scale = Mathf.Min(rootWidth / designWidth, rootHeight / designHeight);
            if (scale <= 0f)
            {
                scale = 1f;
            }

            self.SetScale(scale, scale);
            self.SetXY((rootWidth - designWidth * scale) * 0.5f, (rootHeight - designHeight * scale) * 0.5f);
        }

        private void SelectAt(int index)
        {
            var request = _pendingRequest;
            if (request == null || request.Options == null || index < 0 || index >= request.Options.Count)
            {
                Log.Warning($"[UI3Pick1] SelectAt ignored index={index}");
                return;
            }

            var option = request.Options[index];
            _pendingRequest = null;
            request.CompletionSource?.TrySetResult(option);
            GameApp.UI.CloseUIForm(this);
        }

        private GGraph GetOrCreateHighlightBorder(string name)
        {
            var border = self?.GetChild(name) as GGraph;
            if (border != null)
            {
                return border;
            }

            border = new GGraph
            {
                name = name,
                touchable = false,
                visible = false
            };
            self?.AddChild(border);
            return border;
        }

        private static void ApplyQualityHighlight(GGraph border, GObject target, int qualityTier)
        {
            if (border == null || target == null)
            {
                return;
            }

            var lineColor = GetHighlightColor(qualityTier);
            var fillColor = new Color(0f, 0f, 0f, 0f);
            var width = target.width + HighlightPadding * 2f;
            var height = target.height + HighlightPadding * 2f;

            border.SetXY(target.x - HighlightPadding, target.y - HighlightPadding);
            border.SetSize(width, height);
            border.DrawRect(width, height, HighlightLineSize, lineColor, fillColor);
            border.sortingOrder = target.sortingOrder - 1;
        }

        private static Color GetHighlightColor(int qualityTier)
        {
            switch (qualityTier)
            {
                case 1:
                    return new Color32(0, 255, 0, 255);
                case 2:
                    return new Color32(16, 100, 255, 255);
                case 3:
                    return new Color32(130, 25, 255, 255);
                case 4:
                    return new Color32(255, 163, 26, 255);
                case 5:
                default:
                    return new Color32(255, 64, 64, 255);
            }
        }

        private static void ApplyIcon(GImage image, string iconName)
        {
            if (image == null || string.IsNullOrWhiteSpace(iconName))
            {
                return;
            }

            var texture = LoadOptionIcon(iconName);
            if (texture == null)
            {
                Log.Warning($"[UI3Pick1] icon load failed icon={iconName}");
                return;
            }

            image.texture = texture;
        }

        private static NTexture LoadOptionIcon(string iconName)
        {
            foreach (var candidate in GetIconCandidates(iconName))
            {
                var texture = Resources.Load<Texture2D>(candidate);
                if (texture != null)
                {
                    return new NTexture(texture);
                }

                var sprite = Resources.Load<Sprite>(candidate);
                if (sprite != null)
                {
                    return new NTexture(sprite.texture);
                }
            }

            return null;
        }

        private static IEnumerable<string> GetIconCandidates(string iconName)
        {
            yield return $"UI/ICON/{iconName}";
            yield return $"UI/icon/{iconName}";
            yield return $"UI/icon/skill0/{iconName}";
            yield return $"UI/icon/skill1/{iconName}";

            var normalizedName = NormalizeIconName(iconName);
            if (string.IsNullOrEmpty(normalizedName) || normalizedName == iconName)
            {
                yield break;
            }

            yield return $"UI/ICON/{normalizedName}";
            yield return $"UI/icon/{normalizedName}";
            yield return $"UI/icon/skill0/{normalizedName}";
            yield return $"UI/icon/skill1/{normalizedName}";
        }

        private static string NormalizeIconName(string iconName)
        {
            if (iconName.IndexOf('/') >= 0 || iconName.IndexOf('\\') >= 0)
            {
                return iconName.Replace('\\', '/');
            }

            if (!iconName.StartsWith("icon_", StringComparison.OrdinalIgnoreCase))
            {
                return iconName;
            }

            var suffix = iconName.Substring("icon_".Length);
            return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                ? $"icon{index}"
                : iconName.Replace("_", string.Empty);
        }

        private static IEnumerable<int> GetOptionIds(IReadOnlyList<OptionConfig> options)
        {
            for (var i = 0; i < options.Count; i++)
            {
                yield return options[i]?.OptionId ?? 0;
            }
        }
    }
}
#endif
