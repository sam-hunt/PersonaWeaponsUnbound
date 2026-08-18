#!/usr/bin/env python3
# Persona Weapons Unbound's config shim over the shared translation checker
# (l10n/checker/check_translations.py — the rimworld-l10n submodule). The
# engine holds all logic; this file holds only this repo's config and the
# rationale behind it. Usage is unchanged:
#   python3 Scripts/check-translations.py [--strict] [--root PATH]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "checker"))
import check_translations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

# No [TranslationCanChangeCount]-style matching-token fields in this repo.
engine.PARITY_EXEMPT_FIELDS = set()

# RATIONALE: Royalty is the mod's whole subject matter (persona weapons,
# bladelink traits, Empire techprints) and a hard dependency in About.xml —
# without it the defs do not load at all, so a sidecar generated without it
# would be empty. Nothing else is DLC-gated (no MayRequire in Defs/), so no
# other DLC can silently drop keys from a dump.
engine.REQUIRED_DLCS = {"Royalty"}

# Empty here today; ArchotechAndroidHardware's shim carries the first real
# entry (VREA's AndroidGeneDef -> GeneDef).
engine.DEF_TYPE_ALIASES = {}

# This mod ships a real Keyed surface (1.6/Languages/English/Keyed/PWU_UI.xml),
# so a missing Languages/ tree is a hard config error, not a legal state.
engine.ALLOW_NO_KEYED_SURFACE = False

# The localized Steam Workshop title lives in this Keyed key (the
# settings-window header); the checker enforces the title-coupling rule
# against each .steamworkshop/Description/<Language>.txt title line.
engine.WORKSHOP_TITLE_KEY = "PWU_SettingsCategory"

raise SystemExit(engine.main())
