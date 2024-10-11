using MonoMod.RuntimeDetour;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace PersistentPlayerPosition {
    [ExtendsFromMod("SubworldLibrary")]
    public class SubworldLibraryHook {
        private static Mod SubworldLibrary => ModLoader.GetMod("SubworldLibrary");
        private static Hook beginEnteringHook = null;
        private static Hook exitWorldCallbackHook = null;
        private static FieldInfo currentSubworldField = typeof(SubworldSystem).GetField("current", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo subworldsField = typeof(SubworldSystem).GetField("subworlds", BindingFlags.NonPublic | BindingFlags.Static);

        private delegate void orig_BeginEntering(int index);
        private delegate void orig_ExitWorldCallback(object index);
        
        private static void OnBeginEntering(orig_BeginEntering orig, int index) {
            bool bl = currentSubworldField != null;
            Subworld sub = bl ? currentSubworldField.GetValue(null) as Subworld : null;
            if (bl && sub == null && ModContent.GetInstance<PPPConfig>().SavePositionWhenEnteringSubworld)
                Main.LocalPlayer.GetModPlayer<PositionSavingPlayer>().UpdateData();
            else if (bl && sub != null && ModContent.GetInstance<PPPConfig>().SavePositionInSubworlds && (sub.ShouldSave || ModContent.GetInstance<PPPConfig>().SavePositionInNonPersistentSubworlds)) {
                Main.LocalPlayer.GetModPlayer<PositionSavingPlayer>().UpdateSubworldData(Main.ActiveWorldFileData.UniqueId + ":" + sub.FileName);
            }
            orig(index);
        }

        // scuffed impl attempt at saving position in subworls
        // does not work
        // goodbye
        private static void OnExitWorldCallback(orig_ExitWorldCallback orig, object index) {
            // going to main world
            if ((index == null || (int) index < 0) && ModContent.GetInstance<PPPConfig>().ReturnToPrevPositionWhenExitingSubworld)
                Main.LocalPlayer.GetModPlayer<PositionSavingPlayer>().OnEnterWorld();
            if (index != null && (int) index > 0 && ModContent.GetInstance<PPPConfig>().SavePositionInSubworlds) {
                Subworld sub = (subworldsField.GetValue(null) as List<Subworld>)[(int) index];
                ModLoader.GetMod("PersistentPlayerPosition").Logger.Info("ENTERED SUBWORLD. LOADING...");
                Main.LocalPlayer.GetModPlayer<PositionSavingPlayer>().OnEnterSubworld(Main.ActiveWorldFileData.UniqueId + ":" + sub.FileName);
            }
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
                    //currentSubworldField = subworldSystem.GetField("current", BindingFlags.NonPublic | BindingFlags.Static);
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
