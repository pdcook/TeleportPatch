using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using System.Linq;

namespace TeleportPatch
{
    [BepInPlugin(ModID, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class TeleportPatch : BaseUnityPlugin
    {
        private const string ModID = "pykess.rounds.plugins.teleportpatch";
        private const string ModName = "Teleport Patch";
        public const string Version = "0.0.1";
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
                if (___data?.block?.SuperFirstBlockAction is null) { return false; }
                ___data.block.SuperFirstBlockAction -= __instance.Go;

                return false;
            }
        }

        [HarmonyPatch]
        class TeleportPatchDelayMove
        {
            /// <summary>
            ///  patch for teleport to prevent collision detection from interacting with own player
            /// </summary>
            /// <returns></returns>
            static Type GetNestedMoveType()
            {
                var nestedTypes = typeof(Teleport).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
                Type nestedType = null;

                foreach (var type in nestedTypes)
                {
                    if (type.Name.Contains("DelayMove"))
                    {
                        nestedType = type;
                        break;
                    }
                }

                return nestedType;
            }

            static MethodBase TargetMethod()
            {
                return AccessTools.Method(GetNestedMoveType(), "MoveNext");
            }
            static bool DetectCollider(Vector2 point, float radius)
            {
                // return true if there is a collider at the given point THAT IS NOT A TRIGGER COLLIDER
                Collider2D collider = Physics2D.OverlapCircle(point, radius);
                return (!collider?.isTrigger ?? false);
            }


            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {

                var m_detectCollider = typeof(TeleportPatchDelayMove).GetMethod(nameof(DetectCollider), BindingFlags.NonPublic | BindingFlags.Static);
                var m_overlapcircle = typeof(Physics2D).GetMethod(nameof(Physics2D.OverlapCircle), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[] { typeof(Vector2), typeof(float) }, null);

                // replace all calls to Physics2D.OverlapCircle with calls to our own method
                bool skip = false;
                foreach (var instruction in instructions)
                {
                    if (instruction.Calls(m_overlapcircle))
                    {
                        yield return new CodeInstruction(OpCodes.Call, m_detectCollider);
                        yield return new CodeInstruction(OpCodes.Nop);
                        skip = true; // skip the instruction which tries to convert a Collider2D to a bool, since the value is already a bool
                    }
                    else if (!skip)
                    {
                        yield return instruction;
                    }
                    else if (skip)
                    {
                        skip = false;
                    }
                }

            }
        }
    }
}