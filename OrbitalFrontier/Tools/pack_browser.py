#!/usr/bin/env python3
"""Pack the published browser game and its runtime assets into one offline HTML file."""

import base64
import json
import mimetypes
import re
import sys
from pathlib import Path


def read(path):
    return path.read_text(encoding="utf-8")


def encoded(path):
    return base64.b64encode(path.read_bytes()).decode("ascii")


def required_replace(text, old, new, description):
    if old not in text:
        raise ValueError(f"Could not locate {description}")
    return text.replace(old, new, 1)


def pack(source, output):
    source = source.resolve()
    framework = source / "_framework"
    media = source / "media"

    boot = json.loads(read(framework / "blazor.boot.json"))
    boot_script = "const boot = " + json.dumps(boot, separators=(",", ":")) + ";\n"

    modules = {
        name: encoded(framework / name)
        for name in ("dotnet.native.js", "dotnet.runtime.js")
    }
    native_js = read(framework / "dotnet.native.js")
    native_js = required_replace(
        native_js,
        'wasmBinaryFile=new URL("dotnet.native.wasm",import.meta.url).href',
        "wasmBinaryFile=globalThis.__orbitalNativeWasm",
        "native WebAssembly module URL",
    )
    modules["dotnet.native.js"] = base64.b64encode(native_js.encode("utf-8")).decode("ascii")
    dotnet_js = read(framework / "dotnet.js")
    dotnet_js = required_replace(
        dotnet_js,
        "const t=import.meta.url,o=t.indexOf(\"?\");var n;",
        "const t=globalThis.document.baseURI,o=t.indexOf(\"?\");var n;",
        ".NET runtime module base URL",
    )
    dotnet_js = re.sub(r"\n?//# sourceMappingURL=.*$", "", dotnet_js)
    dotnet_js = base64.b64encode(dotnet_js.encode("utf-8")).decode("ascii")
    binaries = {
        path.name: encoded(path)
        for path in sorted(framework.glob("*.wasm"))
    }
    images = {}
    heights = {}
    for path in sorted(media.iterdir()):
        if not path.is_file() or path.suffix == ".meta":
            continue
        if path.name in ("moon_height.bytes", "mars_height.bytes"):
            heights[path.name] = encoded(path)
        elif path.name == "expedition.json":
            continue
        else:
            mime = mimetypes.guess_type(path.name)[0] or "application/octet-stream"
            images[path.name] = "data:" + mime + ";base64," + encoded(path)

    runtime = (
        boot_script
        + "const embeddedImages = " + json.dumps(images, separators=(",", ":")) + ";\n"
        + "const embeddedHeight = " + json.dumps(heights, separators=(",", ":")) + ";\n"
        + "const embeddedCatalog = " + json.dumps(read(media / "expedition.json")) + ";\n"
        + "function from64(value) { return Uint8Array.from(atob(value), c => c.charCodeAt(0)); }\n"
        + "function moduleURL(value) { return URL.createObjectURL(new Blob([from64(value)],{type:'text/javascript'})); }\n"
        + "const locators = Object.fromEntries(Object.entries("
        + json.dumps(modules, separators=(",", ":"))
        + ").map(([key,value])=>[key,moduleURL(value)]));\n"
        + "const binaries = " + json.dumps(binaries, separators=(",", ":")) + ";\n"
        + "globalThis.__orbitalNativeWasm=URL.createObjectURL(new Blob([from64(binaries['dotnet.native.wasm'])],{type:'application/wasm'}));\n"
        + "const mRuntime = await import(moduleURL('" + dotnet_js + "'));\n"
        + "const dotnet = mRuntime.dotnet.withConfig({...boot, cacheBootResources:false}).withResourceLoader((kind,name)=>{\n"
        + "  if (locators[name]) return locators[name];\n"
        + "  if (binaries[name]) return Promise.resolve(new Response(from64(binaries[name]),{headers:{'content-type':'application/wasm'}}));\n"
        + "  return undefined;\n"
        + "});\n"
        + "const THREE = await import(moduleURL('" + encoded(source / "three.module.min.js") + "'));\n"
    )

    world = read(source / "expedition-world.js")
    world = required_replace(world, "import * as THREE from './three.module.min.js';\n", "", "Three.js import")
    world = required_replace(world, "export function renderExpedition", "function renderExpedition", "world export")

    map_source = read(source / "expedition-map.js")
    map_source = required_replace(map_source, "export function readSurface", "function readSurface", "surface export")
    map_source = required_replace(map_source, "export function createChart", "function createChart", "chart export")

    glossary = required_replace(
        read(source / "expedition-glossary.js"),
        "export const glossary =",
        "const glossary =",
        "glossary export",
    )

    app = read(source / "expedition.js")
    app = re.sub(r"^import .*?;\r?\n", "", app, flags=re.MULTILINE)
    app = required_replace(app, "const file=name=>'./media/'+name;", "const file=name=>embeddedImages[name];", "media URL helper")
    app = required_replace(
        app,
        "const jsonAsset=name=>fetch(file(name)).then(r=>{if(!r.ok)throw Error('Missing '+name);return r.text();});",
        "const jsonAsset=()=>Promise.resolve(embeddedCatalog);",
        "mission catalog loader",
    )
    app = required_replace(
        app,
        "const binaryAsset=name=>fetch(file(name)).then(r=>{if(!r.ok)throw Error('Missing '+name);return r.arrayBuffer();})\n"
        "    .then(buf=>btoa(Array.from(new Uint8Array(buf),c=>String.fromCharCode(c)).join('')));",
        "const binaryAsset=name=>Promise.resolve(embeddedHeight[name]);",
        "terrain loader",
    )

    index = read(source / "index.html")
    flight_css = read(source / "flight.css")
    for filename in ("Barlow-Regular.ttf", "Barlow-Bold.ttf", "IBMPlexMono-Regular.ttf"):
        flight_css = required_replace(
            flight_css,
            "url(media/" + filename + ")",
            "url(data:font/ttf;base64," + encoded(media / filename) + ")",
            "font asset reference for " + filename,
        )
    css = flight_css + read(source / "expedition.css")
    index = required_replace(index, '  <link rel="stylesheet" href="./flight.css">\n', "", "flight stylesheet link")
    index = required_replace(index, '  <link rel="stylesheet" href="./expedition.css">\n', "", "expedition stylesheet link")
    index = required_replace(
        index,
        '  <script type="module" src="./expedition.js"></script>',
        "",
        "browser application script link",
    )
    index = required_replace(index, "</head>", "  <style>\n" + css + "\n  </style>\n</head>", "HTML head")

    licenses = []
    for filename in (
        "Barlow-OFL.txt",
        "IBMPlexMono-OFL.txt",
        "three-LICENSE.txt",
        "dotnet-LICENSE.txt",
        "dotnet-ThirdPartyNotices.txt",
    ):
        path = source / filename
        if path.is_file():
            licenses.append("<!-- " + filename + "\n" + read(path) + "\n-->")

    application = runtime + world + map_source + glossary + app
    index = required_replace(
        index,
        "</head>",
        "  <script type=\"module\">\n" + application + "\n  </script>\n</head>",
        "application script insertion point",
    )
    index = index.replace("</html>", "\n" + "\n".join(licenses) + "\n</html>")

    output = output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(index, encoding="utf-8", newline="\n")
    print(f"Packed offline game: {output} ({output.stat().st_size:,} bytes)")


def main():
    if len(sys.argv) != 3:
        raise SystemExit("Usage: pack_browser.py <published-wwwroot> <output.html>")
    pack(Path(sys.argv[1]), Path(sys.argv[2]))


if __name__ == "__main__":
    main()
