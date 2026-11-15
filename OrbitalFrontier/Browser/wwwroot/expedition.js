import { dotnet } from './_framework/dotnet.js';
import { renderExpedition } from './expedition-world.js';
import { readSurface, createChart } from './expedition-map.js';
import { glossary } from './expedition-glossary.js';

const by=id=>document.getElementById(id);
const html=(id,v)=>{const n=by(id);if(n&&n.innerHTML!==v)n.innerHTML=v;};
const txt=(id,v)=>{const n=by(id);if(n&&n.textContent!==String(v))n.textContent=v;};
const hide=(id,v)=>{const n=by(id);if(n)n.hidden=v;};
const enc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const file=name=>'./media/'+name;
const jsonAsset=name=>fetch(file(name)).then(r=>{if(!r.ok)throw Error('Missing '+name);return r.text();});
const binaryAsset=name=>fetch(file(name)).then(r=>{if(!r.ok)throw Error('Missing '+name);return r.arrayBuffer();})
    .then(buf=>btoa(Array.from(new Uint8Array(buf),c=>String.fromCharCode(c)).join('')));
const commands=['','World','Objective','Difficulty','Launcher','OrbitProfile','Transfer','Vehicle','Install','Detach','Rotate',
    'Launch','Stage','Coast','Capture','Maneuver','BeginDescent','LandingAssist','LandingThrottle','Stabilize','Abort',
    'Move','Face','Inspect','Repair','SwitchPower','Airlock','Sample','Downlink','ReturnDesign','SetRover','Triage','Endurance'];
const slot=['Pressure hull','Tank','Main engine','Power generation','Battery bank','Landing sensor','Antenna','Landing gear',
    'Life support','Entry shield','Field instrument','Data recorder','Spares','Attitude control','Rover'];
const slotHelpKeys=['pressureHull','tank','mainEngine','powerGeneration','batteryBank','landingSensor','antenna','landingGear',
    'lifeSupport','entryShield','fieldInstrument','dataRecorder','spares','attitudeControl','rover'];
const objectives=['Surface science','Orbital survey','Systems recovery'];
const objectiveBrief=[
    'Land, collect both field records, then downlink from the cabin.',
    'Skip landing. Record two products at Science and transmit from orbit.',
    'Repair the power controller and the cabin pressure seal, then return one field record.'
];
const phases=['Design','Ascent','Transfer','Destination orbit','Terminal descent','Surface operations','Complete','Failed'];
const stations=[
    {name:'Helm',x:-.92,z:-8.35,bay:'Flight deck',description:'Stage, start descent, operate throttle and attitude dampers.'},
    {name:'Navigation',x:.92,z:-8.35,bay:'Flight deck',description:'Advance transfer time, capture orbit or adjust the trajectory.'},
    {name:'Engineering',x:-1.75,z:-3,bay:'Systems bay',description:'Measure electrical loads, isolate a fault and replace the power controller.'},
    {name:'Life support',x:1.75,z:-3,bay:'Systems bay',description:'Check pressure, repair the seal and replenish crew suits.'},
    {name:'Science',x:-1.75,z:1.05,bay:'Instrument bay',description:'Measure the landing corridor or acquire orbital science products.'},
    {name:'Communications',x:1.75,z:1.05,bay:'Instrument bay',description:'Check radio power, data volume and Earth link.'},
    {name:'Airlock',x:0,z:6.95,bay:'Aft',description:'Cycle the hatch for surface EVA and return.'}
];
const options = (list,value)=>list.map((label,i)=>'<option value="'+i+'" '+(i===value?'selected':'')+'>'+enc(label)+'</option>').join('');
const termKeys={
    'OBJECTIVE':'objective','LAUNCH VEHICLE':'launcher','DESTINATION ORBIT':'orbitProfile','TRANSFER':'transfer',
    'DIFFICULTY':'difficulty','OPERATIONS WINDOW':'operationsWindow',
    'WET MASS':'wetMass','MISSION COST':'missionCost','MANEUVER RESERVE':'maneuverReserve','SURFACE TWR':'surfaceTwr',
    'POWER / PEAK LOAD':'powerPeakLoad','BATTERY':'batteryCapacity','DATA STORAGE':'dataStorage','DOWNLINK':'downlinkRate',
    'CREW PROVISIONS':'crewProvisions','PLANNED TRANSFER':'plannedTransfer','SPARE LINE UNITS':'spareLineUnits',
    'COAST ORBIT':'coastOrbit','FIELD PACKET':'fieldPacket','DESTINATION POWER':'destinationPower',
    'Mass':'mass','Cost':'cost','Propellant':'propellant','Thrust':'thrust','Specific impulse':'specificImpulse',
    'Draw':'draw','Generation':'generation','Battery':'battery','Data per record':'dataPerRecord','Transmit rate':'transmitRate',
    'ALTITUDE':'altitude','SPEED':'speed','PROPELLANT':'propellant','MET':'missionElapsedTime',
    'OXYGEN':'oxygen','HEALTH':'health','SUIT':'suitCharge','Cabin':'cabinPressure','Time remaining':'timeRemaining',
    'Vehicle speed':'vehicleSpeed','Throttle':'throttle','Descent rate':'descentRate','Lateral drift':'lateralDrift',
    'Ground slope':'groundSlope','Days in transfer':'daysInTransfer','Periapsis':'periapsis','Apoapsis':'apoapsis',
    'Orbital period':'orbitalPeriod','Propellant reserve':'propellantReserve','Active load':'activeLoad',
    'Engine':'engineTemperature','Spare line units':'spareParts','Surface grid':'surfaceGrid','Corridor slope':'corridorSlope',
    'Science products':'scienceProducts','Recorder':'recorder','Earth link':'earthLink',
    'Round-trip light time':'roundTripLightTime','Science on board':'scienceData','Link rate':'linkRate',
    'Return to hatch':'returnRange','SURFACE GRAVITY':'surfaceGravity','MEAN RADIUS':'meanRadius',
    'REFERENCE PRESSURE':'referencePressure','SIDEREAL SPIN':'siderealSpin','MAP SPACING':'mapSpacing','MAP ORIGIN':'mapOrigin',
    'DATA DELIVERED':'scienceData','FUEL REMAINING':'propellant','STAGES SEPARATED':'stageSeparation',
    'ELAPSED TIME':'missionElapsedTime','TOUCHDOWN SPEED':'descentRate','TOUCHDOWN SLOPE':'groundSlope','Hatch':'airlock'
};
const helpButton=key=>{
    const item=glossary[key];
    if(!item)return '';
    return '<button type="button" class="help-mark" data-local="glossary" data-term="'+key+'" aria-label="What does '+enc(item.title)+' mean?" title="Learn about '+enc(item.title)+'">?</button>';
};
const explain=(label,key)=>'<span class="term-help"><span>'+enc(label)+'</span>'+helpButton(key)+'</span>';
const select=(label,act,list,value)=>'<label><span>'+explain(label,termKeys[label])+'</span><select data-select="'+act+'">'+options(list,value)+'</select></label>';
const button=(label,act,n=null,primary=false,disabled=false,a=null)=>'<button class="'+(primary?'primary':'secondary')+'" data-act="'+act+'"'+
    (n===null?'':' data-n="'+n+'"')+(a===null?'':' data-a="'+a+'"')+(disabled?' disabled':'')+'>'+label+(primary?' <span>↗</span>':'')+'</button>';
const number=(x,d=0)=>Number(x??0).toLocaleString('en-US',{maximumFractionDigits:d,minimumFractionDigits:d});
const clock=x=>{x=Math.max(0,Math.floor(x));return String(Math.floor(x/60)).padStart(2,'0')+':'+String(x%60).padStart(2,'0');};
const metric=(l,v,u='',key=termKeys[l])=>'<div class="metric"><span>'+explain(l,key)+'</span><strong>'+v+(u?' <em class="u">'+u+'</em>':'')+'</strong></div>';
const reading=(l,v,key=termKeys[l])=>'<div class="telem-line"><span>'+explain(l,key)+'</span><b>'+v+'</b></div>';
const resource=(l,v,key=termKeys[l])=>'<div class="crew-scale"><span><b>'+explain(l,key)+'</b><b>'+number(v)+'%</b></span><div class="meter"><em style="width:'+v+'%"></em></div></div>';
const source=(url,name)=>'<a href="'+enc(url)+'" target="_blank" rel="noopener">'+enc(name)+' ↗</a>';
const dist=(m,x,z)=>Math.hypot(m.x-x,m.z-z);
const heading=(x,z,me)=>((Math.atan2(x-me.x,me.z-z)*180/Math.PI)+360)%360;
const near=(m,i)=>dist(m,stations[i].x,stations[i].z)<=2.35;

let api, catalog, ground, state, world, chart, seat=0, socket=null, crewError='', crewToken='', joined=false;
let area='desk', chosen=0, preview='exterior', display='first', mapType='region', station=-1;
let worldRevision=-1, previousOutside=false, previousPhase=-1, localStatus='', localTimer=0, atlasIndex=0, lastStat=0;
let phasePanel='',yaw=0,pitch=-1, manual={throttle:.31,x:0,z:0}, trim=12;
let motion=null, dragging=null, pressLook=false, pressInspect=false, firstFlight=false, pinnedStation=-1;
const keys=new Set();
const dest=()=>catalog.worlds[state.design.world];
const person=()=>state.crew[seat];
const photo=name=>file(name+'.jpg');

function receive(out) {
    const old=state?.phase;
    state=out;
    if (old===0&&out.phase===1){station=-1;display='first';yaw=0;pitch=-1;manual.x=manual.z=0;pinnedStation=-1;}
    if (out.phase!==previousPhase) {
        if(out.phase===4) {display='chase';station=0;manual.throttle=out.design.world===0?.24:.36;}
        if(out.phase===5) {display='first';station=-1;}
        previousPhase=out.phase;
    }
    const now=person();
    if(now&&now.outside!==previousOutside){previousOutside=now.outside;station=-1;display='first';yaw=now.yaw;pitch=now.outside?-4:-1;}
}
function command(act,n=0,a=0,b=0,c=0) {
    if(!api||!state)return;
    const index=commands.indexOf(act);
    if(index<=0)return;
    if(joined) {
        if(socket?.readyState===WebSocket.OPEN) socket.send(JSON.stringify({command:index,n,a,b,c}));
        return;
    }
    receive(JSON.parse(api.ApplyExpedition(index,n,a,b,c)));
    if(act!=='Move'&&act!=='Face') render(true);
}

function show(id,content){txt('modal-title',id);html('modal-content',content);if(!by('exp-modal').open)by('exp-modal').showModal();}
function openGlossary(key){
    const item=glossary[key];
    if(!item){console.error('Missing glossary entry: '+key);return;}
    let resource=item.resource;
    if(resource==='terrain')resource={url:dest().source,label:dest().body+' terrain dataset'};
    const link=resource?'<p class="flight-note">Explore further: '+source(resource.url,resource.label)+'</p>':'';
    show(item.title,'<p class="flight-note">'+enc(item.text)+'</p>'+link);
}
function notify(message,seconds=2.8){localStatus=message;localTimer=performance.now()+seconds*1000;}
function pressed(selector,which){
    for(const b of document.querySelectorAll(selector))b.setAttribute('aria-pressed',String(b.dataset.n===String(which)));
}
function connectCrew(){
    if(socket?.readyState===WebSocket.OPEN||socket?.readyState===WebSocket.CONNECTING)return;
    const scheme=location.protocol==='https:'?'wss':'ws';
    crewError='';
    socket=new WebSocket(scheme+'://'+location.host+'/crew'+(crewToken?'?resume='+encodeURIComponent(crewToken):''));
    socket.onmessage=e=>{
        const packet=JSON.parse(e.data);
        if(packet.error){crewError=packet.error;crewModal();return;}
        const first=!joined;
        joined=true;seat=packet.seat;crewToken=packet.resume;
        if (first&&by('exp-modal').open) by('exp-modal').close();
        receive(packet.view);
        if(first&&person()){yaw=person().yaw;pitch=-1;station=-1;}
        render(first);
    };
    socket.onerror=()=>{crewError='Could not reach a crew host at this address.';};
    socket.onclose=()=>{if(!crewError)crewError='Crew connection closed. Rejoin from the room panel.';joined=false;seat=0;receive(JSON.parse(api.TickExpedition(0)));render(true);crewModal();};
}
function crewModal() {
    if(location.protocol==='file:'){
        show('Crew LAN','<p class="flight-note">The downloadable HTML runs offline in solo mode. To fly in a crew, open the LAN host included with the project. Up to four players can join its local address in their browsers.</p>');
        return;
    }
    const count=state.crew.filter(p=>p.active).length;
    show('Crew LAN','<p class="flight-note">Open this address on each crew member’s desktop. Host seat 1 can launch. Each player walks independently and can operate any reachable station.</p>'+
        '<div class="station-notice">'+enc(location.origin)+'</div>'+
        (crewError?'<p class="warn">'+enc(crewError)+'</p>':'')+
        (joined?'<p>You are seat '+(seat+1)+' of 4 · '+count+' connected</p><button class="secondary" data-local="leaveCrew">Leave crew room</button>':
            '<button class="primary" data-local="connectCrew">Join shared crew room</button>'));
}

function dataMetrics(stats){
    const items=[
        ['WET MASS',number(stats.wet)+' / '+number(stats.capacity),'kg'],['MISSION COST',number(stats.cost)+' / '+number(stats.budget),'cr'],
        ['MANEUVER RESERVE',number(stats.reserve)+' / '+number(stats.plannedDV),'m/s'],['SURFACE TWR',number(stats.twr,2),''],
        ['POWER / PEAK LOAD',number(stats.generation)+' / '+number(stats.peak),'W'],['BATTERY',number(stats.battery),'Wh'],
        ['DATA STORAGE',number(stats.storage,1),'MB'],['DOWNLINK',number(stats.bandwidth,2),'MB/s'],
        ['CREW PROVISIONS',number(stats.lifeDays),'days'],['PLANNED TRANSFER',number(stats.travelDays,2),'days'],
        ['SPARE LINE UNITS',number(stats.spares),''],['COAST ORBIT',number(stats.orbitKm),'km'],
        ['FIELD PACKET',number(stats.packet,1),'MB'],['DESTINATION POWER',number(stats.destinationGeneration),'W']
    ];
    return '<div class="compact-metrics">'+items.map(x=>metric(...x)).join('')+'</div>';
}
function readinessHelp(message){
    if(message.startsWith('Install '))return 'requiredHardware';
    if(message.includes('launcher rated'))return 'launchCapacity';
    if(message.includes('pressure hull rating'))return 'hullRating';
    if(message.includes('mission budget'))return 'budgetConstraint';
    if(message.includes('propellant for capture'))return 'deltaVConstraint';
    if(message.includes('Surface, wet TWR'))return 'twrConstraint';
    if(message.includes('Crew provisions'))return 'provisionsConstraint';
    if(message.includes('storage for the required'))return 'storageConstraint';
    return null;
}
function noteHelp(message){
    if(message.startsWith('Idle load'))return 'generationWarning';
    if(message.startsWith('Active instruments'))return 'powerPeakLoad';
    if(message.startsWith('High orbit'))return 'highOrbitWarning';
    if(message.startsWith('Fast transfer'))return 'fastTransferWarning';
    return null;
}
function readiness() {
    const b=state.budget,status=b.ready?'FLIGHT READY':'DESIGN HOLD';
    for(const id of ['ready-badge','ship-ready']){
        const tag=by(id);tag.textContent=status;tag.classList.toggle('good',b.ready);
    }
    const issues=b.issues.map(m=>'<p>'+enc(m)+helpButton(readinessHelp(m))+'</p>').join('');
    const notes=b.notes.map(m=>'<p class="flight-note">'+enc(m)+helpButton(noteHelp(m))+'</p>').join('');
    return (issues?'<div class="readiness">'+issues+'</div>':'')+notes;
}
function renderPlan(){
    const d=state.design,b=state.budget,w=dest(),building=area==='build';
    by('plan-stage').classList.toggle('building',building);
    hide('plan-desk',building);hide('shipyard',!building);hide('preview-switch',!building);
    document.querySelector('.exp-hero-copy .kicker').innerHTML=building?'<i></i> SHIPYARD':'<i></i> MISSION DESIGN';
    html('hero-title',building?'Assembly hangar':'Design the<br><em>expedition.</em>');
    txt('hero-intro',building?'Swap and rotate flight hardware. Click a 3D part to inspect it. The deck plan shows crew stations.':
        'Choose a destination, build a ship, and fly the mission from launch to fieldwork.');
    html('hero-destinations',catalog.worlds.map((q,i)=>'<div class="destination-choice"><button class="destination" data-act="World" data-n="'+i+'" aria-pressed="'+(d.world===i)+'"><b>'+
        enc(q.body)+'</b><small>'+enc(q.name)+'</small></button>'+helpButton('world')+'</div>').join(''));
    txt('preview-label',building?catalog.vehicles[d.vehicle].name.toUpperCase():w.tag+' / '+w.body.toUpperCase());
    txt('preview-credit',building?'CLICK A PART TO INSPECT  ·  DRAG TO ORBIT  ·  SCROLL TO ZOOM':w.credit);
    if(!building){
        html('plan-options','<div class="plan-fields">'+select('OBJECTIVE','Objective',objectives,d.objective)+
            select('LAUNCH VEHICLE','Launcher',catalog.launchers.map(m=>m.name),d.launcher)+
            select('DESTINATION ORBIT','OrbitProfile',[w.orbitLow+' km / low survey',w.orbitHigh+' km / wide coverage'],d.orbit)+
            select('TRANSFER','Transfer',['Efficient','Fast / high energy'],d.route)+
            select('DIFFICULTY','Difficulty',['Guided','Expert / manual'],d.difficulty)+
            select('OPERATIONS WINDOW','Endurance',['Short / 14 min','Standard / 24 min','Extended / 36 min'],d.endurance)+
            '</div><p class="flight-note">'+enc(objectiveBrief[d.objective])+'</p><p class="flight-note">Operations window '+number(b.sessionMinutes)+' min · light-time '+number(w.lightTime,1)+' s round trip.</p>');
        html('design-resources',dataMetrics(b));
        html('design-status',readiness()+'<p class="flight-note">'+(b.ready?'Verify instruments, power and habitat before committing to launch.':'Resolve the design holds before launch.')+
            '</p><div class="plan-actions">'+button('Launch expedition','Launch',null,true,!b.ready||seat!==0)+
            '<button class="secondary" data-local="firstFlight">First-flight guide</button><button class="secondary" data-local="build">Open shipyard</button><button class="secondary" data-local="crew">Crew LAN</button></div>'+
            (joined&&seat!==0?'<p class="flight-note">Seat 1 launches when the team is ready.</p>':''));
    }else{
        const matches=catalog.parts.map((p,i)=>({p,i})).filter(a=>a.p.slot===chosen);
        const installed=catalog.parts.find(p=>p.id===d.parts[chosen]);
        html('ship-presets',catalog.vehicles.map((m,i)=>'<button class="secondary" data-act="Vehicle" data-n="'+i+'" aria-pressed="'+(d.vehicle===i)+'">'+enc(m.name)+'</button>').join(''));
        html('ship-mounts',slot.map((name,i)=>{
            const p=catalog.parts.find(p=>p.id===d.parts[i]);
            return '<div class="mount-choice"><button class="secondary mount-item" data-local="part" data-n="'+i+'" aria-pressed="'+(i===chosen)+'"><b>'+enc(name)+'</b><small>'+
                enc(p?.name??'Empty mount')+'</small></button>'+helpButton(slotHelpKeys[i])+'</div>';
        }).join(''));
        const numbers=installed?[
            ['Mass',installed.mass,'kg'],['Cost',installed.cost,'cr'],['Propellant',installed.fuel,'kg'],['Thrust',installed.thrust,'N'],
            ['Specific impulse',installed.isp,'s'],['Draw',installed.draw,'W'],['Generation',installed.generation,'W'],
            ['Battery',installed.wattHours,'Wh'],['Data per record',installed.mb,'MB'],['Transmit rate',installed.rate,'MB/s']
        ].filter(r=>r[1]).map(([l,v,u])=>reading(l,number(v,v<1?2:0)+' '+u)).join(''):'';
        txt('rotation-readout',number(d.rotations[chosen]*90)+'° MOUNT');
        html('component-inspector','<span class="mono subtle">'+slot[chosen].toUpperCase()+'</span><h3 class="component-title">'+
            enc(installed?.name??'Empty mount')+'</h3><p class="flight-note">'+enc(installed?.description??'Choose hardware rated for this mounting point.')+
            '</p><div class="component-specs">'+numbers+'</div><select class="mount-select" data-select="Install" aria-label="Compatible parts">'+
            '<option value="-1">Install a different part…</option>'+matches.map(({p,i})=>'<option value="'+i+'" '+(p.id===installed?.id?'disabled':'')+'>'+enc(p.name)+'</option>').join('')+
            '</select><div class="station-actions">'+button('Rotate 90°','Rotate',chosen,false,!installed)+button('Detach','Detach',chosen,false,!installed)+'</div>'+
            (chosen===0?'<img class="heritage-preview" src="'+enc(photo(catalog.vehicles[d.vehicle].photo))+'" alt="Apollo spacecraft reference photograph"><p class="flight-note">Apollo reference photograph · NASA archive. Flyable assemblies use simulation specifications.</p>':''));
        html('ship-resources',dataMetrics(b));
        html('assembly-actions',readiness()+'<div class="plan-actions"><button class="primary" data-local="desk">Back to flight plan <span>↗</span></button></div>');
    }
    pressed('[data-local=shipPreview]',preview);
    world.update(state,building?(preview==='deck'?'deck':'ship'):'planet',seat,building?chosen:-1);
}
function setNav(key) {
    for (const i of ['desk','build','maps']) {
        const el=by('nav-'+i);
        if(i===key)el.setAttribute('aria-current','page');else el.removeAttribute('aria-current');
    }
}

function missionPrompt(s) {
    const t=s.telemetry;
    if(s.phase===1)return t.staged
        ?['Stage from the helm','Use W to reach the flight deck. Open Helm and separate the spent stage.']
        :['Monitor the burn','The launch profile runs until cutoff. Inspect the Helm to prepare for separation.'];
    if(s.phase===2) {
        if(t.fault===1)return ['Power controller degraded','Walk to Engineering. Run a diagnostic, then replace the power controller.'];
        if(t.captureBurn)return ['Capture burn running','Monitor engine temperature, battery and propellant.'];
        if(t.progress>=1)return ['Capture the orbit','At Navigation, engage the capture burn.'];
        return ['Transfer underway','Approach Navigation and select accelerated coast. Check systems during transfer.'];
    }
    if(s.phase===3) {
        if(s.design.objective===1)return ['Capture orbital science','Use Science to record both products, then downlink at Communications.'];
        if(t.measured===0)return ['Measure the landing corridor','At Science, measure terrain slope and the landing sensor return.'];
        return ['Commit to descent','Use Helm to control manual descent or engage the guidance computer.'];
    }
    if(s.phase===4)return ['Fly terminal descent','Track AGL, vertical velocity and drift. Use damper hold and monitor the touchdown envelope.'];
    if(s.design.objective===2&&t.fault===2)return ['Cabin pressure loss','Return to Life support, diagnose the pressure regulator and replace its seal.'];
    if(t.samples<(s.design.objective===2?1:2))return ['Take field measurements','Cycle the aft airlock, open the surface chart, then approach the science beacons on foot.'];
    return ['Return and transmit','Return through the airlock and send the field data from Communications.'];
}

function nearTarget(){
    const me=person(), t=state.telemetry,w=dest();
    if(!t||!me)return null;
    if(!me.outside){
        const list=stations.map((p,i)=>({i,d:dist(me,p.x,p.z)})).filter(p=>p.d<=2.35).sort((a,b)=>a.d-b.d);
        if(!list.length)return null;
        const n=list[0].i;
        return {id:n,name:n===6?'Open airlock controls':'Use '+stations[n].name,type:'station'};
    }
    if(dist(me,t.x,t.z+9.8)<=6)return {name:'Enter lander airlock',type:'airlock'};
    if(state.design.parts[14]&&(me.rover||t.roverId<0&&dist(me,t.roverX,t.roverZ)<=5))return {name:me.rover?'Leave rover':'Mount rover',type:'rover'};
    const index=w.points.findIndex((p,i)=>!t.marks[i]&&dist(me,p.x,p.z)<=10);
    return index>=0?{name:'Measure '+w.points[index].name,type:'sample',id:index}:null;
}
function useTarget(){
    const target=nearTarget();
    if(!target){notify('Move closer to a crew station, the hatch or a field beacon.');return;}
    if(target.type==='station'){if(document.pointerLockElement)document.exitPointerLock();station=target.id;render(true);}
    else if(target.type==='airlock')command('Airlock');
    else if(target.type==='sample')command('Sample',target.id);
    else command('SetRover');
}
function stationPicker(){
    const me=person(),s=state,t=s.telemetry,w=dest();
    if(me.outside){
        let body='<h3>Field instruments</h3><p class="science-note">'+enc(w.credit)+
            '</p>'+w.points.map((p,i)=>'<div class="point-card"><b>'+String.fromCharCode(65+i)+'. '+enc(p.name)+'</b><small>'+
                (t.marks[i]?'RECORDED · '+number(t.slopes[i],1)+'° · '+number(t.elevations[i],1)+' m':
                    number(dist(me,p.x,p.z))+' m · '+number(heading(p.x,p.z,me))+'° true')+'</small>'+
                (t.marks[i]?enc(p.observation)+source(p.source,'Research note'):'')+'</div>').join('');
        body+='<div class="station-actions">'+button('Measure site A','Sample',0,false,me.rover||t.marks[0]>0||dist(me,w.points[0].x,w.points[0].z)>10)+
            button('Measure site B','Sample',1,false,me.rover||t.marks[1]>0||dist(me,w.points[1].x,w.points[1].z)>10)+
            (s.design.parts[14]?button(me.rover?'Dismount':'Mount rover','SetRover',null,false,!me.rover&&(t.roverId>=0||dist(me,t.roverX,t.roverZ)>7)):'')+
            button('Re-enter lander','Airlock',null,true,dist(me,t.x,t.z+9.8)>6)+'</div>';
        const range=dist(me,t.x,t.z+9.8),azimuth=heading(t.x,t.z+9.8,me);
        return body+reading('Return to hatch','<span id="return-range">'+number(range)+'</span> m · '+number(azimuth)+'°')+
            '<p class="flight-note">Activate the surface map to check the hatch and both beacons.</p>';
    }
    return '<h3>Walk to a console</h3><p class="science-note">Click the viewport to look. Use W, A, S, D to move and E to interact.</p><div class="station-links">'+
        stations.map((p,i)=>'<button class="secondary" data-local="panel" data-n="'+i+'" aria-label="Open '+enc(p.name)+'">'+enc(p.name)+'<small>'+
            (near(me,i)?'IN REACH':number(dist(me,p.x,p.z),1)+' m')+'</small></button>').join('')+'</div>';
}
function stationContents(i){
    const s=state,t=s.telemetry,b=s.budget,w=dest(),me=person(),p=stations[i];
    let controls='';
    switch(i) {
        case 0:
            controls=reading('Vehicle speed',number(t.speed,1)+' m/s')+reading('Throttle',number(t.throttle*100)+'%')+
                (s.phase===4?reading('Descent rate',number(t.vertical,1)+' m/s')+reading('Lateral drift',number(Math.hypot(t.vx,t.vz),1)+' m/s')+
                    reading('Ground slope',number(t.soilSlope,1)+'°')+
                    '<label class="station-slider">MANUAL THROTTLE <input id="terminal-throttle" type="range" min="0" max="100" step="1" value="'+number(manual.throttle*100)+'"></label>'+
                    '<div class="station-actions">'+button(t.assist?'Switch to manual':'Engage landing assist','LandingAssist',t.assist?0:1,true)+
                    button(t.dampers?'Dampers on':'Dampers off','Stabilize')+
                    '<button class="secondary" data-local="manualNudge" data-n="0,-.55">North thrust</button><button class="secondary" data-local="manualNudge" data-n="0,.55">South thrust</button>'+
                    '<button class="secondary" data-local="manualNudge" data-n="-.55,0">West thrust</button><button class="secondary" data-local="manualNudge" data-n=".55,0">East thrust</button>'+
                    '<button class="secondary" data-local="manualNudge" data-n="0,0">Center translation</button>'+button('Abort descent','Abort')+'</div>':
                    '<div class="station-actions">'+button(t.stageCount===0?'Separate booster':'Separate transfer stage','Stage',null,true,s.phase!==1||!t.staged)+
                    button('Begin terminal descent','BeginDescent',null,false,s.phase!==3||!t.measured||s.design.objective===1)+'</div>');
            break;
        case 1:
            controls=reading('Days in transfer',number(t.cruiseDays,2)+' / '+number(b.travelDays,2))+reading('Periapsis',number(t.orbitalPeri,1)+' km')+
                reading('Apoapsis',number(t.orbitalApo,1)+' km')+reading('Orbital period',number(t.orbitalPeriod/60,1)+' min')+
                reading('Propellant reserve',number(t.deltaV)+' m/s')+
                '<div class="station-actions">'+button('Accelerated coast','Coast',1,false,s.phase!==2&&s.phase!==3)+
                button('Normal rate','Coast',0,false,s.phase!==2&&s.phase!==3)+
                button(t.captureBurn?'Burn running':'Execute orbital capture','Capture',null,true,s.phase!==2||t.progress<1||t.captureBurn)+
                '<button class="secondary" data-local="regionMap">Route / orbit map</button></div>'+
                (s.phase===3?'<label class="number-field">TANGENTIAL TRIM ±35 m/s <input id="trim-amount" type="number" min="-35" max="35" value="'+trim+'"></label><div class="station-actions"><button class="secondary" data-local="trim">Execute trim</button></div>':'');
            break;
        case 2:
            controls=reading('Generation',number(t.power)+' W')+reading('Active load',number(t.demand)+' W')+reading('Battery',number(t.battery)+' Wh')+
                reading('Engine',number(t.engineTemp,1)+' K')+reading('Spare line units',t.spares)+
                '<div class="station-actions">'+button('Run diagnostics','Inspect',i,true)+
                button('Replace controller','Repair',i,false,t.fault!==1||!(t.inspectedMask&(1<<i))||!t.spares)+
                button(t.charger?'Open generation breaker':'Close generation breaker','SwitchPower',0)+'</div>';
            if(t.fault===1)controls+='<p class="station-notice">Charge controller fault active.</p>';
            break;
        case 3:
            controls=reading('Cabin pressure',number(t.pressure,1)+' kPa')+reading('Crew oxygen',number(me.oxygen,1)+'%')+
                reading('Suit charge',number(me.suit,1)+'%')+reading('Crew health',number(me.health,1)+'%')+
                '<div class="station-actions">'+button('Check oxygen circuit','Inspect',i,true)+
                button('Replace pressure seal','Repair',i,false,t.fault!==2||!(t.inspectedMask&(1<<i))||!t.spares)+
                button(t.support?'Turn blower off':'Turn blower on','SwitchPower',1)+button('Treat & refill suit','Triage')+'</div>';
            break;
        case 4:
            controls=reading('Surface grid',number(w.spacing)+' m')+reading('Corridor slope',t.measured?number(t.soilSlope,1)+'°':'—')+
                reading('Science products',(s.design.objective===1?t.orbitalPasses:t.samples)+' / '+(s.design.objective===2?1:2))+
                reading('Recorder',number(t.stored,1)+' / '+number(b.storage,1)+' MB')+
                '<div class="station-actions">'+button(s.design.objective===1?'Acquire orbital product':'Measure landing corridor','Inspect',i,true)+
                '<button class="secondary" data-local="regionMap">Open chart</button></div>'+source(w.source,'Terrain dataset');
            if(t.finding)controls+='<p class="station-notice">'+enc(t.finding)+'</p>';
            break;
        case 5: {
            const required=s.design.objective===2?1:2;
            const measured=s.design.objective===1?t.orbitalPasses:t.samples;
            controls=reading('Earth link',t.link?'Available':'Occulted')+reading('Round-trip light time',number(t.signalDelay,2)+' s')+
                reading('Science on board',number(t.stored,2)+' MB')+reading('Link rate',number(b.bandwidth,2)+' MB/s')+
                (t.transferring?reading('Awaiting acknowledgment',number(t.txRemaining)+' sim s'):'')+
                '<div class="station-actions">'+button(t.transferring?'Transmitting…':'Transmit mission data','Downlink',null,true,
                    t.transferring||!t.link||!t.transmitter||measured<required)+button(t.transmitter?'Power radio off':'Power radio on','SwitchPower',2)+'</div>';
            break;
        }
        case 6:
            controls=reading('Hatch',s.phase===5?'Surface access':'Flight interlock')+reading('Suit oxygen',number(me.oxygen)+'%')+
                '<div class="station-actions">'+button('Cycle hatch and begin EVA','Airlock',null,true,s.phase!==5)+'</div>'+
                '<p class="science-note">Return through the aft hatch before the final downlink.</p>';
            break;
    }
    return '<p class="station-head"><span>'+enc(p.bay.toUpperCase())+'</span><span class="good">CONNECTED</span></p><p class="science-note">'+enc(p.description)+'</p>'+controls;
}

function flightHUD(){
    const me=person(),s=state,t=s.telemetry,w=dest(),g=ground[s.design.world];
    if(!me||!t)return;
    const title=phases[s.phase];
    txt('mission-phase',title.toUpperCase());
    txt('mission-title',w.name);
    let h=t.altitude,u='m';
    if(s.phase===4)h-=g.height(t.x,t.z);
    if(s.phase===5)h=0;
    if(h>=1000){h/=1000;u='km';}
    const shown=s.phase===5&&me.outside?[
        ['SUIT OXYGEN',number(me.oxygen),'%'],['HEALTH',number(me.health),'%'],
        ['ELEVATION',number(w.datumHeight+g.height(me.x,me.z),1),'m'],['MET',clock(t.time),'']
    ]:[
        ['ALTITUDE',number(h,u==='km'?1:0),u],['SPEED',number(t.speed,1),'m/s'],
        ['PROPELLANT',number(t.fuel),'kg'],['MET',clock(t.time),'']
    ];
    html('mission-telemetry',shown.map(x=>metric(...x)).join(''));
    const phaseHelp=['ascentPhase','transferPhase','orbitPhase','descentPhase','surfacePhase','downlinkPhase'];
    html('phase-steps',['Ascent','Transfer','Destination orbit','Terminal descent','Surface operations','Downlink'].map((item,i)=>{
        let arrived=s.phase-1===i;
        if(i===5&&t.transferring)arrived=true;
        return '<li class="'+(arrived?'current':s.phase-1>i?'done':'')+'"><b>'+(s.phase-1>i?'✓':'0'+(i+1))+
            '</b>'+explain(item,phaseHelp[i])+'</li>';
    }).join(''));
    const [lead,hint]=missionPrompt(s);
    html('progress-callout','<strong>'+enc(lead)+'</strong><p>'+enc(hint)+'</p>'+reading('Time remaining',clock(t.timeLeft)));
    html('crew-vitals',resource('OXYGEN',me.oxygen)+resource('HEALTH',me.health)+resource('SUIT',me.suit)+
        reading('Cabin',number(t.pressure,1)+' kPa'));
    html('event-log',(t.log??[]).slice(0,4).map(i=>'<div>'+enc(i)+'</div>').join(''));
    if(station>=0&&!near(me,station)&&!me.outside)station=-1;
    txt('station-name',station<0?(me.outside?'EVA operations':'Crew stations'):stations[station].name);
    const stationHelp=station<0?me.outside?'surfacePhase':'crewStations':
        ['helm','navigationStation','engineeringStation','lifeSupportStation','scienceStation','communicationsStation','airlock'][station];
    by('station-help').dataset.term=stationHelp;
    by('station-help').setAttribute('aria-label','What does '+glossary[stationHelp].title+' mean?');
    hide('unseat',station<0);
    const focus=document.activeElement?.id;
    if(focus!=='terminal-throttle'&&focus!=='trim-amount'&&!dragging)
        html('room-station',station>=0?stationContents(station):stationPicker());
    pressed('[data-local=motionView]',display);hide('reticle',display!=='first');
    const desc=display==='map'?(mapType==='deck'?'SHIP DECK PLAN':'OPERATIONAL CHART'):
        display==='chase'?'EXTERIOR CAMERA':me.outside?(me.rover?'ROVER CAMERA':'EVA / HELMET CAMERA'):'INTERIOR / CREW CAMERA';
    txt('view-caption',desc);
    let controls='';
    if(display==='map'){
        controls='<button class="secondary" data-local="directoryMap" data-n="deck" '+(me.outside?'disabled':'')+'>Deck plan</button>'+
            '<button class="secondary" data-local="directoryMap" data-n="region">'+(s.phase>=4?'Surface':'Route / orbit')+'</button>';
    }else if(display==='first'){
        controls='<button class="secondary" data-local="look">'+(document.pointerLockElement?'Press Esc to release':'Mouse look')+'</button>';
    }
    html('view-controls',controls);
    html('next-station',guidance());
    const choice=display==='first'&&!me.down&&station<0?nearTarget():null;
    hide('interact',!choice);if(choice)txt('interact',choice.name);
    hide('spatial-map',display!=='map');
    if(display==='map')html('chart-legend','<span>'+enc(chart(s,seat,mapType))+'</span>'+helpButton('mapLegend'));
    txt('local-feedback',performance.now()<localTimer?localStatus:me.message??t.status??'');
    world.update(s,display,seat,-1);
    world.map(yaw,pitch);
}
function debrief(){
    const t=state.telemetry,w=dest(),success=state.phase===6;
    txt('report-heading',success?'Expedition complete':'Expedition ended');
    html('exp-report','<div class="kicker"><i></i>'+enc(w.body.toUpperCase())+' · '+enc(objectives[state.design.objective].toUpperCase())+
         '</div><h2>'+enc(w.name)+'</h2>'+(success?'<div class="score">'+t.reportScore+' <small>/ 100</small></div>':'')+
         '<p>'+enc(t.summary)+'</p><div class="compact-metrics">'+
         metric('DATA DELIVERED',number(t.delivered,2),'MB')+metric('FUEL REMAINING',number(t.fuel,1),'kg')+
         metric('STAGES SEPARATED',t.stageCount)+metric('ELAPSED TIME',clock(t.time))+
         (state.design.objective!==1?metric('TOUCHDOWN SPEED',number(t.touchdownV,2),'m/s')+metric('TOUCHDOWN SLOPE',number(t.touchdownSlope,2),'°'):'')+
         '</div><p class="flight-note">'+enc(t.repaired?'The crew restored onboard systems during the mission.':'Systems remained in their last reported state.')+'</p>'+
         (success?'<div class="station-actions"><button class="secondary" data-local="maps">Review region and sources</button></div>':''));
    html('exp-log',(t.log??[]).map(entry=>'<div class="debrief-log">'+enc(entry)+'</div>').join(''));
}
function render(force=false) {
    if(!state)return;
    const p=state.phase;
    hide('planner',p!==0);hide('mission',p<1||p>5);hide('debrief',p<6);hide('load-status',true);
    txt('session-label',joined?'CREW '+(seat+1)+' / '+state.crew.filter(m=>m.active).length:'SOLO SESSION');
    setNav(p===0?area:'');
    if(p===0){
        world.show('plan');
        if(force||worldRevision!==state.revision){
            renderPlan();
            worldRevision=state.revision;
        }
    }else if(p<=5){
        world.show('flight');
        if(force||performance.now()-lastStat>135){
            lastStat=performance.now();flightHUD();
        }else if(display==='first')world.map(yaw,pitch);
    }else{
        world.show('none');
        if(force||phasePanel!==p+'/'+state.revision){debrief();phasePanel=p+'/'+state.revision;}
    }
}
function atlas(){
    const w=catalog.worlds[atlasIndex];
    show('Planetary atlas','<div class="segments">'+catalog.worlds.map((q,i)=>'<button data-local="atlasPage" data-n="'+i+'" aria-pressed="'+(i===atlasIndex)+'">'+enc(q.body)+'</button>').join('')+
        '</div><h3 class="component-title">'+enc(w.name)+'</h3><p class="flight-note">'+enc(w.subtitle)+'</p><div class="compact-metrics">'+
        metric('SURFACE GRAVITY',number(w.gravity,3),'m/s²')+metric('MEAN RADIUS',number(w.radius/1000,1),'km')+
        metric('REFERENCE PRESSURE',w.pressure?number(w.pressure,2):'Exosphere',''+(w.pressure?'kPa':''))+
        metric('SIDEREAL SPIN',number(w.spin/3600,2),'h')+metric('MAP SPACING',number(w.spacing),'m')+
        metric('MAP ORIGIN',number(w.latitude,3)+'° N / '+number(w.longitude,3)+'° E')+
        '</div><p class="science-note">'+enc(w.description)+'</p><p class="flight-note">'+source(w.source,'Terrain dataset')+
        ' · '+source(w.research,'Mission research')+'</p>');
}
function controls(){
    show('Flight and EVA controls',
        '<div class="atlas-text"><p class="flight-note"><b>Move</b> with W, A, S, D or arrow keys. Click the viewport to capture mouse look. Press Esc to release it. Drag inside the view if capture is unavailable.</p>'+
        '<p class="flight-note"><b>Interact</b> using E or the action prompt. The crew directory shows distance to each console. It opens the console once you are within reach.</p>'+
        '<p class="flight-note"><b>Maps</b> show the deck plan, transfer route, orbital track or surface chart. Press M for the map and V to cycle views.</p>'+
        '<p class="flight-note"><b>Guided mode</b> flies terminal descent with thrust and lateral corrections. In Expert, use the helm throttle and thrusters. [ and ] adjust throttle in 4% steps. You can switch guidance in flight.</p>'+
        '<p class="flight-note"><b>EVA</b> begins at the aft airlock. Return before oxygen runs out. Operate the field instrument at each beacon, then transmit the data from the cabin.</p></div>');
}
function setMode(mode,kind){
    display=mode;
    if(kind)mapType=kind;
    if(!kind&&mode==='map')mapType=state.phase>=4||person().outside?'region':'deck';
    if(mode!=='first'&&document.pointerLockElement)document.exitPointerLock();
    render(true);
}
function requestLook(){
    if(display!=='first')return;
    const surface=world.canvas;
    if(!surface?.requestPointerLock)return;
    try{surface.requestPointerLock()?.catch(()=>notify('Drag the viewport to look.'));}catch{notify('Drag the viewport to look.');}
}
function firstFlightBrief(){
    show('First-flight guide','<p class="flight-note">This guided flight loads the default Moon ship and adds directions to the next station. You still walk there and operate each control.</p>'+
        '<ol class="guide-list"><li><b>Helm:</b> separate both spent stages.</li><li><b>Navigation:</b> accelerate coast, then capture orbit.</li>'+
        '<li><b>Engineering:</b> diagnose and replace the failed charge controller.</li><li><b>Science, then Helm:</b> measure the site and fly terminal descent.</li>'+
        '<li><b>Aft airlock:</b> begin EVA. Follow the map to A and B and collect both records.</li><li><b>Communications:</b> return to the cabin and send the data.</li></ol>'+
        '<div class="station-actions"><button class="primary" data-local="guidedLaunch" '+(seat!==0?'disabled':'')+'>Fly the guided Moon mission <span>↗</span></button></div>');
}
function guidedLaunch(){
    if(seat!==0||state.phase!==0)return;
    firstFlight=true;area='desk';
    for(const [act,value] of [['Vehicle',0],['Objective',0],['Difficulty',0],['Launcher',0],['Transfer',0],['OrbitProfile',0],['Endurance',1]]) command(act,value);
    by('exp-modal').close();
    command('Launch');
}
function nextStation(s){
    const t=s.telemetry;
    if(!t||person().outside)return -1;
    if(s.phase===1||s.phase===4)return 0;
    if(s.phase===2)return t.fault===1?2:1;
    if(s.phase===3)return s.design.objective===1?(t.orbitalPasses>=2?5:4):(t.measured===0?4:0);
    if(s.phase===5)return t.fault===2?3:t.samples>=(s.design.objective===2?1:2)?5:6;
    return -1;
}
function guidance() {
    if(!firstFlight&&pinnedStation<0) return '<button class="ghost" data-local="toggleGuide">Show first-flight guidance</button>';
    const me=person(), id=pinnedStation<0?nextStation(state):pinnedStation;
    if(me.outside)return '<div class="guide-band"><b>Surface checklist</b><span>Use the Surface chart and the field instrument range to find both survey points.</span></div>';
    if(id<0)return '';
    const s=stations[id],meters=dist(me,s.x,s.z),bear=heading(s.x,s.z,me);
    return '<div class="guide-band"><b>'+enc(s.name)+' · '+enc(s.bay)+'</b><span>'+
        (meters<=2.35?'In reach. Use E or open the console from the directory.':
            number(meters,1)+' m · '+number(bear)+'° relative to bow. Open the Deck plan to follow your marker.')+
        '</span><button class="ghost" data-local="deckGuide">Deck plan ↗</button></div>';
}
function goArea(kind){
    if(state.phase!==0){
        if(seat!==0){notify('The host manages mission reset.');return;}
        show('Return to mission design','<p class="flight-note">Reset the current expedition and keep the spacecraft configuration?</p>'+
            '<div class="station-actions"><button class="primary" data-local="confirmReset" data-n="'+kind+'">Return to design</button></div>');
        return;
    }
    area=kind;station=-1;render(true);
}
document.addEventListener('click',e=>{
    const el=e.target.closest('button');if(!el||el.disabled||!state)return;
    const act=el.dataset.act;
    if(act){
        if(act==='LandingAssist'&&el.dataset.n==='0'){
            manual.throttle=state.telemetry.throttle;manual.x=manual.z=0;
            command('LandingThrottle',0,manual.throttle,0,0);
        }else command(act,+el.dataset.n||0,+el.dataset.a||0);
        return;
    }
    const n=el.dataset.n;
    switch(el.dataset.local){
        case 'glossary':openGlossary(el.dataset.term);break;
        case 'desk':case 'build':goArea(el.dataset.local);break;
        case 'confirmReset':
            by('exp-modal').close();area=n;station=-1;command('ReturnDesign');break;
        case 'maps':atlasIndex=state.design.world;atlas();break;
        case 'atlasPage':atlasIndex=+n;atlas();break;
        case 'help':controls();break;
        case 'crew':crewModal();break;
        case 'firstFlight':firstFlightBrief();break;
        case 'guidedLaunch':guidedLaunch();break;
        case 'toggleGuide':firstFlight=true;render(true);break;
        case 'deckGuide':setMode('map','deck');break;
        case 'connectCrew':connectCrew();break;
        case 'leaveCrew':
            if(socket){socket.onclose=null;socket.close();}joined=false;seat=0;previousOutside=false;
            crewError='';receive(JSON.parse(api.TickExpedition(0)));
            by('exp-modal').close();render(true);break;
        case 'close':by('exp-modal').close();break;
        case 'part':chosen=+n;render(true);break;
        case 'shipPreview':preview=n;render(true);break;
        case 'resetCamera':world.reset();break;
        case 'motionView':setMode(n);break;
        case 'directoryMap':setMode('map',n);break;
        case 'regionMap':setMode('map','region');break;
        case 'panel':
            if(near(person(),+n)){if(document.pointerLockElement)document.exitPointerLock();station=+n;pinnedStation=-1;render(true);}
            else {pinnedStation=+n;notify(stations[+n].name+' • '+stations[+n].bay+' · open the Deck plan to locate it.');render(true);}
            break;
        case 'interact':useTarget();break;
        case 'unseat':station=-1;render(true);break;
        case 'look':requestLook();break;
        case 'trim':command('Maneuver',0,trim);break;
        case 'manualNudge': {
            const [x,z]=n.split(',').map(Number);manual.x=x;manual.z=z;
            command('LandingThrottle',0,manual.throttle,x,z);break;
        }
    }
});
document.addEventListener('change',e=>{
    const part=e.target.dataset.select;
    if(part&&+e.target.value>=0)command(part,+e.target.value);
    if(e.target.id==='terminal-throttle'){
        manual.throttle=+e.target.value/100;
        command('LandingThrottle',0,manual.throttle,manual.x,manual.z);
    }
    if(e.target.id==='trim-amount'){trim=Math.max(-35,Math.min(35,Number(e.target.value)||0));}
});
document.addEventListener('input',e=>{
    if(e.target.id==='terminal-throttle')manual.throttle=+e.target.value/100;
    if(e.target.id==='trim-amount')trim=+e.target.value;
});
const directionKeys=['KeyW','KeyA','KeyS','KeyD','ArrowUp','ArrowDown','ArrowRight','ArrowLeft'];
document.addEventListener('keydown',e=>{
    if(!state||state.phase<1||state.phase>5||e.target.closest('input,select,textarea')||by('exp-modal').open)return;
    if(directionKeys.includes(e.code)){keys.add(e.code);e.preventDefault();return;}
    if(e.repeat)return;
    if(e.code==='KeyE'){e.preventDefault();useTarget();}
    if(e.code==='BracketLeft'||e.code==='BracketRight'){
        if(state.phase===4&&near(person(),0)){
            manual.throttle=Math.max(0,Math.min(1,manual.throttle+(e.code==='BracketRight'?.04:-.04)));
            command('LandingThrottle',0,manual.throttle,manual.x,manual.z);
        }
    }
    if(e.code==='KeyM'){e.preventDefault();setMode(display==='map'?'first':'map');}
    if(e.code==='KeyV'){e.preventDefault();setMode(display==='first'?'chase':display==='chase'?'map':'first');}
    if(e.code==='KeyR'&&person().outside)command('SetRover');
    if(e.code==='Escape'&&station>=0){station=-1;render(true);}
});
document.addEventListener('keyup',e=>keys.delete(e.code));
window.addEventListener('blur',()=>{keys.clear();motion=null;});
function alterLook(dx,dy){
    if(!state||state.phase<1||state.phase>5||display!=='first')return;
    yaw=(yaw+dx*.18+360)%360;pitch=Math.max(-74,Math.min(74,pitch-dy*.15));
    world.map(yaw,pitch);
}
document.addEventListener('mousemove',e=>{
    if(document.pointerLockElement===world?.canvas)alterLook(e.movementX,e.movementY);
});
document.addEventListener('pointerlockchange',()=>{if(state)render(true);});
function bindViewport(){
    const canvas=world.canvas;
    if(!canvas)return;
    canvas.addEventListener('pointerdown',e=>{
        if(e.button!==0)return;
        dragging=[e.clientX,e.clientY];pressLook=state.phase>=1&&display==='first';
        pressInspect=state.phase===0&&area==='build'&&preview==='exterior';
        canvas.setPointerCapture(e.pointerId);
    });
    canvas.addEventListener('pointermove',e=>{
        if(!dragging||document.pointerLockElement===canvas)return;
        const dx=e.clientX-dragging[0],dy=e.clientY-dragging[1];
        dragging=[e.clientX,e.clientY];if(Math.abs(dx)+Math.abs(dy)>2){pressLook=false;pressInspect=false;}
        if(state.phase===0||display!=='first')world.rotate(dx,dy);else alterLook(dx,dy);
    });
    canvas.addEventListener('pointerup',e=>{
        dragging=null;
        if(pressLook)requestLook();
        if(pressInspect){
            const picked=world.pick?.(e.clientX,e.clientY)??-1;
            if(picked>=0){chosen=picked;render(true);}
        }
        pressLook=pressInspect=false;
    });
    canvas.addEventListener('pointercancel',()=>{dragging=null;pressLook=pressInspect=false;});
    canvas.addEventListener('wheel',e=>{
        if(state.phase===0&&area==='build'||display==='chase'){e.preventDefault();world.zoom(e.deltaY);}
    },{passive:false});
    if(matchMedia('(pointer: coarse)').matches){
        const pad=document.createElement('div');pad.className='mobile-motion';
        pad.innerHTML='<button data-key="KeyW" aria-label="Move forward">↑</button><button data-key="KeyS" aria-label="Move back">↓</button><button data-key="KeyA" aria-label="Move left">←</button><button data-key="KeyD" aria-label="Move right">→</button>';
        by('flight-world').parentElement.append(pad);
        pad.addEventListener('pointerdown',e=>{const key=e.target.dataset.key;if(key){keys.add(key);e.target.setPointerCapture(e.pointerId);}});
        pad.addEventListener('pointercancel',()=>keys.clear());
        pad.addEventListener('pointerup',e=>keys.delete(e.target.dataset.key));
    }
}
try{
    const [runtime,expeditionData,moon,mars]=await Promise.all([
        dotnet.withDiagnosticTracing(false).create(),jsonAsset('expedition.json'),binaryAsset('moon_height.bytes'),binaryAsset('mars_height.bytes')
    ]);
    api=(await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).BrowserGame;
    catalog=JSON.parse(expeditionData);
    ground=[readSurface(catalog.worlds[0],moon),readSurface(catalog.worlds[1],mars)];
    receive(JSON.parse(api.StartExpedition(expeditionData,moon,mars)));
    world=renderExpedition({plan:by('plan-world'),flight:by('flight-world')},file,catalog,ground);
    chart=createChart(by('map-canvas'),catalog,ground,file,stations);
    bindViewport();render(true);
    let last=performance.now();
    setInterval(()=>{
        const now=performance.now();const dt=(now-last)/1000;last=now;
        if(document.hidden||state.phase<1||state.phase>5)return;
        const walking=(display==='first'||display==='map')&&!by('exp-modal').open;
        const x=walking?Number(keys.has('KeyD')||keys.has('ArrowRight'))-Number(keys.has('KeyA')||keys.has('ArrowLeft')):0;
        const z=walking?Number(keys.has('KeyW')||keys.has('ArrowUp'))-Number(keys.has('KeyS')||keys.has('ArrowDown')):0;
        const m=x+','+z+','+Math.round(yaw);
        if(m!==motion||x||z){motion=m;command('Move',0,x,z,yaw);}
        if(!joined)receive(JSON.parse(api.TickExpedition(Math.min(.25,dt))));
        render();
    },85);
}catch(error){
    txt('load-status','The flight computer could not start. Open the complete offline build in a desktop browser.');
    console.error(error);
}
