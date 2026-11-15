using UnityEngine;
using static OrbitalFrontier.Runtime.FrontierConsole;

namespace OrbitalFrontier.Runtime
{
    // A compact, first-run walkthrough for the Tranquillity Survey vertical slice.
    // No special assets, additional packages, or new scene setup required.
    public sealed partial class FrontierGame
    {
        int tutorialPage;

        static readonly string[] TutorialTitles = {
            "Your first lunar mission", "Configure your spacecraft", "Correct the lunar orbit",
            "Scan the three regions", "Transmit your findings", "Flight controls & what's next"
        };

        static readonly string[] TutorialDescriptions = {
            "Your mission starts in lunar orbit. Build a viable spacecraft, establish a safe survey orbit, map three sites, and send a landing-site recommendation back to Earth.",
            "In the Assembly hangar, select a preset and inspect the seven mounts. Swap a tank, engine, solar array or scientific instrument. The Engineering panel updates mass, fuel, power, delta-v and budget.",
            "Deployment starts in a 23 x 180 km lunar orbit. Raise the low point (perilune) to the mission's 46-92 km range. A correct maneuver qualifies the orbit for surveying.",
            "Visit the Lidar station. Scan all three sites and compare the 90th-percentile slope readings. Collect a higher-resolution Precision map for the site you intend to recommend.",
            "The final objective is to send a precision map and your chosen landing-site recommendation to Earth. A radio link, available power, and a quiet engine are required.",
            "You can operate the flight systems manually, inspect the 3D spacecraft, pause or speed up time, and review detailed planetary data. The full landing-and-rover sandbox is not implemented in this prototype."
        };

        static readonly string[,] TutorialActions = {
            { "1. Open Enter the hangar.", "2. Follow the objective list on the left once deployed.", "3. Finish the downlink to receive your mission score." },
            { "1. Start with the ready-to-fly Aster preset.", "2. Click a mount to view compatible parts; watch Engineering.", "3. When FLIGHT READY, click Deploy into lunar orbit." },
            { "1. Select Nav (station 1).", "2. Click Suggest correction, then Execute burn.", "3. Allow the burn to complete; check perilune = 46-92 km." },
            { "1. Select Lidar (station 4), then choose a site.", "2. Coarse 8x8 > Coast to access > Start scan; repeat x3.", "3. Scan your best candidate with Precision 24x24." },
            { "1. Select Comms (station 5) and choose the precision-mapped site.", "2. Wait until Earth visible; ensure radio and power are ON.", "3. Click Transmit recommendation; read the score report." },
            { "1-5 switches stations; Space pauses; X cancels operations.", "Drag the 3D viewport to orbit the camera; scroll to zoom.", "Orbit / Chase / Terrain changes view (Terrain needs a scan)." }
        };

        void DrawTutorial(float x, float y, float w)
        {
            int n = TutorialTitles.Length;
            tutorialPage = Mathf.Clamp(tutorialPage, 0, n - 1);
            ui.Text(new Rect(x, y, w, 19), "GUIDED WALKTHROUGH   /   " + (tutorialPage + 1) + " OF " + n,
                10, Accent, false, true);
            ui.Text(new Rect(x, y + 24, w, 34), TutorialTitles[tutorialPage], 25, Ink, true);
            ui.Text(new Rect(x, y + 70, w, 83), TutorialDescriptions[tutorialPage],
                15, Muted, false, false, TextAnchor.UpperLeft, true);
            ui.Rule(x, y + 163, w);
            ui.Text(new Rect(x, y + 180, w, 20), "WHAT TO DO", 11, Accent, true);
            for (int i = 0; i < 3; i++)
            {
                float row = y + 214 + i * 46;
                ui.Text(new Rect(x + 4, row, w - 8, 40), TutorialActions[tutorialPage, i],
                    14, Ink, false, false, TextAnchor.UpperLeft, true);
            }
            ui.Rule(x, y + 361, w);
            ui.Text(new Rect(x, y + 374, w, 37),
                "TIP: The cockpit has Nav, Flight, Power, Lidar and Comms tabs. Choose Controls for keyboard shortcuts.",
                12, Muted, false, false, TextAnchor.UpperLeft, true);
            if (ui.Button(new Rect(x, y + 422, 118, 32), "Previous", enabled: tutorialPage > 0))
                tutorialPage--;
            if (ui.Button(new Rect(x + 126, y + 422, 118, 32), "Next", true,
                enabled: tutorialPage < n - 1)) tutorialPage++;
            if (ui.Button(new Rect(x + w - 126, y + 422, 126, 32), "Start playing", true))
                overlay = null;
        }
    }
}
