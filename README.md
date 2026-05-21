Awesome—this is totally doable with a safe “file-bridge” pattern. Since Sims 4 script mods can’t (safely) open sockets or HTTP, you’ll:


receive webhooks in a tiny local web service,


drop JSON “commands” into a folder the game can read,


have a Sims 4 Python mod poll that folder and execute actions.


Below is a complete working skeleton you can paste in.

Architecture (quick)


Webhook service (C#/.NET minimal API):
Receives POST /webhook, validates a shared secret (or HMAC), writes a command file to
%USERPROFILE%\Documents\Electronic Arts\The Sims 4\moddata\commands\*.json.


Sims 4 script mod (Python 3.3):
Runs inside the game, scans moddata\commands\ every few in-game minutes, executes action handlers, moves files to processed/ (or failed/).


This is offline/local, within EA’s allowed script-mod model (no network I/O inside the mod).

Command JSON (schema)
{
  "id": "d7b6a8d8-7e6a-4a9e-9c7d-1a2b3c4d5e6f",
  "ts": "2025-11-09T07:45:12Z",
  "action": "add_funds",
  "params": { "amount": 500 },
  "target": { "sim_id": null }, 
  "signature": "optional-HMAC-or-sha256"
}

Common actions you can wire up:


add_funds → add household money


add_buff → apply a buff to a specific Sim


send_notification → show a toast to the active household


run_interaction → queue a specific tuning interaction (advanced; needs tuning IDs)


spawn_object → spawn a debug object (advanced)



1) Webhook service (C#/.NET 8 minimal API)
Create a new console/minimal API:
dotnet new web -n Sims4WebhookBridge
cd Sims4WebhookBridge

Program.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string SharedSecret = "supersecret-change-me";

// Resolve Sims 4 moddata path
string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
string moddata = Path.Combine(docs, "Electronic Arts", "The Sims 4", "moddata");
string commandsDir = Path.Combine(moddata, "commands");
Directory.CreateDirectory(commandsDir);

app.MapPost("/webhook", async (HttpRequest req) =>
{
    // Optional: verify shared secret via header "X-Webhook-Secret"
    if (!req.Headers.TryGetValue("X-Webhook-Secret", out var secret) ||
        !string.Equals(secret.ToString(), SharedSecret, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body)) return Results.BadRequest("Empty body");

    // Optional: HMAC validation with another header, e.g. X-Signature
    // var signature = req.Headers["X-Signature"].ToString();
    // ValidateHmac(signature, body, SharedSecret);

    // Validate minimal JSON
    using var doc = JsonDocument.Parse(body);
    if (!doc.RootElement.TryGetProperty("action", out _))
        return Results.BadRequest("Missing 'action'");

    // Write command file
    var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json";
    var path = Path.Combine(commandsDir, fileName);
    await File.WriteAllTextAsync(path, body, new UTF8Encoding(false));

    return Results.Ok(new { stored = Path.GetFileName(path) });
});

app.Run();

// static string ComputeHmac(string payload, string secret)
// {
//     using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
//     var hash = h.ComputeHash(Encoding.UTF8.GetBytes(payload));
//     return Convert.ToHexString(hash).ToLowerInvariant();
// }

Run it:
dotnet run --urls http://localhost:5055

Test a webhook (PowerShell):
$body = @{
  id = [guid]::NewGuid().ToString()
  ts = (Get-Date).ToUniversalTime().ToString("o")
  action = "add_funds"
  params = @{ amount = 500 }
  target = @{ sim_id = $null }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Method Post -Uri "http://localhost:5055/webhook" `
  -Headers @{ "X-Webhook-Secret" = "supersecret-change-me" } `
  -Body $body -ContentType "application/json"


2) Sims 4 script mod (Python 3.3)

Place the .ts4script (a zipped .py structure) into Documents\Electronic Arts\The Sims 4\Mods\. The mod reads moddata\commands\.

Folder layout (source):
MySims4WebhookMod/
  __init__.py
  bridge.py
  manifest.py          # (optional for organization)

Zip the contents of MySims4WebhookMod and rename to MySims4WebhookMod.ts4script.
bridge.py (core logic; Python 3.3 compatible)
import os, json, shutil, traceback
import services
from sims4 import reload as sims4_reload
from sims4.commands import output as cheats_output
from sims4communitylib.utils.common_time_utils import CommonTimeUtils  # optional if you use S4CL
import alarms
from date_and_time import create_time_span

# Paths
def _get_moddata_dir():
    # Documents\Electronic Arts\The Sims 4\moddata
    return os.path.join(os.path.expanduser("~"), "Documents", "Electronic Arts", "The Sims 4", "moddata")

COMMANDS_DIR = os.path.join(_get_moddata_dir(), "commands")
PROCESSED_DIR = os.path.join(_get_moddata_dir(), "processed")
FAILED_DIR = os.path.join(_get_moddata_dir(), "failed")
for p in (COMMANDS_DIR, PROCESSED_DIR, FAILED_DIR):
    try:
        os.makedirs(p)
    except:
        pass

_ALARM_HANDLE = None
_SCAN_INTERVAL_SIM_MINUTES = 10  # how often to poll, in Sim-minutes

def _log(msg):
    cheats_output("[WebhookMod] {}".format(msg))

def on_zone_load(loaded):
    # Schedule periodic scan
    global _ALARM_HANDLE
    if _ALARM_HANDLE is not None:
        alarms.cancel_alarm(_ALARM_HANDLE)
    time_service = services.time_service()
    _ALARM_HANDLE = alarms.add_alarm(
        None,
        create_time_span(minutes=_SCAN_INTERVAL_SIM_MINUTES),
        _scan_commands,
        repeating=True
    )
    _log("Initialized; polling every {} sim-mins".format(_SCAN_INTERVAL_SIM_MINUTES))

def _scan_commands(_alarm_handle):
    try:
        files = [f for f in os.listdir(COMMANDS_DIR) if f.endswith(".json")]
        files.sort()  # process oldest first
        for fname in files:
            path = os.path.join(COMMANDS_DIR, fname)
            try:
                with open(path, "r") as fp:
                    cmd = json.load(fp)
                _execute_command(cmd)
                shutil.move(path, os.path.join(PROCESSED_DIR, fname))
            except Exception as ex:
                _log("Command {} failed: {}".format(fname, ex))
                traceback.print_exc()
                try:
                    shutil.move(path, os.path.join(FAILED_DIR, fname))
                except:
                    pass
    except Exception as ex:
        _log("Scan error: {}".format(ex))

def _execute_command(cmd):
    action = cmd.get("action")
    params = cmd.get("params", {}) or {}
    target = cmd.get("target", {}) or {}

    handlers = {
        "add_funds": _handle_add_funds,
        "add_buff": _handle_add_buff,
        "send_notification": _handle_send_notification
        # "run_interaction": _handle_run_interaction,  # advanced
        # "spawn_object": _handle_spawn_object,        # advanced
    }

    fn = handlers.get(action)
    if not fn:
        raise ValueError("Unknown action: {}".format(action))
    fn(params, target)

def _resolve_sim_info(sim_id):
    sim_manager = services.sim_info_manager()
    if sim_id:
        return sim_manager.get(int(sim_id))
    active = services.get_active_sim_info()
    return active

def _handle_add_funds(params, target):
    amount = int(params.get("amount", 0))
    if amount == 0:
        return
    household = services.active_household()
    if not household or not hasattr(household, "funds"):
        raise RuntimeError("No active household/funds")
    household.funds.add(amount, reason="Webhook")
    _log("Added {} simoleons to household".format(amount))

def _handle_add_buff(params, target):
    buff_name = params.get("buff")
    sim = _resolve_sim_info(target.get("sim_id"))
    if not sim:
        raise RuntimeError("No sim found")
    # Buffs must exist in your game; replace with a real tuning name
    # Many buff helpers are exposed via libraries; here we use a basic approach:
    try:
        sim.add_buff_from_buff_type_name(buff_name)
    except:
        raise RuntimeError("Buff '{}' not found or add failed".format(buff_name))
    _log("Buff '{}' added to sim {}".format(buff_name, sim))

def _handle_send_notification(params, target):
    title = params.get("title", "Webhook")
    text = params.get("text", "")
    from ui.ui_dialog_notification import UiDialogNotification
    owner = services.get_active_sim_info()
    dialog = UiDialogNotification.TunableFactory().default(owner, text=lambda *a, **k: text, title=lambda *a, **k: title)
    dialog.show_dialog()
    _log("Notification shown: {} - {}".format(title, text))

init.py (hook into zone load)
from sims4.tuning.instance_manager import InstanceManager
from zone import Zone
from .bridge import on_zone_load

# Install zone load callback
def _on_zone_load(event_data=None):
    on_zone_load(True)

# Game inject: this pattern varies; simplest is to listen via services.on_zone_load()
try:
    from services import get_zone_manager
    zone_manager = get_zone_manager()
    # Fallback: use a scheduler when services become available
except:
    pass

# Sims 4 commonly used pattern:
import services
def _startup(_=None):
    zone = services.current_zone()
    if zone is not None:
        on_zone_load(True)

_startup()


Notes
• Exact injection points can vary by patch; the above uses a lightweight “start and poll” approach.
• add_buff_from_buff_type_name is an example; you’ll swap to whatever buff/interaction helpers you prefer (S4CL provides friendlier APIs).
• If you don’t use S4CL, remove that import. The code doesn’t rely on it except the optional line.


Example webhook payloads
Add funds
{
  "id": "c7c4b9c3-324f-4f7c-8f4d-8b7b2adf9acd",
  "ts": "2025-11-09T08:00:00Z",
  "action": "add_funds",
  "params": { "amount": 1000 },
  "target": { "sim_id": null }
}

Add a buff to active Sim
{
  "id": "f5c2b3e8-3b84-4bd6-9a6d-b1f1da0b1b1a",
  "ts": "2025-11-09T08:01:00Z",
  "action": "add_buff",
  "params": { "buff": "Buff_Mood_Energized" },
  "target": { "sim_id": null }
}

Send a notification
{
  "id": "0f4a9e2e-3e39-4b8b-9d0f-8f2c1c3a1d02",
  "ts": "2025-11-09T08:02:00Z",
  "action": "send_notification",
  "params": { "title": "Chat Reward", "text": "Viewers bought you a coffee!" },
  "target": {}
}


Hardening & QoL


Validation: require X-Webhook-Secret or HMAC; reject oversized bodies.


Idempotency: keep a small processed_ids.json and ignore duplicates.


Throttling: cap number of files processed per tick (e.g., 5).


Safety rails: whitelist actions; validate amounts/buffs.


Observability: write a latest.log in moddata with success/fail details (handy for streaming overlays).


Streaming hooks: your C# service can also broadcast to SignalR so Twitch chat sees what was triggered.



How you’ll use this


Put MySims4WebhookMod.ts4script in Mods, enable script mods in game options.


Run the C# webhook bridge.


Send a POST from your bot or StreamElements/IFTTT/etc.


Watch the game execute the action within ~10 sim-minutes (tweak the interval if you want it faster; going too fast can stutter).



If you want, tell me the exact actions you want (e.g., “make Sim dance,” “rain money,” “play VFX,” “spawn cake,” “raise skill”) and I’ll fill in the Python handlers for those with the appropriate tuning/buff/interaction calls and a ready-to-zip mod folder.