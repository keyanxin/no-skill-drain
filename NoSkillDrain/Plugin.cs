using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NoSkillDrain
{
    [BepInPlugin("FixItFelix.no.skill.drain", Plugin.ModName, Plugin.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Version = "1.0.0";
        public const string ModName = "NoSkillDrain";
        Harmony _Harmony;
        public static ManualLogSource Log;

        private void Awake()
        {
#if DEBUG
			Log = Logger;
#else
            Log = new ManualLogSource(null);
#endif
            NoSkillDrain.skillDrainMultiplier = Config.Bind<float>("NoSkillDrain", "Skill Drain Multiplier", -100f, "If set to -100 it will not drain skills at all, other settings will apply the % multiplier accordingly.");

            _Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void OnDestroy()
        {
            if (_Harmony != null) _Harmony.UnpatchSelf();
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

            /// <summary>
            /// We replace the call to Skills.LowerAllSkills with our own stub, which then applies the death multiplier.
            /// </summary>
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
                }
                else
                {
                    Plugin.Log.LogMessage("no skill drain applied on death");
                }
            }
        }

    }
}
