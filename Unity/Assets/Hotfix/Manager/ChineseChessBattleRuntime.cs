using System.Text;
using Cysharp.Threading.Tasks;
using GameFrameX.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hotfix.Manager
{
    public static class ChineseChessBattleRuntime
    {
        private static bool _installed;
        private static GameObject _root;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            _installed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryBuild(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!MatchBattleContext.HasContext || !string.Equals(scene.name, MatchBattleContext.SceneName))
            {
                DestroyRoot();
                return;
            }

            if (!string.Equals(scene.name, "ChineseChess"))
            {
                DestroyRoot();
                return;
            }

            BuildChineseChessDemoView();
        }

        private static void TryBuild(Scene scene)
        {
            if (!MatchBattleContext.HasContext || !string.Equals(scene.name, MatchBattleContext.SceneName))
            {
                DestroyRoot();
                return;
            }

            if (!string.Equals(scene.name, "ChineseChess"))
            {
                DestroyRoot();
                return;
            }

            BuildChineseChessDemoView();
        }

        private static void BuildChineseChessDemoView()
        {
            DestroyRoot();
            Log.Info("ChineseChessBattleRuntime placeholder disabled for gameplay scene.");
        }

        private static string BuildSummaryText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"RoomId : {MatchBattleContext.RoomId}");
            builder.AppendLine($"Scene  : {MatchBattleContext.SceneName}");
            builder.AppendLine($"Seat   : {MatchBattleContext.SeatIndex}");
            builder.AppendLine($"Camp   : {MatchBattleContext.Camp}");
            builder.AppendLine();
            builder.AppendLine("Players");

            for (var i = 0; i < MatchBattleContext.PlayerList.Count; i++)
            {
                var player = MatchBattleContext.PlayerList[i];
                builder.Append("- ");
                builder.Append(player.Name);
                builder.Append(" Lv.");
                builder.Append(player.Level);
                builder.Append(player.IsReady ? " Ready" : " Pending");

                if (i == MatchBattleContext.SeatIndex)
                {
                    builder.Append(" <- You");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void BuildBoard(Transform parent)
        {
            var board = CreateImage("Board", parent, new Color32(193, 154, 93, 255));
            board.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            board.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            board.rectTransform.sizeDelta = new Vector2(720, 720);
            board.rectTransform.anchoredPosition = new Vector2(-430, 0);

            for (var i = 0; i < 10; i++)
            {
                var line = CreateImage($"Row{i}", board.transform, new Color32(89, 53, 25, 255));
                line.rectTransform.anchorMin = new Vector2(0.1f, 0.1f + i * 0.08888889f);
                line.rectTransform.anchorMax = new Vector2(0.9f, 0.1f + i * 0.08888889f);
                line.rectTransform.sizeDelta = new Vector2(0, 4);
            }

            for (var i = 0; i < 9; i++)
            {
                var line = CreateImage($"Col{i}", board.transform, new Color32(89, 53, 25, 255));
                line.rectTransform.anchorMin = new Vector2(0.1f + i * 0.1f, 0.1f);
                line.rectTransform.anchorMax = new Vector2(0.1f + i * 0.1f, 0.9f);
                line.rectTransform.sizeDelta = new Vector2(4, 0);
            }

            var river = CreateText("River", board.transform, 32, TextAnchor.MiddleCenter);
            river.text = "CHU HE           HAN JIE";
            river.color = new Color32(112, 43, 32, 255);
            Stretch(river.rectTransform, new Vector2(0.16f, 0.46f), new Vector2(0.84f, 0.56f));
        }

        private static async UniTaskVoid LeaveBattleDemoAsync()
        {
            try
            {
                if (MatchManager.Instance.CurrentRoomId > 0)
                {
                    await MatchManager.Instance.RequestLeaveRoom();
                }
            }
            finally
            {
                MatchBattleContext.Clear();
                SceneManager.LoadScene("Launcher");
            }
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color32(242, 239, 230, 255);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string caption)
        {
            var buttonImage = CreateImage(name, parent, new Color32(171, 72, 48, 255));
            var button = buttonImage.gameObject.AddComponent<Button>();
            var label = CreateText("Label", buttonImage.transform, 24, TextAnchor.MiddleCenter);
            label.text = caption;
            Stretch(label.rectTransform);
            return button;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            Stretch(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void DestroyRoot()
        {
            if (_root == null)
            {
                return;
            }

            Object.Destroy(_root);
            _root = null;
        }
    }
}
