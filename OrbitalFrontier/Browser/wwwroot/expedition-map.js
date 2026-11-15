export function readSurface(world,encoded) {
    const bytes=Uint8Array.from(atob(encoded),c=>c.charCodeAt(0));
    const values=new DataView(bytes.buffer),n=world.mapSize,spacing=world.spacing, extent=(n-1)*spacing*.5;
    if(bytes.byteLength!==n*n*2)throw Error('Terrain grid mismatch at '+world.name);
    const cell=(a,b)=>values.getInt16((b*n+a)*2,true)*.25;
    const height=(x,z)=>{
        const tx=Math.max(0,Math.min(n-1.000001,(x+extent)/spacing));
        const tz=Math.max(0,Math.min(n-1.000001,(z+extent)/spacing));
        const i=Math.floor(tx),j=Math.floor(tz), fx=tx-i,fz=tz-j;
        return (1-fz)*(cell(i,j)*(1-fx)+cell(i+1,j)*fx)+fz*(cell(i,j+1)*(1-fx)+cell(i+1,j+1)*fx);
    };
    return {extent,height,spacing};
}

export function createChart(canvas, catalog, ground, file, stations) {
    const ctx=canvas.getContext('2d');
    const localMaps=catalog.worlds.map(w=>{const im=new Image();im.src=file(w.texture+'.jpg');return im;});
    const planetMaps=catalog.worlds.map(w=>{const im=new Image();im.src=file((w.id==='moon'?'lunar':'mars')+'_albedo.jpg');return im;});
    const W=canvas.width,H=canvas.height;
    const circle=(x,y,r,color)=>{
        ctx.beginPath();ctx.arc(x,y,r,0,Math.PI*2);ctx.fillStyle=color;ctx.fill();
    };
    const mark=(x,y,color,text,r=5)=>{
        circle(x,y,r,color);
        ctx.font='13px monospace';ctx.fillStyle='#f5e7f4';ctx.fillText(text,x+10,y-7);
    };
    function label(x,y,message,colour='#8e98b6',size=13){
        ctx.font=size+'px monospace';ctx.fillStyle=colour;ctx.fillText(message,x,y);
    }
    function drawCabin(s,me) {
        const scale=25,x=330,y=249;
        const coord=(a,z)=>[x+a*scale,y+z*scale];
        ctx.fillStyle='#222a3f';ctx.fillRect(x-2.4*scale,y-8.84*scale,4.8*scale,16.28*scale);
        ctx.strokeStyle='#8682a6';ctx.strokeRect(x-2.4*scale,y-8.84*scale,4.8*scale,16.28*scale);
        ctx.strokeStyle='#463f63';
        for(const z of [-4.3,4.3]){
            ctx.beginPath();ctx.moveTo(x-2.4*scale,y+z*scale);ctx.lineTo(x-1.23*scale,y+z*scale);
            ctx.moveTo(x+2.4*scale,y+z*scale);ctx.lineTo(x+1.23*scale,y+z*scale);ctx.stroke();
        }
        label(38,41,'SHIP DECK  /  BOW UP','#cabbfa',15);
        stations.forEach((a,i)=>{
            const [u,v]=coord(a.x,a.z),onLeft=i===0||i===2||i===4;
            const hx=onLeft?71:452,hy=v+4;
            ctx.strokeStyle='#847597';ctx.beginPath();ctx.moveTo(u,v);ctx.lineTo(onLeft?hx+124:hx-9,v);ctx.stroke();
            circle(u,v,6,'#d5aa80');
            label(hx,hy,a.name.toUpperCase(),'#dec8e9',12);
        });
        for(const person of s.crew)if(person.active&&!person.outside){
            let [x,y]=coord(person.x,person.z);
            circle(x,y,person.id===me.id?7:5,person.id===me.id?'#9de3ce':'#f0ae78');
        }
        const [a,b]=coord(me.x,me.z),r=me.yaw*Math.PI/180;
        ctx.strokeStyle='#c0f0ed';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(a,b);ctx.lineTo(a+Math.sin(r)*19,b-Math.cos(r)*19);ctx.stroke();
        label(42,461,'4.4 m cabin interior','#8790ad');
        return 'Mint marker = your position · orange markers = crew stations · gold dots = other crew';
    }
    function drawSite(s,me,w) {
        const t=s.telemetry,g=ground[s.design.world],max=430,box=427,ox=(W-box)/2,oy=40;
        const p=(x,z)=>[ox+(x/max+1)*box*.5,oy+(z/max+1)*box*.5];
        const im=localMaps[s.design.world];
        if(im.complete&&im.naturalWidth){
            const repeat=max/g.extent, offset=(1-repeat)*im.naturalWidth*.5;
            ctx.drawImage(im,offset,offset,im.naturalWidth*repeat,im.naturalHeight*repeat,ox,oy,box,box);
            ctx.fillStyle='#120c2635';ctx.fillRect(ox,oy,box,box);
        }
        ctx.strokeStyle='#c2a8e842';ctx.lineWidth=1;
        for(let i=0;i<=8;i++){
            ctx.beginPath();ctx.moveTo(ox,oy+i*box/8);ctx.lineTo(ox+box,oy+i*box/8);
            ctx.moveTo(ox+i*box/8,oy);ctx.lineTo(ox+i*box/8,oy+box);ctx.stroke();
        }
        label(17,28,w.name.toUpperCase(),'#dbcbec',15);
        label(548,60,'N ↑','#e4d6f3');
        label(550,461,'100 m','#e3d5ef');
        ctx.fillStyle='#eee1f4';ctx.fillRect(552,471,box*100/(max*2),2);
        mark(...p(t.x,t.z),'#f7ca92','Lander');
        if(s.phase===5&&s.design.parts[14]&&Math.hypot(t.x-t.roverX,t.z-t.roverZ)>16)mark(...p(t.roverX,t.roverZ),'#d9a965','Rover',4);
        w.points.forEach((a,i)=>mark(...p(a.x,a.z),t.marks[i]?'#9de0bc':'#c4a6fb',i===0?'A':'B'));
        for(const person of s.crew)if(person.active&&person.outside){
            const [x,y]=p(person.x,person.z);
            mark(x,y,person.id===me.id?'#a2e2e1':'#fa9a75',person.id===me.id?'You':String(person.id+1),person.id===me.id?6:4);
            if(person.id!==me.id)continue;
            const heading=person.yaw*Math.PI/180;
            ctx.strokeStyle='#a5e9e0';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(x,y);ctx.lineTo(x+Math.sin(heading)*27,y-Math.cos(heading)*27);ctx.stroke();
        }
        return w.credit+' · orthorectified image · coordinates shown in metres relative to the map origin';
    }
    function drawOrbit(s,w) {
        const x=45,y=82,ww=W-85,hh=305,t=s.telemetry;
        const im=planetMaps[s.design.world];
        if(im.complete&&im.naturalWidth)ctx.drawImage(im,x,y,ww,hh);
        ctx.fillStyle='#15102038';ctx.fillRect(x,y,ww,hh);
        ctx.strokeStyle='#97a3c955';
        for(let i=0;i<=8;i++){
            ctx.beginPath();ctx.moveTo(x+ww*i/8,y);ctx.lineTo(x+ww*i/8,y+hh);ctx.stroke();
        }
        for(let i=0;i<=6;i++){
            ctx.beginPath();ctx.moveTo(x,y+hh*i/6);ctx.lineTo(x+ww,y+hh*i/6);ctx.stroke();
        }
        const p=(lat,lon)=>[x+(lon+180)*ww/360,y+(90-lat)*hh/180];
        label(45,42,w.body.toUpperCase()+'  /  GROUND TRACK','#dfcff1',15);
        if(t.track?.length) {
            ctx.strokeStyle='#e2c8ff';ctx.lineWidth=1.5;ctx.beginPath();
            let px=null;
            for(const a of t.track){
                const lon=Math.atan2(a.z,a.x)*180/Math.PI,lat=Math.asin(a.y/Math.hypot(a.x,a.y,a.z))*180/Math.PI;
                const [u,v]=p(lat,lon);
                if(px===null||Math.abs(u-px)>ww*.5)ctx.moveTo(u,v);else ctx.lineTo(u,v);
                px=u;
            }
            ctx.stroke();
        }
        mark(...p(w.latitude,w.longitude),'#ffb66e',w.name,6);
        if(t.orbit?.p){
            const a=t.orbit.p;
            if(Math.hypot(a.x,a.y,a.z)>0) mark(...p(Math.asin(a.y/Math.hypot(a.x,a.y,a.z))*180/Math.PI,Math.atan2(a.z,a.x)*180/Math.PI),'#87ddc6','Ship',5);
        }
        label(45,420,'PERIAPSIS  '+t.orbitalPeri.toFixed(1)+' km  /  APOAPSIS  '+t.orbitalApo.toFixed(1)+' km');
        return 'Lavender line = computed orbit ground track · orange = survey area · mint = current spacecraft position';
    }
    function drawTravel(s,w) {
        const t=s.telemetry;
        label(49,39,s.phase===1?'ASCENT AND INJECTION':'TRANSFER TO '+w.body.toUpperCase(),'#e2ccfc',15);
        const x=119,end=543,y=229;
        const progress=s.phase===1?Math.min(1,(t.stageCount+t.progress/10)/2):Math.min(1,t.progress);
        ctx.strokeStyle='#8b7ab4';ctx.lineWidth=2;
        ctx.setLineDash([5,7]);ctx.beginPath();ctx.moveTo(x,y);ctx.bezierCurveTo(225,85,421,103,end,y);ctx.stroke();ctx.setLineDash([]);
        mark(x,y,'#69a9e5','Earth',24);mark(end,y,w.id==='mars'?'#e5a679':'#b6abc8',w.body,18);
        let k=1-progress;
        const cx=k*k*k*x+3*k*k*progress*225+3*k*progress*progress*421+progress*progress*progress*end;
        const cy=k*k*k*y+3*k*k*progress*85+3*k*progress*progress*103+progress*progress*progress*y;
        mark(cx,cy,'#b6f1de','Ship',6);
        label(49,382,'Time of flight: '+s.budget.travelDays.toFixed(1)+' d scheduled');
        label(49,410,'Capture corridor: '+s.budget.orbitKm+' km');
        return 'Route view · distance and time of flight are numerically modeled for the mission';
    }
    return (s,seat,kind)=>{
        ctx.clearRect(0,0,W,H);ctx.fillStyle='#121526';ctx.fillRect(0,0,W,H);
        const me=s.crew[seat],w=catalog.worlds[s.design.world];
        if(kind==='deck')return drawCabin(s,me);
        if(s.phase===4||s.phase===5)return drawSite(s,me,w);
        if(s.phase===3)return drawOrbit(s,w);
        return drawTravel(s,w);
    };
}
