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
