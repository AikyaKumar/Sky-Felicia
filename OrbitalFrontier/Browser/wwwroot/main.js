import { dotnet } from './_framework/dotnet.js';
import { createWorld } from './world.js';

const $ = (id) => document.getElementById(id);
const screens = ['home','hangar','flight','report'];
const actions = ['','OpenHangar','Home','LoadPreset','Install','Detach','Rotate','Assistance','Deploy','Warp','Pointing',
  'Throttle','Plan','Suggest','Execute','Stop','System','Target','Resolution','Access','Scan','Recommend','Transmit'];
const mounts = ['Command bus','Propellant tank','Main engine','Solar array','Battery','Laser altimeter','Antenna'];
const mountCodes = ['CB','PT','ME','SA','BT','LA','RF'];
const warps = [0,1,5,25,160];
const headings = ['Prograde','Outward','Retrograde','Inward'];
const fnum = (number, digits = 1) => new Intl.NumberFormat('en', {minimumFractionDigits:digits, maximumFractionDigits:digits}).format(number);
const coordinate = (value, positive, negative) => fnum(Math.abs(value),2)+'° '+(value<0?negative:positive);
const timecode = seconds => [Math.floor(seconds/3600),Math.floor(seconds/60)%60,Math.floor(seconds)%60].map(v=>String(v).padStart(2,'0')).join(':');
const text = (id, value) => { const node=$(id); if(node) node.textContent=value; };
const current = (selector, key, value) => {
    for(const element of document.querySelectorAll(selector)) element.setAttribute('aria-pressed',String(element.dataset[key]===String(value)));
};
const esc = value => String(value).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const metric = (label,value,unit='') => '<div class="metric"><span>'+label+'</span><strong>'+value+(unit?' <em>'+unit+'</em>':'')+'</strong></div>';
const reading = (label,value) => '<div class="telem-line"><span>'+label+'</span><b>'+value+'</b></div>';
const action = (label,act,kind='secondary',n) => '<button class="'+kind+'" data-act="'+act+'"'+(n===undefined?'':' data-n="'+n+'"')+'>'+label+'</button>';
const field = (id) => '<span id="'+id+'">—</span>';
const setPressed = (id,on) => $(id)?.setAttribute('aria-pressed',String(on));
const disabled = (id,on) => { if($(id)) $(id).disabled=on; };
const meter = id => '<div class="meter" aria-hidden="true"><span id="'+id+'"></span></div>';
const setMeter = (id,p) => { if($(id)) $(id).style.width=(100*Math.max(0,Math.min(1,p)))+'%'; };
const photoSrc = name => './media/'+name+'.jpg';
const link = (url,label) => '<a target="_blank" rel="noopener" href="'+esc(url)+'">'+label+' ↗</a>';

let api, data, state, world, view, station='nav', selectedMount=-1, atlasPage=0, cameraMode='map';
let localScreen = null;
let mapKey = '';
const withCockpit = (fn) => { if(state?.flight && state.screen===2) fn(); };
const dispatch = (name,n=0,v=0,v2=0) => {
    if(!api) return;
    localScreen = null;
    const previous=state.screen;
    state=JSON.parse(api.Apply(actions.indexOf(name),n,v,v2));
    if (state.screen===2 && previous!==2) { station='nav'; cameraMode='map'; delete $('station').dataset.current; }
    render(true);
};

function render(force=false) {
    const next = localScreen ?? screens[state.screen];
    if (next!==view) {
        for (const id of screens) $(id).hidden=id!==next;
        world.display(next);
        view=next;
        force=true;
    }
    for (const btn of document.querySelectorAll('[data-nav]')) {
        if(btn.dataset.nav===view) btn.setAttribute('aria-current','page');
        else btn.removeAttribute('aria-current');
    }
    document.querySelector('[data-nav=flight]').hidden=state.screen!==2;
    text('header-state',state.screen===2?'LUNAR TELEMETRY':'SOLO SESSION');
    if (view==='home') {
        try {
            const lastScore = localStorage.getItem('orbital-frontier-last-score');
            text('home-last-score',lastScore===null?'Moon · Near side':('Last survey · '+lastScore+'/100'));
        } catch { /* Storage can be blocked. */ }
    }
    if(view==='hangar' && force) renderHangar();
    if(view==='flight') {
        if(force && $('station').dataset.current!==station) renderStation();
        updateFlight();
    }
    if(view==='report' && force) renderReport();
}

function renderHangar() {
    const design=state.design, stats=state.stats, preset=data.presets[design.preset];
    $('presets').innerHTML=data.presets.map((p,i)=>action(esc(p.name)+'<span>'+esc(p.role)+'</span>','LoadPreset','',i)
        .replace('data-act','data-preset="'+i+'" data-act')).join('');
    current('[data-preset]','preset',design.preset);
    $('mounts').innerHTML=mounts.map((m,i)=>{
        const installed=data.parts.find(p=>p.id===design.parts[i]);
        return '<button data-local="mount" data-n="'+i+'" aria-pressed="'+(selectedMount===i)+'"><b>'+mountCodes[i]+
            '</b><span><strong>'+m+'</strong><small>'+esc(installed?.name??'Mount empty')+' ›</small></span></button>';
    }).join('');
    text('craft-label',preset.name);
    const badge=$('build-state');
    badge.textContent=stats.ready?'FLIGHT READY':'HOLD';
    badge.classList.toggle('good',stats.ready);
    $('craft-stats').innerHTML=metric('WET MASS',fnum(stats.wet,0),'kg')+metric('DELTA-V',fnum(stats.deltaV,0),'m/s')+
        metric('SOLAR / SUNLIT',fnum(stats.power,0),'W')+metric('BUDGET',fnum(stats.cost,0)+' / '+fnum(data.mission.maxCost,0),'cr')+
        metric('FUEL',fnum(stats.fuel,0),'kg')+metric('LUNAR TWR',fnum(stats.lunarTwr,2));
    $('craft-center').innerHTML=reading('CoM x / y / z', [stats.centerX,stats.center,stats.centerZ].map(v=>fnum(v,2)).join(' / ')+' m');
    $('build-issues').hidden=stats.ready;
    $('build-issues').textContent=stats.issues.join(' ');
    disabled('deploy',!stats.ready);
    world.updateCraft(design,selectedMount);
    renderInspector();
}

function renderInspector(hover=selectedMount) {
    const preset=data.presets[state.design.preset];
    const active=hover>=0?data.parts.find(p=>p.id===state.design.parts[hover]):null;
    let content;
    if(hover<0) {
        content='<div class="mono subtle">REFERENCE VEHICLE</div><h3>'+esc(preset.heritage)+'</h3><p>'+esc(preset.description)+'</p>'+
            '<div class="heritage"><button class="photo-button" data-local="heritage" aria-label="Enlarge archival photograph"><img src="'+esc(photoSrc(preset.photo))+'" alt="'+esc(preset.heritage)+' in lunar orbit"></button>'+
            '<div><b>Archive photograph</b><br>'+link(preset.source,'NASA mission record')+'</div></div>'+
            '<div class="historical"><p>'+esc(preset.historical)+'</p>'+link(preset.specSource,'Apollo technical record')+'</div>';
    } else {
        const specs=active?[fnum(active.mass,0)+' kg',fnum(active.cost,0)+' cr',active.thrust?fnum(active.thrust,0)+' N':'',
            active.isp?fnum(active.isp,0)+' s Isp':'',active.fuel?fnum(active.fuel,0)+' kg propellant':'',active.gen?fnum(active.gen,0)+' W output':'',active.load?fnum(active.load,0)+' W':'',active.capacity?fnum(active.capacity,0)+' Wh':''].filter(Boolean):[];
        content='<div class="mono subtle">'+esc(mounts[hover].toUpperCase())+' · SIMULATION PART</div><h3>'+esc(active?.name??'Empty mount')+'</h3>'+
            '<p>'+esc(active?.description??'Choose a compatible part for this socket.')+'</p><div class="mono">'+specs.join(' · ')+'</div>';
        if (hover===selectedMount) {
            content+='<label class="mono subtle" for="replacement">COMPATIBLE HARDWARE</label><select class="mount-select" id="replacement">'+
                '<option value="-1">Select replacement…</option>'+data.parts.map((p,i)=>p.slot===hover?
                '<option value="'+i+'" '+(p.id===active?.id?'disabled':'')+'>'+esc(p.name)+'</option>':'').join('')+
                '</select><div class="mount-controls">'+action('Rotate 90°','Rotate','secondary',hover)+action('Detach','Detach','secondary',hover)+
                '<button class="ghost" data-local="heritage">Heritage photo ↗</button></div>';
        }
    }
    $('inspector').innerHTML='<div class="inspector-content">'+content+'</div>';
}

function renderStation(){
    $('station').dataset.current=station;
    current('[data-station]','station',station);
    let content='';
    if(station==='nav') {
        content='<h2>Trajectory planning</h2><p>Set a tangential correction for the planned ignition time.</p><div class="stats-grid">'+
            metric('PERILUNE',field('nav-peri'),'km')+metric('APOLUNE',field('nav-apo'),'km')+
            metric('INCLINATION',field('nav-inc'),'°')+metric('PERIOD',field('nav-period'),'min')+'</div>'+
            reading('Eccentricity',field('nav-e'))+
            '<div class="inputs"><label>BURN ΔV (m/s)<input id="input-burn" type="number" min="-120" max="120" step="0.1" value="'+state.flight.burn+'"></label>'+
            '<label>START IN (s)<input id="input-delay" type="number" min="0" max="1800" step="1" value="'+Math.round(state.flight.burnAt-state.flight.time)+'"></label></div>'+
            reading('Predicted perilune',field('nav-pred'))+reading('Ignition',field('nav-eta'))+
            '<div class="console-actions">'+action('Suggest correction','Suggest')+'<button class="primary" id="execute" data-act="Execute">Execute burn</button>'+action('Abort','Stop')+'</div>';
    } else if (station==='pilot') {
        content='<h2>Flight control</h2><p>Changing attitude slews the thrust vector in the orbital plane.</p>'+
            '<div class="stats-grid">'+metric('VELOCITY',field('pilot-speed'),'m/s')+metric('ALTITUDE',field('pilot-alt'),'km')+
            metric('THROTTLE',field('pilot-throttle'),'%')+metric('FUEL REMAINING',field('pilot-fuel'),'kg')+'</div>'+
            '<h3>Attitude hold</h3><div class="segments">'+headings.map((h,i)=>action(h,'Pointing','',i)).join('')+'</div>'+
            '<h3>Manual throttle</h3><div class="segments">'+[0,.25,.5,1].map((v,i)=>action([0,25,50,100][i]+'%','Throttle','',v)).join('')+'</div>'+
            '<div class="console-actions">'+action('Abort all operations','Stop')+'</div>';
    } else if (station==='eng') {
        content='<h2>Electrical & thermal</h2><p>Lunar eclipse removes solar power. Battery reserve supplies the bus.</p>'+
            '<div class="stats-grid">'+metric('GENERATION',field('power-generation'),'W')+metric('BUS LOAD',field('power-demand'),'W')+
            metric('BATTERY',field('power-battery'),'Wh')+metric('ENGINE TEMP.',field('power-temp'),'K')+'</div>'+meter('power-meter')+
            ['Solar arrays','Lidar bus','Radio bus'].map((label,i)=>'<button id="sys-'+i+'" class="switch" data-act="System" data-n="'+i+'"><span>'+label+'</span><strong id="sys-label-'+i+'"></strong></button>').join('')+
            reading('Avionics',field('power-av'))+reading('Engine',field('power-engine'));
    } else if (station==='science') {
        content='<h2>Laser altimetry</h2><p>Compare terrain slopes across three NASA LOLA tiles.</p>'+
            '<div id="science-sites" class="segments survey-choices">'+data.sites.map((s,i)=>action(esc(s.name)+' <strong id="scan-site-'+i+'">—</strong>','Target','',i)).join('')+'</div>'+
            '<div class="segments">'+action('Coarse · 8×8','Resolution','',1)+action('Precision · 24×24','Resolution','',2)+'</div>'+
            reading('Slant range',field('scan-range'))+reading('Target access',field('scan-access'))+
            '<div class="console-actions"><button class="secondary" id="access" data-act="Access">Coast to access</button><button class="primary" id="scan" data-act="Scan">Start scan</button></div>'+
            '<div id="scan-state" class="mono" style="margin-top:9px"></div>'+meter('scan-meter')+'<div id="science-product"></div>';
    } else {
        content='<h2>Earth downlink</h2><p>Nominate the lowest-slope candidate and send its precision map.</p>'+
            reading('Line of sight',field('comms-live'))+reading('Science storage',field('comms-data'))+
            '<div id="comms-sites" class="segments survey-choices">'+data.sites.map((s,i)=>action(esc(s.name)+' <strong id="recommend-'+i+'">—</strong>','Recommend','',i)).join('')+'</div>'+
            '<button class="primary wide-btn" id="transmit" data-act="Transmit">Transmit recommendation</button>'+
            '<div class="mono" id="comms-state" style="margin-top:8px"></div>'+meter('comms-meter')+'<div id="comms-product"></div>';
    }
    $('station').innerHTML='<div class="console-inner">'+content+'</div>';
    mapKey='';
    $('input-burn')?.addEventListener('change',setPlanFromInputs);
    $('input-delay')?.addEventListener('change',setPlanFromInputs);
}

function setPlanFromInputs() {
    const v=$('input-burn')?.valueAsNumber, delay=$('input-delay')?.valueAsNumber;
    if(Number.isFinite(v)&&Number.isFinite(delay)) dispatch('Plan',0,v,delay);
}

function markActions(name,n) {
    for(const node of document.querySelectorAll('#station [data-act='+name+']'))
        node.setAttribute('aria-pressed',String(+node.dataset.n===n));
}
function drawProduct(target,product,site) {
    const node=$(target);
    const key=target+'/'+site+'/'+(product?.level??0);
    if(mapKey===key) return;
    mapKey=key;
    if (!product) { node.innerHTML=''; return; }
    node.innerHTML='<div class="survey-output"><canvas aria-label="Color-coded slope map. Green is flatter. Orange is steeper." width="'+product.side+'" height="'+product.side+'"></canvas>'+
        '<div>'+reading('90th pct. slope',fnum(product.slope90,2)+'°')+reading('Cell spacing',fnum(product.cellKm,2)+' km')+
        reading('Relief',fnum(product.high-product.low,0)+' m')+reading('Volume',fnum(product.volume,2)+' MB')+'</div></div>';
    const ctx=node.querySelector('canvas').getContext('2d');
    const id=ctx.createImageData(product.side,product.side);
    product.slope.forEach((s,i)=>{
        const ratio=Math.min(1,s/15), base=i*4;
        id.data[base]=Math.round(38+211*ratio);
        id.data[base+1]=Math.round(120+52*ratio);
        id.data[base+2]=Math.round(119-65*ratio);
        id.data[base+3]=255;
    });
    ctx.putImageData(id,0,0);
}

function updateFlight(){
    const f=state.flight,e=f.elements,tgt=data.sites[f.target],total=f.scanned.filter(v=>v>0).length;
    text('mission-time','MET '+timecode(f.time));
    if(!$('warps').children.length)
        $('warps').innerHTML=warps.map((v,i)=>action(v===0?'Pause':'×'+v,'Warp','',i)).join('');
    for(const button of $('warps').children) button.setAttribute('aria-pressed',String(warps[+button.dataset.n]===f.warp));
    const steps=[
        ['Qualify the orbit','Perilune 46–92 km',f.stage>=1],
        ['Map three sites',total+' / 3 survey products',f.stage>=2],
        ['Choose a candidate','Compare slope and confirm in precision',f.scanned[f.selected]===2&&f.stage>=2],
        ['Downlink the map','Link must have Earth line of sight',f.stage===3]
    ];
    let shown=false;
    $('objectives').innerHTML=steps.map((s,i)=>{
        const next=!shown&&!s[2]; if(next) shown=true;
        return '<li class="'+(s[2]?'done':next?'current':'')+'"><span>'+(s[2]?'✓':'0'+(i+1))+
            '</span><div><strong>'+s[0]+'</strong><small>'+s[1]+'</small></div></li>';
    }).join('');
    $('side-telemetry').innerHTML=reading('Altitude',fnum(e.altitude,1)+' km')+reading('Velocity',fnum(e.speed,0)+' m/s')+
        reading('Reserve',fnum(f.deltaV,0)+' m/s')+reading('Sunlight',f.sunlit?'Day side':'Eclipse')+reading('Earth link',f.visible?'Clear':'Blocked');
    text('map-target',tgt.name.toUpperCase()+'  //  '+coordinate(tgt.lat,'N','S')+'  '+coordinate(tgt.lon,'E','W'));
    text('flight-alert',f.status);
    text('log-feed',f.log[0]??'');
    if(cameraMode==='terrain'&&!f.products[f.target]) {cameraMode='map';world.reset();}
    disabled('terrain-view',!f.products[f.target]);
    current('.flight-mode button','local',cameraMode==='chase'?'chaseView':cameraMode==='terrain'?'terrainView':'mapView');
    text('flight-view-label',cameraMode==='map'?'Orbital map':cameraMode==='chase'?'Chase camera':'Survey terrain');
    text('flight-frame-label',cameraMode==='terrain'?'LOLA ELEVATION MODEL  ·  8× VERTICAL SCALE':'SELENOCENTRIC FRAME');
    if (cameraMode==='terrain') {
        $('legend-a').innerHTML='<i style="background:#68bdae"></i> LOWER SLOPE';
        $('legend-b').innerHTML='<i style="background:#e9ad5e"></i> HIGHER SLOPE';
        $('legend-c').innerHTML='<i style="background:#9895b0"></i> NORTH AT FAR EDGE';
    } else {
        $('legend-a').innerHTML='<i></i> LIVE ORBIT';
        $('legend-b').innerHTML='<i></i> BURN PREVIEW';
        $('legend-c').innerHTML='<i></i> SURVEY TARGET';
    }
    world.updateFlight(f,data,cameraMode);

    if(station==='nav') {
        text('nav-peri',fnum(e.peri,1)); text('nav-apo',e.bound?fnum(e.apo,1):'—'); text('nav-inc',fnum(e.inclination,1));
        text('nav-period',e.bound?fnum(e.period/60,1):'—'); text('nav-e',fnum(e.eccentricity,3)); text('nav-pred',fnum(f.predicted.peri,1)+' km');
        text('nav-eta',f.autoBurn?(f.burnAt>f.time?'T−'+timecode(f.burnAt-f.time):fnum(f.burnRemaining,1)+' m/s left'):'MET '+timecode(f.burnAt));
        disabled('execute',f.autoBurn||Math.abs(f.burn)<.1||Math.abs(f.burn)>f.deltaV||f.scanning||f.transmitting);
    } else if(station==='pilot') {
        text('pilot-speed',fnum(e.speed,0));text('pilot-alt',fnum(e.altitude,1));text('pilot-throttle',fnum(f.throttle*100,0));text('pilot-fuel',fnum(f.fuel,1));
        markActions('Pointing',f.pointing);markActions('Throttle',f.commandedThrottle);
    } else if(station==='eng') {
        text('power-generation',fnum(f.generated,0));text('power-demand',fnum(f.demand,0));
        text('power-battery',fnum(f.battery,0));text('power-temp',fnum(f.temperature,0));setMeter('power-meter',f.battery/f.batteryCapacity);
        [f.solar,f.lidar,f.radio].forEach((status,i)=>{setPressed('sys-'+i,status);text('sys-label-'+i,status?'ONLINE':'OFF');});
        text('power-av',f.bus?'Powered':'Undervoltage');text('power-engine',f.engineLimited?'Thermally limited':'Nominal');
    } else if(station==='science') {
        const names=['Pending','Coarse','Precision'];
        f.scanned.forEach((v,i)=>text('scan-site-'+i,names[v]));
        markActions('Target',f.target);markActions('Resolution',f.scanLevel);
        text('scan-range',fnum(f.scanRange,0)+' km');text('scan-access',f.scanWindow?'In window':(f.accessEta>0?(f.waiting?'ETA ':'In ')+timecode(f.accessEta):'Out of range'));
        disabled('access',f.stage<1||f.scanning||f.transmitting||f.autoBurn||f.scanWindow||f.waiting);
        disabled('scan',!f.available||f.scanning);text('scan-state',f.scanning?'Acquiring elevation cells…':f.scanProgress===1?'Survey stored':'');
        setMeter('scan-meter',f.scanProgress);drawProduct('science-product',f.products[f.target],f.target);
    } else {
        text('comms-live',f.visible?'Earth visible':'Moon occultation');text('comms-data',fnum(f.data,2)+' / 8 MB');
        markActions('Recommend',f.selected);
        f.products.forEach((p,i)=>text('recommend-'+i,p?fnum(p.slope90,2)+'°':'—'));
        disabled('transmit',f.stage<2||f.scanned[f.selected]!==2||!f.visible||!f.radio||!f.bus||f.transmitting||f.scanning||f.autoBurn||f.commandedThrottle>0);
        text('comms-state',f.transmitting?'Awaiting Earth acknowledgement…':'');setMeter('comms-meter',f.transmitProgress);
        drawProduct('comms-product',f.products[f.selected],f.selected);
    }
}

function renderReport(){
    const f=state.flight,success=f.stage===3;
    text('report-title',success?'Survey complete':'Mission ended');
    $('report-result').innerHTML=success?'<div class="kicker"><i></i> EARTH ACKNOWLEDGEMENT RECEIVED</div><h2>'+esc(data.sites[f.selected].name)+'</h2>'+
        '<div class="score">'+f.score+' <small>/ 100</small></div><p>'+esc(f.scoreDetail)+'</p><div class="stats-grid">'+
        metric('DATA DELIVERED',fnum(f.downlinked,2),'MB')+metric('CANDIDATE SLOPE',fnum(f.recommendation,2),'°')+
        metric('PROPELLANT LEFT',fnum(f.fuel,1),'kg')+metric('MISSION TIME',timecode(f.time))+'</div>':
        '<div class="kicker"><i></i> FLIGHT TERMINATED</div><h2>Loss of vehicle</h2><p>'+esc(f.scoreDetail)+'</p>';
    $('report-result').insertAdjacentHTML('beforeend','<div class="console-actions">'+action('Fly another mission','OpenHangar','primary')+'</div>');
    $('report-log').innerHTML=f.log.map(line=>'<div>'+esc(line)+'</div>').join('');
    try { localStorage.setItem('orbital-frontier-last-score',String(f.score)); } catch { /* Storage can be blocked. */ }
}

function showDialog(title,body) {
    text('dialog-title',title);
    $('dialog-body').innerHTML=body;
    if(!$('dialog').open) $('dialog').showModal();
}
function showArchive() {
    if(!data) return;
    const b=data.bodies[atlasPage];
    showDialog('Planetary atlas','<div class="atlas"><div class="segments">'+data.bodies.map((item,i)=>'<button data-local="atlasPage" data-n="'+i+
        '" aria-pressed="'+(i===atlasPage)+'">'+esc(item.name)+'</button>').join('')+'</div><div class="stats-grid">'+
        metric('MEAN RADIUS',fnum(b.radius,1),'km')+metric('MASS',esc(b.mass))+metric('SURFACE GRAVITY',fnum(b.gravity,2),'m/s²')+
        metric('ESCAPE VELOCITY',fnum(b.escape,2),'km/s')+metric('SIDEREAL SPIN',esc(b.rotation))+metric('ORBITAL PERIOD',esc(b.period))+'</div>'+
        '<p>'+esc(b.air)+' · '+esc(b.temperature)+'</p><p>Solar distance: '+esc(b.distance)+'</p><p>'+esc(b.notes)+'</p>'+
        link(b.source,'NASA fact sheet')+' · '+link('https://svs.gsfc.nasa.gov/4720/','NASA CGI Moon Kit')+'</div>');
}
function briefing(){
    showDialog('Tranquillity Survey','<p>You are in a 23 × 180 km lunar orbit. Prepare a survey orbit, then use laser altimetry to compare three candidate regions.</p>'+
        '<ol><li><b>Hangar.</b> The Aster preset is ready to deploy. Change the tank, engine or electrical hardware to see the trade-offs.</li>'+
        '<li><b>Nav.</b> Execute the suggested prograde correction. Guide the perilune into the 46–92 km band.</li>'+
        '<li><b>Lidar.</b> Select a site, coast to access and start a scan. Compare the 90th-percentile slopes and capture a precision map for your preferred site.</li>'+
        '<li><b>Comms.</b> With all sites mapped, select your candidate and transmit when Earth is visible.</li></ol>'+
        '<h3>Data and model</h3><p>NASA LRO/LOLA terrain is sampled at 16 pixels per degree in precision mode. This lets you compare broad terrain slope within the tiles. Footprint-scale landing hazards are below its resolution. The orbit model uses point-mass lunar gravity.</p>'+
        link('https://svs.gsfc.nasa.gov/4720/','NASA CGI Moon Kit'));
}
function settings(){
    showDialog('Controls','<p>Drag in the 3D view to orbit the camera. Scroll to zoom. In flight, choose Orbit or Chase. Capture a lidar scan to unlock the Terrain view.</p>'+
        '<p><b>1–5</b> change cockpit station. <b>Space</b> pauses or resumes mission time. <b>X</b> aborts active operations.</p>'+
        '<p>Use Flight for manual throttle and attitude. Use Nav for maneuver planning. The browser build runs one crew member.</p>'+
        (state&&state.screen<=1?'<div class="segments"><button data-act="Assistance" data-n="1" aria-pressed="'+state.assisted+'">Guided</button>'+
        '<button data-act="Assistance" data-n="0" aria-pressed="'+!state.assisted+'">Manual setup</button></div>':''));
}

document.addEventListener('click',event=>{
    const button=event.target.closest('button');
    if(!button||button.disabled) return;
    if(button.dataset.station){
        station=button.dataset.station;
        renderStation();updateFlight();
    }
    const act=button.dataset.act,n=Number(button.dataset.n??0);
    if(act){
        if(act==='LoadPreset') selectedMount=-1;
        if(act==='Throttle') dispatch(act,0,n);
        else if(act==='Assistance'){
            if(state.screen===0) dispatch('OpenHangar');
            dispatch(act,n);settings();
        } else dispatch(act,n);
        if(act==='Suggest'){
            if($('input-burn')) $('input-burn').value=state.flight.burn;
        }
    }
    switch(button.dataset.local){
        case 'home':
            if(!state) return;
            if(state.screen===2){localScreen='home';render();}
            else dispatch('Home');
            break;
        case 'hangar':
            if(state?.screen===2) {
                showDialog('Return to assembly','<p>Returning to the hangar ends the current survey.</p><div class="console-actions"><button class="primary" data-local="confirmHangar">End survey and open hangar</button><button class="secondary" data-local="close">Keep flying</button></div>');
            } else if (state) dispatch('OpenHangar');
            break;
        case 'confirmHangar':$('dialog').close();dispatch('OpenHangar');break;
        case 'returnFlight':localScreen=null;render();break;
        case 'mount':selectedMount=selectedMount===n?-1:n;renderHangar();document.querySelector('[data-local=mount][data-n="'+n+'"]')?.focus({preventScroll:true});break;
        case 'brief':briefing();break;
        case 'settings':settings();break;
        case 'archive':showArchive();break;
        case 'atlasPage':atlasPage=n;showArchive();break;
        case 'close':$('dialog').close();break;
        case 'resetView':world.reset();break;
        case 'mapView':
        case 'chaseView':
        case 'terrainView':
            cameraMode=button.dataset.local==='mapView'?'map':button.dataset.local==='chaseView'?'chase':'terrain';
            world.setMode(cameraMode);
            updateFlight();
            break;
        case 'log':showDialog('Mission log',state.flight.log.map(s=>'<p class="mono">'+esc(s)+'</p>').join(''));break;
        case 'heritage': {
            const p=data.presets[state.design.preset];
            showDialog(p.heritage,'<img style="width:100%;max-height:48vh;object-fit:contain" src="'+esc(photoSrc(p.photo))+'" alt="'+esc(p.heritage)+' photographed in lunar orbit"><p>'+esc(p.description)+'</p>'+link(p.source,'NASA mission record'));
        } break;
    }
});
$('mounts').addEventListener('pointerover',event=>{
    const node=event.target.closest('button');
    if(node && selectedMount<0) renderInspector(+node.dataset.n);
});
$('mounts').addEventListener('pointerleave',()=>{ if(state) renderInspector(); });
document.addEventListener('change',event=>{
    if(event.target.id==='replacement'&&+event.target.value>=0) dispatch('Install',+event.target.value);
});
document.addEventListener('keydown',event=>{
    if(event.repeat||$('dialog').open||event.target.closest('input,select,textarea')) return;
    withCockpit(()=>{
        if(view!=='flight') return;
        if(event.code==='Space'&&!event.target.closest('button')){event.preventDefault();dispatch('Warp',state.flight.warp===0?1:0);}
        if(event.key.toLowerCase()==='x') dispatch('Stop');
        if(/^[1-5]$/.test(event.key)){
            station=['nav','pilot','eng','science','comms'][+event.key-1];
            renderStation();updateFlight();
        }
    });
});

world=createWorld({home:$('homeworld'),hangar:$('hangarworld'),flight:$('flightworld')},matchMedia('(prefers-reduced-motion: reduce)').matches);
try {
    const [{getAssemblyExports,getConfig}, catalogText]=await Promise.all([
        dotnet.withDiagnosticTracing(false).create(),
        fetch('./media/mission.json').then(response=>{if(!response.ok) throw Error('Mission catalog unavailable');return response.text();})
    ]);
    api=(await getAssemblyExports(getConfig().mainAssemblyName)).BrowserGame;
    data=JSON.parse(catalogText);
    state=JSON.parse(api.Start(catalogText));
    $('enter').disabled=false;
    document.querySelector('[data-nav=hangar]').disabled=false;
    $('load-status').hidden=true;
    render();
    let last=performance.now();
    setInterval(()=>{
        const now=performance.now(), elapsed=(now-last)/1000;
        last=now;
        if(state.screen!==2||document.hidden) return;
        state=JSON.parse(api.Tick(elapsed));
        if(state.screen===3) localScreen=null;
        render();
    },100);
} catch (error) {
    text('load-status','Flight computer unavailable. Serve this folder over HTTP and check browser support.');
    console.error(error);
}
