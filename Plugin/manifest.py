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
