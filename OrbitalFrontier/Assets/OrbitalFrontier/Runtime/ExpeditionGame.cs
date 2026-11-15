using System;
using System.Globalization;
using OrbitalFrontier.Core;
using UnityEngine;

namespace OrbitalFrontier.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class ExpeditionGame : MonoBehaviour
    {
        ExpeditionCatalog catalog;
        SurfaceGrid[] grids;
        ExpeditionSession session;
        ExpeditionStage visual;
        FrontierConsole ui;
        int area, mount, station = -1, mode = 2, lastPhase = -1, atlas, mapKind, previousSeat;
        float yaw, pitch = -1, throttle = .31f, moveTimer;
        bool looking, drag, wasOutside, guiding;
        Vector2 pointerDown;
        Vector2 previousPosition, controlsScroll;
        Rect stage;
        string overlay, trim = "12", hint, error, glossaryTerm;
        double hintUntil;
        double nudgeX, nudgeZ;
        Texture2D[] maps;
#if !UNITY_WEBGL || UNITY_EDITOR
        ExpeditionView remote;
        readonly LanLink lan = new LanLink();
        float broadcast;
        string ip = "127.0.0.1";
        int Seats => lan.CrewCount;
        int Seat => lan.Joined ? lan.Seat : 0;
        ExpeditionView View => lan.Joined && remote != null ? remote : session.View;
        bool IsHost => !lan.Joined;
        const int LanPort = 47611;
        [Serializable]
        sealed class InputFrame
        {
            public int command = 0, n = 0;
            public double a = 0, b = 0, c = 0;
        }
#else
        int Seats => 1;
        int Seat => 0;
        ExpeditionView View => session.View;
        bool IsHost => true;
#endif
        ExpeditionWorld Current => catalog.worlds[View.design.world];
        ExpeditionCrew Me => View.crew[Seat];
        float Scale => Mathf.Clamp(Mathf.Min(Screen.width / 1330f, Screen.height / 790f), .50f, 1.6f);
        float Width => Screen.width / Scale;
        float Height => Screen.height / Scale;
        readonly string[] phaseNames = {"Mission design","Ascent","Transfer","Destination orbit","Terminal descent","Surface operations","Complete","Failed"};

        void Awake()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Application.targetFrameRate = 60;
            try
            {
                var book = Resources.Load<TextAsset>("Frontier/expedition");
                if (!book) throw new InvalidOperationException("The expedition catalog could not be imported.");
                catalog = JsonUtility.FromJson<ExpeditionCatalog>(book.text);
                grids = Array.ConvertAll(catalog.worlds, w =>
                    new SurfaceGrid(w, Resources.Load<TextAsset>("Frontier/" + w.heightFile)?.bytes));
                maps = Array.ConvertAll(catalog.worlds, w => Resources.Load<Texture2D>("Frontier/" + w.texture));
                session = new ExpeditionSession(catalog, grids);
                ui = new FrontierConsole();
                ui.HelpRequested += OpenGlossary;
                visual = new ExpeditionStage(catalog, grids, transform);
            }
            catch (Exception e) { error = e.Message; Debug.LogException(e); }
        }

        void Update()
        {
            if (visual == null) return;
#if !UNITY_WEBGL || UNITY_EDITOR
            lan.PollCrew(Time.unscaledDeltaTime, ReceiveInput, line => {
                try { remote = JsonUtility.FromJson<ExpeditionView>(line); }
                catch (ArgumentException) { }
            });
            if (lan.Hosting)
                for (int i = 1; i < 4; i++)
                    session.CrewActive(i, (lan.CrewMask & (1 << i)) != 0);
            if (!lan.Joined)
            {
                if(remote != null) { remote = null; overlay = "crew"; }
                session.Tick(Time.unscaledDeltaTime);
            }
            broadcast += Time.unscaledDeltaTime;
            if (lan.Hosting && broadcast >= .09f)
            {
                broadcast = 0;
                if (Seats > 1) lan.Broadcast(JsonUtility.ToJson(session.View));
            }
#else
            session.Tick(Time.unscaledDeltaTime);
#endif
            int phase = View.phase;
            if (phase != lastPhase || Seat != previousSeat)
            {
                if (Seat != previousSeat && overlay == "crew") overlay = null;
                lastPhase = phase;
                previousSeat = Seat;
                station = phase == 4 && InReach(0) ? 0 : -1;
                mode = phase == 4 ? 1 : 2;
                yaw = (float)Me.yaw;
                pitch = -1;
                if (phase >= 6 || phase == 0) UnlockCursor();
            }
            if (Me.outside != wasOutside)
            {
                wasOutside = Me.outside;
                station = -1;
                yaw = (float)Me.yaw;
                pitch = Me.outside ? -3 : -1;
                mode = 2;
            }
            if (station >= 0 && !InReach(station)) station = -1;
            InputUpdate();
            float w = Width, h = Height;
            stage = phase == 0 ? new Rect(18,72,w-36,area==0?Mathf.Max(244,h*.34f):Mathf.Max(260,h*.35f)) :
                new Rect(226,138,w-572,Mathf.Max(438,h-166));
            Rect port = new Rect(stage.x/w,1-stage.yMax/h,stage.width/w,stage.height/h);
            visual.Camera.enabled = phase < 6;
            if (phase < 6) visual.Update(View, phase == 0 ? area == 0 ? 0 : mode == 5 ? 5 : 4 : mode,Seat,phase==0&&area==1?mount:-1,port);
        }

        void InputUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
                if(overlay!=null)overlay=null;
                else if(station>=0)station=-1;
            }
            bool flight=View.phase>0&&View.phase<6;
            bool ready=flight&&overlay==null;
            if(ready)
            {
                if(Input.GetKeyDown(KeyCode.M)){mode=mode==3?2:3;mapKind=Me.outside||View.phase>=4?1:0;UnlockCursor();}
                if(Input.GetKeyDown(KeyCode.V)){mode=mode==2?1:mode==1?3:2;UnlockCursor();}
                if(Input.GetKeyDown(KeyCode.E))Interact();
                if(Input.GetKeyDown(KeyCode.R)&&Me.outside)Send(ExpeditionCommand.SetRover);
                if(View.phase==4&&InReach(0))
                {
                    int step = Input.GetKeyDown(KeyCode.RightBracket) ? 1 : Input.GetKeyDown(KeyCode.LeftBracket) ? -1 : 0;
                    if(step!=0)
                    {
                        throttle=Mathf.Clamp01(throttle+step*.04f);
                        Send(ExpeditionCommand.LandingThrottle,0,throttle,nudgeX,nudgeZ);
                    }
                }
                if(looking)
                {
                    yaw=(yaw+Input.GetAxisRaw("Mouse X")*2.4f+360)%360;
                    pitch=Mathf.Clamp(pitch+Input.GetAxisRaw("Mouse Y")*2.0f,-74,74);
                }
                moveTimer+=Time.unscaledDeltaTime;
                if(moveTimer>=.08f)
                {
                    moveTimer=0;
                    double x=mode==2||mode==3?(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0):0;
                    double z=mode==2||mode==3?(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0):0;
                    Send(ExpeditionCommand.Move,0,x,z,yaw);
                }
            }
            visual.HeadYaw=yaw;visual.HeadPitch=pitch;
            Vector3 raw=Input.mousePosition;
            Vector2 p=new Vector2(raw.x/Scale,(Screen.height-raw.y)/Scale);
            var zone=new Rect(stage.x,stage.y+55,stage.width,stage.height-104);
            if(!looking&&zone.Contains(p)&&overlay==null)
            {
                if(Input.GetMouseButtonDown(0))
                {
                    if(ready&&mode==2){looking=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
                    else { drag=true;pointerDown=p; }
                }
                if(drag&&Input.GetMouseButton(0)&&(p-pointerDown).sqrMagnitude>9)visual.Rotate(p.x-previousPosition.x,p.y-previousPosition.y);
                if(drag&&Input.GetMouseButtonUp(0)&&View.phase==0&&area==1&&mode!=5&&(p-pointerDown).sqrMagnitude<=9)
                {
                    int selection=visual.PickMount(raw);
                    if(selection>=0)mount=selection;
                }
                if(Mathf.Abs(Input.mouseScrollDelta.y)>0)visual.Zoom(-Input.mouseScrollDelta.y);
            }
            if(!Input.GetMouseButton(0))drag=false;
            previousPosition=p;
        }

        bool InReach(int i)=>!Me.outside&&ExpeditionSession.Distance(Me.x,Me.z,CrewStation.X[i],CrewStation.Z[i])<=2.35;
        int NearestStation()
        {
            int best=-1;
            double distance=2.35;
            for(int i=0;i<CrewStation.Name.Length;i++)
            {
                double d=ExpeditionSession.Distance(Me.x,Me.z,CrewStation.X[i],CrewStation.Z[i]);
                if(d<=distance){distance=d;best=i;}
            }
            return best;
        }
        void Interact()
        {
            UnlockCursor();
            if (Me.outside)
            {
                var t=View.telemetry;
                if(ExpeditionSession.Distance(Me.x,Me.z,t.x,t.z+9.8)<=6){Send(ExpeditionCommand.Airlock);return;}
                for(int i=0;i<Current.points.Length;i++)
                    if(ExpeditionSession.Distance(Me.x,Me.z,Current.points[i].x,Current.points[i].z)<=10)
                    {Send(ExpeditionCommand.Sample,i);return;}
                SetHint("Approach a survey beacon or the airlock.");
                return;
            }
            int picked=NearestStation();
            if(picked>=0){station=picked;controlsScroll=Vector2.zero;}
            else SetHint("Move closer to a crew station.");
        }
        void SetHint(string message) {hint=message;hintUntil=Time.unscaledTimeAsDouble+3;}

        void OpenGlossary(string key)
        {
            if (!ExpeditionGlossary.TryGet(key, out _))
            {
                Debug.LogError("Missing expedition glossary entry: " + key);
                return;
            }
            glossaryTerm = key;
            overlay = "glossary";
        }
        void UnlockCursor()
        {
            if(!looking)return;
            looking=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        void Send(ExpeditionCommand command,int n=0,double a=0,double b=0,double c=0)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if(lan.Joined){lan.SendCommand(JsonUtility.ToJson(new InputFrame {command=(int)command,n=n,a=a,b=b,c=c}));return;}
#endif
            session.Apply(command,Seat,n,a,b,c);
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        void ReceiveInput(int player,string line)
        {
            try
            {
                var data=JsonUtility.FromJson<InputFrame>(line);
                if(data!=null&&data.command>=1&&data.command<=(int)ExpeditionCommand.Endurance)
                {
                    session.CrewActive(player,true);
                    session.Apply((ExpeditionCommand)data.command,player,data.n,data.a,data.b,data.c);
                }
            }
            catch(ArgumentException) { }
        }
#endif

        static string N(double x,string format="0")=>x.ToString(format,CultureInfo.InvariantCulture);
        static string Clock(double seconds)=>TimeSpan.FromSeconds(Math.Max(0,seconds)).ToString(@"mm\:ss");
        void OnDestroy()
        {
            UnlockCursor();
#if !UNITY_WEBGL || UNITY_EDITOR
            lan.Dispose();
#endif
            visual?.Dispose();ui?.Dispose();
        }
    }
}
