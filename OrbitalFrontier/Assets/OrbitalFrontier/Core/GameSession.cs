using System;

namespace OrbitalFrontier.Core
{
    public enum ScreenMode { Start, Hangar, Flight, Report }
    public enum Command
    {
        OpenHangar = 1, Home, LoadPreset, Install, Detach, Rotate, Assistance, Deploy,
        Warp, Pointing, Throttle, Plan, Suggest, Execute, Stop, System, Target, Resolution,
        Access, Scan, Recommend, Transmit
    }

    [Serializable]
    public sealed class GameView
    {
        public int screen, revision;
        public bool assisted;
        public CraftDesign design;
        public CraftStats stats;
        public FlightConsole flight;
    }

    public sealed class GameSession
    {
        readonly GameCatalog catalog;
        readonly GameView view = new GameView { design = new CraftDesign(), assisted = true };
        Mission mission;
        double shapeClock;

        public GameView View => view;

        public GameSession(GameCatalog catalog)
        {
            this.catalog = catalog;
            SetPreset(0);
            view.screen = (int)ScreenMode.Start;
        }

        public void Tick(double seconds)
        {
            if (mission == null || view.screen != (int)ScreenMode.Flight) return;
            mission.Tick(seconds);
            shapeClock += seconds;
            if (shapeClock > 0.35)
            {
                mission.RefreshTrajectory();
                shapeClock = 0;
            }
            if (!mission.Console.running)
                view.screen = (int)ScreenMode.Report;
        }

        public void Apply(Command action, int n = 0, double value = 0, double secondary = 0, bool host = true)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || double.IsNaN(secondary) || double.IsInfinity(secondary))
                return;
            if (action == Command.OpenHangar && host)
            {
                mission = null;
                view.flight = null;
                view.screen = (int)ScreenMode.Hangar;
            }
            else if (action == Command.Home && host)
            {
                mission = null;
                view.flight = null;
                view.screen = (int)ScreenMode.Start;
            }
            else if (view.screen == (int)ScreenMode.Hangar)
            {
                switch (action)
                {
                    case Command.LoadPreset when n >= 0 && n < catalog.presets.Length:
                        SetPreset(n); break;
                    case Command.Install when n >= 0 && n < catalog.parts.Length:
                        view.design.Install(catalog.parts[n]); break;
                    case Command.Detach when n >= 0 && n < Slot.Names.Length:
                        view.design.Detach(n); break;
                    case Command.Rotate when n >= 0 && n < Slot.Names.Length && !string.IsNullOrEmpty(view.design.parts[n]):
                        view.design.Rotate(n); break;
                    case Command.Assistance when host:
                        view.assisted = n != 0; break;
                    case Command.Deploy when host && view.stats.ready:
                        mission = new Mission(catalog, view.design.Copy(), view.assisted);
                        view.flight = mission.Console;
                        view.screen = (int)ScreenMode.Flight;
                        break;
                    default:
                        return;
                }
                view.stats = SpacecraftStats.Calculate(catalog, view.design);
            }
            else if (mission != null && view.screen == (int)ScreenMode.Flight)
            {
                switch (action)
                {
                    case Command.Warp: mission.SetWarp(n); break;
                    case Command.Pointing: mission.SetPointing(n); break;
                    case Command.Throttle: mission.SetThrottle(value); break;
                    case Command.Plan: mission.SetPlan(value, secondary); break;
                    case Command.Suggest: mission.SuggestPlan(); break;
                    case Command.Execute: mission.ExecutePlan(); break;
                    case Command.Stop: mission.AllStop(); break;
                    case Command.System when n >= 0 && n <= 2: mission.ToggleSystem(n); break;
                    case Command.Target: mission.Target(n); break;
                    case Command.Resolution: mission.SelectResolution(n); break;
                    case Command.Access: mission.CoastToAccess(); break;
                    case Command.Scan: mission.BeginScan(); break;
                    case Command.Recommend: mission.Recommend(n); break;
                    case Command.Transmit: mission.BeginDownlink(); break;
                    default: return;
                }
            }
            else return;
            view.revision++;
        }

        void SetPreset(int n)
        {
            view.design.Load(catalog.presets[n], n);
            view.stats = SpacecraftStats.Calculate(catalog, view.design);
        }
    }
}
