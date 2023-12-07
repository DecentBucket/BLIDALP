using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using Mono.Security.Interface;
using AnimalsLogic;
using JetBrains.Annotations;
using System.Security.Cryptography.Pkcs;
using RimWorld;

namespace db_BloodLossIsntDeadly_AnimalsLogic_Patch
{
    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public class BloodLossIsntDeadlyForAnimals
    {

        public static IEnumerable<Thing> SickAnimalsFiltered
        {
            get
            {
                foreach (Pawn p2 in PawnsFinder.AllMaps_Spawned.Where((Pawn p) => p.PlayerColonyAnimal_Alive_NoCryptosleep()))
                {
                    for (int i = 0; i < p2.health.hediffSet.hediffs.Count; i++)
                    {
                        Hediff diff = p2.health.hediffSet.hediffs[i];
                        if(diff.CurStage != null && diff.CurStage.lifeThreatening && !diff.FullyImmune() && (diff.def.defName != HediffDefOf.BloodLoss.defName || p2.health.hediffSet.hediffs.Any((Hediff y) => y.Bleeding)))
                        {
                            yield return p2;
                            break;
                        }
                    }
                }
            }
        }

        static BloodLossIsntDeadlyForAnimals()
        {
            new Harmony("com.BloodLossIsntDeadlyForAnimals.patch").PatchAll();
        }

        [HarmonyPatch(typeof(Alert_LifeThreateningHediffAnimal), "GetExplanation")]
        [HarmonyPrefix]
        public static bool GetExplanation(ref TaggedString __result)
        {
            StringBuilder sb = new StringBuilder();
            bool amputatable = false;
            foreach (Pawn pawn in AnimalAlertsUtility.SortedAnimalList(SickAnimalsFiltered))
            {
                sb.AppendLine("    " + pawn.LabelShort + " " + ((pawn.Name != null && !pawn.Name.Numerical) ? ("(" + pawn.KindLabel + ")") : "") + " " + (pawn.HasBondRelation() ? "BondBrackets".Translate().ToString() : ""));
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if(hediff.CurStage != null && hediff.CurStage.lifeThreatening && hediff.Part != null && hediff.Part != pawn.RaceProps.body.corePart)
                    {
                        amputatable = true;
                        break;
                    }
                }
            }
            __result = (amputatable ? string.Format("AnimalsWithLifeThreateningDiseaseAmputationDesc".Translate(), sb.ToString()) : string.Format("AnimalsWithLifeThreateningDiseaseDesc".Translate(), sb.ToString()));
            return false;
        }

        [HarmonyPatch(typeof(Alert_LifeThreateningHediffAnimal), "GetReport")]
        [HarmonyPostfix]
        public static void GetReport(ref AlertReport __result)
        {
            if (__result.Equals(false))
            {
                return;
            }
            __result = AlertReport.CulpritsAre(SickAnimalsFiltered.ToList());
        }
    }
}
