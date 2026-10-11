// Serves a Unity WebGL build on localhost, opens a free Cloudflare quick tunnel to it and sends the public link to
// the ntfy topic in C:\Dev\.env.dev-notify (same shared helpers the job-hunter project uses, in C:\Dev\scripts).
//
//   node tools/ship-webgl.mjs [--dir Builds/WebGL] [--port 8765] [--no-tunnel] [--no-notify]
//
// Keep it running: closing it closes the tunnel and the link stops working. No account, no cost, no secrets in the
// build. The quick-tunnel URL is public to anyone who has it, so share it only with people you mean to.
import { createServer } from "node:http";
import { createReadStream, existsSync, statSync } from "node:fs";
import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.dirname(here);
const sharedScripts = process.env.DEV_SCRIPTS ?? path.resolve(projectRoot, "..", "..", "scripts");

const args = process.argv.slice(2);
const flag = (name) => args.includes(name);
const option = (name, fallback) => {
  const i = args.indexOf(name);
  return i >= 0 && args[i + 1] ? args[i + 1] : fallback;
};

const root = path.resolve(projectRoot, option("--dir", "Builds/WebGL"));
const port = Number(option("--port", "8765"));

if (!existsSync(path.join(root, "index.html"))) {
  console.error(`[ship] no index.html in ${root}. Build WebGL first (Unity menu: Pulse Arena > Build WebGL).`);
  process.exit(1);
}

const types = {
  ".html": "text/html; charset=utf-8",
  ".js": "application/javascript",
  ".wasm": "application/wasm",
  ".data": "application/octet-stream",
  ".json": "application/json",
  ".css": "text/css",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".ico": "image/x-icon",
  ".svg": "image/svg+xml",
  ".txt": "text/plain; charset=utf-8",
};

const server = createServer((req, res) => {
  let rel;
  try {
    rel = decodeURIComponent(new URL(req.url, "http://x").pathname);
  } catch {
    res.writeHead(400).end();
    return;
  }
  if (rel.endsWith("/")) rel += "index.html";
  const file = path.resolve(root, "." + rel);
  if (file !== root && !file.startsWith(root + path.sep)) {
    res.writeHead(403).end();
    return;
  }
  if (!existsSync(file) || !statSync(file).isFile()) {
    res.writeHead(404).end("not found");
    return;
  }

  // Unity can emit .br or .gz files (Player Settings > Publishing > Compression). Serve them with the matching
  // Content-Encoding so the browser unpacks them; the content type comes from the name without the suffix.
  let name = file;
  const headers = { "Cache-Control": "no-store" };
  if (name.endsWith(".br")) {
    headers["Content-Encoding"] = "br";
    name = name.slice(0, -3);
  } else if (name.endsWith(".gz")) {
    headers["Content-Encoding"] = "gzip";
    name = name.slice(0, -3);
  }
  headers["Content-Type"] = types[path.extname(name).toLowerCase()] ?? "application/octet-stream";
  headers["Content-Length"] = statSync(file).size;
  res.writeHead(200, headers);
  createReadStream(file).pipe(res);
});

server.listen(port, "127.0.0.1", async () => {
  console.log(`[ship] serving ${root} at http://127.0.0.1:${port}`);
  if (flag("--no-tunnel")) return;

  const tunnelModule = path.join(sharedScripts, "cloudflared-quick.mjs");
  if (!existsSync(tunnelModule)) {
    console.error(`[ship] shared tunnel helper not found at ${tunnelModule}. Set DEV_SCRIPTS to the folder that has it.`);
    process.exit(1);
  }

  const { startQuickTunnel } = await import(pathToFileURL(tunnelModule).href);
  const { url, child } = await startQuickTunnel(port);
  console.log("");
  console.log("==============================================");
  console.log(`  Phone / public URL:  ${url}`);
  console.log("==============================================");
  console.log("");

  if (!flag("--no-notify")) {
    const notify = spawn(process.execPath, [path.join(sharedScripts, "notify-dev-url.mjs"), "pulse-arena", url], {
      stdio: "inherit",
    });
    notify.on("exit", (code) => {
      if (code !== 0) console.error("[ship] the link is live but the ntfy message did not send. Copy the URL above.");
    });
  }

  child.on("exit", (code) => process.exit(code ?? 0));
  process.on("SIGINT", () => {
    child.kill();
    process.exit(0);
  });
});
