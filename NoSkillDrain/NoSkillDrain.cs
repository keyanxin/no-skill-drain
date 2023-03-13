using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NoSkillDrain
{
    [BepInPlugin(ModGuid, ModName, Version)]
    public class NoSkillDrainPlugin : BaseUnityPlugin
    {
        public const string Version = "1.1.0";
        public const string ModName = "No Skill Drain";
        private const string ModGuid = "org.bepinex.plugins.noskilldrain";

        private static NoSkillDrainPlugin _selfReference;
        public static ManualLogSource StaticLogger => _selfReference.Logger;

        private void Awake()
        {
            _selfReference = this;
            NoSkillDrain.SkillDrainMultiplier = Config.Bind(
                "NoSkillDrain",
                "Skill Drain Multiplier",
                -100f,
                "If set to -100 it will not drain skills at all, other settings will apply " +
                "the % multiplier accordingly.");

            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new Harmony(ModGuid);
            harmony.PatchAll(assembly);
        }
    }

    public static class NoSkillDrain
    {
        public static ConfigEntry<float> SkillDrainMultiplier;

        private static float ApplyModifierValue(float targetValue, float value)
        {
            if (value <= -100)
                value = -100;
            return targetValue + ((targetValue / 100) * value);
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
        public static class NoSkillDrainTranspiler
        {
            private static readonly MethodInfo MethodSkillsLowerAllSkills =
                AccessTools.Method(typeof(Skills), nameof(Skills.LowerAllSkills));

            private static readonly MethodInfo MethodLowerAllSkills = 
                AccessTools.Method(typeof(NoSkillDrainTranspiler), nameof(NoSkillDrainTranspiler.LowerAllSkills));

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                foreach (var t in il) {
                    if (t.Calls(MethodSkillsLowerAllSkills))
                    {
                        t.operand = MethodLowerAllSkills;
                    }
                }

                return il.AsEnumerable();
            }

            public static void LowerAllSkills(Skills instance, float factor)
            {
                if (SkillDrainMultiplier.Value > -100.0f)
                {
                    instance.LowerAllSkills(ApplyModifierValue(factor, 
                        SkillDrainMultiplier.Value));
                    NoSkillDrainPlugin.StaticLogger.LogInfo(
                        $"skills reduced by factor {SkillDrainMultiplier.Value}");
                }
                else
                {
                    NoSkillDrainPlugin.StaticLogger.LogInfo("no skill drain applied on death");
                }
            }
        }
    }
}
