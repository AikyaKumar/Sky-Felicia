using System;
using OrbitalFrontier.Core;
using UnityEngine;
using static OrbitalFrontier.Runtime.FrontierConsole;

namespace OrbitalFrontier.Runtime
{
    public sealed partial class ExpeditionGame
    {
        void Flight(float w,float h)
        {
            var s=View;var t=s.telemetry;
            if(t==null)return;
            ui.Text(new Rect(19,69,412,19),phaseNames[s.phase].ToUpperInvariant(),10,Accent,false,true);
            ui.Help(new Rect(418,70,15,15),"flightPhase");
            ui.Text(new Rect(19,89,415,42),Current.name,34,Ink,true);
            double alt=s.phase==5?0:t.altitude;
            if(s.phase==4)alt-=grids[s.design.world].Height(t.x,t.z);
            float xx=w*.38f;
            ui.Metric(new Rect(xx,78,101,48),"ALTITUDE",N(alt>=1000?alt/1000:alt,"0.0"),alt>=1000?"km":"m");
            ui.Metric(new Rect(xx+107,78,101,48),"SPEED",N(t.speed,"0.0"),"m/s");
            ui.Metric(new Rect(xx+214,78,101,48),"PROPELLANT",N(t.fuel),"kg");
            ui.Metric(new Rect(xx+321,78,87,48),"MET",Clock(t.time));
            xx=w-277;
            if(ui.Button(new Rect(xx,88,87,29),"First person",selected:mode==2,size:12)){mode=2;UnlockCursor();}
            if(ui.Button(new Rect(xx+93,88,82,29),"Exterior",selected:mode==1,size:12)){mode=1;UnlockCursor();}
            if(ui.Button(new Rect(xx+181,88,75,29),"Map",selected:mode==3,size:12)){mode=3;mapKind=Me.outside||s.phase>=4?1:0;UnlockCursor();}
            BorderBox(stage);
            if(mode==3) DrawMap(new Rect(stage.x+13,stage.y+49,stage.width-26,stage.height-88));
            if(mode==2)
            {
                var ret=new Rect(stage.center.x-12,stage.center.y-12,24,24);
                ui.Text(ret,"+",17,Mint,false,true,TextAnchor.MiddleCenter);
                if(!looking&&ui.Button(new Rect(stage.xMax-96,stage.y+47,85,25),"Mouse look",size:11))
                {looking=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
                if(station<0)
                {
                    string label=null;
                    if(!Me.outside)
                    {
                        int i=NearestStation();
                        if(i>=0)label="E   Use "+CrewStation.Name[i];
                    }
                    else if(ExpeditionSession.Distance(Me.x,Me.z,t.x,t.z+9.8)<=6)label="E   Enter lander";
                    if(label!=null&&ui.Button(new Rect(stage.center.x-110,stage.yMax-78,220,31),label,size:13))Interact();
                }
            }
            ui.Fill(new Rect(stage.x,stage.y,stage.width,40),new Color(.062f,.071f,.11f,.77f));
            ui.Text(new Rect(stage.x+11,stage.y,stage.width-23,37),mode==3?"OPERATIONAL CHART":mode==1?"EXTERIOR CAMERA":
                Me.outside?"EVA  /  HELMET CAMERA":"INTERIOR  /  CREW CAMERA",9,Accent,false,true);
            if(mode==3)
            {
                ui.Help(new Rect(stage.x+166,stage.y+12,15,15),"mapLegend");
                if(ui.Button(new Rect(stage.xMax-167,stage.y+7,75,25),"Deck plan",selected:mapKind==0,enabled:!Me.outside,size:11))mapKind=0;
                if(ui.Button(new Rect(stage.xMax-86,stage.y+7,75,25),s.phase>=4?"Surface":"Trajectory",selected:mapKind==1,size:11))mapKind=1;
            }
            ui.Fill(new Rect(stage.x,stage.yMax-31,stage.width,31),new Color(.059f,.068f,.108f,.95f));
            ui.Text(new Rect(stage.x+9,stage.yMax-30,stage.width-19,30),
                Time.unscaledTimeAsDouble<hintUntil?hint:Me.message??t.status??"",11,Muted,false,true);
            var left=new Rect(16,stage.y,195,stage.height);
            LeftFlight(left,s,t);
            var right=new Rect(stage.xMax+12,stage.y,w-stage.xMax-28,stage.height);
            RightFlight(right,s,t);
        }

        void LeftFlight(Rect r,ExpeditionView s,ExpeditionTelemetry t)
        {
            ui.Card(r);ui.Title(r,"Flight log");
            float x=r.x+10,w=r.width-20,y=r.y+53;
            string[] phaseHelp={"","ascentPhase","transferPhase","orbitPhase","descentPhase","surfacePhase"};
            for(int i=1;i<=5;i++)
            {
                ui.Text(new Rect(x,y,w-19,25),(s.phase>i?"✓  ":"0"+i+" ")+phaseNames[i],13,s.phase==i?Accent:s.phase>i?Mint:Muted,s.phase==i);
                ui.Help(new Rect(x+w-17,y+4,15,15),phaseHelp[i]);
                ui.Rule(x,y+24,w);y+=27;
            }
            y+=8;
            string objective=s.phase==1?t.staged?"Separate from the Helm.":"Monitor ascent and wait for cutoff.":
                s.phase==2?t.fault==1?"Diagnose and repair the charge controller.":t.progress>=1?"Capture the orbit at Navigation.":"Advance transfer at Navigation.":
                s.phase==3?View.design.objective==1?"Record orbital products at Science.":t.measured==0?"Measure the corridor at Science.":"Begin descent at the Helm.":
                s.phase==4?"Fly the terminal approach.":"Collect field data, return, then transmit.";
            ui.Text(new Rect(x,y,w,47),objective,13,Accent,false,false,TextAnchor.UpperLeft,true);y+=47;
            ui.Reading(x,y,w,"Time remaining",Clock(t.timeLeft));y+=33;
            Vital(x,y,w,"OXYGEN",Me.oxygen);Vital(x,y+37,w,"HEALTH",Me.health);Vital(x,y+74,w,"SUIT POWER",Me.suit);
            y+=112;
            ui.Reading(x,y,w,"Cabin pressure",N(t.pressure,"0.0")+" kPa");y+=35;
            if(ui.Button(new Rect(x,y,w,26),guiding?"Open deck chart":"Show flight guidance",size:11))
            {
                guiding=true;
                mode=3;
                mapKind=Me.outside?1:0;
                UnlockCursor();
            }
            y+=32;
            for(int i=0;i<Math.Min(3,t.log?.Length??0);i++)
            {
                ui.Text(new Rect(x,y,w,46),t.log[i],10,Muted,false,true,TextAnchor.UpperLeft,true);y+=46;
            }
        }
        void Vital(float x,float y,float w,string label,double value)
        {
            string key=ExpeditionGlossary.KeyForLabel(label);
            ui.Text(new Rect(x,y,w-20,16),label,9,Muted,false,true);
            if(!string.IsNullOrEmpty(key))ui.Help(new Rect(x+w-18,y,15,15),key);
            ui.Text(new Rect(x,y,w,16),N(value)+"%",9,Mint,false,true,TextAnchor.MiddleRight);
            ui.Meter(new Rect(x,y+21,w,4),value/100);
        }

        void RightFlight(Rect r,ExpeditionView s,ExpeditionTelemetry t)
        {
            ui.Card(r);ui.Title(r,station<0?(Me.outside?"EVA operations":"Crew stations"):CrewStation.Name[station]);
            if(station>=0&&ui.Button(new Rect(r.xMax-62,r.y+8,50,26),"Close",size:11)){station=-1;controlsScroll=Vector2.zero;}
            var window=new Rect(r.x+7,r.y+47,r.width-14,r.height-50);
            var body=new Rect(0,0,window.width-14,Math.Max(window.height-2,station==0&&s.phase==4?460:Me.outside?449:430));
            controlsScroll=GUI.BeginScrollView(window,controlsScroll,body,false,false);
            float x=3,y=6,w=body.width-8;
            if(Me.outside) FieldControls(x,y,w,s,t);
            else if(station<0)
            {
                ui.Text(new Rect(x,y,w,48),"Walk to a console and press E. The deck plan shows where each station sits.",15,Muted,false,false,TextAnchor.UpperLeft,true);
                y+=53;
                for(int i=0;i<CrewStation.Name.Length;i++)
                {
                    double distance=ExpeditionSession.Distance(Me.x,Me.z,CrewStation.X[i],CrewStation.Z[i]);
                    if(ui.Button(new Rect(x,y,w,37),CrewStation.Name[i]+"   ·   "+(distance<=2.35?"in reach":N(distance,"0.0")+" m"),size:12))
                    {
                        if(InReach(i)){station=i;controlsScroll=Vector2.zero;}
                        else SetHint("Approach "+CrewStation.Name[i]+" to use the console.");
                    }
                    y+=43;
                }
            }
            else StationControls(x,y,w,s,t);
            GUI.EndScrollView();
        }
        void Btn(float x,ref float y,float w,string name,ExpeditionCommand cmd,int n=0,bool ready=true,bool main=false,double arg=0)
        {
            if(ui.Button(new Rect(x,y,w,31),name,main,enabled:ready,size:13))Send(cmd,n,arg);
            y+=37;
        }
        void Row(float x,ref float y,float w,string l,string v)
        {ui.Reading(x,y,w,l,v);y+=23;}

        void StationControls(float x,float y,float w,ExpeditionView s,ExpeditionTelemetry t)
        {
            var b=s.budget;
            switch(station)
            {
                case CrewStation.Helm:
                    Row(x,ref y,w,"Vehicle speed",N(t.speed,"0.0")+" m/s");
                    Row(x,ref y,w,"Throttle",N(t.throttle*100)+"%");
                    if(s.phase==4)
                    {
                        Row(x,ref y,w,"Descent rate",N(t.vertical,"0.0")+" m/s");
                        Row(x,ref y,w,"Lateral drift",N(Math.Sqrt(t.vx*t.vx+t.vz*t.vz),"0.0")+" m/s");
                        Row(x,ref y,w,"Ground slope",N(t.soilSlope,"0.0")+"°");
                        float changed=GUI.HorizontalSlider(new Rect(x,y+12,w-8,18),throttle,0,1);
                        if(Math.Abs(changed-throttle)>.012f){throttle=changed;Send(ExpeditionCommand.LandingThrottle,0,throttle,nudgeX,nudgeZ);}
                        y+=32;
                        if(ui.Button(new Rect(x,y,w,31),t.assist?"Switch to manual":"Engage landing assist",true,size:13))
                        {
                            if(t.assist)
                            {
                                throttle=(float)t.throttle;
                                nudgeX=nudgeZ=0;
                                Send(ExpeditionCommand.LandingThrottle,0,throttle,0,0);
                            }
                            else Send(ExpeditionCommand.LandingAssist,1);
                        }
                        y+=37;
                        Btn(x,ref y,w,t.dampers?"Turn dampers off":"Turn dampers on",ExpeditionCommand.Stabilize);
                        string[] labels={"West","East","North","South","Center thrust"};
                        for(int i=0;i<labels.Length;i++)
                            if(ui.Button(new Rect(x+(i%2)*(w*.5f+2),y+(i/2)*32,w*.5f-3,28),labels[i],size:12))
                            {
                                nudgeX=i==0?-.55:i==1?.55:0;nudgeZ=i==2?-.55:i==3?.55:0;
                                Send(ExpeditionCommand.LandingThrottle,0,throttle,nudgeX,nudgeZ);
                            }
                        y+=99;
                        Btn(x,ref y,w,"Abort descent",ExpeditionCommand.Abort);
                    }
                    else
                    {
                        y+=11;
                        Btn(x,ref y,w,t.stageCount==0?"Separate booster":"Separate transfer stage",ExpeditionCommand.Stage,ready:s.phase==1&&t.staged,main:true);
                        Btn(x,ref y,w,"Begin terminal descent",ExpeditionCommand.BeginDescent,ready:s.phase==3&&t.measured>0&&s.design.objective!=1);
                    }
                    break;
                case CrewStation.Nav:
                    Row(x,ref y,w,"Days in transfer",N(t.cruiseDays,"0.00")+"/"+N(b.travelDays,"0.00"));
                    Row(x,ref y,w,"Peri / Apo",N(t.orbitalPeri,"0.0")+"/"+N(t.orbitalApo,"0.0")+" km");
                    Row(x,ref y,w,"Period",N(t.orbitalPeriod/60,"0.0")+" min");
                    Row(x,ref y,w,"Maneuver reserve",N(t.deltaV)+" m/s");
                    y+=10;
                    Btn(x,ref y,w,"Accelerated coast",ExpeditionCommand.Coast,1,ready:s.phase==2||s.phase==3);
                    Btn(x,ref y,w,"Normal rate",ExpeditionCommand.Coast,0,ready:s.phase==2||s.phase==3);
                    Btn(x,ref y,w,"Execute orbital capture",ExpeditionCommand.Capture,ready:s.phase==2&&t.progress>=1&&!t.captureBurn,main:true);
                    if(s.phase==3)
                    {
                        ui.Text(new Rect(x,y,w,18),"TANGENTIAL TRIM ±35 m/s",9,Muted,false,true);
                        trim=ui.Field(new Rect(x,y+21,w,29),"trim",trim);
                        y+=55;
                        if(ui.Button(new Rect(x,y,w,31),"Execute trim",size:13)&&double.TryParse(trim,out double dv))
                            Send(ExpeditionCommand.Maneuver,0,dv);
                    }
                    break;
                case CrewStation.Engineering:
                    Row(x,ref y,w,"Generation",N(t.power)+" W");Row(x,ref y,w,"Demand",N(t.demand)+" W");
                    Row(x,ref y,w,"Battery",N(t.battery)+" Wh");Row(x,ref y,w,"Engine",N(t.engineTemp)+" K");
                    Row(x,ref y,w,"Spares",t.spares.ToString());y+=11;
                    Btn(x,ref y,w,"Run diagnostics",ExpeditionCommand.Inspect,station,main:true);
                    Btn(x,ref y,w,"Replace controller",ExpeditionCommand.Repair,station,t.fault==1&&t.spares>0&&(t.inspectedMask&(1<<station))!=0);
                    Btn(x,ref y,w,t.charger?"Open generation breaker":"Close generation breaker",ExpeditionCommand.SwitchPower);
                    break;
                case CrewStation.Life:
                    Row(x,ref y,w,"Pressure",N(t.pressure,"0.0")+" kPa");Row(x,ref y,w,"Oxygen",N(Me.oxygen,"0.0")+"%");
                    Row(x,ref y,w,"Health",N(Me.health,"0.0")+"%");Row(x,ref y,w,"Suit",N(Me.suit,"0.0")+"%");y+=11;
                    Btn(x,ref y,w,"Check oxygen circuit",ExpeditionCommand.Inspect,station,main:true);
                    Btn(x,ref y,w,"Replace pressure seal",ExpeditionCommand.Repair,station,t.fault==2&&t.spares>0&&(t.inspectedMask&(1<<station))!=0);
                    Btn(x,ref y,w,t.support?"Turn blower off":"Turn blower on",ExpeditionCommand.SwitchPower,1);
                    Btn(x,ref y,w,"Treat and refill suit",ExpeditionCommand.Triage);
                    break;
                case CrewStation.Science:
                    Row(x,ref y,w,"Surface cell spacing",N(Current.spacing)+" m");
                    Row(x,ref y,w,"Corridor slope",t.measured==0?"—":N(t.soilSlope,"0.0")+"°");
                    Row(x,ref y,w,"Science products",(s.design.objective==1?t.orbitalPasses:t.samples).ToString());
                    Row(x,ref y,w,"Recorder",N(t.stored,"0.0")+"/"+N(b.storage,"0.0")+" MB");y+=11;
                    Btn(x,ref y,w,s.design.objective==1?"Acquire orbital product":"Measure landing corridor",ExpeditionCommand.Inspect,station,main:true);
                    if(ui.Button(new Rect(x,y,w,31),"Open chart",size:13)){mode=3;mapKind=1;UnlockCursor();}
                    if(!string.IsNullOrEmpty(t.finding))
                        ui.Text(new Rect(x,y+39,w,83),t.finding,14,Muted,false,false,TextAnchor.UpperLeft,true);
                    break;
                case CrewStation.Comms:
                    Row(x,ref y,w,"Earth link",t.link?"Available":"Occulted");Row(x,ref y,w,"Round-trip light time",N(t.signalDelay,"0.00")+" s");
                    Row(x,ref y,w,"Data on board",N(t.stored,"0.00")+" MB");Row(x,ref y,w,"Link rate",N(b.bandwidth,"0.00")+" MB/s");y+=11;
                    if(t.transferring)Row(x,ref y,w,"Acknowledgment in",N(t.txRemaining)+" sim s");
                    Btn(x,ref y,w,t.transferring?"Transmitting…":"Transmit mission data",ExpeditionCommand.Downlink,
                        ready:!t.transferring&&t.link&&t.transmitter&&(s.design.objective==1?t.orbitalPasses:t.samples)>=(s.design.objective==2?1:2),main:true);
                    Btn(x,ref y,w,t.transmitter?"Power radio off":"Power radio on",ExpeditionCommand.SwitchPower,2);
                    break;
                default:
                    Row(x,ref y,w,"Hatch",s.phase==5?"Surface access":"Flight interlock");
                    Row(x,ref y,w,"Oxygen",N(Me.oxygen)+"%");y+=11;
                    Btn(x,ref y,w,"Cycle hatch and begin EVA",ExpeditionCommand.Airlock,ready:s.phase==5,main:true);
                    break;
            }
        }

        void FieldControls(float x,float y,float w,ExpeditionView s,ExpeditionTelemetry t)
        {
            ui.Text(new Rect(x,y,w,38),Current.credit,12,Muted,false,false,TextAnchor.UpperLeft,true);
            y+=48;
            for(int i=0;i<Current.points.Length;i++)
            {
                var p=Current.points[i];double d=ExpeditionSession.Distance(Me.x,Me.z,p.x,p.z);
                ui.Text(new Rect(x,y,w,25),p.name,16,Ink,true);y+=25;
                Row(x,ref y,w,t.marks[i]>0?"Recorded slope": "Range",t.marks[i]>0?N(t.slopes[i],"0.0")+"°":N(d)+" m");
                Btn(x,ref y,w,"Record field measurement",ExpeditionCommand.Sample,i,!Me.rover&&t.marks[i]==0&&d<=10);
                if(t.marks[i]>0)
                {
                    ui.Text(new Rect(x,y,w,49),p.observation,12,Muted,false,false,TextAnchor.UpperLeft,true);
                    y+=51;
                }
            }
            if(!string.IsNullOrEmpty(s.design.parts[14]))
                Btn(x,ref y,w,Me.rover?"Dismount rover":"Mount rover",ExpeditionCommand.SetRover,
                    ready:Me.rover||t.roverId<0&&ExpeditionSession.Distance(Me.x,Me.z,t.roverX,t.roverZ)<=7);
            Btn(x,ref y,w,"Re-enter lander",ExpeditionCommand.Airlock,ready:ExpeditionSession.Distance(Me.x,Me.z,t.x,t.z+9.8)<=6,main:true);
        }
        void DrawMap(Rect box)
        {
            ui.Card(box);
            var s=View;
            var t=s.telemetry;
            if(mapKind==0&&!Me.outside)
            {
                float scale=Math.Min(box.height/18.5f,box.width/11);
                Vector2 Center=new Vector2(box.center.x,box.center.y + scale);
                Vector2 Pixel(double a,double z)=>Center+new Vector2((float)a*scale,(float)z*scale);
                var deck=new Rect(Center.x-2.35f*scale,Center.y-8.9f*scale,4.7f*scale,16.25f*scale);
                ui.Fill(deck,new Color(.15f,.18f,.26f));BorderBox(deck);
                ui.Text(new Rect(box.x+14,box.y+12,box.width-28,23),"CREW DECK / BOW UP",12,Accent,false,true);
                for(int i=0;i<CrewStation.Name.Length;i++)
                {
                    var p=Pixel(CrewStation.X[i],CrewStation.Z[i]);
                    ui.Fill(new Rect(p.x-4,p.y-4,8,8),Amber);
                    bool placeLeft=i%2==0;
                    var label=new Rect(placeLeft?p.x-144:p.x+12,p.y-10,131,20);
                    ui.Text(label,CrewStation.Name[i],11,Ink,false,true,placeLeft?TextAnchor.MiddleRight:TextAnchor.MiddleLeft);
                }
                foreach(var c in s.crew)
                    if(c.active&&!c.outside)
                    {
                        var p=Pixel(c.x,c.z);ui.Fill(new Rect(p.x-4,p.y-4,8,8),c.id==Seat?Mint:Amber);
                    }
                return;
            }
            if(s.phase>=4)
            {
                float side=Math.Min(box.width-65,box.height-68);
                var r=new Rect(box.center.x-side/2,box.y+36,side,side);
                var img=maps[s.design.world];
                float q=(float)(430/grids[s.design.world].Extent);
                if(img)GUI.DrawTextureWithTexCoords(r,img,new Rect((1-q)/2,(1-q)/2,q,q));
                BorderBox(r);
                Vector2 Point(double x,double z)=>r.center+new Vector2((float)x*side/860,(float)z*side/860);
                void Mark(double x,double z,string text,Color c)
                {
                    var p=Point(x,z);ui.Fill(new Rect(p.x-3,p.y-3,7,7),c);
                    ui.Text(new Rect(p.x+8,p.y-10,125,24),text,11,Ink,false,true);
                }
                Mark(t.x,t.z,"Lander",Amber);
                for(int i=0;i<Current.points.Length;i++)
                    Mark(Current.points[i].x,Current.points[i].z,i==0?"A":"B",t.marks[i]>0?Mint:Accent);
                foreach(var c in s.crew)
                    if(c.active&&c.outside)Mark(c.x,c.z,c.id==Seat?"YOU":"Crew "+(c.id+1),Mint);
                ui.Text(new Rect(box.x+14,box.y+5,box.width-25,25),Current.name.ToUpperInvariant()+" / NORTH UP",10,Accent,false,true);
                return;
            }
            if(s.phase==3)
            {
                var lat=Orbit.Rad;
                var r=new Rect(box.x+23,box.y+39,box.width-46,box.height-95);
                var img=Resources.Load<Texture2D>("Frontier/"+(s.design.world==0?"lunar_albedo":"mars_albedo"));
                if(img)GUI.DrawTexture(r,img,ScaleMode.StretchToFill);
                Color dim=new Color(.043f,.047f,.10f,.25f);ui.Fill(r,dim);
                Vector2 Point(double p,double q)=>new Vector2(r.x+(float)((q+180)*r.width/360),r.y+(float)((90-p)*r.height/180));
                foreach(var a in t.track)
                {
                    var p=Point(Math.Asin(a.y/a.Norm)/lat,Math.Atan2(a.z,a.x)/lat);ui.Fill(new Rect(p.x-2,p.y-2,4,4),Accent);
                }
                var m=Point(Current.latitude,Current.longitude);ui.Fill(new Rect(m.x-4,m.y-4,8,8),Amber);
                ui.Text(new Rect(box.x+16,box.y+6,box.width-30,23),Current.body.ToUpperInvariant()+" / GROUND TRACK",11,Accent,false,true);
                return;
            }
            float left=box.x+51,right=box.xMax-51,y=box.center.y;
            ui.Text(new Rect(box.x+16,box.y+6,box.width-30,23),"TRANSFER TO "+Current.body.ToUpperInvariant(),11,Accent,false,true);
            ui.Fill(new Rect(left,y,right-left,2),Border);
            ui.Fill(new Rect(left+(right-left)*(float)Math.Max(0,t.progress)-4,y-4,9,9),Mint);
            ui.Text(new Rect(left,y+13,116,22),"EARTH",11,Muted,false,true);
            ui.Text(new Rect(right-116,y+13,116,22),Current.body.ToUpperInvariant(),11,Muted,false,true,TextAnchor.MiddleRight);
        }

        void Debrief(float w,float h)
        {
            var s=View;var t=s.telemetry;
            ui.Text(new Rect(19,79,580,26),"MISSION DEBRIEF",11,Accent,false,true);
            ui.Text(new Rect(18,103,600,55),s.phase==6?"Expedition complete":"Expedition ended",39,Ink,true);
            if(ui.Button(new Rect(w-190,112,170,34),"Return to design",enabled:IsHost)){Send(ExpeditionCommand.ReturnDesign);area=0;}
            float span=(w-52)/2;
            var left=new Rect(18,174,span,520);
            var right=new Rect(left.xMax+12,174,span,520);
            ui.Card(left);ui.Title(left,Current.name,s.phase==6?"EARTH ACKNOWLEDGMENT":"FLIGHT ENDED");
            ui.Text(new Rect(33,234,span-36,85),s.phase==6?t.reportScore+" / 100":"Mission terminated",54,Accent,true);
            ui.Text(new Rect(35,324,span-38,68),t.summary,18,Muted,false,false,TextAnchor.UpperLeft,true);
            ui.Metric(new Rect(33,410,span*.48f,61),"DATA RETURNED",N(t.delivered,"0.00"),"MB");
            ui.Metric(new Rect(left.center.x+5,410,span*.45f,61),"PROPELLANT",N(t.fuel,"0.0"),"kg");
            ui.Metric(new Rect(33,478,span*.48f,61),"TOUCHDOWN SPEED",N(t.touchdownV,"0.00"),"m/s");
            ui.Metric(new Rect(left.center.x+5,478,span*.45f,61),"LOCAL SLOPE",N(t.touchdownSlope,"0.0"),"°");
            ui.Card(right);ui.Title(right,"Mission record");
            for(int i=0;i<t.log.Length;i++)
                ui.Text(new Rect(right.x+13,right.y+53+38*i,right.width-26,37),t.log[i],12,Muted,false,true,TextAnchor.UpperLeft,true);
        }

        void Overlay(float w,float h)
        {
            ui.Fill(new Rect(0,0,w,h),new Color(.012f,.015f,.029f,.88f));
            ExpeditionGlossary.Entry helpEntry=null;
            bool glossaryOpen=overlay=="glossary"&&ExpeditionGlossary.TryGet(glossaryTerm,out helpEntry);
            float height=glossaryOpen?300:overlay=="atlas"?385:overlay=="controls"?327:overlay=="firstFlight"?423:278;
            var r=new Rect((w-600)/2,(h-height)/2,600,height);
            ui.Card(r);
            string title=glossaryOpen?helpEntry.title:overlay=="atlas"?"Planetary atlas":overlay=="crew"?"Crew LAN":overlay=="reset"?"Return to design?":overlay=="firstFlight"?"First-flight guide":"Controls";
            ui.Title(r,title);
            if(ui.Button(new Rect(r.xMax-66,r.y+8,54,28),"Close",size:12)){overlay=null;return;}
            float x=r.x+15,y=r.y+57,span=r.width-30;
            switch(overlay)
            {
                case "glossary":
                {
                    if(!glossaryOpen){overlay=null;return;}
                    ui.Text(new Rect(x,y,span,145),helpEntry.text,16,Muted,false,false,TextAnchor.UpperLeft,true);
                    string resourceUrl=helpEntry.url;
                    string resourceLabel=helpEntry.link;
                    if(string.IsNullOrEmpty(resourceUrl)&&ExpeditionGlossary.UsesTerrainSource(glossaryTerm))
                    {
                        resourceUrl=Current.source;
                        resourceLabel=Current.body+" terrain dataset";
                    }
                    if(!string.IsNullOrEmpty(resourceUrl)&&ui.Button(new Rect(x,y+163,span,34),
                        string.IsNullOrEmpty(resourceLabel)?"Explore resource ↗":resourceLabel+" ↗",size:13))
                        Application.OpenURL(resourceUrl);
                    break;
                }
                case "atlas":
                    for(int i=0;i<catalog.worlds.Length;i++)
                        if(ui.Button(new Rect(x+i*106,y,100,30),catalog.worlds[i].body,selected:atlas==i))atlas=i;
                    var world=catalog.worlds[atlas];
                    ui.Text(new Rect(x,y+39,span,36),world.name,26,Ink,true);
                    ui.Text(new Rect(x,y+77,span,22),world.subtitle,15,Muted);
                    ui.Reading(x,y+110,span,"Gravity",N(world.gravity,"0.000")+" m/s²");
                    ui.Reading(x,y+135,span,"Mean radius",N(world.radius/1000,"0.0")+" km");
                    ui.Reading(x,y+160,span,"Terrain grid",N(world.spacing)+" m");
                    ui.Reading(x,y+185,span,"Coordinates",N(world.latitude,"0.000")+"° N / "+N(world.longitude,"0.000")+"° E");
                    ui.Text(new Rect(x,y+213,span,44),world.description,14,Muted,false,false,TextAnchor.UpperLeft,true);
                    if(ui.Button(new Rect(x,y+269,139,32),"Terrain dataset",size:12))Application.OpenURL(world.source);
                    if(ui.Button(new Rect(x+150,y+269,142,32),"Mission research",size:12))Application.OpenURL(world.research);
                    break;
                case "crew": CrewOverlay(x,y,span);break;
                case "firstFlight":
                    ui.Text(new Rect(x,y,span,261),
                        "1   Helm — separate both spent stages.\n\n2   Navigation — accelerate coast. Return here for orbital capture.\n\n3   Engineering — diagnose and replace the failed controller.\n\n4   Science and Helm — measure the landing corridor and descend.\n\n5   Aft airlock — begin EVA and collect two records.\n\n6   Communications — return to the cabin and downlink.",
                        16,Muted,false,false,TextAnchor.UpperLeft,true);
                    if(ui.Button(new Rect(x,y+276,248,35),"Fly the guided Moon mission",true,enabled:IsHost))
                    {
                        guiding=true;
                        Send(ExpeditionCommand.Vehicle,0);
                        Send(ExpeditionCommand.Objective,0);
                        Send(ExpeditionCommand.Difficulty,0);
                        Send(ExpeditionCommand.Launcher,0);
                        Send(ExpeditionCommand.Transfer,0);
                        Send(ExpeditionCommand.OrbitProfile,0);
                        Send(ExpeditionCommand.Endurance,1);
                        overlay=null;
                        Send(ExpeditionCommand.Launch);
                    }
                    break;
                case "reset":
                    ui.Text(new Rect(x,y,span,67),"Return to mission design and keep the current assembly? The active flight will end.",
                        17,Muted,false,false,TextAnchor.UpperLeft,true);
                    if(ui.Button(new Rect(x,y+76,158,35),"Return to design",true,enabled:IsHost)){Send(ExpeditionCommand.ReturnDesign);overlay=null;}
                    break;
                default:
                    ui.Text(new Rect(x,y,span,197),
                        "WASD or arrows — walk. Click the viewport for mouse look. Esc releases it.\n\nE — use a reachable console, airlock or surface beacon.\n\nM — deck or mission chart. V — cycle views. R — mount or dismount an equipped rover.\n\nUse Guided for automatic terminal descent. Expert starts in manual. Monitor suit oxygen, cabin pressure and crew health.",
                        17,Muted,false,false,TextAnchor.UpperLeft,true);
                    if(ui.Button(new Rect(x,y+206,133,33),"Crew LAN"))overlay="crew";
                    break;
            }
        }

        void CrewOverlay(float x,float y,float w)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ui.Text(new Rect(x,y,w,49),"Host one shared expedition for up to four players. Team members move independently and operate any reachable station.",
                15,Muted,false,false,TextAnchor.UpperLeft,true);
            if(lan.Hosting||lan.Joined||lan.Joining)
            {
                ui.Text(new Rect(x,y+55,w,26),lan.Hosting?"Hosting on port "+LanPort:lan.Joining?"Connecting…":"Joined as seat "+(Seat+1),13,Mint,false,true);
                ui.Text(new Rect(x,y+84,w,26),"Crew count "+Seats+" / 4",14,Muted,false,true);
                if(ui.Button(new Rect(x,y+119,135,34),lan.Hosting?"Close crew lobby":"Leave crew lobby"))
                {
                    lan.Leave();remote=null;
                    for(int i=1;i<4;i++)session.CrewActive(i,false);
                }
            }
            else
            {
                if(ui.Button(new Rect(x,y+49,130,32),"Host LAN",true))lan.StartHost(LanPort);
                ip=ui.Field(new Rect(x,y+104,231,31),"ip",ip);
                if(ui.Button(new Rect(x+239,y+104,105,31),"Join crew"))lan.StartJoin(ip.Trim(),LanPort);
                ui.Text(new Rect(x,y+149,w,45),lan.Error??("Join by host IPv4 address using TCP "+LanPort+". Use 127.0.0.1 on one desktop."),
                    13,lan.Error==null?Muted:Amber,false,false,TextAnchor.UpperLeft,true);
            }
#else
            ui.Text(new Rect(x,y,w,63),"Use a native Windows/Linux Unity build or the bundled browser crew host for shared expeditions.",
                16,Muted,false,false,TextAnchor.UpperLeft,true);
#endif
        }
    }
}
