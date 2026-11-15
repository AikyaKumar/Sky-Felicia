using System;
using System.Collections.Generic;
using OrbitalFrontier.Core;
using UnityEngine;
using static OrbitalFrontier.Runtime.FrontierConsole;

namespace OrbitalFrontier.Runtime
{
    public sealed partial class ExpeditionGame
    {
        void OnGUI()
        {
            if (ui == null)
            {
                if(!string.IsNullOrEmpty(error)) GUI.Label(new Rect(20,20,1000,35),error);
                return;
            }
            if(stage.width<=0)return;
            var previous=GUI.matrix;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Scale,Scale,1));
            float w=Width,h=Height;
            bool old=GUI.enabled;
            GUI.enabled=old&&overlay==null&&!looking;
            if (View.phase >= 6) ui.Fill(new Rect(0,0,w,h),Bg);
            else
            {
                ui.Fill(new Rect(0,0,w,stage.y),Bg);
                ui.Fill(new Rect(0,stage.y,stage.x,stage.height),Bg);
                ui.Fill(new Rect(stage.xMax,stage.y,w-stage.xMax,stage.height),Bg);
                ui.Fill(new Rect(0,stage.yMax,w,h-stage.yMax),Bg);
            }
            Header(w);
            if(View.phase==0) Design(w,h);
            else if(View.phase<6) Flight(w,h);
            else Debrief(w,h);
            GUI.enabled=old;
            if(overlay!=null) Overlay(w,h);
            GUI.matrix=previous;
        }

        void Header(float w)
        {
            ui.Fill(new Rect(0,0,w,57),new Color(.055f,.055f,.089f));
            ui.Rule(0,56,w);
            ui.Text(new Rect(20,6,240,27),"ORBITAL FRONTIER",21,Ink,true);
            ui.Text(new Rect(21,31,240,15),"EXPEDITION OPERATIONS",9,Accent,false,true);
            float x=w*.5f-188;
            if(ui.Button(new Rect(x,14,106,29),"Mission design",selected:View.phase==0&&area==0)) GoDesign(0);
            if(ui.Button(new Rect(x+112,14,89,29),"Shipyard",selected:View.phase==0&&area==1)) GoDesign(1);
            if(ui.Button(new Rect(x+207,14,63,29),"Atlas")) {overlay="atlas";atlas=View.design.world;}
            if(ui.Button(new Rect(x+276,14,72,29),"Controls")) overlay="controls";
            ui.Text(new Rect(w-238,13,220,31),Seats>1?"CREW "+(Seat+1)+" / "+Seats:"SOLO SESSION",10,Mint,false,true,TextAnchor.MiddleRight);
        }

        void GoDesign(int target)
        {
            if(View.phase==0){area=target;mode=2;return;}
            if(!IsHost){SetHint("The room host manages mission reset.");return;}
            area=target;overlay="reset";
        }

        void Design(float w,float h)
        {
            BorderBox(stage);
            if(area==0)
            {
                ui.Fill(new Rect(stage.x,stage.y,stage.width*.31f,stage.height),new Color(.038f,.041f,.076f,.84f));
                ui.Text(new Rect(stage.x+23,stage.y+15,460,23),"MISSION DESIGN",11,Accent,false,true);
                ui.Text(new Rect(stage.x+19,stage.y+35,600,81),"Design the",75,Ink,true);
                ui.Text(new Rect(stage.x+19,stage.y+106,620,78),"expedition.",75,Accent,true);
                ui.Text(new Rect(stage.x+22,stage.y+183,440,29),"Choose a world and configure the mission.",18,Muted);
                for(int i=0;i<catalog.worlds.Length;i++)
                {
                    float buttonX=stage.x+23+i*115;
                    if(ui.Button(new Rect(buttonX,stage.y+220,87,32),catalog.worlds[i].body,selected:i==View.design.world))
                        Send(ExpeditionCommand.World,i);
                    ui.Help(new Rect(buttonX+90,stage.y+228,16,16),"world");
                }
                ui.Text(new Rect(stage.xMax-371,stage.y+11,355,23),Current.tag,10,Accent,false,true,TextAnchor.MiddleRight);
            }
            else
            {
                ui.Text(new Rect(stage.x+14,stage.y+14,504,44),"Assembly hangar",33,Ink,true);
                ui.Text(new Rect(stage.x+16,stage.y+57,335,27),"Pick a 3D part or mount to inspect it.",15,Muted);
                if(ui.Button(new Rect(stage.xMax-176,stage.y+13,76,31),"Exterior",selected:mode!=5)) mode=2;
                if(ui.Button(new Rect(stage.xMax-91,stage.y+13,77,31),"Deck plan",selected:mode==5)) mode=5;
            }
            ui.Fill(new Rect(stage.x,stage.yMax-27,stage.width,27),new Color(.065f,.067f,.108f,.91f));
            ui.Text(new Rect(stage.x+12,stage.yMax-27,stage.width-25,27),area==0?Current.credit:"DRAG TO ORBIT   ·   SCROLL TO ZOOM",10,Muted,false,true);
            float top=stage.yMax+11, span=(w-59)/3, body=Math.Max(405,h-top-13);
            var first=new Rect(18,top,span,body);var second=new Rect(first.xMax+11,top,span,body);
            var third=new Rect(second.xMax+11,top,span,body);
            if(area==0)
            {
                DrawPlan(first);
                DrawBudget(second);
                Readiness(third,true);
            }
            else
            {
                DrawMounts(first);
                DrawPart(second);
                DrawBudget(third);
            }
        }

        void DrawPlan(Rect r)
        {
            ui.Card(r);ui.Title(r,"Flight plan","EARTH DEPARTURE");
            var d=View.design;
            float x=r.x+11,y=r.y+54,m=r.width-22;
            Steps(x,y,m,"Objective",ExpeditionSession.Objectives,d.objective,ExpeditionCommand.Objective);
            Steps(x,y+58,m,"Launcher",Array.ConvertAll(catalog.launchers,v=>v.name),d.launcher,ExpeditionCommand.Launcher);
            Steps(x,y+116,m,"Orbit profile",new[]{N(Current.orbitLow)+" km / low",N(Current.orbitHigh)+" km / high"},d.orbit,ExpeditionCommand.OrbitProfile);
            Steps(x,y+174,m,"Transfer",new[]{"Efficient","Fast / high energy"},d.route,ExpeditionCommand.Transfer);
            Steps(x,y+232,m,"Difficulty",new[]{"Guided","Expert / manual"},d.difficulty,ExpeditionCommand.Difficulty);
            Steps(x,y+290,m,"Operations window",new[]{"Short","Standard","Extended"},d.endurance,ExpeditionCommand.Endurance);
        }

        void Steps(float x,float y,float width,string label,string[] values,int active,ExpeditionCommand command)
        {
            string key=ExpeditionGlossary.KeyForLabel(label);
            ui.Text(new Rect(x,y,width-(string.IsNullOrEmpty(key)?0:21),17),label.ToUpperInvariant(),9,Subtle,false,true);
            if(!string.IsNullOrEmpty(key))ui.Help(new Rect(x+width-18,y,15,15),key);
            float each=(width-(values.Length-1)*4)/values.Length;
            for(int i=0;i<values.Length;i++)
                if(ui.Button(new Rect(x+i*(each+4),y+20,each,29),values[i],selected:i==active,size:12)) Send(command,i);
        }

        void DrawBudget(Rect r)
        {
            var b=View.budget;
            ui.Card(r);ui.Title(r,"Mission resources",b.ready?"FLIGHT READY":"DESIGN HOLD");
            float x=r.x+11,y=r.y+53,w=(r.width-27)/2;
            var names=new[]{"WET MASS","MISSION COST","DELTA-V RESERVE","SURFACE TWR","POWER / PEAK","BATTERY","STORAGE","DOWNLINK","PROVISIONS","TRAVEL","SPARES","COAST ORBIT"};
            var v=new[]{N(b.wet)+"/"+N(b.capacity),N(b.cost)+"/"+N(b.budget),N(b.reserve)+"/"+N(b.plannedDV),N(b.twr,"0.00"),
                N(b.generation)+"/"+N(b.peak),N(b.battery),N(b.storage,"0.0"),N(b.bandwidth,"0.00"),N(b.lifeDays),N(b.travelDays,"0.00"),N(b.spares),N(b.orbitKm)};
            var u=new[]{"kg","cr","m/s","","W","Wh","MB","MB/s","d","d","","km"};
            for(int i=0;i<names.Length;i++)
                ui.Metric(new Rect(x+(i%2)*(w+5),y+(i/2)*50,w,45),names[i],v[i],u[i]);
        }
        void Readiness(Rect r,bool buttons)
        {
            var b=View.budget;
            ui.Card(r);ui.Title(r,"Flight readiness",b.ready?"FLIGHT READY":"DESIGN HOLD");
            float x=r.x+10,y=r.y+56,span=r.width-20;
            if(b.ready)
                ui.Text(new Rect(x,y,span,44),"Check the launch vehicle, payload, power and crew reserves before committing to flight.",14,Muted,false,false,TextAnchor.UpperLeft,true);
            else
            {
                foreach(string issue in b.issues)
                {
                    string key=ExpeditionGlossary.KeyForHold(issue);
                    ui.Text(new Rect(x,y,span-22,27),issue,12,Amber,false,false,TextAnchor.UpperLeft,true);
                    if(!string.IsNullOrEmpty(key))ui.Help(new Rect(x+span-19,y+4,15,15),key);
                    y+=27;
                }
            }
            if(b.ready)y+=49;
            if(buttons)
            {
                if(ui.Button(new Rect(x,y,span,37),"Launch expedition",true,enabled:b.ready&&IsHost))Send(ExpeditionCommand.Launch);
                if(ui.Button(new Rect(x,y+43,span*.52f-4,30),"Open shipyard")) area=1;
                if(ui.Button(new Rect(x+span*.52f,y+43,span*.48f,30),"Crew LAN")) overlay="crew";
                if(ui.Button(new Rect(x,y+78,span,28),"First-flight guide",size:12)) overlay="firstFlight";
                y+=117;
            }
            foreach(string note in b.notes)
            {
                string key=ExpeditionGlossary.KeyForNote(note);
                ui.Text(new Rect(x,y,span-22,33),note,12,Muted,false,false,TextAnchor.UpperLeft,true);
                if(!string.IsNullOrEmpty(key))ui.Help(new Rect(x+span-19,y+4,15,15),key);
                y+=36;
            }
        }

        void DrawMounts(Rect r)
        {
            var d=View.design;
            ui.Card(r);ui.Title(r,"Ship assembly","15 MOUNTS");
            float x=r.x+8,w=r.width-16,y=r.y+51,each=(w-6)/3;
            for(int i=0;i<catalog.vehicles.Length;i++)
                if(ui.Button(new Rect(x+i*(each+3),y,each,29),catalog.vehicles[i].name,selected:d.vehicle==i,size:12))
                    Send(ExpeditionCommand.Vehicle,i);
            each=(w-4)/2;
            for(int i=0;i<ExpeditionSlots.Names.Length;i++)
            {
                var b=new Rect(x+(i%2)*(each+4),y+39+(i/2)*32,each-21,29);
                if(ui.Button(b,"",selected:mount==i))mount=i;
                ui.Text(new Rect(b.x+5,b.y+1,b.width-10,15),ExpeditionSlots.Names[i],11,Ink,true);
                ui.Text(new Rect(b.x+5,b.y+15,b.width-10,12),catalog.Get(d.parts[i])?.name??"Empty mount",8,Muted,false,true);
                ui.Help(new Rect(b.xMax+3,b.y+6,15,15),ExpeditionGlossary.KeyForSlot(i));
            }
        }
        void DrawPart(Rect r)
        {
            var d=View.design;
            var part=catalog.Get(d.parts[mount]);
            ui.Card(r);ui.Title(r,"Component inspector",N(d.rotations[mount]*90)+"°");
            float x=r.x+11,y=r.y+53,m=r.width-22;
            ui.Text(new Rect(x,y,m,16),ExpeditionSlots.Names[mount].ToUpperInvariant(),10,Accent,false,true);
            ui.Text(new Rect(x,y+18,m,37),part?.name??"Empty mount",23,Ink,true);
            ui.Text(new Rect(x,y+57,m,36),part?.description??"Choose hardware rated for this mount.",14,Muted,false,false,TextAnchor.UpperLeft,true);
            y+=103;
            if(part!=null)
            {
                ui.Reading(x,y,m,"Mass / cost",N(part.mass)+" kg · "+N(part.cost)+" cr");
                if(part.thrust>0)ui.Reading(x,y+24,m,"Thrust / ISP",N(part.thrust)+" N · "+N(part.isp)+" s");
                else if(part.generation>0)ui.Reading(x,y+24,m,"Generation",N(part.generation)+" W");
                else if(part.fuel>0)ui.Reading(x,y+24,m,"Propellant",N(part.fuel)+" kg");
                else if(part.draw>0)ui.Reading(x,y+24,m,"Load",N(part.draw)+" W");
            }
            y+=57;int options=0;
            for(int i=0;i<catalog.parts.Length;i++)
            {
                var option=catalog.parts[i];if(option.slot!=mount)continue;
                if(ui.Button(new Rect(x,y+options*31,m,27),option.name,selected:option.id==part?.id,size:12))
                    Send(ExpeditionCommand.Install,i);
                options++;
            }
            y+=options*31+5;
            if(ui.Button(new Rect(x,y,97,28),"Rotate 90°",enabled:part!=null,size:12))Send(ExpeditionCommand.Rotate,mount);
            if(ui.Button(new Rect(x+103,y,84,28),"Detach",enabled:part!=null,size:12))Send(ExpeditionCommand.Detach,mount);
            if(mount==0&&r.yMax-y>63)
            {
                var pic=Resources.Load<Texture2D>("Frontier/"+catalog.vehicles[d.vehicle].photo);
                if(pic)GUI.DrawTexture(new Rect(x+193,y,m-192,Math.Min(69,r.yMax-y-5)),pic,ScaleMode.ScaleAndCrop);
            }
        }

        void BorderBox(Rect r)
        {
            ui.Fill(new Rect(r.x-1,r.y-1,r.width+2,1),Border);
            ui.Fill(new Rect(r.x-1,r.yMax,r.width+2,1),Border);
            ui.Fill(new Rect(r.x-1,r.y,1,r.height),Border);
            ui.Fill(new Rect(r.xMax,r.y,1,r.height),Border);
        }
    }
}
