﻿namespace SlugBase.SaveData
{
    internal static class SaveDataHooks
    {
        public static void Apply()
        {
            On.Menu.SlugcatSelectMenu.MineForSaveData += SlugcatSelectMenu_MineForSaveData;
            On.DeathPersistentSaveData.SaveToString += DeathPersistentSaveData_SaveToString;
            On.MiscWorldSaveData.ToString += MiscWorldSaveData_ToString;
            On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString;
        }

        public static void UnApply()
        {
            On.Menu.SlugcatSelectMenu.MineForSaveData -= SlugcatSelectMenu_MineForSaveData;
            On.DeathPersistentSaveData.SaveToString -= DeathPersistentSaveData_SaveToString;
            On.MiscWorldSaveData.ToString -= MiscWorldSaveData_ToString;
            On.PlayerProgression.MiscProgressionData.ToString -= MiscProgressionData_ToString;
        }

        // Mine for SlugBase save data
        private static Menu.SlugcatSelectMenu.SaveGameData SlugcatSelectMenu_MineForSaveData(On.Menu.SlugcatSelectMenu.orig_MineForSaveData orig, ProcessManager manager, SlugcatStats.Name slugcat)
        {
            var origData = orig(manager, slugcat);

            if(origData != null)
                MinedSaveData.Data.Add(origData, new MinedSaveData(manager.rainWorld, slugcat));

            return origData;
        }

        private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
        {
            self.GetSlugBaseData().SaveToStrings(self.unrecognizedSaveStrings);
            return orig(self);
        }

        private static string MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
        {
            self.GetSlugBaseData().SaveToStrings(self.unrecognizedSaveStrings);
            return orig(self);
        }

        private static string DeathPersistentSaveData_SaveToString(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            self.GetSlugBaseData().SaveToStrings(self.unrecognizedSaveStrings);
            return orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
        }
    }
}