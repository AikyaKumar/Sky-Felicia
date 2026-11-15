using System;
using System.Collections.Generic;

namespace OrbitalFrontier.Runtime
{
    static class ExpeditionGlossary
    {
        internal sealed class Entry
        {
            public readonly string title, text, url, link;

            public Entry(string title, string text, string url = "", string link = "")
            {
                this.title = title;
                this.text = text;
                this.url = url;
                this.link = link;
            }
        }

        const string RocketEquation = "https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/ideal-rocket-equation/";
        const string PlanetaryFacts = "https://nssdc.gsfc.nasa.gov/planetary/factsheet/";
        const string OrbitalParameters = "https://ssd.jpl.nasa.gov/astro_par.html";
        static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>
        {
            ["world"] = new Entry("Destination body", "Choose the Moon or Mars scenario. The atlas reports sourced physical data for the body, while mission trajectories, equipment, and operations are scenario simulations.", PlanetaryFacts, "NASA Planetary Fact Sheet"),
            ["planetaryAtlas"] = new Entry("Planetary atlas", "Compare the selected body’s physical data and regional map. The game uses a spherical reference body; terrain details come from the linked regional dataset.", PlanetaryFacts, "NASA Planetary Fact Sheet"),
            ["flightPlan"] = new Entry("Flight plan", "Defines the objective, launch class, orbit, transfer profile, difficulty, and available operations time. These choices drive the live resource estimates and readiness checks."),
            ["objective"] = new Entry("Mission objective", "Sets the mission work and hardware and data requirements. Surface science needs landing and field records; orbital survey collects two products in orbit; systems recovery adds a repair task."),
            ["launcher"] = new Entry("Launch vehicle", "A fictional launcher class with a payload limit and mission cost. The selected destination and orbit profile change how much mass it can deliver."),
            ["orbitProfile"] = new Entry("Destination orbit", "The target orbit altitude around the destination. Higher orbit changes coverage and link windows, but reduces launcher delivery capacity and can increase maneuver needs.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["transfer"] = new Entry("Transfer profile", "The simulated cruise from Earth departure to arrival. Fast transfer saves time but increases injection and capture energy demands.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["difficulty"] = new Entry("Difficulty", "Guided mode automates terminal-descent thrust and lateral corrections. Expert mode leaves those controls to you and shortens the available operations window."),
            ["operationsWindow"] = new Entry("Operations window", "The available mission time for surface or orbital work. Extended windows cost more mission credits; Expert mode reduces the selected time."),
            ["missionResources"] = new Entry("Mission resources", "Live estimates compare the design with the mission limits. Values update with the selected destination, plan, launcher, and hardware. Credits and hardware statistics are game parameters."),
            ["flightReadiness"] = new Entry("Flight readiness", "A ready design meets the modeled payload, structure, budget, maneuver, crew-provision, and science-storage checks. Each design hold explains its specific requirement."),
            ["shipAssembly"] = new Entry("Ship assembly", "Select a mount to inspect or change its installed component. Only parts rated for that mount are offered."),
            ["componentInspector"] = new Entry("Component inspector", "Shows the selected part’s role and engineering values. Component figures are game parameters with real units, not flight-hardware specifications."),
            ["flightLog"] = new Entry("Flight log", "Tracks mission stages, the current objective, remaining operations time, and crew condition."),
            ["missionRecord"] = new Entry("Mission record", "Summarizes the outcome, returned science, propellant, elapsed time, and landing measurements. Scores are game evaluations, not mission certification."),
            ["stageSeparation"] = new Entry("Stages separated", "The number of launch-vehicle stages discarded after their propellant or role is complete."),
            ["massCost"] = new Entry("Mass / cost", "Mass is the component’s weight contribution in kilograms; cost is its fictional mission-credit value. Mass affects launch and maneuver limits, while cost counts against the mission budget."),
            ["wetMass"] = new Entry("Wet mass", "Spacecraft dry mass plus loaded propellant. It must fit both launcher delivery capacity and the pressure-hull rating.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["dryMass"] = new Entry("Dry mass", "The assembled spacecraft mass without propellant. The game uses dry and wet mass to estimate maneuver reserve.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["missionCost"] = new Entry("Mission cost", "A fictional mission-credit total for the launcher, parts, transfer and orbit options, and operations window. It must not exceed the mission budget."),
            ["maneuverReserve"] = new Entry("Maneuver reserve (Δv)", "The ideal velocity change the loaded propellant can provide, estimated with the rocket equation. The reserve must cover the planned arrival, descent, and contingency maneuvers.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["surfaceTwr"] = new Entry("Surface thrust-to-weight ratio (TWR)", "Engine thrust divided by the spacecraft’s weight at the surface. It is dimensionless. Surface missions require a wet TWR of at least 1.70 in this simulation."),
            ["powerPeakLoad"] = new Entry("Power / peak load", "Generator output compared with the estimated peak electrical demand. When active demand exceeds generation, the battery supplies the shortfall."),
            ["batteryCapacity"] = new Entry("Battery capacity", "Stored electrical energy, shown in watt-hours (Wh). It buffers loads when generation is insufficient; it is not a power-generation rate."),
            ["dataStorage"] = new Entry("Data storage", "Recorder capacity in megabytes (MB). It must hold the science products required by the selected objective."),
            ["downlinkRate"] = new Entry("Downlink rate", "The modeled radio data-transfer rate in megabytes per second (MB/s). Higher rates shorten the time needed to send recorded science."),
            ["crewProvisions"] = new Entry("Crew provisions", "Installed life-support duration in days. Surface missions require enough for the planned cruise plus a two-day reserve."),
            ["plannedTransfer"] = new Entry("Planned transfer", "The scenario’s estimated cruise duration. Flight time is compressed for play and does not model a real launch window."),
            ["spareLineUnits"] = new Entry("Spare line units", "The number of repair spares carried. Required repairs consume a spare after the crew runs diagnostics."),
            ["coastOrbit"] = new Entry("Coast orbit", "The selected target orbit altitude in kilometers. Periapsis and apoapsis readings describe the orbit once captured.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["fieldPacket"] = new Entry("Field packet", "The estimated data volume of one science record in megabytes. Recorder capacity must fit all required packets."),
            ["destinationPower"] = new Entry("Destination power", "Estimated electrical generation at the destination. Solar output is adjusted for distance and scenario conditions; compare it with continuous system load."),
            ["mass"] = new Entry("Mass", "The component’s mass in kilograms. It contributes to dry mass and, with propellant, to wet mass."),
            ["cost"] = new Entry("Cost", "The fictional mission-credit cost assigned to this component. It contributes to the mission budget."),
            ["propellant"] = new Entry("Propellant", "Reaction mass expelled by the engine to change spacecraft velocity. Burning it reduces vehicle mass and the remaining maneuver reserve.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["thrust"] = new Entry("Thrust", "The force produced by an engine, measured in newtons (N). Whether it can lift or slow a vehicle depends on thrust relative to vehicle weight."),
            ["thrustIsp"] = new Entry("Thrust / specific impulse (Isp)", "Thrust is the engine force, measured in newtons (N). Specific impulse is propulsion efficiency in seconds; higher Isp gives more ideal velocity change from the same propellant and mass ratio.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["specificImpulse"] = new Entry("Specific impulse (Isp)", "A measure of propulsion efficiency, expressed in seconds. In the ideal rocket equation, higher Isp means more velocity change from the same propellant and mass ratio.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["generation"] = new Entry("Power generation", "Electrical power supplied by the generator, measured in watts (W). Destination output can differ from its nominal rating."),
            ["battery"] = new Entry("Battery", "Stored electrical energy, measured in watt-hours (Wh). A battery can cover power shortfalls but cannot create energy."),
            ["draw"] = new Entry("Electrical draw", "The electrical power a component consumes while operating, measured in watts (W). Instruments can draw from the battery when generation cannot meet the load."),
            ["pressureHull"] = new Entry("Pressure hull", "The crew-cabin structure. Its mass rating is checked against spacecraft wet mass."),
            ["tank"] = new Entry("Propellant tank", "Stores the engine’s propellant. Tank contents add to launch mass and maneuver reserve.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["mainEngine"] = new Entry("Main engine", "Produces thrust for major maneuvers and terminal descent. Propellant use changes vehicle mass during a burn.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["powerGeneration"] = new Entry("Power generation", "Supplies electrical power to spacecraft systems and instruments. Generator output and component loads are compared in the live resource panel."),
            ["batteryBank"] = new Entry("Battery bank", "Stores electrical energy for periods when the generator cannot meet system or instrument demand."),
            ["landingSensor"] = new Entry("Landing sensor", "Provides the simulated range and terrain measurements used to assess the landing corridor. The terrain is sampled from the cited regional elevation dataset."),
            ["antenna"] = new Entry("Antenna", "Enables the modeled radio link for returning science data. Transmission also needs radio power and a clear line of sight to Earth."),
            ["landingGear"] = new Entry("Landing gear", "Supports the vehicle at touchdown. The simulation also checks the hardware against measured terrain slope."),
            ["lifeSupport"] = new Entry("Life support", "The installed life-support hardware and station maintain modeled crew provisions and cabin support. Oxygen, cabin pressure, suit charge, and health can affect operations."),
            ["entryShield"] = new Entry("Entry shield", "Protects the vehicle during the simulated atmospheric-entry segment. It is optional when the mission does not use that segment."),
            ["fieldInstrument"] = new Entry("Field instrument", "Records required surface measurements at marked sites. Locations are game waypoints; terrain elevations and slopes come from the cited regional dataset."),
            ["dataRecorder"] = new Entry("Data recorder", "Stores science products until transmission. Capacity must cover the products required by the objective."),
            ["spares"] = new Entry("Repair spares", "Replacement components consumed by modeled repairs. Diagnose the fault before a repair can be performed."),
            ["attitudeControl"] = new Entry("Attitude control", "Small control thrusters that adjust spacecraft orientation and translation. Their modeled reserve is shown in line units."),
            ["rover"] = new Entry("Rover", "A surface vehicle for moving between field sites. It is optional; crew can walk to the marked locations."),
            ["altitude"] = new Entry("Altitude", "Vehicle height above the destination reference surface. During terminal descent, the display uses height above local ground; elsewhere it uses the modeled body reference."),
            ["speed"] = new Entry("Speed", "The magnitude of spacecraft velocity, in meters per second. It is not necessarily the downward descent rate."),
            ["missionElapsedTime"] = new Entry("Mission elapsed time (MET)", "Time elapsed in the mission simulation, displayed as minutes and seconds. It is a game clock, not a calendar date."),
            ["timeRemaining"] = new Entry("Time remaining", "The remaining mission operations window. If it expires before the objective is complete, the mission can end."),
            ["oxygen"] = new Entry("Crew oxygen", "The crew member’s remaining oxygen percentage. Suit oxygen decreases during EVA; return through the airlock before it is depleted."),
            ["health"] = new Entry("Crew health", "A simplified crew-condition indicator. It can be affected by modeled life-support hazards; it is not a medical model."),
            ["suitCharge"] = new Entry("Suit charge", "Remaining suit power, shown as a percentage. It is separate from oxygen and is modeled as a resource during EVA."),
            ["cabinPressure"] = new Entry("Cabin pressure", "Modeled air pressure inside the crew cabin, measured in kilopascals (kPa). A pressure-system fault requires diagnosis and repair."),
            ["vehicleSpeed"] = new Entry("Vehicle speed", "Current magnitude of spacecraft velocity, in meters per second. This differs from vertical descent rate and horizontal drift."),
            ["throttle"] = new Entry("Throttle", "The commanded fraction of available engine thrust, shown as a percentage. Manual throttle directly changes descent thrust."),
            ["descentRate"] = new Entry("Descent rate", "Vertical velocity during landing approach, in meters per second. Watch its magnitude near the surface; a fast touchdown can damage the mission."),
            ["lateralDrift"] = new Entry("Lateral drift", "Horizontal speed across the surface, in meters per second. Excess drift can carry the vehicle away from the intended landing area."),
            ["groundSlope"] = new Entry("Ground slope", "Local terrain inclination in degrees, calculated from neighboring elevation samples. Hazards smaller than the source grid are not modeled."),
            ["daysInTransfer"] = new Entry("Days in transfer", "Elapsed simulated cruise time divided by the planned transfer duration. Flight time is compressed for play."),
            ["periapsis"] = new Entry("Periapsis", "The lowest point of the current orbit above the body’s reference surface. A dangerously low or negative value can put the spacecraft on an impact trajectory.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["apoapsis"] = new Entry("Apoapsis", "The highest point of the current orbit above the body’s reference surface. Together with periapsis, it describes the orbit’s altitude range.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["periapsisApoapsis"] = new Entry("Periapsis / apoapsis", "Periapsis is the lowest point of the orbit above the body’s reference surface; apoapsis is the highest. A dangerously low periapsis can put the spacecraft on an impact trajectory.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["orbitalPeriod"] = new Entry("Orbital period", "The modeled time required to complete one orbit around the destination body.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["propellantReserve"] = new Entry("Propellant reserve", "Remaining ideal velocity-change capability from loaded propellant, shown in meters per second. Engine burns reduce it.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["activeLoad"] = new Entry("Active load", "Electrical power currently required by running systems, measured in watts. If it exceeds generation, the battery supplies the difference."),
            ["engineTemperature"] = new Entry("Engine temperature", "The engine’s modeled thermal state in kelvins. It changes during operation and can constrain continued engine use."),
            ["spareParts"] = new Entry("Spare parts", "The number of repair spares remaining on board. Each required repair consumes a spare."),
            ["surfaceGrid"] = new Entry("Surface grid spacing", "Approximate distance between neighboring terrain samples, in meters. Features smaller than this grid may not appear in the model."),
            ["corridorSlope"] = new Entry("Corridor slope", "Measured terrain inclination for the landing corridor, in degrees. Landing hardware has a modeled slope limit."),
            ["scienceProducts"] = new Entry("Science products", "The number of required measurements or survey records completed. The selected objective determines how many are needed."),
            ["recorder"] = new Entry("Recorder", "Science data stored on board compared with recorder capacity, in megabytes."),
            ["earthLink"] = new Entry("Earth link", "Whether the simulated spacecraft-to-Earth radio path is available. The destination body can block the link (occultation)."),
            ["roundTripLightTime"] = new Entry("Round-trip light time", "Estimated time for a radio signal to travel from the spacecraft to Earth and for a reply to return. The scenario uses a fixed approximation."),
            ["scienceData"] = new Entry("Science data on board", "Recorded mission data waiting to be transmitted to Earth, measured in megabytes."),
            ["linkRate"] = new Entry("Link rate", "Modeled communication throughput in megabytes per second. It affects the time needed to return recorded data."),
            ["surfaceGravity"] = new Entry("Surface gravity", "Gravitational acceleration at the body’s surface, measured in meters per second squared. It affects vehicle weight and TWR.", PlanetaryFacts, "NASA Planetary Fact Sheet"),
            ["meanRadius"] = new Entry("Mean radius", "Average distance from the body’s center to its surface, measured in kilometers. The simulation uses a spherical reference body.", PlanetaryFacts, "NASA Planetary Fact Sheet"),
            ["mapSpacing"] = new Entry("Map spacing", "Approximate distance between adjacent terrain samples in the playable regional grid."),
            ["mapOrigin"] = new Entry("Map origin", "Latitude and longitude used as the center reference for the local playable terrain map."),
            ["launchCapacity"] = new Entry("Launcher delivery capacity", "Maximum wet mass this launcher can deliver for the selected destination and orbit profile. Reduce mass or select a suitable launcher if this limit is exceeded."),
            ["requiredHardware"] = new Entry("Required hardware", "This objective requires the named component or mount. Select a compatible part in the shipyard; optional hardware is identified by the current mission design."),
            ["hullRating"] = new Entry("Pressure-hull mass rating", "Maximum wet mass allowed by the installed hull. Choose a higher-rated hull or reduce spacecraft mass if this limit is exceeded."),
            ["budgetConstraint"] = new Entry("Mission budget constraint", "The sum of fictional component and mission-option costs must be at or below the selected mission budget."),
            ["deltaVConstraint"] = new Entry("Maneuver-reserve constraint", "Ideal maneuver reserve from the installed engine, propellant, and mass must meet or exceed the scenario’s planned Δv requirement.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["twrConstraint"] = new Entry("Surface TWR constraint", "Surface missions require wet thrust-to-weight ratio of at least 1.70 in this simulation. Increase thrust or reduce wet mass."),
            ["provisionsConstraint"] = new Entry("Crew-provisions constraint", "Surface missions need provisions for the planned cruise plus a two-day reserve."),
            ["storageConstraint"] = new Entry("Science-storage constraint", "The recorder must have room for every science product required by the selected objective."),
            ["generationWarning"] = new Entry("Power-generation warning", "At the destination, continuous system demand exceeds estimated generation. The battery will be depleted unless load or power source changes."),
            ["highOrbitWarning"] = new Entry("High-orbit trade-off", "High orbit improves coverage and communication windows in this scenario, but reduces launcher delivery capacity and can increase maneuver needs.", OrbitalParameters, "JPL Astrodynamic Parameters"),
            ["fastTransferWarning"] = new Entry("Fast-transfer trade-off", "The fast profile reduces simulated cruise time but raises injection and capture energy requirements.", RocketEquation, "NASA Glenn: Ideal Rocket Equation"),
            ["flightPhase"] = new Entry("Mission phase", "The mission is divided into modeled stages: ascent, transfer, destination orbit, terminal descent, surface operations, and data return."),
            ["ascentPhase"] = new Entry("Ascent", "The authored launch profile carries the spacecraft away from Earth. Separate each spent stage when prompted."),
            ["transferPhase"] = new Entry("Transfer", "The compressed cruise phase from Earth departure to the destination. The selected profile changes simulated duration and energy demands."),
            ["orbitPhase"] = new Entry("Destination orbit", "The spacecraft is captured into an orbit around the Moon or Mars. Periapsis and apoapsis describe its lowest and highest altitude."),
            ["descentPhase"] = new Entry("Terminal descent", "The final powered approach to the surface. Monitor altitude, vertical speed, lateral drift, terrain slope, propellant, and touchdown conditions."),
            ["surfacePhase"] = new Entry("Surface operations", "Crew can leave through the airlock, visit marked field sites, and collect measurements. Suit resources limit EVA duration."),
            ["downlinkPhase"] = new Entry("Downlink", "Recorded science data is transmitted from the spacecraft to Earth. The radio needs power, stored data, and an available line of sight."),
            ["crewStations"] = new Entry("Crew stations", "Walk to a station and interact to operate that subsystem. Each console exposes controls and telemetry relevant to its job."),
            ["evaOperations"] = new Entry("EVA operations", "Extra-vehicular activity (EVA) takes the crew outside the lander. Monitor suit oxygen and charge, and return through the airlock before either runs low."),
            ["helm"] = new Entry("Helm", "Controls launch-stage separation and terminal descent. During manual descent, throttle and lateral thrusters affect the vehicle’s approach."),
            ["navigationStation"] = new Entry("Navigation", "Advances the simulated transfer, initiates orbital capture, and exposes the route and orbit map."),
            ["engineeringStation"] = new Entry("Engineering", "Checks electrical generation, active loads, battery, engine temperature, and repairable system faults."),
            ["scienceStation"] = new Entry("Science", "Measures the landing corridor or acquires orbital products, depending on the selected objective."),
            ["communicationsStation"] = new Entry("Communications", "Checks line of sight, radio power, data volume, and link rate before transmitting science to Earth."),
            ["airlock"] = new Entry("Airlock", "The crew passage for EVA. Suit oxygen and power continue to matter outside; return through the hatch before resources run low."),
            ["mapLegend"] = new Entry("Map legend and coordinates", "The map legend describes markers in the current view. Surface coordinates are meters relative to the local map origin; orbital and route views are schematic, not a full ephemeris.")
        };

        static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["objective"] = "objective", ["launcher"] = "launcher", ["orbit profile"] = "orbitProfile",
            ["transfer"] = "transfer", ["difficulty"] = "difficulty", ["operations window"] = "operationsWindow",
            ["wet mass"] = "wetMass", ["mission cost"] = "missionCost", ["delta-v reserve"] = "maneuverReserve",
            ["maneuver reserve"] = "maneuverReserve", ["surface twr"] = "surfaceTwr", ["power / peak"] = "powerPeakLoad",
            ["battery"] = "batteryCapacity", ["storage"] = "dataStorage", ["downlink"] = "downlinkRate",
            ["provisions"] = "crewProvisions", ["travel"] = "plannedTransfer", ["spares"] = "spareLineUnits",
            ["coast orbit"] = "coastOrbit", ["pressure hull"] = "pressureHull", ["tank"] = "tank",
            ["main engine"] = "mainEngine", ["power generator"] = "powerGeneration", ["battery bank"] = "batteryBank",
            ["landing sensor"] = "landingSensor", ["antenna"] = "antenna", ["landing gear"] = "landingGear",
            ["life support"] = "lifeSupport", ["entry shield"] = "entryShield", ["field instrument"] = "fieldInstrument",
            ["data recorder"] = "dataRecorder", ["attitude control"] = "attitudeControl", ["rover"] = "rover",
            ["mass / cost"] = "massCost", ["mass"] = "mass", ["cost"] = "cost", ["propellant"] = "propellant",
            ["thrust / isp"] = "thrustIsp", ["generation"] = "generation", ["load"] = "draw",
            ["altitude"] = "altitude", ["speed"] = "speed", ["met"] = "missionElapsedTime",
            ["time remaining"] = "timeRemaining", ["oxygen"] = "oxygen", ["health"] = "health",
            ["suit"] = "suitCharge", ["suit power"] = "suitCharge", ["cabin pressure"] = "cabinPressure",
            ["vehicle speed"] = "vehicleSpeed", ["throttle"] = "throttle", ["descent rate"] = "descentRate",
            ["lateral drift"] = "lateralDrift", ["ground slope"] = "groundSlope", ["days in transfer"] = "daysInTransfer",
            ["peri / apo"] = "periapsisApoapsis", ["period"] = "orbitalPeriod",
            ["power"] = "generation", ["demand"] = "activeLoad", ["engine"] = "engineTemperature",
            ["surface cell spacing"] = "surfaceGrid", ["corridor slope"] = "corridorSlope",
            ["science products"] = "scienceProducts", ["recorder"] = "recorder", ["earth link"] = "earthLink",
            ["round-trip light time"] = "roundTripLightTime", ["data on board"] = "scienceData",
            ["link rate"] = "linkRate", ["gravity"] = "surfaceGravity", ["mean radius"] = "meanRadius",
            ["terrain grid"] = "mapSpacing", ["coordinates"] = "mapOrigin",
            ["data returned"] = "scienceData", ["touchdown speed"] = "descentRate", ["local slope"] = "groundSlope",
            ["stages separated"] = "stageSeparation", ["eva operations"] = "evaOperations",
            ["flight plan"] = "flightPlan", ["flight readiness"] = "flightReadiness", ["ship assembly"] = "shipAssembly",
            ["component inspector"] = "componentInspector", ["flight log"] = "flightLog", ["mission record"] = "missionRecord",
            ["planetary atlas"] = "planetaryAtlas", ["helm"] = "helm", ["navigation"] = "navigationStation",
            ["engineering"] = "engineeringStation", ["science"] = "scienceStation", ["communications"] = "communicationsStation"
        };

        public static bool TryGet(string key, out Entry entry) => Entries.TryGetValue(key ?? "", out entry);

        public static string KeyForLabel(string label)
        {
            return Labels.TryGetValue(label ?? "", out var key) ? key : "";
        }

        public static string KeyForTitle(string title) => KeyForLabel(title);

        public static string KeyForSlot(int index)
        {
            string[] keys = {"pressureHull","tank","mainEngine","powerGeneration","batteryBank","landingSensor","antenna",
                "landingGear","lifeSupport","entryShield","fieldInstrument","dataRecorder","spares","attitudeControl","rover"};
            return index >= 0 && index < keys.Length ? keys[index] : "";
        }

        public static bool UsesTerrainSource(string key)
        {
            return key == "landingSensor" || key == "fieldInstrument" || key == "groundSlope" ||
                key == "surfaceGrid" || key == "corridorSlope" || key == "mapSpacing" || key == "mapOrigin";
        }

        public static string KeyForHold(string message)
        {
            if (message.StartsWith("Install ")) return "requiredHardware";
            if (message.Contains("launcher rated")) return "launchCapacity";
            if (message.Contains("pressure hull rating")) return "hullRating";
            if (message.Contains("mission budget")) return "budgetConstraint";
            if (message.Contains("propellant for capture")) return "deltaVConstraint";
            if (message.Contains("Surface, wet TWR")) return "twrConstraint";
            if (message.Contains("Crew provisions")) return "provisionsConstraint";
            if (message.Contains("storage for the required")) return "storageConstraint";
            return "";
        }

        public static string KeyForNote(string message)
        {
            if (message.StartsWith("Idle load")) return "generationWarning";
            if (message.StartsWith("Active instruments")) return "powerPeakLoad";
            if (message.StartsWith("High orbit")) return "highOrbitWarning";
            if (message.StartsWith("Fast transfer")) return "fastTransferWarning";
            return "";
        }
    }
}
