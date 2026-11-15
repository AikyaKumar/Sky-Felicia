import * as THREE from './three.module.min.js';

const vector = (x=0,y=0,z=0) => new THREE.Vector3(x,y,z);
const materials = {
    hull:'#d8d5d0', deck:'#343847', wall:'#5a5e69', steel:'#a3a7ad', dark:'#222638',
    cable:'#d49852', stripe:'#eca964', cyan:'#78d9d1', purple:'#a998f5', red:'#e67d6b', boot:'#404653'
};

function metal(color,roughness=.57,metalness=.33) {
    return new THREE.MeshStandardMaterial({color,roughness,metalness});
}
function glow(color,strength=1) {
    return new THREE.MeshStandardMaterial({color,emissive:color,emissiveIntensity:strength,roughness:.7});
}
function hardware() {
    return Object.fromEntries(Object.entries(materials).map(([key,value])=>[key,['cyan','purple','red'].includes(key)?glow(value,.67):metal(value)]));
}
function piece(target,geometry,material,x,y,z) {
    const model=new THREE.Mesh(geometry,material);
    model.position.set(x,y,z);
    target.add(model);
    return model;
}
function box(parent,m,x,y,z,w,h,l) { return piece(parent,new THREE.BoxGeometry(w,h,l),m,x,y,z); }
function cone(parent,m,x,y,z,a,b,h,sides=26) { return piece(parent,new THREE.CylinderGeometry(a,b,h,sides),m,x,y,z); }
function sphere(parent,m,x,y,z,r,sides=24) { return piece(parent,new THREE.SphereGeometry(r,sides,16),m,x,y,z); }
function rod(parent,m,p1,p2,r) {
    const delta=p2.clone().sub(p1);
    const mid=p1.clone().lerp(p2,.5);
    const tube=cone(parent,m,mid.x,mid.y,mid.z,r,r,delta.length(),10);
    tube.quaternion.setFromUnitVectors(vector(0,1,0),delta.normalize());
    return tube;
}
function starField(scene,color=0xd4d5ff) {
    let value=351787;
    const rand=()=> (value=(Math.imul(value,1664525)+1013904223)>>>0)/4294967296;
    const positions=[], shades=[];
    for(let n=0;n<2300;n++){
        const z=rand()*2-1,p=rand()*Math.PI*2,r=Math.sqrt(1-z*z)*110;
        positions.push(r*Math.cos(p),z*110,r*Math.sin(p));
        const level=.23+.77*Math.pow(rand(),3.2);
        shades.push(level,level,level);
    }
    const geo=new THREE.BufferGeometry();
    geo.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));
    geo.setAttribute('color',new THREE.Float32BufferAttribute(shades,3));
    scene.add(new THREE.Points(geo,new THREE.PointsMaterial({color,vertexColors:true,size:.17,sizeAttenuation:true})));
}
function planetGeometry(radius) {
    const geometry=new THREE.SphereGeometry(radius,90,62);
    const p=geometry.attributes.position,uv=geometry.attributes.uv;
    for(let i=0;i<p.count;i++){
        const a=(uv.getX(i)-.5)*2*Math.PI,b=(uv.getY(i)-.5)*Math.PI;
        p.setXYZ(i,radius*Math.cos(b)*Math.cos(a),radius*Math.sin(b),-radius*Math.cos(b)*Math.sin(a));
    }
    geometry.computeVertexNormals();
    return geometry;
}
function label(text,color='#d8eee9',bg='#142333',w=512,h=120) {
    const canvas=document.createElement('canvas');canvas.width=w;canvas.height=h;
    const ctx=canvas.getContext('2d');
    ctx.fillStyle=bg;ctx.fillRect(0,0,w,h);
    ctx.strokeStyle=color;ctx.lineWidth=4;ctx.strokeRect(7,7,w-14,h-14);
    ctx.font='bold 35px monospace';ctx.fillStyle=color;ctx.textAlign='center';
    ctx.fillText(text,w/2,h/2+12,w-36);
    const map=new THREE.CanvasTexture(canvas);map.colorSpace=THREE.SRGBColorSpace;
    return new THREE.MeshBasicMaterial({map,side:THREE.DoubleSide});
}
function displayFace(group,text,point,angle,look,wide=1.35,height=.33) {
    const mesh=piece(group,new THREE.PlaneGeometry(wide,height),label(text,look),point.x,point.y,point.z);
    mesh.rotation.y=angle;return mesh;
}

function buildVessel(design,selected,mat) {
    const vessel=new THREE.Group();
    const hullWidth=design.vehicle===2?1.26:design.vehicle===1?1.12:.93;
    const module=(i,draw)=>{
        if(!design.parts[i])return;
        const m=new THREE.Group();
        m.rotation.y=design.rotations[i]*Math.PI/2;
        vessel.add(m);m.userData.slot=i;
        draw(m,design.parts[i]);
        if(i===selected) m.traverse(node=>{
            if(!node.isMesh)return;
            const edge=new THREE.LineSegments(new THREE.EdgesGeometry(node.geometry,22),new THREE.LineBasicMaterial({color:0xe1baff,transparent:true,opacity:.52}));
            node.add(edge);
        });
    };
    module(0,g=>{
        cone(g,mat.hull,0,.88,0,hullWidth*.68,hullWidth,1.35,14);
        cone(g,mat.dark,0,.088,0,hullWidth,hullWidth*.92,.24,16);
        cone(g,mat.steel,0,1.66,0,.41,.62,.30,16);
        for(let i=0;i<4;i++){
            const a=Math.PI*.5*i+.35;
            const view=box(g,mat.dark,Math.sin(a)*hullWidth*.845,1,Math.cos(a)*hullWidth*.845,.57,.23,.10);
            view.rotation.set(-.12,a,0,'YXZ');
            const vent=box(g,mat.steel,Math.sin(a)*hullWidth*.98,.43,Math.cos(a)*hullWidth*.98,.31,.24,.21);
            vent.rotation.y=a;
        }
    });
    module(1,(g,id)=>{
        const r=id==='compact'?.69:id==='stretched'?.83:1.08;
        cone(g,mat.cable,0,-.47,0,r,r,.92);
        for(const y of [-.94,-.73,-.25,.01])cone(g,mat.steel,0,y,0,r*1.06,r*1.06,.087);
        for(let i=0;i<10;i++){
            const a=i*Math.PI/5;
            const strut=box(g,mat.hull,Math.sin(a)*r,-.47,Math.cos(a)*r,.09,.84,.058);
            strut.rotation.y=a;
        }
    });
    module(2,(g,id)=>{
        const burners=id==='atlas'?[-.63,0,.63]:[0];
        burners.forEach(x=>{
            const r=burners.length>1?.32:id==='horizon'?.43:.61;
            cone(g,mat.dark,x,-1.21,0,.19,r,.63);
            cone(g,mat.steel,x,-1.52,0,r*.98,r,.104);
            cone(g,mat.dark,x,-1.577,0,r*.88,r*.9,.03);
        });
    });
    module(3,(g,id)=>{
        if(id==='isotope'){
            box(g,mat.dark,1.29,-.21,0,.6,.65,.58);
            for(let a=0;a<7;a++)box(g,mat.steel,1.30,-.53+a*.10,0,.86,.022,.77);
            return;
        }
        const solar=metal(0x273b89,.43,.4), length=id==='wide'?2.3:1.55;
        for(const sign of [-1,1]){
            box(g,mat.steel,sign*1.13,.23,0,.7,.12,.15);
            box(g,mat.hull,sign*(1.37+length/2),.22,0,length,.07,.94);
            for(let x=0;x<7;x++)for(let z=0;z<4;z++){
                box(g,solar,sign*(1.37+(x+.5)*length/7),.263,(z-1.5)*.226,length/7-.019,.023,.206);
            }
        }
    });
    module(4,(g,id)=>box(g,mat.hull,-1.02,-.59,.42,.45,id==='cell'?.39:.61,.49));
    module(5,(g,id)=>{
        box(g,mat.steel,.55,.24,.88,.56,.25,.41);
        sphere(g,id==='radar'?mat.purple:mat.cyan,.55,.24,1.105,.12);
    });
    module(6,(g,id)=>{
        const tip=vector(1.17,1.37,.33);rod(g,mat.steel,vector(.61,.66,.3),tip,.066);
        if(id==='dish'){
            const dish=piece(g,new THREE.SphereGeometry(.49,28,15,0,2*Math.PI,.15,Math.PI*.46),mat.hull,tip.x,tip.y,tip.z);
            dish.rotation.z=-2.29;dish.material.side=THREE.DoubleSide;
        }else box(g,mat.hull,tip.x,tip.y,tip.z,.46,.12,.47).rotation.z=-.36;
    });
    module(7,(g,id)=>{
        const r=id==='shocklegs'?1.66:1.43;
        for(let i=0;i<4;i++){
            const a=Math.PI*(i+.5)/2;
            const foot=vector(Math.cos(a)*r,-1.51,Math.sin(a)*r);
            rod(g,mat.steel,vector(Math.cos(a)*.80,-.63,Math.sin(a)*.80),foot,id==='shocklegs'?.078:.054);
            cone(g,mat.hull,foot.x,foot.y-.055,foot.z,.16,.21,.10);
        }
    });
    module(8,g=>box(g,mat.hull,1.00,-.52,-.45,.38,.47,.50));
    module(9,(g,id)=>cone(g,mat.dark,0,-.86,0,id==='shield-plus'?1.41:1.24,.77,.18,22));
    module(10,g=>box(g,mat.cyan,-.55,.27,.92,.36,.21,.28));
    module(11,g=>box(g,mat.dark,-.44,.61,-.92,.38,.34,.27));
    module(12,g=>box(g,mat.cable,.52,-.46,1.02,.43,.38,.23));
    module(13,g=>{for(const x of [-.99,.99])box(g,mat.dark,x,.79,0,.29,.21,.40);});
    module(14,g=>box(g,mat.stripe,0,-1.03,-.93,.62,.24,.30));
    return vessel;
}

const deckStations=[
    ['HELM',-.92,-8.35,'#b1e9e1',0],['NAV',.92,-8.35,'#bcabf9',0],
    ['ENGINEERING',-1.75,-3,'#f2bf7a',Math.PI/2],['LIFE SUPPORT',1.75,-3,'#9bdeb9',-Math.PI/2],
    ['SCIENCE',-1.75,1.05,'#b7a6f1',Math.PI/2],['COMMS',1.75,1.05,'#98c8ed',-Math.PI/2],
    ['AIRLOCK',0,6.95,'#ecc08b',Math.PI]
];

function panelTexture(i,accent) {
    const canvas=document.createElement('canvas');canvas.width=512;canvas.height=320;
    const ctx=canvas.getContext('2d');
    ctx.fillStyle='#102035';ctx.fillRect(0,0,512,320);
    ctx.strokeStyle='#2b4964';ctx.lineWidth=2;
    for(let x=0;x<512;x+=34){ctx.beginPath();ctx.moveTo(x,59);ctx.lineTo(x,283);ctx.stroke();}
    for(let y=59;y<300;y+=28){ctx.beginPath();ctx.moveTo(12,y);ctx.lineTo(500,y);ctx.stroke();}
    ctx.fillStyle=accent;ctx.font='bold 27px monospace';ctx.fillText(deckStations[i][0],18,37);
    ctx.font='15px monospace';ctx.fillText(['FLIGHT DIRECTOR','ORBITAL SOLUTION','PWR DISTRIBUTION','CABIN ENVIRONMENT','FIELD DATA','S-BAND TRANSCEIVER'][i]??'DOOR CONTROL',19,300);
    ctx.strokeStyle=accent;ctx.lineWidth=4;
    if(i===1||i===4){
        ctx.strokeRect(72,83,366,181);
        ctx.beginPath();ctx.ellipse(249,176,118,58,Math.PI*.18,0,2*Math.PI);ctx.stroke();
        ctx.beginPath();ctx.ellipse(249,176,13,13,0,0,2*Math.PI);ctx.stroke();
    }else if(i===2||i===3){
        for(let j=0;j<6;j++){ctx.beginPath();ctx.strokeStyle=j%2?accent:'#75a6c5';ctx.moveTo(22,87+j*30);ctx.lineTo(280+j*27,87+j*30);ctx.stroke();}
    }else{
        ctx.beginPath();
        for(let j=0;j<14;j++){const y=158+34*Math.sin(j*.71+i)+((j%3)-1)*14;j===0?ctx.moveTo(20,y):ctx.lineTo(20+35*j,y);}
        ctx.stroke();
    }
    const tex=new THREE.CanvasTexture(canvas);tex.colorSpace=THREE.SRGBColorSpace;
    const screen=new THREE.MeshBasicMaterial({map:tex,side:THREE.DoubleSide});
    screen.userData.live={ctx,tex,accent,last:''};
    return screen;
}

function cabin(scene,deckOnly=false) {
    const m=hardware(),root=new THREE.Group(),monitors=[];scene.add(root);
    box(root,m.deck,0,-.16,-.8,4.95,.32,16.9);
    for(let i=0;i<26;i++){
        const z=-8.7+i*.63;
        box(root,m.steel,0,.016,z,4.36,.023,.026);
        box(root,m.stripe,-1.23,.013,z,.10,.026,.26);
        box(root,m.stripe,1.23,.013,z,.10,.026,.26);
        if(i%2===0){
            box(root,m.dark,-1.88,-.001,z,.46,.037,.26);
            box(root,m.dark,1.88,-.001,z,.46,.037,.26);
        }
    }
    if(!deckOnly) {
        box(root,m.deck,0,2.94,-.8,4.95,.28,16.9);
        for(const sign of [-1,1])for(let i=0;i<8;i++){
            const z=-7.9+2.1*i;
            box(root,m.wall,sign*2.47,1.36,z,.28,2.81,2.05);
            box(root,m.dark,sign*2.285,.25,z,.14,.46,2.0);
            box(root,m.steel,sign*2.307,2.61,z,.18,.25,2.0);
            box(root,i%3===0?m.cable:m.dark,sign*2.29,1.92,z,.16,.035,1.78);
        }
        for(const z of [-7.2,-4.3,0.3,4.31,6.65]){
            box(root,m.steel,0,2.62,z,4.60,.25,.27);
            box(root,m.steel,-2.14,1.32,z,.15,2.63,.27);
            box(root,m.steel,2.14,1.32,z,.15,2.63,.27);
            box(root,m.cyan,0,2.49,z,.60,.053,.32);
        }
        for(const z of [-4.3,4.31])for(const side of [-1,1]){
            box(root,m.wall,side*1.96,1.29,z,1.26,2.6,.43);
            box(root,m.stripe,side*1.32,1.3,z,.10,2.53,.49);
        }
        for(const side of [-1,1]){
            rod(root,m.cable,vector(side*1.83,2.30,-8.7),vector(side*1.83,2.30,7.0),.034);
            rod(root,m.cable,vector(side*1.70,2.38,-8.7),vector(side*1.70,2.38,7.0),.023);
        }
        box(root,m.wall,0,.46,-9,4.75,.94,.30);
        box(root,m.wall,0,2.54,-9,4.75,.52,.30);
        for(const x of [-2.15,0,2.15])box(root,m.steel,x,1.62,-8.98,.21,1.56,.32);
        for(const x of [-1.26,1.26])box(root,m.wall,x,1.18,7.32,2.20,2.38,.32);
        box(root,m.wall,0,2.49,7.32,4.75,.27,.32);
        box(root,m.dark,0,1.32,7.30,1.49,2.45,.33);
        box(root,m.stripe,.75,1.33,7.11,.06,2.44,.07);
        box(root,m.stripe,-.75,1.33,7.11,.06,2.44,.07);
    } else {
        for(const side of [-1,1])box(root,m.wall,side*2.37,.23,-.8,.28,.47,16.8);
    }
    for(let i=0;i<6;i++){
        const [text,x,z,color,angle]=deckStations[i];
        const obj=new THREE.Group();obj.position.set(x,0,z);obj.rotation.y=angle;root.add(obj);
        box(obj,m.dark,0,.66,.04,1.64,1.32,.63);
        box(obj,m.steel,0,1.29,.11,1.73,.17,.72);
        box(obj,m.dark,0,1.64,.02,1.57,.77,.24);
        const monitor=piece(obj,new THREE.PlaneGeometry(1.42,.66),panelTexture(i,color),0,1.63,.149);
        monitor.material.depthWrite=true;
        monitors.push(monitor.material.userData.live);
        for(let c=0;c<5;c++){
            box(obj,c%2?m.cyan:m.cable,-.48+c*.21,1.391,.33,.13,.039,.071);
            box(obj,m.dark,-.49+c*.21,1.40,.28,.026,.053,.040);
        }
        displayFace(obj,text,vector(0,2.16,.15),0,color);
    }
    displayFace(root,'EVA AIRLOCK',vector(0,2.31,7.10),Math.PI,'#f6b578');
    for(let i=0;i<5;i++){
        const z=-7.35+i*3.15;
        const light=new THREE.PointLight(0xc3d0ef,16,7,2);
        light.position.set(0,2.35,z); scene.add(light);
    }
    const warm=new THREE.PointLight(0xf4b279,11,6,2);
    warm.position.set(0,1.7,6.93);scene.add(warm);
    scene.add(new THREE.HemisphereLight(0xd5dfec,0x4d4851,1.42));
    return monitors;
}

function fixedSky(mars) {
    if(!mars)return new THREE.Color(0x090a11);
    const cn=document.createElement('canvas');cn.width=16;cn.height=192;const ctx=cn.getContext('2d');
    const grad=ctx.createLinearGradient(0,0,0,192);
    grad.addColorStop(0,'#48384a');grad.addColorStop(.47,'#9c645d');grad.addColorStop(.76,'#cf8f6d');grad.addColorStop(1,'#d6aa84');
    ctx.fillStyle=grad;ctx.fillRect(0,0,16,192);
    const t=new THREE.CanvasTexture(cn);t.colorSpace=THREE.SRGBColorSpace;
    return t;
}

export function renderExpedition(hosts,files,data,sets) {
    let renderer;
    try{renderer=new THREE.WebGLRenderer({alpha:true,antialias:true});}
    catch(e) {for(const host of Object.values(hosts))host.classList.add('no-webgl');return {show(){},update(){},reset(){},rotate(){},zoom(){},map(){}};}
    renderer.setPixelRatio(Math.min(1.5,window.devicePixelRatio));
    renderer.setClearColor(0x090b13,0);
    renderer.outputColorSpace=THREE.SRGBColorSpace;
    renderer.toneMapping=THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure=1.18;
    const textures=new THREE.TextureLoader();
    const picture=name=>{const t=textures.load(files(name+'.jpg'));t.colorSpace=THREE.SRGBColorSpace;t.anisotropy=Math.min(8,renderer.capabilities.getMaxAnisotropy());return t;};
    const planetMaps=data.worlds.map(w=>picture(w.id==='moon'?'lunar_albedo':'mars_albedo'));
    const earthMap=picture('earth_albedo');
    const skyMars=fixedSky(true),skyMoon=fixedSky(false);
    const surfaceMaps=data.worlds.map(w=>picture(w.texture));
    const hangar=new THREE.Scene(), cosmos=new THREE.Scene(), interior=new THREE.Scene(), surface=new THREE.Scene(), cutaway=new THREE.Scene();
    const camera=new THREE.PerspectiveCamera(48,1,.06,9800);
    const overview=new THREE.PerspectiveCamera(37,1,.018,180);
    const craftMat=hardware();
    function light(scene,sky=0xbac7ff,ground=0x252536){
        scene.add(new THREE.HemisphereLight(sky,ground,1.55));
        const key=new THREE.DirectionalLight(0xffe0bf,2.75);key.position.set(-3,4,5);scene.add(key);
        const fill=new THREE.DirectionalLight(0x9eaeff,.72);fill.position.set(4,0,-4);scene.add(fill);
    }
    light(hangar);light(cosmos,0x62687d,0x160f24);light(surface); 
    starField(cosmos);
    const gl=piece(cosmos,planetGeometry(1.38),new THREE.MeshPhongMaterial({map:planetMaps[0],shininess:3,color:0xdacab8,bumpMap:planetMaps[0],bumpScale:.007}),0,0,0);
    gl.rotation.y=.80;
    const orbitLine=new THREE.Line(new THREE.BufferGeometry(),new THREE.LineBasicMaterial({color:0xc8b9ff,transparent:true,opacity:.8}));
    orbitLine.frustumCulled=false;cosmos.add(orbitLine);
    const dot=sphere(cosmos,glow(0xfca45f),0,0,0,.023,12);
    const orbShip=new THREE.Group();cosmos.add(orbShip);
    const grid=new THREE.GridHelper(28,56,0x5b527a,0x38354b);grid.position.y=-1.75;hangar.add(grid);
    const cabinMonitors=cabin(interior);cabin(cutaway,true);
    const pane=piece(interior,planetGeometry(165),new THREE.MeshPhongMaterial({map:earthMap,shininess:2,color:0xc4dcff}),-28,-37,-455);
    pane.rotation.y=.70;
    const cabinPlanetLight=new THREE.DirectionalLight(0xffe7d1,3.3);cabinPlanetLight.position.set(3,4,6);interior.add(cabinPlanetLight);
    const powerLed=box(interior,craftMat.cyan,-1.75,1.82,-2.68,.24,.11,.044);
    const remoteCrew=new Array(4).fill(null).map((_,i)=>{
        function avatar(){
            const g=new THREE.Group();
            const coat=metal([0xcec9db,0xca874f,0x62a4b4,0xcf99ba][i]);
            sphere(g,craftMat.hull,0,1.60,0,.21);
            sphere(g,craftMat.dark,0,1.61,-.103,.18);
            box(g,coat,0,1.05,0,.43,.59,.29);
            box(g,craftMat.dark,0,1.18,.195,.39,.46,.16);
            for(const x of [-.15,.15]){
                cone(g,coat,x,.43,0,.102,.113,.75,12);
                box(g,craftMat.dark,x,.12,-.034,.21,.17,.31);
                rod(g,coat,vector(x*1.65,1.26,.0),vector(x*1.99,.74,-.05),.072);
            }
            return g;
        }
        const inside=avatar(),outside=avatar();interior.add(inside);surface.add(outside);
        return [inside,outside];
    });
    const surfaceScene=new THREE.Group();surface.add(surfaceScene);
    const surfaceShip=new THREE.Group();surface.add(surfaceShip);
    const rover=new THREE.Group();surface.add(rover);
    box(rover,craftMat.hull,0,.65,0,1.5,.28,2.28);
    box(rover,craftMat.dark,0,.89,.30,.82,.33,.86);
    for(const x of [-.80,.80])for(const z of [-.71,.71]){
        const wheel=cone(rover,craftMat.dark,x,.31,z,.34,.34,.29,21);wheel.rotation.z=Math.PI/2;
    }
    const dustLight=new THREE.DirectionalLight(0xffd3aa,1.10);dustLight.position.set(-800,430,-500);surface.add(dustLight);

    const raycaster=new THREE.Raycaster();
    let view='plan',visual='planet',localSeat=0,yaw=.59,pitch=.28,dist=10,shipKey='',worldIndex=-1;
    let spacecraft=null,exterior=null,launchVehicle=null,landMesh=null,fieldWorld=null,lastState=null,lastTrack=null;
    let deferredWidth=0,deferredHeight=0;
    const resize=new ResizeObserver(fit);
    function fit(){
        const h=hosts[view];if(!h?.clientWidth||!h.clientHeight)return;
        if(h.clientWidth===deferredWidth&&h.clientHeight===deferredHeight)return;
        deferredWidth=h.clientWidth;deferredHeight=h.clientHeight;
        renderer.setSize(deferredWidth,deferredHeight,false);
        for(const c of [camera,overview]){c.aspect=deferredWidth/deferredHeight;c.updateProjectionMatrix();}
    }
    function show(target){
        if(view===target&&renderer.domElement.parentElement===hosts[target])return;
        view=target;resize.disconnect();
        if(hosts[view]){hosts[view].append(renderer.domElement);resize.observe(hosts[view]);fit();}
    }
    function dispose(model){
        if(!model)return;
        model.traverse(obj=>{
            obj.geometry?.dispose();
            if(obj.material&&!Object.values(craftMat).includes(obj.material)){obj.material.map?.dispose();obj.material.dispose();}
        });
        model.removeFromParent();
    }
    function updateShip(d,n){
        const key=JSON.stringify([d.parts,d.rotations,d.vehicle,n]);if(key===shipKey)return;
        shipKey=key;
        dispose(spacecraft);dispose(exterior);
        spacecraft=buildVessel(d,n,craftMat);hangar.add(spacecraft);
        exterior=buildVessel(d,-1,craftMat);
        exterior.scale.setScalar(3.75);surfaceShip.add(exterior);
    }
    function buildSite(i){
        if(i===worldIndex)return;
        worldIndex=i;
        const w=data.worlds[i],g=sets[i];fieldWorld=w;
        gl.material.map=planetMaps[i];gl.material.bumpMap=planetMaps[i];
        gl.material.color.set(i?'#e2b6a2':'#e0d6d1');
        gl.material.needsUpdate=true;
        surface.background=i===1?skyMars:skyMoon;
        surface.fog=i===1?new THREE.FogExp2(0xa77962,.00033):null;
        while(surfaceScene.children.length){
            const obj=surfaceScene.children[0];
            obj.traverse(x=>{
                x.geometry?.dispose();
                if(x.material&&!Object.values(craftMat).includes(x.material))x.material.dispose();
            });
            obj.removeFromParent();
        }
        const seg=Math.min(176,w.mapSize-1),ext=g.extent;
        const geo=new THREE.PlaneGeometry(ext*2,ext*2,seg,seg);
        geo.rotateX(-Math.PI/2);
        const points=geo.attributes.position;
        for(let i=0;i<points.count;i++) points.setY(i,g.height(points.getX(i),points.getZ(i)));
        geo.computeVertexNormals();
        landMesh=piece(surfaceScene,geo,new THREE.MeshStandardMaterial({map:surfaceMaps[i],color:i?0xe7c0a5:0xd0c9bd,roughness:.99}),0,0,0);
        const outer=piece(surfaceScene,new THREE.PlaneGeometry(8200,8200),metal(i?0x7a5140:0x4c4848,.97,0),0,-40,0);
        outer.rotation.x=-Math.PI/2;
        for(const [n,p] of w.points.entries()){
            const h=g.height(p.x,p.z),beacon=craftMat.purple;
            const ring=piece(surfaceScene,new THREE.TorusGeometry(5.5,.14,8,48),beacon,p.x,h+.65,p.z);
            ring.rotation.x=Math.PI/2;
            const v=vector(p.x,h+6,p.z);
            rod(surfaceScene,beacon,vector(p.x,h+.3,p.z),v,.14);
            sphere(surfaceScene,beacon,v.x,v.y,v.z,.53,12);
            const icon=displayFace(surfaceScene,n===0?'SITE A':'SITE B',vector(p.x,h+5.0,p.z+1),0,'#e8bffe',5.8,1.2);
            icon.material.depthTest=true;
        }
        if(i===0){
            const earth=piece(surfaceScene,planetGeometry(12),new THREE.MeshBasicMaterial({map:earthMap}),83,124,-670);
            earth.rotation.y=.95;
        }
    }
    function updateLaunchVehicle(selection,stage) {
        const key=selection+':'+stage;
        if(launchVehicle?.userData.key===key)return;
        dispose(launchVehicle);
        launchVehicle=new THREE.Group();
        launchVehicle.userData.key=key;
        const shell=craftMat.hull, stripes=craftMat.dark, lower=craftMat.cable;
        const r=.21+selection*.045;
        cone(launchVehicle,shell,0,.22,0,r,.94*r,.57);
        cone(launchVehicle,shell,0,.57,0,.015,r,.14);
        if(stage===0){
            cone(launchVehicle,lower,0,-.25,0,.99*r,1.07*r,.39);
            for(const x of selection===0?[]:[-r*1.31,r*1.31]){
                cone(launchVehicle,shell,x,-.018,0,.61*r,.61*r,.65);
                cone(launchVehicle,shell,x,.335,0,.012,.61*r,.10);
                cone(launchVehicle,stripes,x,-.365,0,.5*r,.67*r,.098);
            }
        }
        cone(launchVehicle,stripes,0,stage===0?-.475:-.12,0,.56*r,1.09*r,.155);
        const plume=piece(launchVehicle,new THREE.ConeGeometry(r*.6,.49,18),glow(0xfb954e,1.8),0,stage===0?-.75:-.38,0);
        plume.rotation.z=Math.PI;
        launchVehicle.userData.plume=plume;
        launchVehicle.rotation.z=-.68;
        return orbShip.add(launchVehicle);
    }
    function reset(){yaw=visual==='deck'?.04:.60;pitch=visual==='deck'?1.04:.31;dist=visual==='deck'?29:visual==='ship'?10:4.6;}
    function setCam(mode){if(mode===visual)return;visual=mode;reset();}
    function update(s,mode,seat,selected){
        lastState=s;localSeat=seat;setCam(mode);
        buildSite(s.design.world);
        updateShip(s.design,selected);
    }
    const orient=(c,target,r=dist)=>{
        const d=vector(Math.sin(yaw)*Math.cos(pitch),Math.sin(pitch),Math.cos(yaw)*Math.cos(pitch)).multiplyScalar(r);
        c.position.copy(target).add(d); c.lookAt(target);
    };
    function visitCabin(s,u,angle){
        camera.far=11000;camera.fov=67;
        const a=angle[0]*Math.PI/180,b=angle[1]*Math.PI/180;
        camera.position.set(u.x,1.63,u.z);
        camera.lookAt(u.x+Math.sin(a)*Math.cos(b),1.63+Math.sin(b),u.z-Math.cos(a)*Math.cos(b));
        camera.updateProjectionMatrix();
        pane.position.set(-28,-37,-455);
        pane.material.map=s.phase<2?earthMap:planetMaps[s.design.world];
        pane.material.needsUpdate=true;
        pane.rotation.y=.80+(s.telemetry?.progress??0)*.4;
        interior.background=new THREE.Color(0x090b14);
        powerLed.material=s.telemetry?.fault===1?craftMat.red:craftMat.cyan;
        const t=s.telemetry;
        if(t){
            const lines=[
                Math.round(t.altitude)+' m  |  '+Math.round(t.speed)+' m/s',
                (t.orbitalPeri||0).toFixed(0)+' x '+(t.orbitalApo||0).toFixed(0)+' km  /  '+t.cruiseDays.toFixed(1)+' d',
                Math.round(t.power)+' W  /  '+Math.round(t.battery)+' Wh',
                t.pressure.toFixed(1)+' kPa  /  O2 '+u.oxygen.toFixed(0)+'%',
                t.samples+' EVA  /  '+t.orbitalPasses+' orbital products',
                (t.link?'LINK CLEAR':'LINK BLOCKED')+'  /  '+t.stored.toFixed(1)+' MB'
            ];
            cabinMonitors.forEach((m,i)=>{
                if(m.last===lines[i])return;
                m.last=lines[i];
                m.ctx.fillStyle='#102035';m.ctx.fillRect(13,276,477,37);
                m.ctx.fillStyle=m.accent;m.ctx.font='19px monospace';m.ctx.fillText(m.last,19,300,470);
                m.tex.needsUpdate=true;
            });
        }
        for(let i=0;i<remoteCrew.length;i++){
            const mate=s.crew[i],person=remoteCrew[i][0];
            person.visible=i!==localSeat&&mate.active&&!mate.outside;
            person.position.set(mate.x,0,mate.z);person.rotation.y=-mate.yaw*Math.PI/180;
        }
        renderer.render(interior,camera);
    }
    function orbitView(s){
        const t=s.telemetry,g=fieldWorld;
        gl.position.set(0,0,0);gl.scale.setScalar(1);
        gl.material.map=s.phase<2?earthMap:planetMaps[s.design.world];
        gl.material.bumpMap=gl.material.map;
        gl.material.color.set(s.phase<2?'#cbdcf2':'#dccec4');
        gl.rotation.y=.77;
        overview.position.set(1.81,1.35,2.94);
        overview.lookAt(0,0,0);
        orbitLine.visible=s.phase===3;dot.visible=s.phase===3;
        if(orbitLine.visible&&t.track?.length&&t.track!==lastTrack){
            lastTrack=t.track;const k=1.38/g.radius;
            orbitLine.geometry.dispose();orbitLine.geometry=new THREE.BufferGeometry().setFromPoints(t.track.map(q=>vector(q.x*k,q.y*k,-q.z*k)));
        }
        if(dot.visible){const k=1.38/g.radius;dot.position.set(t.orbit.p.x*k,t.orbit.p.y*k,-t.orbit.p.z*k);}
        orbShip.visible=true;
        let marker;
        if(s.phase===1){
            updateLaunchVehicle(s.design.launcher,t.stageCount);
            if(!launchVehicle.parent)orbShip.add(launchVehicle);
            marker=launchVehicle;
            marker.userData.plume.visible=!t.staged;
            orbShip.position.set(-.64,.32+t.progress*.015,1.38);
        }else{
            if(!orbShip.userData.craft||orbShip.userData.key!==shipKey){
                dispose(orbShip.userData.craft);
                orbShip.userData.craft=buildVessel(s.design,-1,craftMat);
                orbShip.userData.craft.scale.setScalar(s.phase===2?.16:.076);
                orbShip.userData.craft.rotation.z=-.78;
                orbShip.userData.key=shipKey;
                orbShip.add(orbShip.userData.craft);
            }
            marker=orbShip.userData.craft;
            marker.scale.setScalar(s.phase===2?.16:.076);
            if(s.phase===3)orbShip.position.copy(dot.position);
            else orbShip.position.set(-.81+(t.progress*.89),.32,1.40);
            if(s.phase===3)dot.visible=false;
        }
        for(const obj of orbShip.children)obj.visible=obj===marker;
        renderer.render(cosmos,overview);
    }
    function outdoor(s,u,angle){
        const g=sets[s.design.world],t=s.telemetry,landing=s.phase===4;
        const ground=landing?t.altitude:g.height(t.x,t.z);
        surfaceShip.position.set(t.x,ground+5.9,t.z);
        if(landing) surfaceShip.rotation.set(Math.max(-.11,Math.min(.11,t.vz/140)),0,-Math.max(-.13,Math.min(.13,t.vx/140)));
        else surfaceShip.rotation.set(0,0,0);
        rover.visible=!landing&&!!s.design.parts[14];
        const roverX=t.roverX,roverZ=t.roverZ;
        rover.position.set(roverX,g.height(roverX,roverZ),roverZ);
        rover.rotation.y=-t.roverYaw*Math.PI/180;
        for(let i=0;i<remoteCrew.length;i++){
            const mate=s.crew[i],person=remoteCrew[i][1];
            person.visible=i!==localSeat&&mate.active&&mate.outside;
            person.position.set(mate.x,g.height(mate.x,mate.z),mate.z);person.rotation.y=-mate.yaw*Math.PI/180;
        }
        camera.far=9800;
        if(visual==='first'&&u.outside){
            camera.fov=64;
            const a=angle[0]*Math.PI/180,b=angle[1]*Math.PI/180;
            const h=g.height(u.x,u.z)+(u.rover?1.24:1.78);
            camera.position.set(u.x,h,u.z);
            camera.lookAt(u.x+Math.sin(a)*Math.cos(b),h+Math.sin(b),u.z-Math.cos(a)*Math.cos(b));
        }else{
            camera.fov=48;
            const d=landing?Math.min(148,34+Math.max(0,ground)*.25):44;
            camera.position.set(t.x-Math.sin(yaw)*d,ground+(landing?d*.31:16),t.z+Math.cos(yaw)*d);
            camera.lookAt(t.x,ground+(landing?-4:1.2),t.z-11);
        }
        camera.updateProjectionMatrix();
        renderer.render(surface,camera);
    }
    function pick(clientX,clientY) {
        if(view!=='plan'||visual!=='ship'||!spacecraft)return -1;
        const r=renderer.domElement.getBoundingClientRect();
        if(r.width===0||r.height===0)return -1;
        raycaster.setFromCamera(new THREE.Vector2((clientX-r.left)/r.width*2-1,1-(clientY-r.top)/r.height*2),overview);
        for(const hit of raycaster.intersectObject(spacecraft,true)){
            if(!hit.object.isMesh)continue;
            for(let obj=hit.object;obj&&obj!==spacecraft;obj=obj.parent)
                if(Number.isInteger(obj.userData.slot))return obj.userData.slot;
        }
        return -1;
    }
    let angles=[0,0];
    function frame(){
        requestAnimationFrame(frame);
        if(!lastState||!hosts[view]||!deferredWidth||document.hidden)return;
        const s=lastState;
        if(view==='plan'){
            if(visual==='deck'){orient(overview,vector(0,0,-.84));renderer.render(cutaway,overview);}
            else if(visual==='ship'){orient(overview,vector(0,0,0));renderer.render(hangar,overview);}
            else {
                gl.material.map=planetMaps[s.design.world];gl.position.set(.98,.025,0);gl.rotation.y=.94+performance.now()*.00006;
                gl.scale.setScalar(1.10);orbitLine.visible=dot.visible=orbShip.visible=false;overview.position.set(0,0,4.65);overview.lookAt(0,0,0);renderer.render(cosmos,overview);
            }
        }else if(visual==='map'){renderer.clear();}
        else if(visual==='first'&&!s.crew[localSeat].outside){
            visitCabin(s,s.crew[localSeat],angles);
        }else if(s.phase===4||s.phase===5){outdoor(s,s.crew[localSeat],angles);}
        else orbitView(s);
    }
    frame();
    return {
        show,update,reset,pick,
        rotate(x,y){yaw-=x*.008;pitch=Math.max(-.80,Math.min(1.41,pitch+y*.008));},
        zoom(d){dist=Math.max(visual==='deck'?13:5.0,Math.min(39,dist+d*.015));},
        map(a,b){angles=[a,b];},
        canvas:renderer.domElement
    };
}
