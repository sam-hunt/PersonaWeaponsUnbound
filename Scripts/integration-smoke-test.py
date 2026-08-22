#!/usr/bin/env python3
# Pre-release integration smoke test: boots the real game once with PWU plus
# every mod PWU integrates with, on a pinned minimal list where the baseline
# is a clean log, then classifies every Player.log error/warning by origin
# and fails on anything attributed to PWU or an integration seam. Thin shim
# over the shared engine in l10n/smoke/startup_smoke.py (see its header for
# mechanics and the BetterTradersGuild v1.1.0 CWTL incident this exists to
# catch).
#
# Run this before every release, with the game closed:
#   python3 Scripts/integration-smoke-test.py              # boot + scan
#   python3 Scripts/integration-smoke-test.py --no-launch  # rescan last log
#   python3 Scripts/integration-smoke-test.py --strict     # any error fails

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "smoke"))
import startup_smoke as engine  # noqa: E402

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "shunter.personaweaponsunbound"

# RATIONALE: base list = this repo's l10n CANONICAL_ACTIVE_MODS (the family
# boots together). VEF and VPWE (Vanilla Persona Weapons Expanded) are added
# on top because PWU's manual Harmony patch on VEF's
# CompGraphicCustomization.CompFloatMenuOptions, and its VPWEIntegration
# reflection, only activate with them present. VEF loads before VPWE (its
# hard dep). Probe last (auto-quit).
engine.SMOKE_ACTIVE_MODS = [
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.royalty",
    "ludeon.rimworld.ideology",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.odyssey",
    "oskarpotocki.vanillafactionsexpanded.core",
    "vanillaexpanded.vpersonaweaponse",
    "shunter.uniquemeleeweapons",
    "shunter.uniqueweaponsunbound",
    "shunter.personaweaponsunbound",
    "shunter.l10nprobe",
]

engine.OWN_PATTERNS = ["PersonaWeaponsUnbound", "PWU_"]

# The other mod's namespaces/prefixes: an error mentioning any of these gates
# the test even when the exception fires inside their code - the v1.1.0
# incident surfaced as a red error inside CWTL's own static ctor.
engine.INTEGRATION_PATTERNS = {
    "VEF": ["VEF."],
    "VPWE": ["VanillaPersonaWeaponsExpanded", "VPersonaWeaponsE"],
    "UWU": ["UniqueWeaponsUnbound", "UWU_"],
    "UMW": ["UniqueMeleeWeapons", "UMW_"],
}

raise SystemExit(engine.main())
