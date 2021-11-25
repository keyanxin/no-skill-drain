using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NoSkillDrain
{
    [BepInPlugin(NoSkillDrainPlugin.ModGUID, NoSkillDrainPlugin.ModName, NoSkillDrainPlugin.Version)]
    public class NoSkillDrainPlugin : BaseUnityPlugin
    {
        public const string Version = "1.0.1";
        public const string ModName = "No Skill Drain";
        public const string ModGUID = "org.bepinex.plugins.noskilldrain";

        private static NoSkillDrainPlugin selfReference = null;
        public static ManualLogSource logger => selfReference.Logger;

        private void Awake()
        {
            selfReference = this;
            NoSkillDrain.skillDrainMultiplier = Config.Bind<float>(
                "NoSkillDrain",
                "Skill Drain Multiplier",
                -100f,
                "If set to -100 it will not drain skills at all, other settings will apply " +
                "the % multiplier accordingly.");

            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new Harmony(ModGUID);
            harmony.PatchAll(assembly);
        }
    }

    public static class NoSkillDrain
    {
        public static ConfigEntry<float> skillDrainMultiplier;

        public static float applyModifierValue(float targetValue, float value)
        {
            if (value <= -100)
                value = -100;
            return targetValue + ((targetValue / 100) * value);
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
        public static class NoSkillDrainTranspiler
        {
            private static MethodInfo method_Skills_LowerAllSkills = AccessTools.Method(typeof(Skills), nameof(Skills.LowerAllSkills));
            private static MethodInfo method_LowerAllSkills = AccessTools.Method(typeof(NoSkillDrainTranspiler), nameof(NoSkillDrainTranspiler.LowerAllSkills));

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].Calls(method_Skills_LowerAllSkills))
                    {
                        il[i].operand = method_LowerAllSkills;
                    }
                }

                return il.AsEnumerable();
            }

            public static void LowerAllSkills(Skills instance, float factor)
            {
                if (NoSkillDrain.skillDrainMultiplier.Value > -100.0f)
                {
                    instance.LowerAllSkills(NoSkillDrain.applyModifierValue(factor, NoSkillDrain.skillDrainMultiplier.Value));
                    NoSkillDrainPlugin.logger.LogInfo($"skills reduced by factor {NoSkillDrain.skillDrainMultiplier.Value}");
                }
                else
                {
                    NoSkillDrainPlugin.logger.LogInfo("no skill drain applied on death");
                }
            }
        }

    }
}
