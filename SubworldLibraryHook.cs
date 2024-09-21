using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PersistentPlayerPosition {
    [ExtendsFromMod("SubworldLibrary")]
    public class SubworldLibraryHook {
        private static Mod SubworldLibrary => ModLoader.GetMod("SubworldLibrary");
        private static Hook beginEnteringHook = null;
        private static Hook exitWorldCallbackHook = null;
        private static FieldInfo currentSubworldField = null;

        private delegate void orig_BeginEntering(int index);
        private delegate void orig_ExitWorldCallback(object index);
        
        private static void OnBeginEntering(orig_BeginEntering orig, int index) {
            if (currentSubworldField != null && currentSubworldField.GetValue(null) == null && ModContent.GetInstance<PPPConfig>().SavePositionWhenEnteringSubworld)
                Main.LocalPlayer.GetModPlayer<PositionSavingPlayer>().UpdateData(null);
            orig(index);
        }

        private static void OnExitWorldCallback(orig_ExitWorldCallback orig, object index) {
            // going to main world
            if ((index == null || (int) index < 0) && ModContent.GetInstance<PPPConfig>().ReturnToPrevPositionWhenExitingSubworld)
                Main.LocalPlayer.GetModPlayer<PositionSavingPlayer>().OnEnterWorld();
            // i have no idea how nice this will play in multiplayer, but fingers crossed it actually works as intended there
            orig(index);
        }

        public static void Load() {
            if (SubworldLibrary != null) {
                Type subworldSystem = null;
                MethodInfo exitWorldCallbackInfo = null;
                MethodInfo beginEnteringInfo = null;

                foreach (Type t in SubworldLibrary.GetType().Assembly.GetTypes())
                    if (t.Name == "SubworldSystem")
                        subworldSystem = t;

                if (subworldSystem != null) {
                    beginEnteringInfo = subworldSystem.GetMethod("BeginEntering", BindingFlags.NonPublic | BindingFlags.Static);
                    exitWorldCallbackInfo = subworldSystem.GetMethod("ExitWorldCallback", BindingFlags.NonPublic | BindingFlags.Static);
                    currentSubworldField = subworldSystem.GetField("current", BindingFlags.NonPublic | BindingFlags.Static);
                }
                if (beginEnteringInfo != null) {
                    beginEnteringHook = new Hook(beginEnteringInfo, OnBeginEntering);
                    beginEnteringHook.Apply();
                }
                if (exitWorldCallbackInfo != null) {
                    exitWorldCallbackHook = new Hook(exitWorldCallbackInfo, OnExitWorldCallback);
                    exitWorldCallbackHook.Apply();
                }
            }
        }

        public static void Unload() {
            beginEnteringHook?.Undo();
            exitWorldCallbackHook?.Undo();
        }
    }
}
