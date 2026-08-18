#!/usr/bin/env python3
# Persona Weapons Unbound's config shim over the shared sidecar-refresh
# engine (l10n/refresh/refresh_expectations.py — the rimworld-l10n
# submodule), which drives the L10nProbe dev mod (source at l10n/probe/;
# build/deploy it only from the canonical ~/dev/rimworld-l10n checkout). The
# engine holds all logic; this file holds only this repo's config and the
# rationale behind it. Usage is unchanged (game must be closed):
#   python3 Scripts/refresh-translation-expectations.py [--no-launch]
# If l10n/ is empty, run: git submodule update --init

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "l10n" / "refresh"))
import refresh_expectations as engine  # noqa: E402  (import after sys.path edit)

engine.REPO_ROOT = Path(__file__).resolve().parent.parent

engine.PACKAGE_ID = "shunter.personaweaponsunbound"

# RATIONALE: Royalty is a hard dependency (About.xml's modDependencies) and
# the mod's whole subject matter. Ideology/Biotech/Odyssey ride along because
# they are declared loadAfter and are part of the family's shared DLC set;
# nothing in this repo's Defs/ MayRequires them today, but keeping the probe
# boot's DLC set matched across the family avoids a divergent dump if that
# ever changes. UniqueMeleeWeapons and UniqueWeaponsUnbound ride along as
# family siblings — this repo is the only sidecar-bearing mod whose own
# correctness this boot proves, but pinning the same family list lets one
# boot refresh every sibling's dump. See the engine's header for the general
# membership rule, the lowercase-id warning, and the pinning rationale;
# order is load order, the probe last.
engine.CANONICAL_ACTIVE_MODS = [
    "brrainz.harmony",
    "ludeon.rimworld",
    "ludeon.rimworld.royalty",
    "ludeon.rimworld.ideology",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.odyssey",
    "shunter.uniquemeleeweapons",
    "shunter.uniqueweaponsunbound",
    "shunter.personaweaponsunbound",
    "shunter.l10nprobe",
]

raise SystemExit(engine.main())
