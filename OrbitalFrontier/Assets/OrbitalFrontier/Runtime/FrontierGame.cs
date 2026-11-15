using System;
using System.Globalization;
using OrbitalFrontier.Core;
using UnityEngine;

namespace OrbitalFrontier.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class FrontierGame : MonoBehaviour
    {
        [SerializeField] Transform spacecraftPrefab = null;
        [SerializeField, Range(0, 2)] int initialScreen = 0;
        public const int ListenPort = 47610;
        GameCatalog catalog;
        GameSession session;
        FrontierConsole ui;
        FlightStage stage;
        int panel, inspected = -1, lastScreen = -1, lastRevision = -1, atlas, drawnSite = -1, viewMode;
        string overlay, inputBurn = "", inputDelay = "18";
        bool motion = true, viewportDragging;
        Vector2 oldPointer;
        Rect viewport;
        Texture2D surveyImage;
        SurveyProduct drawnProduct;
        string dataError;
#if !UNITY_WEBGL || UNITY_EDITOR
        GameView remote;
        float broadcastTimer;
        string joinAddress = "127.0.0.1";
        readonly LanLink network = new LanLink();
        bool awaitingFirst;
        bool HasHost => network.Joined && remote != null;
        bool CrewOnline => network.Joined || network.Hosting;
        string NetStatus => network.Joined ? "CREW  " + network.CrewCount + "/4" : network.Hosting
            ? "LAN HOST  " + network.CrewCount + "/4" : "SOLO SESSION";
#else
        bool HasHost => false;
        bool CrewOnline => false;
        string NetStatus => "SOLO SESSION";
#endif
        GameView View
        {
            get
            {
#if !UNITY_WEBGL || UNITY_EDITOR
                if (HasHost) return remote;
#endif
                return session.View;
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        [Serializable]
        sealed class WireInput
        {
            public int command, n;
            public double value, secondary;
        }

#endif

        void Awake()
        {
            Application.targetFrameRate = 60;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var data = Resources.Load<TextAsset>("Frontier/mission");
            if (!data)
            {
                dataError = "Mission catalog is missing. Reimport Assets/OrbitalFrontier/Resources.";
                return;
            }
            catalog = JsonUtility.FromJson<GameCatalog>(data.text);
            session = new GameSession(catalog);
            if (initialScreen > 0) session.Apply(Command.OpenHangar);
            if (initialScreen == 2) session.Apply(Command.Deploy);
            ui = new FrontierConsole();
            stage = gameObject.AddComponent<FlightStage>();
            stage.Make(catalog, spacecraftPrefab);
            // One-time welcome; guide remains accessible from the top navigation.
            if (PlayerPrefs.GetInt("OrbitalFrontier.GuideSeen", 0) == 0)
            {
                tutorialPage = 0;
                overlay = "tutorial";
                PlayerPrefs.SetInt("OrbitalFrontier.GuideSeen", 1);
                PlayerPrefs.Save();
            }
        }

        void Update()
        {
            if (session == null) return;
#if !UNITY_WEBGL || UNITY_EDITOR
            network.Poll(Time.unscaledDeltaTime, ReceiveInput, ReceiveState);
            if (!network.Joined && remote != null)
            {
                remote = null;
                overlay = "crew";
                lastScreen = -1;
            }
            if (network.Joined && remote != null && awaitingFirst)
            {
                overlay = null;
                awaitingFirst = false;
            }
            if (!network.Joined)
#endif
                session.Tick(Time.unscaledDeltaTime);

#if !UNITY_WEBGL || UNITY_EDITOR
            broadcastTimer += Time.unscaledDeltaTime;
            if (network.Hosting && broadcastTimer >= 0.12f)
            {
                broadcastTimer = 0;
                if (network.CrewCount > 1) network.Broadcast(JsonUtility.ToJson(session.View));
            }
#endif

            var view = View;
            if (view.screen != lastScreen)
            {
                lastScreen = view.screen;
                stage.Show(lastScreen);
                if (overlay == "log" && lastScreen != 2) overlay = null;
                if (lastScreen == 2)
                {
                    panel = 0;
                    viewMode = 0;
                    ReadPlan();
                }
                if (lastScreen == 3 && view.flight != null)
                {
                    PlayerPrefs.SetInt("OrbitalFrontier.LastSurveyScore", view.flight.score);
                    PlayerPrefs.Save();
                }
            }
            if (lastRevision != view.revision && view.flight != null)
            {
                lastRevision = view.revision;
                if (HasHost) ReadPlan();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (overlay == null) Open("controls");
                else overlay = null;
            }

            if (viewMode == 2 && view.flight?.products[view.flight.target] == null)
            {
                viewMode = 0;
                stage.Reset(0);
            }
            float width = CanvasWidth;
            float height = CanvasHeight;
            viewport = StageRect(view.screen, width, height);
            var normalized = new Rect(viewport.x / width, 1 - viewport.yMax / height,
                viewport.width / width, viewport.height / height);
            stage.UpdateView(view, inspected, normalized, motion);
            UpdateCameraInput();
        }

        void UpdateCameraInput()
        {
            var raw = Input.mousePosition;
            var p = new Vector2(raw.x / Scale, (Screen.height - raw.y) / Scale);
            bool supportsDrag = overlay == null && (View.screen == 1 || View.screen == 2 && viewMode != 1);
            var limits = new Rect(viewport.x, viewport.y + 58, viewport.width, Mathf.Max(0, viewport.height - 102));
            bool inside = limits.Contains(p);
            if (Input.GetMouseButtonDown(0)) viewportDragging = supportsDrag && inside;
            if (!Input.GetMouseButton(0) || !supportsDrag) viewportDragging = false;
            if (viewportDragging)
                stage.Drag(p.x - oldPointer.x, p.y - oldPointer.y);
            if (supportsDrag && inside && Mathf.Abs(Input.mouseScrollDelta.y) > 0)
                stage.Zoom(-Input.mouseScrollDelta.y);
            oldPointer = p;
        }

        float Scale => Mathf.Clamp(Mathf.Min(Screen.width / 1260f, Screen.height / 745f), .52f, 1.55f);
        float CanvasWidth => Screen.width / Scale;
        float CanvasHeight => Screen.height / Scale;

        Rect StageRect(int screen, float width, float height)
        {
            if (screen == 0) return new Rect(0, 60, width, height - 60);
            float left = screen == 1 ? 226 : 211;
            return new Rect(left + 21, 151, width - left - 371, Mathf.Max(450, height - (screen == 1 ? 184 : 233)));
        }

        void Send(Command command, int option = 0, double value = 0, double secondary = 0)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (network.Joined)
            {
                network.SendCommand(JsonUtility.ToJson(new WireInput { command = (int)command, n = option, value = value, secondary = secondary }));
                return;
            }
#endif
            session.Apply(command, option, value, secondary);
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        void ReceiveInput(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<WireInput>(json);
                if (command != null && command.command >= (int)Command.LoadPreset && command.command <= (int)Command.Transmit)
                    session.Apply((Command)command.command, command.n, command.value, command.secondary, false);
            }
            catch (ArgumentException) { }
        }

        void ReceiveState(string json)
        {
            try
            {
                var incoming = JsonUtility.FromJson<GameView>(json);
                if (incoming != null && incoming.design != null && incoming.stats != null && incoming.screen >= 0 && incoming.screen <= 3)
                {
                    if (incoming.flight?.products != null)
                        for (int i = 0; i < incoming.flight.products.Length; i++)
                            if (incoming.flight.scanned[i] == 0) incoming.flight.products[i] = null;
                    remote = incoming;
                }
            }
            catch (ArgumentException) { }
        }
#endif

        void ReadPlan()
        {
            var f = View.flight;
            if (f == null) return;
            inputBurn = f.burn.ToString("0.0");
            inputDelay = Math.Max(0, f.burnAt - f.time).ToString("0");
        }

        void ApplyPlan(bool execute)
        {
            if (!double.TryParse(inputBurn, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv) ||
                !double.TryParse(inputDelay, NumberStyles.Float, CultureInfo.InvariantCulture, out double delay)) return;
            Send(Command.Plan, 0, dv, delay);
            if (execute) Send(Command.Execute);
        }

        void SwitchPanel(int panelIndex)
        {
            if (panelIndex == panel) return;
            GUI.FocusControl(null);
            panel = panelIndex;
        }

        void Open(string name)
        {
            overlay = name;
        }

        void OnDestroy()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            network.Dispose();
#endif
            if (surveyImage) Destroy(surveyImage);
            ui?.Dispose();
        }

        static string N(double v, string format = "0.0") => v.ToString(format, CultureInfo.InvariantCulture);
        static string Clock(double time) => TimeSpan.FromSeconds(Math.Max(0, time)).ToString(@"hh\:mm\:ss");
    }
}
