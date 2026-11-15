import * as THREE from './three.module.min.js';

const radians = Math.PI / 180;
const colors = { blue: 0xb6a2ff, mint: 0x86e2c7, amber: 0xffba78 };
const toView = (v, r = 1) => new THREE.Vector3(v.x * r, v.y * r, -v.z * r);
const setup = () => new THREE.PerspectiveCamera(38, 1, 0.015, 180);

function stars(scene) {
    let state = 613;
    const random = () => ((state = (Math.imul(1664525, state) + 1013904223) >>> 0) / 4294967296);
    const pos = [], col = [];
    for (let i = 0; i < 2500; i++) {
        const yy = random() * 2 - 1;
        const phi = random() * 2 * Math.PI;
        const rr = 69 * Math.sqrt(1 - yy * yy);
        pos.push(rr * Math.cos(phi), yy * 69, rr * Math.sin(phi));
        const gray = 0.2 + Math.pow(random(), 2.6) * 0.79;
        col.push(gray * 0.86, gray * 0.91, gray);
    }
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
    scene.add(new THREE.Points(geometry, new THREE.PointsMaterial({
        color: 0xffffff, size: 0.09, sizeAttenuation: true, vertexColors: true
    })));
}

function makeMoon(texture, radius) {
    const geometry = new THREE.SphereGeometry(radius, 96, 64);
    // At longitude zero the surface is +X. Positive longitude maps to -Z.
    const pos = geometry.attributes.position, uv = geometry.attributes.uv;
    for (let i = 0; i < pos.count; i++) {
        const a = uv.getX(i) * Math.PI * 2 - Math.PI;
        const lat = (uv.getY(i) - 0.5) * Math.PI;
        pos.setXYZ(i, radius * Math.cos(lat) * Math.cos(a), radius * Math.sin(lat), -radius * Math.cos(lat) * Math.sin(a));
    }
    geometry.computeVertexNormals();
    const material = new THREE.MeshPhongMaterial({ map: texture, color: 0xe1ddf6, shininess: 3, specular: 0x131123, bumpMap: texture, bumpScale: 0.012 });
    return new THREE.Mesh(geometry, material);
}

function shape(parent, geometry, material, x = 0, y = 0, z = 0) {
    const mesh = new THREE.Mesh(geometry, material);
    mesh.position.set(x, y, z);
    parent.add(mesh);
    return mesh;
}

function buildCraft(design, hoverSlot) {
    const model = new THREE.Group();
    const white = new THREE.MeshStandardMaterial({ color: 0xc5c5cd, roughness: 0.45, metalness: 0.50 });
    const black = new THREE.MeshStandardMaterial({ color: 0x252a39, roughness: 0.42, metalness: 0.60 });
    const steel = new THREE.MeshStandardMaterial({ color: 0x858baa, roughness: 0.35, metalness: 0.70 });
    const gold = new THREE.MeshStandardMaterial({ color: 0xcc9360, roughness: 0.5, metalness: 0.65 });
    const blue = new THREE.MeshStandardMaterial({ color: 0x193fa2, roughness: 0.34, metalness: 0.30 });
    const window = new THREE.MeshStandardMaterial({ color: 0x182847, emissive: 0x233b72, emissiveIntensity: 0.4, roughness: 0.3 });
    const wire = new THREE.LineBasicMaterial({ color: 0xc7b7ff, transparent: true, opacity: 0.4 });
    const cyl = (group, rt, rb, h, mat, x, y, z, edges = 24) => shape(group, new THREE.CylinderGeometry(rt, rb, h, edges), mat, x, y, z);
    const box = (group, ax, ay, az, mat, x, y, z) => shape(group, new THREE.BoxGeometry(ax, ay, az), mat, x, y, z);
    const ring = (group, r, w, mat, y) => {
        const mesh = shape(group, new THREE.TorusGeometry(r, w, 8, 36), mat, 0, y, 0);
        mesh.rotation.x = Math.PI / 2;
    };
    const port = (i, draw) => {
        if (!design.parts[i]) return;
        const group = new THREE.Group();
        group.rotation.y = design.angles[i] * Math.PI / 2;
        model.add(group);
        draw(group, design.parts[i].endsWith('-l'));
        if (i === hoverSlot) {
            group.traverse(child => {
                if (!child.isMesh) return;
                child.add(new THREE.LineSegments(new THREE.EdgesGeometry(child.geometry, 26), wire));
            });
        }
    };
    port(0, g => {
        cyl(g, .56, .79, .74, white, 0, 1.00, 0, 12);
        cyl(g, .72, .83, .17, black, 0, .58, 0, 12);
        cyl(g, .36, .55, .19, white, 0, 1.44, 0);
        cyl(g, .29, .34, .17, steel, 0, 1.60, 0);
        ring(g, .29, .025, black, 1.68);
        for (let i = 0; i < 4; i++) {
            const a = i * Math.PI / 2 + Math.PI / 4;
            const pane = box(g, .30, .145, .045, window, Math.sin(a) * .66, 1.07, Math.cos(a) * .66);
            pane.rotation.set(-.21, a, 0, 'YXZ');
            const rcs = box(g, .18, .17, .25, black, Math.sin(a) * .79, .54, Math.cos(a) * .79);
            rcs.rotation.y = a;
        }
    });
    port(1, (g, big) => {
        const r = big ? .83 : .66;
        cyl(g, r, r, .96, gold, 0, -.05, 0, 24);
        cyl(g, r * .87, r * .68, .25, white, 0, -.66, 0, 18);
        [-.44, -.31, .14, .30, .42].forEach(y => ring(g, r, .031, steel, y));
        for (let i = 0; i < 10; i++) {
            const a = i * Math.PI / 5;
            const x = Math.sin(a)*r, z = Math.cos(a)*r;
            const guard = box(g, .101, .95, .037, i % 2 ? steel : white, x, -.05, z);
            guard.rotation.y = a;
        }
    });
    port(2, (g, big) => {
        const r = big ? .63 : .49;
        cyl(g,.31,.3,.25,steel,0,-.90,0);
        cyl(g,.19,r,.53,black,0,-1.28,0);
        cyl(g,r * .72,r * .91,.16,steel,0,-1.54,0);
        cyl(g,r * .70,r * .77,.02,black,0,-1.627,0);
        [-1.33,-1.44,-1.60].forEach((y,i)=> ring(g,r*(.68+.14*i),.028,gold,y));
    });
    port(3, (g, big) => {
        for (const sign of [-1,1]) {
            const length = big ? 1.85 : 1.45;
            box(g, .55,.11,.14,steel,sign*.83, .17,0);
            box(g,length,.067,.91,white,sign*(1.05+length/2),.17,0);
            for (let x=0;x<6;x++) for(let z=0;z<4;z++) {
                const xx = (x+.5)*length/6;
                const zz=(z-1.5)*.219;
                box(g,length/6-.017,.035,.200,blue,sign*(1.05+xx),.212,zz);
                box(g,length/6-.017,.025,.200,blue,sign*(1.05+xx),.127,zz);
            }
            box(g,.077,.086,.91,steel,sign*(1.05+length/2),.183,0);
        }
    });
    port(4, (g, big) => {
        box(g,.34,.49,big?.52:.33,white,.72,-.38,.10);
        box(g,.37,.13,big?.50:.32,black,.72,-.39,.10);
    });
    port(5, (g, big) => {
        box(g,.47,.24,big?.58:.43,steel,.17,.68,.74);
        const sensor = cyl(g,.119,.14,.10,window,.17,.68,1.02);
        sensor.rotation.x = Math.PI/2;
    });
    port(6, (g, big) => {
        const stand = cyl(g,.063,.084,.53,steel,.90,.93,.09);
        stand.rotation.z = -.66;
        if (big) {
            const dish = shape(g,new THREE.SphereGeometry(.38,28,16,0,2*Math.PI,Math.PI*.50,Math.PI*.46),white,1.12,1.17,.08);
            dish.rotation.z = -.42;
            cyl(g,.039,.048,.4,gold,1.17,1.15,.08);
        } else {
            const patch = box(g,.43,.042,.36,white,1.10,1.17,.09);
            patch.rotation.z = -.43;
            const feed = box(g,.18,.050,.15,gold,1.10,1.21,.09);
            feed.rotation.z = -.43;
        }
    });
    return model;
}

function clearModel(root, model) {
    root.remove(model);
    const used = new Set();
    model.traverse(child => {
        if (child.geometry) child.geometry.dispose();
        if (child.material) used.add(child.material);
    });
    for (const material of used) material.dispose();
}

class FlightWorld {
    constructor(hosts, reduced) {
        this.hosts = hosts;
        this.reduced = reduced;
        this.frame = 0;
        this.view = 'home';
        this.yaw = 0;
        this.pitch = 0.22;
        this.distance = 4.4;
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.6));
        this.renderer.setClearColor(0x080b14, 0);
        this.renderer.outputColorSpace = THREE.SRGBColorSpace;
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.23;
        const texture = new THREE.TextureLoader().load('./media/lunar_albedo.jpg');
        texture.colorSpace = THREE.SRGBColorSpace;
        texture.anisotropy = Math.min(8, this.renderer.capabilities.getMaxAnisotropy());

        this.hero = new THREE.Scene();
        this.heroCam = setup();
        stars(this.hero);
        this.heroMoon = makeMoon(texture, 1.54);
        this.heroMoon.rotation.y = -1.24;
        this.heroMoon.rotation.z = -.22;
        this.hero.add(this.heroMoon);
        this.hero.add(new THREE.AmbientLight(0xb3baff, .17));
        const key = new THREE.DirectionalLight(0xffe1c8, 3.4);
        key.position.set(-2.2,1.0,3.1);
        this.hero.add(key);

        this.hangar = new THREE.Scene();
        this.hangarCam = setup();
        this.hangar.add(new THREE.HemisphereLight(0xc7d9ff, 0x30253d, 2.1));
        const hangarKey = new THREE.DirectionalLight(0xffecdb, 3.4);
        hangarKey.position.set(3,5,6);
        this.hangar.add(hangarKey);
        const fill = new THREE.DirectionalLight(0xb4a7ff, 2.0);
        fill.position.set(-5,4,-5);
        this.hangar.add(fill);
        this.hangar.add(new THREE.GridHelper(13, 33, 0x534d7c, 0x2a2b44));
        this.hangar.children[this.hangar.children.length-1].position.y = -1.80;

        this.flight = new THREE.Scene();
        this.flightCam = setup();
        stars(this.flight);
        this.flightMoon = makeMoon(texture, 1);
        this.flight.add(this.flightMoon);
        this.flight.add(new THREE.AmbientLight(0xb3baff, .22));
        this.sun = new THREE.DirectionalLight(0xf9e4c8, 2.7);
        this.flight.add(this.sun);
        this.live = this.line(colors.blue);
        this.preview = this.line(colors.mint);
        this.markers = [...Array(3)].map(() => shape(this.flight,new THREE.SphereGeometry(.02,16,12),
            new THREE.MeshBasicMaterial({color:colors.amber})));
        this.vessel = shape(this.flight,new THREE.OctahedronGeometry(.042),
            new THREE.MeshBasicMaterial({color:0xe5f5ff}));
        this.beacon = new THREE.PointLight(0xafbfff, 1.35, 0.35);
        this.flight.add(this.beacon);
        this.terrain = new THREE.Scene();
        this.terrainCam = setup();
        this.terrain.add(new THREE.HemisphereLight(0xe9edff, 0x424164, 2.1));
        const terrainLight = new THREE.DirectionalLight(0xffdfbd, 2.1);
        terrainLight.position.set(-2.9,2.8,-1.4);
        this.terrain.add(terrainLight);
        this.terrainGrid = new THREE.GridHelper(6, 18, 0x565779, 0x2c2d45);
        this.terrain.add(this.terrainGrid);
        this.state = null;
        this.resize = new ResizeObserver(() => this.fit());
        this.bind();
        this.display('home');
        this.animate();
    }
    line(color) {
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position',new THREE.BufferAttribute(new Float32Array(144*3),3));
        const path = new THREE.Line(geometry,new THREE.LineBasicMaterial({color, transparent:true,opacity:.89}));
        path.frustumCulled = false;
        this.flight.add(path);
        return path;
    }
    trace(line, data, scale) {
        const buffer = line.geometry.attributes.position;
        if (!data?.length) { line.visible = false; return; }
        line.visible = true;
        for (let i=0; i<data.length; i++) buffer.setXYZ(i,data[i].x*scale,data[i].y*scale,-data[i].z*scale);
        buffer.needsUpdate = true;
    }
    bind() {
        const canvas = this.renderer.domElement;
        let drag = null;
        canvas.addEventListener('pointerdown', e => {
            if (this.view === 'home') return;
            drag = [e.clientX,e.clientY];
            canvas.setPointerCapture(e.pointerId);
        });
        canvas.addEventListener('pointermove', e => {
            if (!drag) return;
            this.yaw -= (e.clientX-drag[0])*.009;
            this.pitch = Math.max(-1.1,Math.min(1.1,this.pitch+(e.clientY-drag[1])*.009));
            drag = [e.clientX,e.clientY];
        });
        canvas.addEventListener('pointerup',()=>drag=null);
        canvas.addEventListener('pointercancel',()=>drag=null);
        canvas.addEventListener('wheel',e=>{
            if (this.view === 'home') return;
            e.preventDefault();
            this.distance = Math.min(this.view === 'hangar' ? 17 : 7,Math.max(this.view === 'hangar' || this.mode==='terrain' ? 3.5 : 2,this.distance+e.deltaY*.006));
        },{passive:false});
    }
    fit() {
        const host = this.hosts[this.view];
        if (!host?.clientWidth || !host?.clientHeight) return;
        this.renderer.setSize(host.clientWidth,host.clientHeight,false);
        for (const camera of [this.heroCam,this.hangarCam,this.flightCam,this.terrainCam]) {
            camera.aspect=host.clientWidth/host.clientHeight;
            camera.updateProjectionMatrix();
        }
    }
    display(view) {
        if (!this.hosts[view]) {
            this.view = view;
            return;
        }
        this.view = view;
        this.resize.disconnect();
        this.resize.observe(this.hosts[view]);
        this.hosts[view].append(this.renderer.domElement);
        this.reset();
        this.fit();
    }
    reset() {
        this.yaw = this.view === 'hangar' ? .62 : 1.18;
        this.pitch = this.view === 'hangar' ? .28 : .75;
        this.distance = this.view === 'hangar' ? 10 : this.mode === 'terrain' ? 6.4 : 4.1;
        if (this.mode === 'terrain' && this.view === 'flight') {
            this.yaw = .42;
            this.pitch = .84;
        }
    }
    setMode(mode) {
        this.mode = mode;
        this.reset();
    }
    updateCraft(design, slot) {
        const key = JSON.stringify([design.parts,design.angles,slot]);
        if (key === this.craftKey) return;
        this.craftKey=key;
        if (this.model) clearModel(this.hangar,this.model);
        if (this.chaseCraft) clearModel(this.flight,this.chaseCraft);
        this.model = buildCraft(design,slot);
        this.chaseCraft=buildCraft(design,-1);
        this.chaseCraft.scale.setScalar(.018);
        this.chaseCraft.visible = false;
        this.hangar.add(this.model);
        this.flight.add(this.chaseCraft);
    }
    updateFlight(state, catalog, mode) {
        this.state = state;
        if (mode!==this.mode) this.setMode(mode);
        if (mode==='terrain') this.updateTerrain(state.products[state.target],state.target);
        const scale=1/catalog.moonRadius;
        this.sun.position.copy(toView(state.sun,5));
        this.flightMoon.rotation.y = state.time/catalog.moonDay*2*Math.PI;
        this.trace(this.live,state.trajectory,scale);
        this.trace(this.preview,state.preview,scale);
        for(let i=0;i<this.markers.length;i++){
            const marker=this.markers[i];
            marker.position.copy(toView(state.markers[i],1.009));
            marker.scale.setScalar(i===state.target?1.4:.80);
            marker.material.color.set(state.scanned[i]?colors.mint:colors.amber);
        }
        this.vessel.position.copy(toView(state.orbit.p,scale));
        this.beacon.position.copy(this.vessel.position).multiplyScalar(1.03);
        this.vessel.visible=mode==='map';
        if (this.chaseCraft) {
            const radial=toView(state.orbit.p).normalize();
            const along=toView(state.orbit.v).normalize();
            along.addScaledVector(radial,-radial.dot(along)).normalize();
            const heading = along.clone().multiplyScalar(Math.cos(state.heading)).addScaledVector(radial,Math.sin(state.heading));
            this.chaseCraft.visible=mode==='chase';
            this.chaseCraft.position.copy(this.vessel.position);
            this.chaseCraft.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),heading);
        }
        this.live.visible = this.live.visible && mode === 'map';
        this.preview.visible = this.preview.visible && mode === 'map';
    }
    updateTerrain(product, site) {
        if (!product) return;
        const key = site+'/'+product.level;
        if (key === this.terrainKey) return;
        this.terrainKey = key;
        if (this.terrainMesh) clearModel(this.terrain,this.terrainMesh);
        const geo = new THREE.PlaneGeometry(3.35,3.35,product.side-1,product.side-1);
        geo.rotateX(-Math.PI/2);
        const pos = geo.attributes.position;
        const color = [];
        const middle = (product.low+product.high)/2;
        const worldPerMeter = 3.35/((product.side-1)*product.cellKm*1000);
        this.terrainGrid.position.y = (product.low-middle)*worldPerMeter*8-.05;
        const gentle = new THREE.Color(0x328d86), steep = new THREE.Color(0xe7a55c);
        for (let i = 0; i < pos.count; i++) {
            pos.setY(i,(product.height[i]-middle)*worldPerMeter*8);
            const shade = gentle.clone().lerp(steep,Math.min(product.slope[i]/15,1));
            color.push(shade.r,shade.g,shade.b);
        }
        geo.computeVertexNormals();
        geo.setAttribute('color',new THREE.Float32BufferAttribute(color,3));
        const mat = new THREE.MeshStandardMaterial({vertexColors:true,side:THREE.DoubleSide,roughness:0.87,metalness:0.04});
        this.terrainMesh = new THREE.Mesh(geo,mat);
        this.terrain.add(this.terrainMesh);
    }
    orient(cam, center = new THREE.Vector3()) {
        const d = this.distance;
        cam.position.set(Math.sin(this.yaw)*Math.cos(this.pitch)*d,Math.sin(this.pitch)*d,Math.cos(this.yaw)*Math.cos(this.pitch)*d);
        cam.position.add(center);
        cam.lookAt(center);
    }
    animate() {
        requestAnimationFrame(()=>this.animate());
        this.frame++;
        if (this.view === 'home') {
            if (!this.reduced) this.heroMoon.rotation.y += .00035;
            const aspect=this.heroCam.aspect;
            this.heroMoon.position.set(aspect>1.25 ? .77 : .57, .10, 0);
            this.heroMoon.scale.setScalar(aspect>1.25 ? 1 : .87);
            this.heroCam.position.set(0,0,5.05);
            this.heroCam.lookAt(0,0,0);
            this.renderer.render(this.hero,this.heroCam);
        } else if (this.view==='hangar'){
            this.orient(this.hangarCam);
            this.renderer.render(this.hangar,this.hangarCam);
        } else if (this.view==='flight' && this.state){
            if(this.mode==='terrain'){
                this.orient(this.terrainCam);
                this.renderer.render(this.terrain,this.terrainCam);
                return;
            }
            if(this.mode==='chase'){
                const p=this.vessel.position;
                const radial=p.clone().normalize();
                const velocity=toView(this.state.orbit.v).normalize();
                const lateral=new THREE.Vector3().crossVectors(radial,velocity).normalize();
                this.flightCam.position.copy(p).addScaledVector(velocity,-.16).addScaledVector(radial,.055).addScaledVector(lateral,.07);
                this.flightCam.lookAt(p.clone().addScaledVector(velocity,.032).addScaledVector(radial,-.034));
            } else this.orient(this.flightCam);
            this.renderer.render(this.flight,this.flightCam);
        }
    }
}

export function createWorld(hosts, reduced) {
    try { return new FlightWorld(hosts,reduced); }
    catch (error) {
        console.warn('3D view unavailable.',error.message);
        for (const host of Object.values(hosts)) host.classList.add('no-webgl');
        return { display(){}, reset(){}, updateCraft(){}, updateFlight(){} };
    }
}
