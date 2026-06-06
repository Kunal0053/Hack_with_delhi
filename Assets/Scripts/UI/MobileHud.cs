using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VortexGame.Core;
using VortexGame.Input;

namespace VortexGame.UI
{
    public sealed class MobileHud : MonoBehaviour
    {
        private VortexGameManager manager;
        private VortexEntity player;
        private Slider energyBar;
        private Text scoreText;
        private Text modeText;
        private Text stateText;

        public VirtualJoystick Joystick { get; private set; }
        public AbilityButton BoostButton { get; private set; }
        public AbilityButton ShieldButton { get; private set; }
        public AbilityButton ShockButton { get; private set; }
        public AbilityButton ReleaseButton { get; private set; }
        public AbilityButton ModeButton { get; private set; }
        public AbilityButton MatchButton { get; private set; }

        public static MobileHud Create(VortexGameManager gameManager)
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("MobileHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MobileHud));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            MobileHud hud = canvasObject.GetComponent<MobileHud>();
            hud.manager = gameManager;
            hud.Build();
            return hud;
        }

        public void BindPlayer(VortexEntity playerVortex)
        {
            player = playerVortex;
        }

        private void Build()
        {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Color panelColor = new Color(0.05f, 0.08f, 0.12f, 0.5f);

            energyBar = CreateSlider("EnergyBar", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 32f), panelColor);
            scoreText = CreateLabel("ScoreText", transform, font, 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(360f, 120f), new Vector2(40f, -32f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            modeText = CreateLabel("ModeText", transform, font, 26, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(420f, 40f), new Vector2(0f, -18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            stateText = CreateLabel("StateText", transform, font, 22, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(720f, 42f), new Vector2(0f, 26f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

            Joystick = CreateJoystick();
            BoostButton = CreateAbilityButton("Boost", new Vector2(-300f, 180f), font);
            ShieldButton = CreateAbilityButton("Shield", new Vector2(-120f, 290f), font);
            ShockButton = CreateAbilityButton("Shock", new Vector2(-120f, 80f), font);
            ReleaseButton = CreateAbilityButton("Release", new Vector2(-500f, 80f), font);
            ModeButton = CreateAbilityButton("Mode", new Vector2(-500f, 290f), font);
            MatchButton = CreateAbilityButton("Mode+", new Vector2(-220f, 420f), font);
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            energyBar.maxValue = Mathf.Max(100f, player.Energy + 25f);
            energyBar.value = player.Energy;
            scoreText.text = $"Energy {player.Energy:0}\nOrbiters {player.OrbitingObjects.Count}\nZone {player.ZoneScore:0.0}";
            modeText.text = $"{manager.ModeLabel}  |  {manager.TimeRemainingLabel}";
            stateText.text = player.IsPullMode ? "Pull field active" : "Push field active";
        }

        private VirtualJoystick CreateJoystick()
        {
            RectTransform root = CreatePanel("JoystickRoot", transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(220f, 220f), new Vector2(180f, 180f), new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, 0f));
            RectTransform handle = CreatePanel("Handle", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(82f, 82f), Vector2.zero, new Color(0.4f, 0.9f, 1f, 0.7f), new Vector2(0.5f, 0.5f));
            VirtualJoystick joystick = root.gameObject.AddComponent<VirtualJoystick>();
            joystick.Initialize(root, handle, 72f);
            return joystick;
        }

        private AbilityButton CreateAbilityButton(string label, Vector2 anchoredPosition, Font font)
        {
            RectTransform root = CreatePanel($"{label}Button", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(150f, 150f), anchoredPosition, new Color(0f, 0f, 0f, 0.25f), new Vector2(1f, 0f));

            GameObject ringObject = new GameObject("Ring", typeof(Image));
            ringObject.transform.SetParent(root, false);
            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            Stretch(ringRect);
            Image ringImage = ringObject.GetComponent<Image>();
            ringImage.color = new Color(0.2f, 0.9f, 1f, 0.4f);
            ringImage.type = Image.Type.Filled;
            ringImage.fillMethod = Image.FillMethod.Radial360;
            ringImage.fillOrigin = 2;
            ringImage.fillAmount = 1f;

            CreateLabel("Label", root, font, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(130f, 50f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).text = label;

            AbilityButton button = root.gameObject.AddComponent<AbilityButton>();
            button.Initialize(ringImage);
            return button;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color backgroundColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            GameObject bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(root.transform, false);
            Image bgImage = bg.GetComponent<Image>();
            bgImage.color = backgroundColor;
            Stretch(bg.GetComponent<RectTransform>());

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(10f, 6f);
            fillAreaRect.offsetMax = new Vector2(-10f, -6f);

            GameObject fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.2f, 0.95f, 1f, 0.85f);
            Stretch(fill.GetComponent<RectTransform>());

            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.maxValue = 100f;
            slider.value = 50f;
            return slider;
        }

        private static Text CreateLabel(string name, Transform parent, Font font, int fontSize, TextAnchor anchor, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition, Color color, Vector2 pivot)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
