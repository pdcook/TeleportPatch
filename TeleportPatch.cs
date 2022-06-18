using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace TeleportPatch
{
    [BepInPlugin(ModID, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class TeleportPatch : BaseUnityPlugin
    {
        private const string ModID = "pykess.rounds.plugins.teleportpatch";
        private const string ModName = "Teleport Patch";
        public const string Version = "0.0.0";
        private string CompatibilityModName => ModName.Replace(" ", "");

        public static TeleportPatch instance;

        private Harmony harmony;

#if DEBUG
        public static readonly bool DEBUG = true;
#else
        public static readonly bool DEBUG = false;
#endif
        internal static void Log(string str)
        {
            if (DEBUG)
            {
                UnityEngine.Debug.Log($"[{ModName}] {str}");
            }
        }


        private void Awake()
        {
            instance = this;
            
            harmony = new Harmony(ModID);
            harmony.PatchAll();
        }
        private void Start()
        {
        }

        private void OnDestroy()
        {
            harmony.UnpatchAll(ModID);
        }
        [HarmonyPatch(typeof(Teleport), "Start")]
        static class TeleportPatchStart
        {
            static bool Prefix(Teleport __instance, ref CharacterData ___data, ref AttackLevel ___level)
            {
                __instance.parts = __instance.GetComponentsInChildren<ParticleSystem>();
                ___data = __instance.GetComponentInParent<CharacterData>();
                ___level = __instance.GetComponentInParent<AttackLevel>();

                ___data.block.SuperFirstBlockAction += __instance.Go;

                return false;
            }
        }
        [HarmonyPatch(typeof(Teleport), "OnDestroy")]
        static class TeleportPatchOnDestroy
        {
            static bool Prefix(Teleport __instance, CharacterData ___data)
            {
                ___data.block.SuperFirstBlockAction -= __instance.Go;

                return false;
            }
        }
    }
}