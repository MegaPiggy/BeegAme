using HarmonyLib;
using SALT;
using SALT.Config.Attributes;
using SALT.Extensions;
using SALT.Utils;
using SALT.Registries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BeegAme
{
    [ConfigFile("config", "SETTINGS")]
    internal static class Configs
    {
        //[ConfigName("Beeg Ame Size")]
        //[ConfigComment("The size of Beeg Ame. Can be set to anything between 1 and 100. Default size is 5.")]
        [ConfigCallback("CallSizeChanged")]
        [ConfigRange(1, 100)]
        public static int SIZE = 5;

        private static void CallSizeChanged(object oldValue, object newVal)
        {
            PlayerScript player = SALT.Main.actualPlayer;
            if (player == null)
                return;
            if (player.GetCurrentCharacter() != Main.BEEG)
                return;
            float oldSize = player.size;//(float)(int)oldValue;
            float size = (float)(int)newVal;
            player.size = size;
            player.jumpStrength /= Main.jumpCalc(oldSize);
            player.maxSpeed /= Main.speedCalc(oldSize);
            player.jumpStrength *= Main.jumpCalc(size);
            player.maxSpeed *= Main.speedCalc(size);
        }
    }


    public class Main : ModEntryPoint
    {
        internal static float jumpCalc(float size) => 1f + ((size - 1) / 8f);//1.5f;
        internal static float speedCalc(float size) => 1f + ((size - 1) / 4f);//2f;


        // THE EXECUTING ASSEMBLY
        public static Assembly execAssembly;

        internal static Character BEEG;

        // Called before MainScript.Awake
        // You want to register new things and enum values here, as well as do all your harmony patching
        public override void PreLoad()
        {
            // Gets the Assembly being executed
            execAssembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(execAssembly);
            BEEG = CharacterRegistry.CreateCharacterId("BEEG");
            CharacterRegistry.RegisterCharacterSprite(BEEG, CreateSpriteFromImage("AmeWide.png"));
        }


        // Called before MainScript.Start
        // Used for registering things that require a loaded MainScript
        public override void Load()
        {
            GameObject ameliaPrefab = PrefabUtils.CopyPrefab(CharacterRegistry.GetCharacter(Character.AMELIA));
            ameliaPrefab.name = "beegAmePack";
            ameliaPrefab.GetComponent<CharacterIdentifiable>().Id = BEEG;
            CharacterRegistry.RegisterCharacterPrefab(ameliaPrefab);
            //CharacterRegistry.RegisterCharacterSprite(BEEG, CreateSpriteFromImage("AmeWideSprite.png"));//CharacterRegistry.GetIcon(Character.AMELIA)
        }

        // Called after all mods Load's have been called
        // Used for editing existing assets in the game, not a registry step
        public override void PostLoad()
        {
            Callbacks.OnLevelLoaded += () => {
                if (Levels.isRedHeart())
                    OnRedHeart();
            };
        }

        private void OnRedHeart()
        {
            GameObject WOWHelper = new GameObject("WOWHelper");
            WOWHelper.layer = 15;
            WOWHelper.transform.position = new Vector3(260, 110, 0);
            var box = WOWHelper.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(10, 5);
            WOWHelper.AddComponent<WowTrigger>();
        }

        internal class WowTrigger : MonoBehaviour
        {
            private void OnTriggerEnter2D(Collider2D collision)
            {
                if (collision.gameObject.layer != 16)
                    return;
                PlayerScript player = SALT.Main.actualPlayer;
                if (player == null)
                    return;
                if (player.GetCurrentCharacter() != Main.BEEG)
                    return;
                player.transform.position = player.transform.position.SetY(this.transform.position.y - 10);
            }
        }

        [HarmonyPatch(typeof(PlayerScript))]
        [HarmonyPatch("SpawnCharacter")]
        internal static class PlayerSpawnCharacterPatch
        {
            [HarmonyPriority(Priority.First - 1)]
            public static void Postfix(PlayerScript __instance, int i)
            {
                Character character = EnumUtils.FromInt<Character>(i);
                if (character == Main.BEEG)
                {
                    float size = (float)Configs.SIZE;
                    __instance.size = size;//5f;
                    __instance.jumpStrength *= jumpCalc(size);
                    __instance.maxSpeed *= speedCalc(size);
                    SALT.Main.StopSave();
                }
            }
        }
    }
}