using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace PersonaWeaponsUnbound
{
    // Fabrication-bench classification (vanilla FabricationBench plus any VEF
    // equivalent) and runtime workbench search for weapon customization.
    public static class WorkbenchUtility
    {
        private static HashSet<ThingDef> fabricationDefs;
        private static string fabricationLabel;

        // Initializes the fabrication-bench set. Must be called during
        // StaticConstructorOnStartup (after all defs are loaded). A non-null
        // report absorbs any fatal exception so the rest of the mod can still
        // initialize; passing null preserves the throwing contract for direct
        // callers.
        public static void Initialize(InitDiagnostics report = null)
        {
            try
            {
                fabricationDefs = ResolveDefSet("FabricationBench");
                fabricationLabel = ResolveWorkbenchLabel(fabricationDefs);
                ExpandFromVEF();
            }
            catch (Exception ex)
            {
                if (report == null) throw;
                report.RecordFailure(nameof(WorkbenchUtility), ex);
            }
        }

        // Result of searching for a valid workbench to customize a weapon at.
        // Either contains a workbench or the highest-priority rejection reason.
        public struct WorkbenchSearchResult
        {
            public Building_WorkTable Workbench;
            public AcceptanceReport BestRejection;
            public bool Found => Workbench != null;
        }

        // Finds the closest valid colonist workbench for customizing the specified weapon.
        // Pawn-specific overload: checks reachability via the pawn's pathfinder and
        // forbidden status relative to the pawn.
        public static WorkbenchSearchResult FindBestWorkbench(
            Pawn pawn, ThingDef baseDef, ThingDef personaDef, TechLevel weaponTechLevel,
            IntVec3 distanceOrigin)
        {
            return FindBestWorkbenchCore(pawn.Map, baseDef, personaDef, weaponTechLevel,
                distanceOrigin, pawn, workbench =>
                {
                    if (!pawn.CanReach(workbench, PathEndMode.InteractionCell, Danger.Deadly))
                        return "NoPath".Translate();
                    if (workbench.IsForbidden(pawn))
                        return "ForbiddenLower".Translate();
                    return true;
                });
        }

        // Finds the closest valid colonist workbench for customizing the specified weapon.
        // Pawn-independent overload: checks generic reachability from a map position and
        // forbidden status relative to the player faction. Used for gizmo enabled/disabled
        // state where no specific pawn is known yet.
        public static WorkbenchSearchResult FindBestWorkbench(
            Map map, ThingDef baseDef, ThingDef personaDef, TechLevel weaponTechLevel,
            IntVec3 distanceOrigin)
        {
            return FindBestWorkbenchCore(map, baseDef, personaDef, weaponTechLevel,
                distanceOrigin, null, workbench =>
                {
                    if (!map.reachability.CanReach(distanceOrigin, workbench,
                            PathEndMode.InteractionCell,
                            TraverseParms.For(TraverseMode.PassDoors)))
                        return "NoPath".Translate();
                    if (workbench.IsForbidden(Faction.OfPlayer))
                        return "ForbiddenLower".Translate();
                    return true;
                });
        }

        // Common core for workbench search. Iterates colonist workbenches, applies the
        // fabrication-set and operational checks, then delegates reachability/forbidden
        // checks to the caller-provided predicate. Returns the closest valid workbench or
        // the highest-priority rejection reason. baseDef/personaDef/weaponTechLevel are
        // accepted for call-site stability but unused now that there is a single bench
        // tier (§8).
        private static WorkbenchSearchResult FindBestWorkbenchCore(
            Map map, ThingDef baseDef, ThingDef personaDef, TechLevel weaponTechLevel,
            IntVec3 distanceOrigin, Pawn pawn,
            Func<Building_WorkTable, AcceptanceReport> accessCheck)
        {
            // Track two tiers: prefer unreserved benches, fall back to reserved.
            // This avoids interrupting in-progress work when a free bench is available,
            // while still allowing it when all valid benches are occupied.
            Building_WorkTable bestFree = null;
            float bestFreeDistSq = float.MaxValue;
            Building_WorkTable bestReserved = null;
            float bestReservedDistSq = float.MaxValue;
            int bestRejectionPriority = -1;
            AcceptanceReport bestRejection = false;

            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (!(building is Building_WorkTable workbench))
                    continue;
                if (!fabricationDefs.Contains(workbench.def))
                    continue;

                // Operational check (priority 3)
                AcceptanceReport opReport = GetWorkbenchOperationalReport(workbench);
                if (!opReport.Accepted)
                {
                    if (bestRejectionPriority < 3)
                    {
                        bestRejectionPriority = 3;
                        bestRejection = opReport;
                    }
                    continue;
                }

                // Caller-provided access check (reachability + forbidden)
                AcceptanceReport accessReport = accessCheck(workbench);
                if (!accessReport.Accepted)
                {
                    // Determine priority from the rejection reason
                    int priority = accessReport.Reason == "ForbiddenLower".Translate() ? 1 : 2;
                    if (bestRejectionPriority < priority)
                    {
                        bestRejectionPriority = priority;
                        bestRejection = accessReport;
                    }
                    continue;
                }

                // Valid candidate — sort into free vs reserved, track closest in each
                float distSq = (distanceOrigin - workbench.Position).LengthHorizontalSquared;
                bool reservedByOther = pawn != null
                    ? map.reservationManager.IsReserved(workbench)
                        && !map.reservationManager.ReservedBy(workbench, pawn)
                    : map.reservationManager.IsReserved(workbench);
                if (reservedByOther)
                {
                    if (distSq < bestReservedDistSq)
                    {
                        bestReservedDistSq = distSq;
                        bestReserved = workbench;
                    }
                }
                else
                {
                    if (distSq < bestFreeDistSq)
                    {
                        bestFreeDistSq = distSq;
                        bestFree = workbench;
                    }
                }
            }

            var result = new WorkbenchSearchResult();
            Building_WorkTable bestWorkbench = bestFree ?? bestReserved;
            if (bestWorkbench != null)
            {
                result.Workbench = bestWorkbench;
            }
            else
            {
                result.BestRejection = bestRejectionPriority >= 0
                    ? bestRejection
                    : "PWU_NoSuitableWorkbench".Translate();
            }
            return result;
        }

        // Whether the workbench is a fabrication bench (or a VEF-recognized
        // equivalent), making it eligible to show the customization float menu option.
        public static bool IsCustomizationWorkbench(Building_WorkTable workbench)
        {
            return fabricationDefs.Contains(workbench.def);
        }

        // Whether the given workbench supports customizing weapons at all. With the
        // single fabrication-bench tier (§8) this is just set membership; baseDef/
        // personaDef/weaponTechLevel are accepted for call-site stability only.
        // Returns AcceptanceReport naming the fabrication bench when the workbench
        // doesn't qualify.
        public static AcceptanceReport CanCustomizeAtWorkbench(
            ThingDef baseDef, ThingDef personaDef, TechLevel weaponTechLevel,
            Building_WorkTable workbench)
        {
            if (fabricationDefs.Contains(workbench.def))
                return true;
            return "PWU_RequiresWorkbench".Translate(fabricationLabel);
        }

        // Whether the workbench is operational (powered and/or fueled as required).
        // Returns AcceptanceReport with a rejection reason if not operational.
        public static AcceptanceReport GetWorkbenchOperationalReport(Building_WorkTable workbench)
        {
            CompPowerTrader power = workbench.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                return "NoPower".Translate();

            CompRefuelable fuel = workbench.TryGetComp<CompRefuelable>();
            if (fuel != null && !fuel.HasFuel)
                return "NoFuel".Translate();

            return true;
        }

        // Resolves an array of defNames into a set of ThingDefs, silently skipping any that don't exist.
        private static HashSet<ThingDef> ResolveDefSet(params string[] defNames)
        {
            var set = new HashSet<ThingDef>();
            foreach (string name in defNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def != null)
                    set.Add(def);
            }
            return set;
        }

        // Resolves a display label for the fabrication-bench set. With a single
        // vanilla anchor this is just its label.
        private static string ResolveWorkbenchLabel(HashSet<ThingDef> defs)
        {
            foreach (ThingDef def in defs)
                return def.label;
            return "?";
        }

        // Expands the fabrication set with benches that inherit recipes from a
        // vanilla fabrication bench via VEF's RecipeInheritanceExtension — directly,
        // or transitively through another bench this pass has already classified
        // (e.g. a bench that inherits from VFE's compact fabrication bench, which
        // itself inherits from FabricationBench). Repeats until a pass adds nothing,
        // so classification doesn't depend on DefDatabase iteration order. No-op when
        // VEF is not loaded or its integration surface has drifted (the integration
        // class logs the drift warning once at static-ctor time).
        private static void ExpandFromVEF()
        {
            if (!VEFRecipeInheritanceIntegration.Available)
                return;

            bool addedThisPass;
            do
            {
                addedThisPass = false;
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    try
                    {
                        if (ClassifyVEFInheritedDef(def))
                            addedThisPass = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[Persona Weapons Unbound] Skipped VEF fabrication classification for "
                            + def.SourceForLog() + " due to error: " + ex);
                    }
                }
            } while (addedThisPass);
        }

        // Classifies a single def as a fabrication-bench equivalent if it carries a
        // VEF RecipeInheritanceExtension reaching an already-classified bench.
        // Returns true if the def was newly added.
        private static bool ClassifyVEFInheritedDef(ThingDef def)
        {
            if (def.modExtensions == null || fabricationDefs.Contains(def))
                return false;

            foreach (DefModExtension ext in def.modExtensions)
            {
                if (!VEFRecipeInheritanceIntegration.TryGetInheritFrom(ext, out List<ThingDef> inheritFrom))
                    continue;

                foreach (ThingDef source in inheritFrom)
                {
                    if (fabricationDefs.Contains(source))
                    {
                        fabricationDefs.Add(def);
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
    }
}
