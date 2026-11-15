using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MISSION DIRECTOR: A complete, single-file, lightweight Unity mission strategy game.
/// Attach to ONE GameObject in any scene. No packages, prefabs, scenes or assets required.
/// Unity 6 compatible. Intentionally a game model, not real flight-dynamics software.
///
/// Loop: pick destination -> build rocket/payload -> launch plan -> flight readiness
/// review -> ascent -> transfer -> orbit insertion -> orbital operations -> debrief.
/// Science must be COLLECTED and DOWNLINKED. Credits fund bigger future missions.
/// Random hazards reward contingency planning; all decisions have trade-offs.
/// The in-progress mission is saved to PlayerPrefs, including incident choices.
/// </summary>
public class SpaceMissionGame : MonoBehaviour
{
    private const string SaveKey = "MissionDirector_Full_2026_v1_";
    private const int InitialCredits = 1800;
    private const int MaxOrbitTurns = 15;
    private const float W = 1280f;
    private const float H = 800f;

    private enum Page { Missions, Build, Plan, Review, Operations, Incident, Debrief }
    private enum FlightPhase { Ascent, Transfer, Insertion, Orbit }

    private class Destination
    {
        public string Name, ShortName, Summary, Objective, Environment;
        public int BaseCost, Reward, ScienceGoal, RequiredInstrument, UnlockAfter;
        public float Danger;

        public Destination(string name, string shortName, string summary, string objective,
            string environment, int cost, int reward, int goal, int required, int unlock, float danger)
        {
            Name = name; ShortName = shortName; Summary = summary; Objective = objective;
            Environment = environment; BaseCost = cost; Reward = reward;
            ScienceGoal = goal; RequiredInstrument = required;
            UnlockAfter = unlock; Danger = danger;
        }
    }

    private class Rocket
    {
        public string Name, Details;
        public int Cost, Capacity, Fuel, Power, Integrity;
        public Rocket(string name, string details, int cost, int capacity, int fuel, int power, int integrity)
        {
            Name = name; Details = details; Cost = cost; Capacity = capacity;
            Fuel = fuel; Power = power; Integrity = integrity;
        }
    }

    private class Instrument
    {
        public string Name, Details;
        public int Cost, Mass;
        public Instrument(string name, string details, int cost, int mass)
        {
            Name = name; Details = details; Cost = cost; Mass = mass;
        }
    }

    private class Response
    {
        public string Name, Preview;
        public int Fuel, Power, Hull, Stability, Data;
        public float Risk, Reliability;
        public int Setback;
        public Response(string name, string preview, int fuel, int power, int hull,
            int stability, int data, float risk, float reliability, int setback)
        {
            Name = name; Preview = preview; Fuel = fuel; Power = power;
            Hull = hull; Stability = stability; Data = data; Risk = risk;
            Reliability = reliability; Setback = setback;
        }
    }

    private class Hazard
    {
        public string Title, Situation;
        public Response[] Choices;
        public Hazard(string title, string situation, params Response[] choices)
        { Title = title; Situation = situation; Choices = choices; }
    }

    [Serializable]
    private class FlightSave
    {
        public int Page, Destination, Rocket, FirstInstrument, SecondInstrument;
        public int Route, Window, Doctrine, Weather, Solar;
        public int Phase, Turns, AllowedTurns, Altitude, IncidentIndex;
        public int Fuel, Power, Hull, Stability, Buffer, Downlinked, Investment;
        public float Risk;
        public string Status;
        public string[] Log;
    }

    private readonly Destination[] destinations =
    {
        new Destination("EARTH ORBIT", "EARTH", "Deploy an Earth-observation research satellite.",
            "Map weather systems and send 72 science points back to Earth.",
            "Orbital debris / atmospheric drag", 230, 890, 72, 0, -1, .16f),
        new Destination("LUNAR ORBIT", "MOON", "Map the lunar surface and study mineral signatures.",
            "Collect and downlink 90 science points from lunar orbit.",
            "Navigation uncertainty / weak communications", 560, 1820, 90, 1, 0, .22f),
        new Destination("MARS ORBIT", "MARS", "Explore the Martian atmosphere from orbit.",
            "Collect and downlink 110 science points about Mars.",
            "Long transfer / dust / communications delay", 1060, 3960, 110, 2, 1, .28f),
        new Destination("JUPITER FLYBY", "JUPITER", "Survey Jupiter's magnetosphere and major moons.",
            "Collect and downlink 128 science points during the science orbit phase.",
            "Intense radiation / distant communications", 1810, 8200, 128, 3, 2, .35f)
    };

    private readonly Rocket[] rockets =
    {
        new Rocket("PIONEER", "Cheap / light / low reserve", 0, 3, 78, 82, 78),
        new Rocket("PATHFINDER", "Balanced / reliable / versatile", 180, 5, 106, 108, 96),
        new Rocket("ODYSSEY", "Heavy / generous reserves", 390, 8, 138, 125, 115)
    };

    // 0-3 are mission instruments; 4-7 are optional support systems.
    private readonly Instrument[] instruments =
    {
        new Instrument("EARTH IMAGER", "High-resolution Earth science", 90, 1),
        new Instrument("LUNAR SPECTROMETER", "Detects lunar surface minerals", 135, 2),
        new Instrument("ATMOSPHERIC PROBE", "Studies Martian atmosphere", 165, 2),
        new Instrument("MAGNETOMETER", "Studies Jovian magnetic fields", 185, 2),
        new Instrument("HIGH-GAIN ANTENNA", "+18 downlink / +40 data storage", 155, 2),
        new Instrument("SOLAR POWER ARRAY", "+25 initial power / better recharge", 120, 2),
        new Instrument("RADIATION SHIELD", "+18 hull / event protection", 225, 3),
        new Instrument("EXTRA FUEL TANK", "+28 fuel reserve", 170, 2)
    };

    private Page page = Page.Missions;
    private FlightPhase phase = FlightPhase.Ascent;
    private int selectedMission, selectedRocket = 1, firstInstrument, secondInstrument = 5;
    private int selectedSlot, trajectory = 1, launchWindow = 1, doctrine = 1;
    private int weather, solarForecast, fault = -2, repairCost;
    private bool diagnosticDone, showHelp, confirmReset;
    private readonly bool[] signoffs = new bool[3];
    private readonly int[] completed = new int[4];
    private int credits;

    private int fuel, power, hull, stability, dataBuffer, downlinked, orbitTurns, allowedTurns;
    private int orbitAltitude = 1, activeIncident, missionInvestment;
    private float risk;
    private bool missionSuccess;
    private int missionReward;
    private string resultTitle = "", resultDetails = "";
    private string status = "Choose your destination to begin.";
    private readonly List<string> log = new List<string>();

    private GUIStyle headingStyle, titleStyle, textStyle, smallStyle, tinyStyle;
    private GUIStyle mutedStyle, accentStyle, warningStyle, moneyStyle;
    private GUIStyle buttonStyle, selectedButtonStyle, primaryStyle, disabledStyle;
    private Texture2D normalTexture, selectedTexture, primaryTexture, mutedTexture;
    private Camera spawnedCamera;

    private void Awake()
    {
        credits = PlayerPrefs.GetInt(SaveKey + "Credits", InitialCredits);
        for (int i = 0; i < completed.Length; i++)
            completed[i] = PlayerPrefs.GetInt(SaveKey + "Completed" + i, 0);
        RestoreFlight();
        EnsureCamera();
    }

    private void EnsureCamera()
    {
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
            if (cameras[i] != null && cameras[i].enabled && cameras[i].targetDisplay == 0)
                return;
        GameObject cameraObject = new GameObject("Mission Director UI Camera");
        cameraObject.transform.SetParent(transform, false);
        spawnedCamera = cameraObject.AddComponent<Camera>();
        spawnedCamera.clearFlags = CameraClearFlags.SolidColor;
        spawnedCamera.backgroundColor = new Color(.025f, .040f, .082f);
        spawnedCamera.cullingMask = 0;
        spawnedCamera.transform.position = new Vector3(0f, 0f, -10f);
        spawnedCamera.depth = -10f;
        spawnedCamera.targetDisplay = 0;
    }

    private void SaveCampaign()
    {
        PlayerPrefs.SetInt(SaveKey + "Credits", credits);
        for (int i = 0; i < completed.Length; i++)
            PlayerPrefs.SetInt(SaveKey + "Completed" + i, completed[i]);
        PlayerPrefs.Save();
    }

    private void SaveFlight()
    {
        if (page != Page.Operations && page != Page.Incident) return;
        FlightSave s = new FlightSave();
        s.Page = (int)page; s.Destination = selectedMission; s.Rocket = selectedRocket;
        s.FirstInstrument = firstInstrument; s.SecondInstrument = secondInstrument;
        s.Route = trajectory; s.Window = launchWindow; s.Doctrine = doctrine;
        s.Weather = weather; s.Solar = solarForecast;
        s.Phase = (int)phase; s.Turns = orbitTurns; s.AllowedTurns = allowedTurns;
        s.Altitude = orbitAltitude; s.IncidentIndex = activeIncident;
        s.Fuel = fuel; s.Power = power; s.Hull = hull; s.Stability = stability;
        s.Buffer = dataBuffer; s.Downlinked = downlinked; s.Investment = missionInvestment;
        s.Risk = risk; s.Status = status; s.Log = log.ToArray();
        PlayerPrefs.SetString(SaveKey + "Flight", JsonUtility.ToJson(s));
        PlayerPrefs.Save();
    }

    private void RestoreFlight()
    {
        if (!PlayerPrefs.HasKey(SaveKey + "Flight")) return;
        try
        {
            FlightSave s = JsonUtility.FromJson<FlightSave>(PlayerPrefs.GetString(SaveKey + "Flight"));
            if (s == null || s.Destination < 0 || s.Destination >= destinations.Length ||
                s.Rocket < 0 || s.Rocket >= rockets.Length ||
                s.FirstInstrument < 0 || s.FirstInstrument >= instruments.Length ||
                s.SecondInstrument < 0 || s.SecondInstrument >= instruments.Length) return;
            selectedMission = s.Destination; selectedRocket = s.Rocket;
            firstInstrument = s.FirstInstrument; secondInstrument = s.SecondInstrument;
            trajectory = s.Route; launchWindow = s.Window; doctrine = s.Doctrine;
            weather = s.Weather; solarForecast = s.Solar;
            phase = (FlightPhase)Mathf.Clamp(s.Phase, 0, 3);
            orbitTurns = s.Turns; allowedTurns = s.AllowedTurns;
            orbitAltitude = s.Altitude; activeIncident = s.IncidentIndex;
            fuel = s.Fuel; power = s.Power; hull = s.Hull;
            stability = s.Stability; dataBuffer = s.Buffer;
            downlinked = s.Downlinked; missionInvestment = s.Investment;
            risk = s.Risk;
            page = s.Page == (int)Page.Incident ? Page.Incident : Page.Operations;
            status = "Mission restored. " + s.Status;
            log.Clear();
            if (s.Log != null) log.AddRange(s.Log);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not restore mission save: " + e.Message);
            PlayerPrefs.DeleteKey(SaveKey + "Flight");
        }
    }

    private bool Unlocked(int mission)
    {
        return destinations[mission].UnlockAfter < 0 ||
            completed[destinations[mission].UnlockAfter] > 0;
    }

    private void SelectMission(int index)
    {
        selectedMission = index;
        selectedRocket = index >= 3 ? 2 : 1;
        firstInstrument = destinations[index].RequiredInstrument;
        secondInstrument = index == 3 ? 6 : 5;
        weather = UnityEngine.Random.Range(0, 3);
        solarForecast = UnityEngine.Random.Range(0, 3);
        ResetReview();
        status = Unlocked(index) ? "Mission briefing loaded." :
            "Complete the previous destination to unlock this campaign mission.";
    }

    private void ResetReview()
    {
        diagnosticDone = false; fault = -2; repairCost = 0;
        for (int i = 0; i < signoffs.Length; i++) signoffs[i] = false;
    }

    private int PayloadMass { get { return instruments[firstInstrument].Mass + instruments[secondInstrument].Mass; } }
    private int MissionCost
    {
        get
        {
            int windowCost = launchWindow == 1 ? 65 : (launchWindow == 2 ? 135 : 0);
            return destinations[selectedMission].BaseCost + rockets[selectedRocket].Cost +
                instruments[firstInstrument].Cost + instruments[secondInstrument].Cost +
                windowCost + (diagnosticDone ? 40 : 0) + repairCost;
        }
    }

    private bool HasInstrument(int index) { return firstInstrument == index || secondInstrument == index; }
    private bool PlanValid
    {
        get
        {
            return Unlocked(selectedMission) && firstInstrument != secondInstrument &&
                HasInstrument(destinations[selectedMission].RequiredInstrument) &&
                PayloadMass <= rockets[selectedRocket].Capacity && MissionCost <= credits;
        }
    }

    private string PlanProblem()
    {
        if (!Unlocked(selectedMission)) return "LOCKED: complete the preceding campaign mission.";
        if (firstInstrument == secondInstrument) return "Choose two different instruments.";
        if (!HasInstrument(destinations[selectedMission].RequiredInstrument))
            return "Required instrument missing: " + instruments[destinations[selectedMission].RequiredInstrument].Name;
        if (PayloadMass > rockets[selectedRocket].Capacity)
            return "PAYLOAD TOO HEAVY: " + PayloadMass + " / " + rockets[selectedRocket].Capacity + " mass units.";
        if (MissionCost > credits)
            return "BUDGET SHORTFALL: need " + (MissionCost - credits) + " more credits.";
        return "DESIGN APPROVED: cost, payload, and spacecraft are compatible.";
    }

    private void Equip(int index)
    {
        if (index == firstInstrument || index == secondInstrument)
        {
            selectedSlot = index == firstInstrument ? 0 : 1;
            status = "Instrument already equipped; select a different instrument to replace it.";
            return;
        }
        if (selectedSlot == 0) firstInstrument = index;
        else secondInstrument = index;
        ResetReview();
        status = instruments[index].Name + " installed in payload slot " + (selectedSlot + 1) + ".";
    }

    private void RunDiagnostic()
    {
        diagnosticDone = true;
        fault = UnityEngine.Random.value < .36f ? UnityEngine.Random.Range(0, 3) : -1;
        repairCost = 0;
        for (int i = 0; i < signoffs.Length; i++) signoffs[i] = false;
        status = fault == -1 ? "DIAGNOSTICS PASS: all sampled systems nominal." :
            "DIAGNOSTICS WARNING: " + FaultName() + ". Repair before launch or accept extra risk.";
    }

    private string FaultName()
    {
        if (fault == 0) return "engine valve anomaly";
        if (fault == 1) return "power-bus instability";
        if (fault == 2) return "navigation sensor offset";
        return "no known faults";
    }

    private void RepairFault()
    {
        if (fault < 0) return;
        int cost = fault == 0 ? 110 : (fault == 1 ? 85 : 75);
        if (MissionCost + cost > credits)
        {
            status = "Not enough credits to repair. Change the mission design or accept the risk.";
            return;
        }
        repairCost += cost;
        status = "Repair work completed for " + cost + " credits. Preflight fault resolved.";
        fault = -1;
    }

    private float PredictedRisk()
    {
        float result = destinations[selectedMission].Danger;
        result += trajectory == 0 ? .07f : (trajectory == 2 ? -.055f : 0);
        result += doctrine == 0 ? -.075f : (doctrine == 2 ? .095f : 0);
        int remainingWeather = Mathf.Max(0, weather - launchWindow);
        result += remainingWeather * .075f;
        if (fault >= 0) result += .16f;
        if (HasInstrument(6)) result -= .065f;
        return Mathf.Clamp(result, .055f, .66f);
    }

    private void BeginFlight()
    {
        if (!PlanValid || !diagnosticDone) return;
        for (int i = 0; i < signoffs.Length; i++) if (!signoffs[i]) return;

        missionInvestment = MissionCost;
        credits -= missionInvestment;
        SaveCampaign();
        Rocket rocket = rockets[selectedRocket];
        fuel = rocket.Fuel + (HasInstrument(7) ? 28 : 0);
        power = rocket.Power + (HasInstrument(5) ? 25 : 0);
        hull = rocket.Integrity + (HasInstrument(6) ? 18 : 0);
        stability = 54; dataBuffer = 0; downlinked = 0;
        orbitTurns = 0; allowedTurns = 9; orbitAltitude = 1;
        risk = PredictedRisk();
        phase = FlightPhase.Ascent; page = Page.Operations;
        log.Clear();
        AddLog("MISSION GO. Budget committed: " + missionInvestment + " cr.");
        AddLog("Forecast: weather " + WeatherName() + ", solar " + SolarName() + ".");
        status = "LAUNCH AUTHORIZED. Choose your ascent profile.";
        SaveFlight();
    }

    private string WeatherName() { return weather == 0 ? "GREEN" : weather == 1 ? "AMBER" : "RED"; }
    private string SolarName() { return solarForecast == 0 ? "QUIET" : solarForecast == 1 ? "VARIABLE" : "ACTIVE"; }

    private void AddLog(string message)
    {
        log.Insert(0, message);
        if (log.Count > 7) log.RemoveAt(log.Count - 1);
    }

    private int ScienceYield(bool deep)
    {
        float amount = 25 + selectedMission * 4;
        amount *= orbitAltitude == 0 ? 1.30f : (orbitAltitude == 2 ? .79f : 1f);
        amount *= doctrine == 0 ? .87f : (doctrine == 2 ? 1.24f : 1f);
        if (HasInstrument(destinations[selectedMission].RequiredInstrument)) amount += 7f;
        if (HasInstrument(0) && selectedMission != 0) amount += 2f;
        if (deep) amount *= 1.72f;
        return Mathf.RoundToInt(amount);
    }

    private int DownlinkRate()
    {
        int rate = 33 + (HasInstrument(4) ? 18 : 0);
        if (orbitAltitude == 2) rate += 7;
        if (selectedMission == 2) rate -= 6;
        if (selectedMission == 3) rate -= 10;
        return Mathf.Max(15, rate);
    }

    private int StorageCapacity { get { return HasInstrument(4) ? 150 : 105; } }

    private void ChangeResources(int df, int dp, int dh, int ds, int buffered, float dr)
    {
        fuel += df; power += dp;
        if (dh < 0 && HasInstrument(6)) dh = Mathf.RoundToInt(dh * .65f);
        hull += dh; stability += ds;
        dataBuffer = Mathf.Clamp(dataBuffer + buffered, 0, StorageCapacity);
        risk = Mathf.Clamp(risk + dr, .025f, .82f);
        power = Mathf.Min(power, 165);
        stability = Mathf.Min(stability, 100);
    }

    private bool CheckCritical()
    {
        if (hull <= 0) { FinishMission("Spacecraft lost: structural integrity reached zero.", false); return true; }
        if (fuel < 0) { FinishMission("Propellant exhausted during an essential maneuver.", false); return true; }
        if (power < 0) { FinishMission("The power bus shut down during an operation.", false); return true; }
        if (stability <= 0) { FinishMission("Orbit became unstable; science operations were aborted.", false); return true; }
        return false;
    }

    private void FlightChoice(int choice)
    {
        if (page != Page.Operations || phase == FlightPhase.Orbit) return;
        if (phase == FlightPhase.Ascent)
        {
            if (choice == 0) { ChangeResources(-9, -4, 0, 6, 0, -.045f); status = "Gentle ascent reduces strain but uses more time."; }
            if (choice == 1) { ChangeResources(-13, -7, 0, 2, 0, 0); status = "Nominal ascent follows the flight plan."; }
            if (choice == 2) { ChangeResources(-19, -8, -3, 0, 0, .065f); status = "Maximum thrust shortens ascent but stresses the vehicle."; }
            // Immediate launch in bad weather imposes a real physical cost.
            int badWeather = Mathf.Max(0, weather - launchWindow);
            if (badWeather > 0) ChangeResources(0, 0, -badWeather * 5, 0, 0, badWeather * .02f);
        }
        else if (phase == FlightPhase.Transfer)
        {
            if (choice == 0) { ChangeResources(-14, -6, 0, 15, 0, -.065f); status = "Course correction improves arrival geometry."; }
            if (choice == 1) { ChangeResources(-4, 4, 0, -12, 0, .075f); status = "Coasting saves fuel but increases navigation uncertainty."; }
            if (choice == 2) { ChangeResources(-8, -11, 0, -4, 10, .025f); status = "Cruise observation gathered early science, but spent power."; }
            if (trajectory == 0) ChangeResources(9, -7, 0, 0, 0, .045f);
            if (trajectory == 2) ChangeResources(-13, 0, 0, 0, 0, -.035f);
        }
        else if (phase == FlightPhase.Insertion)
        {
            orbitAltitude = choice;
            if (choice == 0) { ChangeResources(-23, -10, 0, 4, 0, .085f); status = "LOW ORBIT: high science yield; greater hazard and drag."; }
            if (choice == 1) { ChangeResources(-16, -7, 0, 19, 0, -.025f); status = "MID ORBIT: balanced science and stability."; }
            if (choice == 2) { ChangeResources(-11, -5, 0, 29, 0, -.05f); status = "HIGH ORBIT: safer and better communications; weaker scans."; }
        }
        AddLog(status);
        AfterAction();
    }

    private void OrbitChoice(int choice)
    {
        if (page != Page.Operations || phase != FlightPhase.Orbit || orbitTurns >= allowedTurns) return;
        if (choice == 0) { ChangeResources(-2, -12, 0, -5, ScienceYield(false), .014f); status = "Payload scan: new science stored onboard."; }
        if (choice == 1) { ChangeResources(-5, -23, 0, -11, ScienceYield(true), .070f); status = "Deep scan: high data yield but substantial strain."; }
        if (choice == 2)
        {
            int sent = Mathf.Min(dataBuffer, DownlinkRate());
            ChangeResources(-2, -13, 0, -3, -sent, -.015f);
            downlinked += sent;
            status = "Downlink successful: " + sent + " science points reached Earth.";
        }
        if (choice == 3)
        {
            ChangeResources(-1, HasInstrument(5) ? 37 : 27, 0, -4, 0, -.025f);
            status = "Power systems recharged; instruments paused.";
        }
        if (choice == 4) { ChangeResources(-13, -6, 0, 31, 0, -.06f); status = "Orbit correction improved spacecraft stability."; }
        if (choice == 5) { ChangeResources(-1, 11, 0, -6, 0, -.080f); status = "Safe mode reduced exposure but lost science time."; }
        orbitTurns++;
        // Orbital exposure accumulates: extending indefinitely is not free.
        if (orbitTurns > 7) risk = Mathf.Clamp(risk + .012f, .025f, .82f);
        if (selectedMission == 3 && !HasInstrument(6) && UnityEngine.Random.value < .23f)
        {
            ChangeResources(0, 0, -5, 0, 0, .025f);
            AddLog("Jovian radiation degraded unshielded electronics.");
        }
        AddLog("ORBIT " + orbitTurns + ": " + status);
        AfterAction();
    }

    private void AfterAction()
    {
        if (CheckCritical()) return;
        float chance = risk + (solarForecast * .035f) +
            (phase == FlightPhase.Orbit && orbitAltitude == 0 ? .08f : 0f);
        if (phase == FlightPhase.Orbit && orbitTurns > 9) chance += .06f;
        chance = Mathf.Clamp(chance, .08f, .75f);
        if (UnityEngine.Random.value < chance)
        {
            activeIncident = UnityEngine.Random.Range(0, 7);
            page = Page.Incident;
            status = "UNEXPECTED INCIDENT. Choose how Mission Control responds.";
            AddLog("ALERT: " + CreateHazard(activeIncident).Title);
        }
        else
        {
            AdvancePhase();
        }
        SaveFlight();
    }

    private void AdvancePhase()
    {
        if (phase != FlightPhase.Orbit)
        {
            phase = (FlightPhase)((int)phase + 1);
            if (phase == FlightPhase.Orbit)
            {
                AddLog("ORBIT ESTABLISHED. Begin orbital science operations.");
                status = "Science is only credited after a successful downlink to Earth.";
            }
            else if (phase == FlightPhase.Transfer) status = "Transfer underway: pick a navigation strategy.";
            else if (phase == FlightPhase.Insertion) status = "Destination reached: choose your orbital altitude.";
        }
        page = Page.Operations;
    }

    private Hazard CreateHazard(int index)
    {
        switch (index)
        {
            case 0: return new Hazard("SOLAR RADIATION STORM", "Charged particles threaten spacecraft electronics. Do you protect the ship or keep collecting data?",
                new Response("SHIELD + SAFE MODE", "Reliable / -15 power / -2 hull", 0, -15, -2, 0, 0, -.045f, .95f, 5),
                new Response("CHANGE ATTITUDE", "Fuel -7 / modest shielding", -7, -5, -5, -3, 0, -.01f, .82f, 9),
                new Response("KEEP SCANNING", "+16 data / high damage risk", 0, -8, -13, -3, 16, .07f, .56f, 17));
            case 1: return new Hazard("MICROMETEOROID WARNING", "Tracking predicts a close pass through a debris field.",
                new Response("AVOIDANCE BURN", "Fuel -11 / high success", -11, -4, 0, 9, 0, -.045f, .95f, 6),
                new Response("REORIENT SHIELD", "Power -9 / some damage", 0, -9, -6, -4, 0, -.015f, .82f, 10),
                new Response("HOLD COURSE", "No fuel / severe uncertainty", 0, 0, -14, -9, 0, .065f, .58f, 19));
            case 2: return new Hazard("PROPELLANT LEAK", "A valve reports unexpected propellant loss and uncertain pressure.",
                new Response("ISOLATE THE LINE", "Fuel -5 / power -8 / safe", -5, -8, 0, -3, 0, -.045f, .95f, 4),
                new Response("REMOTE RESEAL", "Fuel -2 / medium reliability", -2, -7, -3, 0, 0, -.025f, .80f, 10),
                new Response("IGNORE THE LEAK", "Fuel -18 / preserves power", -18, 0, 0, -7, 0, .09f, .60f, 14));
            case 3: return new Hazard("GUIDANCE SENSOR DRIFT", "Navigation telemetry disagrees with the predicted orbit.",
                new Response("CALIBRATE STARS", "Power -12 / stability +12", 0, -12, 0, 12, 0, -.045f, .94f, 5),
                new Response("CORRECTION BURN", "Fuel -9 / stability +22", -9, -5, 0, 22, 0, -.02f, .85f, 10),
                new Response("TRUST OLD EPHEMERIS", "Save resources / stability -13", 0, 0, 0, -13, 0, .09f, .57f, 15));
            case 4: return new Hazard("THERMAL CONTROL ALARM", "Sun-facing instruments are overheating.",
                new Response("ROTATE + COOL", "Power -12 / safe", 0, -12, -2, -3, 0, -.04f, .94f, 5),
                new Response("REDUCE PAYLOAD LOAD", "Power +8 / lose orbital time", 0, 8, -5, -4, 0, -.025f, .82f, 9),
                new Response("CONTINUE OBSERVING", "+12 data / hull -12", 0, -9, -12, 0, 12, .07f, .55f, 17));
            case 5: return new Hazard("COMMUNICATIONS BLACKOUT", "The Earth link is disrupted. A risky high-power signal could recover contact.",
                new Response("WAIT + RETUNE", "Power -8 / reliable", 0, -8, 0, -4, 0, -.04f, .94f, 4),
                new Response("HIGH-GAIN BURST", "Power -19 / +14 Earth data", 0, -19, 0, -6, -5, .01f, .76f, 12),
                new Response("TRANSMIT BLIND", "Power -13 / uncertain link", 0, -13, -7, -4, 0, .07f, .55f, 14));
            default: return new Hazard("POWER BUS ANOMALY", "A temporary electrical fault threatens the payload bus.",
                new Response("REBOOT SUBSYSTEM", "Power -9 / strong reliability", 0, -9, -1, -3, 0, -.035f, .94f, 5),
                new Response("ISOLATE SCIENCE BUS", "Power +12 / stability -8", 0, 12, 0, -8, 0, -.01f, .83f, 9),
                new Response("OVERRIDE LIMITS", "Power +19 / hull -12", 0, 19, -12, -2, 0, .075f, .56f, 17));
        }
    }

    private void ResolveIncident(int selected)
    {
        if (page != Page.Incident) return;
        Hazard incident = CreateHazard(activeIncident);
        Response r = incident.Choices[selected];
        // All three responses are real trade-offs. High reliability can cost fuel/power.
        ChangeResources(r.Fuel, r.Power, r.Hull, r.Stability, r.Data, r.Risk);
        float reliability = Mathf.Clamp(r.Reliability - risk * .13f +
            (HasInstrument(6) ? .04f : 0f), .20f, .99f);
        bool handled = UnityEngine.Random.value < reliability;
        if (!handled) ChangeResources(-2, -3, -r.Setback, -7, 0, .045f);
        if (activeIncident == 5 && selected == 1 && handled)
        {
            // High-gain burst still cannot exceed what has actually been measured.
            int transmitted = Mathf.Min(dataBuffer, 14 + (HasInstrument(4) ? 8 : 0));
            dataBuffer -= transmitted;
            downlinked += transmitted;
        }
        status = handled ? "INCIDENT MANAGED: " + r.Name :
            "PARTIAL FAILURE: " + r.Name + " caused additional spacecraft damage.";
        AddLog(status);
        if (CheckCritical()) return;
        AdvancePhase();
        SaveFlight();
    }

    private void ExtendOrbit()
    {
        if (phase != FlightPhase.Orbit || orbitTurns < allowedTurns ||
            allowedTurns >= MaxOrbitTurns) return;
        allowedTurns = Mathf.Min(MaxOrbitTurns, allowedTurns + 3);
        risk = Mathf.Clamp(risk + .035f, .025f, .82f);
        status = "Extended by 3 orbital actions. Radiation and systems exposure increased.";
        AddLog(status);
        SaveFlight();
    }

    private void FinishMission(string reason, bool deliberate)
    {
        if (page == Page.Debrief) return;
        Destination destination = destinations[selectedMission];
        missionSuccess = downlinked >= destination.ScienceGoal &&
            hull > 0 && stability > 0 && fuel >= 0 && power >= 0;
        float completionRatio = downlinked / (float)destination.ScienceGoal;
        float achieved = Mathf.Clamp01(completionRatio);
        missionReward = 0;
        if (missionSuccess)
        {
            int performance = Mathf.RoundToInt(destination.Reward *
                Mathf.Min(.30f, Mathf.Max(0f, completionRatio - 1f) * .5f));
            int discovery = completed[selectedMission] == 0 ? Mathf.RoundToInt(destination.Reward * .14f) : 0;
            int bold = doctrine == 2 ? Mathf.RoundToInt(destination.Reward * .08f) : 0;
            int safety = hull >= 75 ? Mathf.RoundToInt(destination.Reward * .05f) : 0;
            missionReward = destination.Reward + performance + discovery + bold + safety;
            completed[selectedMission]++;
            resultTitle = "MISSION ACCOMPLISHED";
            resultDetails = "Science goal met and data transmitted successfully.";
        }
        else
        {
            // Partial data has value. Players can choose a controlled early finish.
            if (deliberate && downlinked > 0)
                missionReward = Mathf.RoundToInt(destination.Reward * achieved * .28f);
            resultTitle = deliberate ? "MISSION ENDED EARLY" : "MISSION LOST";
            resultDetails = reason;
        }
        credits += missionReward;
        if (credits < 550)
        {
            int grant = 550 - credits;
            credits += grant;
            resultDetails += " Emergency recovery grant: +" + grant + " credits.";
        }
        SaveCampaign();
        PlayerPrefs.DeleteKey(SaveKey + "Flight");
        PlayerPrefs.Save();
        page = Page.Debrief;
        status = resultTitle + ": " + reason;
        AddLog(status);
    }

    private void ResetCampaign()
    {
        credits = InitialCredits;
        for (int i = 0; i < completed.Length; i++) completed[i] = 0;
        selectedMission = 0; selectedRocket = 1;
        firstInstrument = 0; secondInstrument = 5;
        ResetReview();
        PlayerPrefs.DeleteKey(SaveKey + "Flight");
        SaveCampaign();
        confirmReset = false;
        status = "Campaign reset. A new space agency starts with " + credits + " credits.";
    }

    // ------------------------ LIGHTWEIGHT BUILT-IN UI ------------------------
    // IMGUI is deliberately used here: the whole game is one .cs file and does
    // not require a Canvas, EventSystem, prefabs, or any 3D models.

    private readonly Texture2D[] planetTextures = new Texture2D[4];

    private void OnGUI()
    {
        PrepareStyles();
        float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / W, Screen.height / H));
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - W * scale) * .5f,
            (Screen.height - H * scale) * .5f, 0f), Quaternion.identity,
            new Vector3(scale, scale, 1f));

        Fill(new Rect(0, 0, W, H), new Color(.025f, .044f, .083f));
        DrawStars();
        Fill(new Rect(0, 0, W, 80), new Color(.060f, .10f, .17f, .98f));
        Fill(new Rect(0, 78, W, 2), new Color(.20f, .69f, .83f));
        Label(new Rect(22, 14, 290, 34), "MISSION DIRECTOR", headingStyle);
        Label(new Rect(24, 49, 270, 21), "SPACE AGENCY  /  FLIGHT COMMAND", tinyStyle);

        DrawProgressHeader();
        Label(new Rect(1086, 18, 174, 23), "AGENCY CREDITS", tinyStyle);
        Label(new Rect(1050, 39, 205, 32), credits.ToString("N0") + " CR", moneyStyle);

        Panel(new Rect(18, 96, 270, 687));
        Panel(new Rect(303, 96, 683, 687));
        Panel(new Rect(1001, 96, 261, 687));

        DrawSidebar();
        if (page == Page.Missions) DrawMissions();
        else if (page == Page.Build) DrawBuild();
        else if (page == Page.Plan) DrawPlan();
        else if (page == Page.Review) DrawReview();
        else if (page == Page.Operations) DrawOperations();
        else if (page == Page.Incident) DrawIncident();
        else DrawDebrief();

        DrawTelemetry();
        if (showHelp) DrawHelpOverlay();
        GUI.matrix = previous;
    }

    private void DrawStars()
    {
        for (int i = 0; i < 80; i++)
        {
            float x = 13f + (i * 197 + 37) % 1253;
            float y = 88f + (i * 131 + 19) % 696;
            float size = i % 7 == 0 ? 2.2f : 1.1f;
            Fill(new Rect(x, y, size, size), new Color(.52f, .75f, 1f, i % 3 == 0 ? .22f : .12f));
        }
    }

    private void DrawProgressHeader()
    {
        string[] labels = { "01 BRIEF", "02 BUILD", "03 PLAN", "04 REVIEW", "05 OPERATE", "06 REPORT" };
        int step = page == Page.Missions ? 0 : page == Page.Build ? 1 :
            page == Page.Plan ? 2 : page == Page.Review ? 3 :
            page == Page.Debrief ? 5 : 4;
        for (int i = 0; i < labels.Length; i++)
        {
            Rect r = new Rect(328 + i * 117, 27, 111, 31);
            Fill(r, i == step ? new Color(.11f, .39f, .49f) : new Color(.09f, .15f, .24f));
            Label(r, labels[i], i == step ? accentStyle : tinyStyle);
        }
    }

    private void DrawSidebar()
    {
        Label(new Rect(35, 115, 240, 29), "MISSION BOARD", titleStyle);
        Label(new Rect(35, 149, 235, 22), "UNLOCK THE SOLAR SYSTEM", tinyStyle);
        for (int i = 0; i < destinations.Length; i++)
        {
            Destination destination = destinations[i];
            bool unlocked = Unlocked(i);
            bool canSelect = page == Page.Missions;
            string lockLabel = unlocked ? "AVAILABLE" : "LOCKED";
            string caption = (i + 1) + "  " + destination.Name + "\n" +
                lockLabel + "  |  REWARD " + destination.Reward.ToString("N0") + " CR\n" +
                "SUCCESSES " + completed[i];
            if (Btn(new Rect(35, 185 + i * 112, 234, 98), caption,
                canSelect, false, selectedMission == i)) SelectMission(i);
        }

        Fill(new Rect(35, 646, 235, 1), new Color(.18f, .35f, .42f));
        Label(new Rect(35, 660, 236, 53),
            "MISSION FUNDS ARE LIMITED.\nSAVED BETWEEN PLAY SESSIONS.", tinyStyle);
        if (Btn(new Rect(35, 725, 233, 36), showHelp ? "HIDE HOW TO PLAY" : "HOW TO PLAY", true))
            showHelp = !showHelp;

    }

    private void DrawHelpOverlay()
    {
        Fill(new Rect(305, 100, 677, 675), new Color(.05f, .11f, .19f));
        Label(new Rect(335, 135, 615, 45), "HOW TO PLAY", headingStyle);
        Label(new Rect(335, 205, 610, 415),
            "1. PICK A MISSION. Earth funds your first lunar expedition.\n\n" +
            "2. BUILD. Fit a mission-required science instrument and one support tool. " +
            "A heavier payload needs a stronger launch vehicle.\n\n" +
            "3. PLAN. Choose your transfer route, launch window and operational doctrine. " +
            "Every plan changes costs and hazard exposure.\n\n" +
            "4. REVIEW. Run diagnostics, decide whether to repair faults and sign GO / NO-GO.\n\n" +
            "5. FLY. Handle ascent, course corrections, orbital insertion and surprise incidents.\n\n" +
            "6. OPERATE. Collect science into onboard memory. DOWNLINK that data " +
            "to Earth before declaring success. Balance fuel, power, hull and orbit stability.\n\n" +
            "7. INVEST. Earn credits, unlock new missions, and improve your strategy.", textStyle);
        if (Btn(new Rect(337, 686, 609, 57), "UNDERSTOOD  /  RETURN TO MISSION", true, true))
            showHelp = false;
    }

    private void DrawMissions()
    {
        Destination d = destinations[selectedMission];
        PageTitle("MISSION BRIEFING", "Select a destination and review its scientific objective.");
        if (planetTextures[selectedMission] != null)
            GUI.DrawTexture(new Rect(785, 193, 160, 160), planetTextures[selectedMission], ScaleMode.ScaleToFit, true);
        Label(new Rect(332, 220, 425, 44), d.Name, headingStyle);
        Label(new Rect(333, 277, 430, 67), d.Summary, textStyle);
        Divider(366);
        Label(new Rect(333, 384, 585, 24), "PRIMARY SCIENCE TASK", accentStyle);
        Label(new Rect(333, 413, 600, 60), d.Objective, textStyle);
        Label(new Rect(333, 492, 585, 24), "REQUIRED SCIENCE INSTRUMENT", accentStyle);
        Label(new Rect(333, 522, 607, 32), instruments[d.RequiredInstrument].Name, titleStyle);
        Label(new Rect(333, 571, 605, 55),
            "MISSION HAZARDS: " + d.Environment + "\n" +
            "BASE ALLOCATION: " + d.BaseCost + " CR     SUCCESS REWARD: " + d.Reward + " CR", textStyle);
        if (!Unlocked(selectedMission))
            Label(new Rect(336, 650, 600, 35),
                "LOCKED: complete " + destinations[d.UnlockAfter].Name + " first.", warningStyle);
        else
            Label(new Rect(335, 651, 613, 36), "READY FOR CONCEPT DEVELOPMENT.", accentStyle);
        if (Btn(new Rect(331, 704, 629, 61),
            Unlocked(selectedMission) ? "DESIGN SPACECRAFT  >" : "DESTINATION LOCKED",
            Unlocked(selectedMission), true))
        {
            firstInstrument = d.RequiredInstrument;
            if (secondInstrument == firstInstrument) secondInstrument = 4;
            ResetReview(); page = Page.Build;
            status = "Select your launch vehicle and two payload instruments.";
        }
    }

    private void DrawBuild()
    {
        PageTitle("SPACECRAFT ENGINEERING", "Choose one launch vehicle and exactly two instruments.");
        Label(new Rect(330, 190, 620, 25), "01  SELECT LAUNCH VEHICLE", accentStyle);
        for (int i = 0; i < rockets.Length; i++)
        {
            Rocket r = rockets[i];
            string label = r.Name + "\n" + r.Details + "\n" +
                "CAP " + r.Capacity + "  |  +" + r.Cost + " CR";
            if (Btn(new Rect(330 + i * 212, 222, 204, 94), label, true, false, selectedRocket == i))
            { selectedRocket = i; ResetReview(); status = "Launch vehicle: " + r.Name; }
        }
        Label(new Rect(330, 331, 620, 23), "02  SELECT PAYLOAD  /  CLICK SLOT THEN INSTRUMENT", accentStyle);
        for (int i = 0; i < instruments.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;
            Instrument instrument = instruments[i];
            string text = instrument.Name + "   " + instrument.Cost + " CR  /  MASS " + instrument.Mass + "\n" +
                instrument.Details;
            if (Btn(new Rect(330 + column * 320, 362 + row * 70, 309, 62), text,
                true, false, HasInstrument(i))) Equip(i);
        }
        Label(new Rect(331, 652, 195, 23), "ACTIVE PAYLOAD SLOT:", tinyStyle);
        if (Btn(new Rect(482, 645, 225, 40), "SLOT 1: " + instruments[firstInstrument].Name,
            true, false, selectedSlot == 0)) selectedSlot = 0;
        if (Btn(new Rect(714, 645, 246, 40), "SLOT 2: " + instruments[secondInstrument].Name,
            true, false, selectedSlot == 1)) selectedSlot = 1;
        Label(new Rect(332, 693, 609, 23),
            "PAYLOAD " + PayloadMass + "/" + rockets[selectedRocket].Capacity +
            " MASS UNITS   |   ESTIMATED COST " + MissionCost + " CR", PlanValid ? accentStyle : warningStyle);
        if (Btn(new Rect(333, 729, 175, 38), "<  MISSION BRIEF", true)) page = Page.Missions;
        if (Btn(new Rect(521, 725, 440, 45), "CONTINUE TO LAUNCH PLAN  >", PlanValid, true))
        { page = Page.Plan; ResetReview(); }
    }

    private void DrawPlan()
    {
        PageTitle("LAUNCH & TRANSFER PLANNING", "Choose risk, speed and cost before committing funds.");
        Label(new Rect(330, 190, 610, 25), "01  TRAJECTORY: FUEL VS EXPOSURE", accentStyle);
        string[] routeOptions = {
            "EFFICIENT TRANSFER\nSave ~9 fuel\nLonger exposure + risk",
            "BALANCED TRANSFER\nNominal fuel / duration\nPredictable operations",
            "FAST DIRECT ROUTE\nBurn ~13 extra fuel\nLess exposure + risk"
        };
        DrawOptionRow(routeOptions, trajectory, 220, 91, delegate(int i)
        { trajectory = i; ResetReview(); });

        Label(new Rect(330, 332, 620, 24), "02  LAUNCH WINDOW: WEATHER VS COST", accentStyle);
        string[] windowOptions = {
            "LAUNCH NOW\nNo schedule cost\nAccept weather forecast",
            "WAIT FOR CLEARER SKY\n+65 credits\nReduce weather severity",
            "PRECISION WINDOW\n+135 credits\nAvoid poor launch weather"
        };
        DrawOptionRow(windowOptions, launchWindow, 360, 91, delegate(int i)
        { launchWindow = i; ResetReview(); });

        Label(new Rect(330, 472, 622, 24), "03  MISSION DOCTRINE: SCIENCE VS SAFETY", accentStyle);
        string[] doctrineOptions = {
            "CAUTIOUS\nLower event risk\n-13% science production",
            "BALANCED\nNormal risk\nNormal science production",
            "BOLD SCIENCE\nHigher incident risk\n+24% science / +8% payout"
        };
        DrawOptionRow(doctrineOptions, doctrine, 500, 91, delegate(int i)
        { doctrine = i; ResetReview(); });

        Label(new Rect(332, 615, 611, 59),
            "WEATHER: " + WeatherName() + "    SOLAR ACTIVITY: " + SolarName() + "\n" +
            "PROJECT COST: " + MissionCost.ToString("N0") + " CR    /    AVAILABLE: " + credits.ToString("N0") + " CR",
            PlanValid ? textStyle : warningStyle);
        Label(new Rect(333, 679, 615, 24), PlanProblem(), PlanValid ? accentStyle : warningStyle);
        if (Btn(new Rect(333, 728, 178, 39), "<  SPACECRAFT", true)) page = Page.Build;
        if (Btn(new Rect(524, 723, 437, 47), "OPEN PRE-LAUNCH REVIEW  >", PlanValid, true))
        { page = Page.Review; status = "Prelaunch review board convened."; }
    }

    private void DrawOptionRow(string[] options, int selected, float y, float height, Action<int> apply)
    {
        for (int i = 0; i < options.Length; i++)
            if (Btn(new Rect(330 + i * 212, y, 204, height), options[i], true, false, selected == i))
                apply(i);
    }

    private void DrawReview()
    {
        PageTitle("FLIGHT READINESS REVIEW", "The last opportunity to repair, scrub or authorize launch.");
        Destination d = destinations[selectedMission];
        Fill(new Rect(329, 189, 630, 98), new Color(.065f, .14f, .22f));
        Label(new Rect(344, 202, 593, 69),
            "TARGET: " + d.Name + "     |     PAYLOAD: " + PayloadMass + "/" + rockets[selectedRocket].Capacity + "\n" +
            "TOTAL AUTHORIZATION: " + MissionCost.ToString("N0") + " CR\n" +
            "ESTIMATED INCIDENT EXPOSURE: " + Mathf.RoundToInt(PredictedRisk() * 100f) + "%",
            textStyle);
        Label(new Rect(331, 307, 614, 24), "01  SYSTEM DIAGNOSTICS", accentStyle);
        if (!diagnosticDone)
        {
            if (Btn(new Rect(330, 341, 630, 51), "RUN PRE-LAUNCH DIAGNOSTIC  (+40 CR)",
                PlanValid, true)) RunDiagnostic();
        }
        else
        {
            Label(new Rect(337, 343, 612, 45),
                fault == -1 ? "PASS: All reported faults resolved." :
                "WARNING: " + FaultName().ToUpper() + "  (+16% risk if ignored)",
                fault == -1 ? accentStyle : warningStyle);
            if (fault >= 0)
            {
                int price = fault == 0 ? 110 : (fault == 1 ? 85 : 75);
                if (Btn(new Rect(332, 392, 626, 44),
                    "REPAIR FAULT  /  +" + price + " CR   (OR ACCEPT RISK)",
                    MissionCost + price <= credits, false)) RepairFault();
            }
        }

        Label(new Rect(331, 454, 610, 25), "02  FLIGHT DIRECTOR GO / NO-GO CHECKLIST", accentStyle);
        string[] checklist = {
            "Payload, rocket capacity and scientific objective verified",
            "Power, communications and navigation plan reviewed",
            "Weather, mission risks and contingency strategy accepted"
        };
        for (int i = 0; i < checklist.Length; i++)
        {
            if (Btn(new Rect(332, 488 + i * 51, 626, 44),
                (signoffs[i] ? "[GO]  " : "[  ]  ") + checklist[i],
                diagnosticDone, false, signoffs[i])) signoffs[i] = !signoffs[i];
        }

        bool authorized = diagnosticDone && PlanValid;
        for (int i = 0; i < signoffs.Length; i++) authorized &= signoffs[i];
        Label(new Rect(334, 646, 617, 53),
            authorized ? "FLIGHT STATUS: GO. The launch cost will be deducted once." :
                "NO-GO. Complete diagnostic, budget review and all three signoffs.",
            authorized ? accentStyle : warningStyle);
        if (Btn(new Rect(332, 721, 194, 48), "<  EDIT LAUNCH PLAN", true)) page = Page.Plan;
        if (Btn(new Rect(540, 716, 420, 54),
            authorized ? "AUTHORIZE LAUNCH   /   GO  >" : "LAUNCH AUTHORIZATION LOCKED",
            authorized, true)) BeginFlight();
    }

    private void DrawOperations()
    {
        string stage = phase == FlightPhase.Ascent ? "LAUNCH ASCENT" :
            phase == FlightPhase.Transfer ? "INTERPLANETARY TRANSFER" :
            phase == FlightPhase.Insertion ? "ORBITAL INSERTION" : "ORBITAL OPERATIONS";
        PageTitle(stage, destinations[selectedMission].Name + "  /  " + rockets[selectedRocket].Name);
        if (phase != FlightPhase.Orbit)
        {
            string prompt = phase == FlightPhase.Ascent ?
                "T-minus zero. Choose your ascent strategy. Weather and spacecraft mass have consequences." :
                phase == FlightPhase.Transfer ?
                "The transfer trajectory is evolving. Do you use fuel to correct it, coast to save resources, or collect extra data?" :
                "Your probe has reached its target. Which orbit best serves your science objective?";
            Label(new Rect(330, 217, 622, 100), prompt, titleStyle);
            string[] options;
            if (phase == FlightPhase.Ascent)
                options = new[] {
                    "CAUTIOUS ASCENT     /     -9 fuel, -4 power\nLower strain and lower incident exposure",
                    "NOMINAL ASCENT     /     -13 fuel, -7 power\nBalanced thrust and mission margins",
                    "RAPID ASCENT     /     -19 fuel, -8 power, -3 hull\nHigh thrust, greater incident exposure"
                };
            else if (phase == FlightPhase.Transfer)
                options = new[] {
                    "PRECISION CORRECTION     /     -14 fuel, -6 power\nImprove stability and lower risk",
                    "CONSERVATIVE COAST     /     -4 fuel, +4 power\nSave propellant, but drift off the planned course",
                    "CRUISE SCIENCE     /     -8 fuel, -11 power\nCollect 10 early science data; accept some risk"
                };
            else
                options = new[] {
                    "LOW SCIENCE ORBIT     /     -23 fuel, -10 power\n+30% scanning data, more orbital exposure",
                    "MID STABLE ORBIT     /     -16 fuel, -7 power\nStable orbit, balanced scan and communication",
                    "HIGH SAFE ORBIT     /     -11 fuel, -5 power\nSafer operations and downlinks; weaker scans"
                };
            for (int i = 0; i < options.Length; i++)
            {
                int neededFuel = phase == FlightPhase.Ascent ? new[] { 9, 13, 19 }[i] :
                    phase == FlightPhase.Transfer ? new[] { 14, 4, 8 }[i] :
                    new[] { 23, 16, 11 }[i];
                int neededPower = phase == FlightPhase.Ascent ? new[] { 4, 7, 8 }[i] :
                    phase == FlightPhase.Transfer ? (i == 1 ? 0 : i == 0 ? 6 : 11) :
                    new[] { 10, 7, 5 }[i];
                if (Btn(new Rect(332, 345 + 104 * i, 625, 87),
                    (i + 1) + "  " + options[i], fuel >= neededFuel && power >= neededPower))
                    FlightChoice(i);
            }
            Label(new Rect(334, 670, 612, 42),
                "FLIGHT RULE: There is no universally correct button. Outcomes depend on " +
                "available resources, your mission plan and random incidents.", mutedStyle);
            if (Btn(new Rect(772, 723, 183, 40), "ABORT / SALVAGE", true))
                FinishMission("Flight director aborted before science operations could be completed.", true);
        }
        else
        {
            Destination d = destinations[selectedMission];
            Label(new Rect(331, 200, 615, 31),
                "ORBIT ACTION " + orbitTurns + " / " + allowedTurns +
                "     |     ALTITUDE: " + (orbitAltitude == 0 ? "LOW" : orbitAltitude == 1 ? "MEDIUM" : "HIGH"),
                accentStyle);
            Fill(new Rect(331, 242, 620, 90), new Color(.065f, .14f, .22f));
            Label(new Rect(345, 251, 591, 70),
                "SCIENCE TARGET: " + d.ScienceGoal + " data downlinked to Earth\n" +
                "ONBOARD STORAGE: " + dataBuffer + "/" + StorageCapacity +
                "    |    RECEIVED ON EARTH: " + downlinked + " / " + d.ScienceGoal,
                textStyle);
            string[] actions = {
                "PAYLOAD SCAN\n-2 fuel / -12 power\n+" + ScienceYield(false) + " stored data",
                "DEEP SCIENCE SCAN\n-5 fuel / -23 power\n+" + ScienceYield(true) + " data; higher risk",
                "DOWNLINK TO EARTH\n-2 fuel / -13 power\nUp to " + DownlinkRate() + " data sent",
                "RECHARGE + COOL\n-1 fuel / +" + (HasInstrument(5) ? 37 : 27) + " power\nNo science collected",
                "STATIONKEEPING BURN\n-13 fuel / -6 power\n+31 stability; safer orbit",
                "PROTECTIVE SAFE MODE\n-1 fuel / +11 power\nLess risk; lose time"
            };
            for (int i = 0; i < actions.Length; i++)
            {
                int x = 330 + (i % 2) * 319;
                int y = 350 + (i / 2) * 102;
                bool affordable = orbitTurns < allowedTurns;
                if (i == 0) affordable &= fuel >= 2 && power >= 12 && dataBuffer < StorageCapacity;
                if (i == 1) affordable &= fuel >= 5 && power >= 23 && dataBuffer < StorageCapacity;
                if (i == 2) affordable &= fuel >= 2 && power >= 13 && dataBuffer > 0;
                if (i == 3) affordable &= fuel >= 1;
                if (i == 4) affordable &= fuel >= 13 && power >= 6;
                if (i == 5) affordable &= fuel >= 1;
                if (Btn(new Rect(x, y, 310, 88), actions[i], affordable)) OrbitChoice(i);
            }
            bool goalMet = downlinked >= d.ScienceGoal;
            Label(new Rect(495, 670, 460, 32),
                goalMet ? "SCIENCE COMPLETE: You can finalize now or gather bonus data." :
                    "SCIENCE PENDING: Scanned data only counts once DOWNLINKED.",
                goalMet ? accentStyle : warningStyle);
            if (Btn(new Rect(331, 675, 150, 30), "ABORT / SALVAGE", true))
                FinishMission("Mission Control ordered an emergency withdrawal.", true);
            if (orbitTurns >= allowedTurns && allowedTurns < MaxOrbitTurns &&
                Btn(new Rect(333, 714, 292, 52), "EXTEND ORBIT BY 3 TURNS  +RISK", true)) ExtendOrbit();
            if (Btn(new Rect(640, 714, 317, 52),
                goalMet ? "COMPLETE MISSION  >" : "END MISSION / PARTIAL DATA  >",
                orbitTurns >= 3, goalMet))
            {
                FinishMission(goalMet ? "All science objectives achieved." :
                    "Mission Control ordered an early finish before the data target was reached.", true);
            }
        }
    }

    private void DrawIncident()
    {
        Hazard h = CreateHazard(activeIncident);
        PageTitle("UNEXPECTED MISSION INCIDENT", "The situation changed after your latest action.");
        Fill(new Rect(327, 208, 637, 137), new Color(.22f, .105f, .105f));
        Label(new Rect(341, 221, 610, 37), "!  " + h.Title, headingStyle);
        Label(new Rect(343, 271, 602, 63), h.Situation, textStyle);
        Label(new Rect(332, 370, 607, 28),
            "FLIGHT CONTROLLER: Choose a response. Reliability is approximate.", accentStyle);
        for (int i = 0; i < h.Choices.Length; i++)
        {
            Response r = h.Choices[i];
            int likely = Mathf.RoundToInt(Mathf.Clamp(r.Reliability - risk * .13f +
                (HasInstrument(6) ? .04f : 0f), .2f, .99f) * 100f);
            string caption = (i + 1) + "  " + r.Name + "    /    ~" + likely + "% RELIABLE\n" + r.Preview;
            bool affordable = fuel + r.Fuel >= 0 && power + r.Power >= 0;
            if (Btn(new Rect(332, 414 + i * 99, 626, 84), caption, affordable)) ResolveIncident(i);
        }
        Label(new Rect(337, 709, 600, 31),
            "A high-risk response may yield more science or save fuel, but a failed " +
            "maneuver can also damage systems and destabilize your orbit.", mutedStyle);
        if (Btn(new Rect(752, 746, 203, 29), "EMERGENCY ABORT", true))
            FinishMission("Mission Control chose to terminate during an incident.", true);
    }

    private void DrawDebrief()
    {
        PageTitle("MISSION DEBRIEF", destinations[selectedMission].Name + "  /  Flight director's report");
        Fill(new Rect(330, 218, 629, 106), missionSuccess ?
            new Color(.075f, .26f, .24f) : new Color(.24f, .12f, .15f));
        Label(new Rect(350, 235, 584, 45), resultTitle, headingStyle);
        Label(new Rect(352, 286, 576, 32),
            missionSuccess ? "Science delivered. Your agency reputation grows." :
                "The mission did not fulfill every scientific objective.", textStyle);
        Label(new Rect(338, 345, 610, 33), "SCIENCE RETURN", accentStyle);
        Label(new Rect(339, 381, 595, 40),
            downlinked + " / " + destinations[selectedMission].ScienceGoal +
            " DATA POINTS RECEIVED ON EARTH", titleStyle);
        Meter(new Rect(338, 431, 612, 15),
            downlinked / (float)destinations[selectedMission].ScienceGoal,
            new Color(.31f, .82f, .75f));
        Label(new Rect(339, 464, 608, 37), "MISSION INVESTMENT:  -" + missionInvestment + " CR", textStyle);
        Label(new Rect(339, 511, 608, 39), "MISSION PAYOUT:  +" + missionReward + " CR", moneyStyle);
        Label(new Rect(339, 568, 608, 55),
            "NET MISSION RESULT:  " + (missionReward - missionInvestment) + " CR\n" +
            "REMAINING AGENCY BALANCE:  " + credits.ToString("N0") + " CR", textStyle);
        Label(new Rect(338, 646, 605, 49), resultDetails, mutedStyle);
        if (Btn(new Rect(331, 716, 629, 56), "RETURN TO MISSION BOARD  >", true, true))
        {
            page = Page.Missions;
            SelectMission(selectedMission);
            status = "Review the campaign board and plan your next expedition.";
        }
    }

    private void DrawTelemetry()
    {
        Label(new Rect(1019, 117, 229, 27),
            page == Page.Operations || page == Page.Incident ? "LIVE TELEMETRY" : "MISSION INTELLIGENCE",
            titleStyle);
        if (planetTextures[selectedMission] != null)
            GUI.DrawTexture(new Rect(1063, 164, 142, 142),
                planetTextures[selectedMission], ScaleMode.ScaleToFit, true);
        if (page == Page.Operations || page == Page.Incident)
        {
            DrawOrbitRadar(1133, 235, 83);
            Metric("FUEL", fuel, 160, 330, new Color(.36f, .77f, .92f));
            Metric("POWER", power, 165, 374, new Color(.94f, .82f, .36f));
            Metric("INTEGRITY", hull, 145, 418, new Color(.43f, .88f, .68f));
            Metric("ORBIT STABILITY", stability, 100, 462, new Color(.90f, .56f, .40f));
            Label(new Rect(1018, 515, 225, 31),
                "HAZARD EXPOSURE " + Mathf.RoundToInt(risk * 100f) + "%", warningStyle);
            Fill(new Rect(1019, 558, 226, 1), new Color(.18f, .35f, .43f));
            Label(new Rect(1019, 568, 220, 25), "FLIGHT LOG / RECENT", accentStyle);
            for (int i = 0; i < Mathf.Min(4, log.Count); i++)
                Label(new Rect(1020, 595 + i * 42, 224, 39),
                    "• " + log[i], tinyStyle);
        }
        else
        {
            Label(new Rect(1018, 325, 225, 28), "SITUATIONAL FORECAST", accentStyle);
            Label(new Rect(1018, 360, 221, 81),
                "LAUNCH WEATHER: " + WeatherName() + "\n" +
                "SOLAR ACTIVITY: " + SolarName() + "\n" +
                "HAZARDS: " + Mathf.RoundToInt(destinations[selectedMission].Danger * 100f) + "% BASE",
                smallStyle);
            Fill(new Rect(1018, 453, 226, 1), new Color(.18f, .35f, .43f));
            Label(new Rect(1018, 467, 227, 25), "ENGINEERING STATUS", accentStyle);
            Label(new Rect(1018, 507, 223, 99),
                "ROCKET: " + rockets[selectedRocket].Name + "\n" +
                "MASS: " + PayloadMass + "/" + rockets[selectedRocket].Capacity + "\n" +
                "PROPOSED COST: " + MissionCost + " CR\n" +
                "BUDGET: " + credits + " CR", smallStyle);
            Label(new Rect(1018, 638, 228, 92), status, mutedStyle);
            if (page == Page.Missions)
            {
                if (Btn(new Rect(1019, 726, 225, 35),
                    confirmReset ? "CONFIRM RESET?" : "RESET CAMPAIGN", true))
                {
                    if (!confirmReset) confirmReset = true;
                    else ResetCampaign();
                }
            }
        }
    }

    private void DrawOrbitRadar(float centerX, float centerY, float radius)
    {
        for (int i = 0; i < 42; i++)
        {
            float angle = i * Mathf.PI * 2f / 42f;
            Fill(new Rect(centerX + Mathf.Cos(angle) * radius,
                centerY + Mathf.Sin(angle) * radius * .58f, 2f, 2f),
                new Color(.34f, .69f, .79f, .7f));
        }
        float progress = (orbitTurns * .74f + (int)phase * 1.15f) % (Mathf.PI * 2f);
        Fill(new Rect(centerX + Mathf.Cos(progress) * radius - 4f,
            centerY + Mathf.Sin(progress) * radius * .58f - 4f, 8f, 8f),
            new Color(.54f, 1f, .82f));
    }

    private void Metric(string name, int value, int maximum, float y, Color color)
    {
        Label(new Rect(1018, y, 224, 19), name + "   " + Mathf.Max(0, value), smallStyle);
        Meter(new Rect(1019, y + 23, 225, 9), value / (float)maximum, color);
    }

    private void Meter(Rect r, float portion, Color color)
    {
        Fill(r, new Color(.13f, .23f, .33f));
        Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(portion), r.height), color);
    }

    private void PageTitle(string title, string subtitle)
    {
        Label(new Rect(329, 115, 627, 40), title, headingStyle);
        Label(new Rect(331, 159, 616, 32), subtitle, mutedStyle);
    }

    private void Divider(float y) { Fill(new Rect(330, y, 630, 1), new Color(.21f, .43f, .51f)); }

    private void Panel(Rect r)
    {
        Fill(r, new Color(.065f, .103f, .169f, .98f));
        Fill(new Rect(r.x, r.y, r.width, 1f), new Color(.17f, .33f, .46f));
        Fill(new Rect(r.x, r.y, 1f, r.height), new Color(.15f, .28f, .38f));
    }

    private void Fill(Rect r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void Label(Rect r, string value, GUIStyle style)
    { GUI.Label(r, value, style); }

    private bool Btn(Rect r, string value, bool enabled, bool primary = false, bool selected = false)
    {
        bool old = GUI.enabled;
        GUI.enabled = old && enabled;
        bool pressed = GUI.Button(r, value,
            !enabled ? disabledStyle : primary ? primaryStyle :
            selected ? selectedButtonStyle : buttonStyle);
        GUI.enabled = old;
        return pressed && enabled;
    }

    private Texture2D SolidTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    // These four planet thumbnails are generated mathematically once at startup.
    // No imported graphics, downloads, or GPU-heavy shaders are needed.
    private Texture2D PlanetTexture(int destination)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - 63.5f) / 59f;
                float ny = (y - 63.5f) / 59f;
                float radiusSq = nx * nx + ny * ny;
                if (radiusSq > 1f) { pixels[y * size + x] = Color.clear; continue; }
                float light = Mathf.Clamp01(.45f + .55f * (-nx * .75f + ny * .28f + .60f));
                float pattern = Mathf.Sin(nx * 9.3f + ny * 14.2f) * Mathf.Cos(ny * 12f - nx * 5f);
                Color color;
                if (destination == 0)
                    color = pattern > .20f ? new Color(.19f, .68f, .49f) :
                        pattern < -.45f ? new Color(.82f, .89f, .91f) : new Color(.15f, .44f, .79f);
                else if (destination == 1)
                    color = new Color(.58f, .61f, .68f) * (pattern > .5f ? .72f : 1f);
                else if (destination == 2)
                    color = new Color(.81f, .38f, .24f) * (.89f + .13f * pattern);
                else
                {
                    float band = Mathf.Sin(ny * 35f + nx * 2.2f);
                    color = Color.Lerp(new Color(.79f, .55f, .37f),
                        new Color(.96f, .84f, .66f), Mathf.Clamp01(.5f + band * .46f));
                    if (nx > .16f && nx < .65f && ny > -.30f && ny < -.11f)
                        color = Color.Lerp(color, new Color(.77f, .31f, .22f), .55f);
                }
                float edge = Mathf.Clamp01((1f - Mathf.Sqrt(radiusSq)) * 24f);
                color *= light;
                color.a = edge;
                pixels[y * size + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private GUIStyle Style(int size, Color color, bool bold = false)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = size;
        style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        style.normal.textColor = color;
        style.wordWrap = true;
        style.alignment = TextAnchor.UpperLeft;
        return style;
    }

    private void PrepareStyles()
    {
        if (headingStyle != null) return;
        Color white = new Color(.91f, .97f, 1f);
        Color pale = new Color(.73f, .84f, .92f);
        Color turquoise = new Color(.51f, .95f, .84f);
        headingStyle = Style(27, white, true);
        titleStyle = Style(20, white, true);
        textStyle = Style(17, pale);
        smallStyle = Style(14, pale);
        tinyStyle = Style(12, new Color(.69f, .81f, .87f), true);
        mutedStyle = Style(15, new Color(.63f, .79f, .87f));
        accentStyle = Style(15, turquoise, true);
        warningStyle = Style(16, new Color(1f, .70f, .47f), true);
        moneyStyle = Style(23, turquoise, true);
        moneyStyle.alignment = TextAnchor.MiddleRight;

        normalTexture = SolidTexture(new Color(.11f, .19f, .29f));
        selectedTexture = SolidTexture(new Color(.13f, .35f, .45f));
        primaryTexture = SolidTexture(new Color(.37f, .86f, .80f));
        mutedTexture = SolidTexture(new Color(.085f, .14f, .21f));

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.wordWrap = true;
        buttonStyle.alignment = TextAnchor.MiddleLeft;
        buttonStyle.padding = new RectOffset(14, 9, 5, 5);
        buttonStyle.normal.background = normalTexture;
        buttonStyle.hover.background = selectedTexture;
        buttonStyle.active.background = selectedTexture;
        buttonStyle.focused.background = selectedTexture;
        buttonStyle.normal.textColor = white;
        buttonStyle.hover.textColor = white;
        buttonStyle.active.textColor = white;

        selectedButtonStyle = new GUIStyle(buttonStyle);
        selectedButtonStyle.normal.background = selectedTexture;
        primaryStyle = new GUIStyle(buttonStyle);
        primaryStyle.normal.background = primaryTexture;
        primaryStyle.hover.background = primaryTexture;
        primaryStyle.active.background = primaryTexture;
        primaryStyle.normal.textColor = new Color(.03f, .12f, .15f);
        primaryStyle.hover.textColor = new Color(.03f, .12f, .15f);
        primaryStyle.active.textColor = new Color(.03f, .12f, .15f);
        primaryStyle.fontSize = 16;
        primaryStyle.alignment = TextAnchor.MiddleCenter;
        disabledStyle = new GUIStyle(buttonStyle);
        disabledStyle.normal.background = mutedTexture;
        disabledStyle.normal.textColor = new Color(.37f, .47f, .55f);

        for (int i = 0; i < planetTextures.Length; i++) planetTextures[i] = PlanetTexture(i);
    }

    private void OnDestroy()
    {
        if (normalTexture != null) Destroy(normalTexture);
        if (selectedTexture != null) Destroy(selectedTexture);
        if (primaryTexture != null) Destroy(primaryTexture);
        if (mutedTexture != null) Destroy(mutedTexture);
        for (int i = 0; i < planetTextures.Length; i++)
            if (planetTextures[i] != null) Destroy(planetTextures[i]);
    }
}
