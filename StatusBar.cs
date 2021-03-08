using System;
using UnityEngine;
using UnityEngine.UI;

namespace GrassyKnight
{
    abstract class StatusBar {
        public abstract bool Visible { get; set; }
        public abstract void Update(GrassStats scene, GrassStats global);
    }

    class UnderSoulStatusBar : StatusBar
    {
        private const int _FONT_SIZE = 36;
        private const int _MARGIN_TOP = 20;

        private GameObject _canvas;
        private GameObject _textOnCanvas;

        public override bool Visible
        {
            get => _canvas.GetComponent<Canvas>().enabled;
            set => _canvas.GetComponent<Canvas>().enabled = value;
        }

        public UnderSoulStatusBar()
        {
            _canvas = new GameObject("Grassy StatusBar Canvas",
                                     typeof(Canvas));
            UnityEngine.Object.DontDestroyOnLoad(_canvas);

            Canvas canvasComponent = _canvas.GetComponent<Canvas>();
            canvasComponent.pixelPerfect = true;
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.enabled = false;

            _textOnCanvas = new GameObject(
                "Grassy StatusBar",
                typeof(Text),
                typeof(CanvasRenderer));
            UnityEngine.Object.DontDestroyOnLoad(_textOnCanvas);
            _textOnCanvas.transform.parent = canvasComponent.transform;
            _textOnCanvas.transform.localPosition =
                new Vector3(
                    -1 * canvasComponent.pixelRect.width / 2.5f + _FONT_SIZE / 3.25f,
                    canvasComponent.pixelRect.height / 3.25f - _FONT_SIZE / 2 - _MARGIN_TOP,
                    0);
            Text textComponent = _textOnCanvas.GetComponent<Text>();
            textComponent.font = Modding.CanvasUtil.TrajanBold;
            textComponent.text = "loading...";
            textComponent.fontSize = _FONT_SIZE;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private string PrettyStats(GrassStats stats)
        {
            int struck = stats[GrassState.Cut] + stats[GrassState.ShouldBeCut];
            string result =  $"{struck}/{stats.Total()}";
            return result;
        }

        public override void Update(GrassStats scene, GrassStats global)
        {
            string statusText = "";

            if (scene == null)
            {
                statusText += $"Pls...";
            }
            else if (scene[GrassState.Cut] + scene[GrassState.ShouldBeCut] - scene.Total() == 0)
            {
                statusText += $"  ";
            }
            else
            {
                statusText += $"{PrettyStats(scene)} ";
            }

            statusText += $"\n{PrettyStats(global)} ";
            _textOnCanvas.GetComponent<Text>().text = statusText;
        }
    }

    class TopMiddleStatusBar : StatusBar {
        private const int _FONT_SIZE = 21;
        private const int _MARGIN_TOP = 10;

        private GameObject _canvas;
        private GameObject _textOnCanvas;

        public override bool Visible {
            get => _canvas.GetComponent<Canvas>().enabled;
            set => _canvas.GetComponent<Canvas>().enabled = value;
        }

        public bool ShowShamefulGrass = false;

        public TopMiddleStatusBar(bool showShamefulGrass) {
            ShowShamefulGrass = showShamefulGrass;

            _canvas = new GameObject("GrassyKnight StatusBar Canvas",
                                     typeof(Canvas));
            UnityEngine.Object.DontDestroyOnLoad(_canvas);

            Canvas canvasComponent = _canvas.GetComponent<Canvas>();
            canvasComponent.pixelPerfect = true;
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.enabled = false;

            _textOnCanvas = new GameObject(
                "GrassyKnight StatusBar",
                typeof(Text),
                typeof(CanvasRenderer));
            UnityEngine.Object.DontDestroyOnLoad(_textOnCanvas);
            _textOnCanvas.transform.parent = canvasComponent.transform;
            _textOnCanvas.transform.localPosition =
                new Vector3(
                    0,
                    // Aligns the vertical center to top of screen
                    canvasComponent.pixelRect.height / 2
                        // Adjusts downwards so top of text is now along top
                        // of screen.
                        - _FONT_SIZE / 2
                        // Finally some margin space to taste. uwu.
                        - _MARGIN_TOP,
                    0);

            Text textComponent = _textOnCanvas.GetComponent<Text>();
            textComponent.font = Modding.CanvasUtil.TrajanBold;
            textComponent.text = "Loading GrassyKnight...";
            textComponent.fontSize = _FONT_SIZE;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private string PrettyStats(GrassStats stats) {
            int struck = stats[GrassState.Cut] + stats[GrassState.ShouldBeCut];
            string result =  $"{struck}/{stats.Total()}";
            if (ShowShamefulGrass && stats[GrassState.ShouldBeCut] > 0) {
                result += $" ({stats[GrassState.ShouldBeCut]} shameful)";
            }
            return result;
        }

        public override void Update(GrassStats scene, GrassStats global) {
            string statusText = "";

            if (scene == null) {
                statusText += $"(not in a room) ";
            } else {
                statusText += $"in room: {PrettyStats(scene)} ";
            }

            statusText += $"-- globally: {PrettyStats(global)}";

            _textOnCanvas.GetComponent<Text>().text = statusText;
        }
    }
}
