using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RWCustom;
using SlugBase.SaveData;
using UnityEngine;

namespace IteratorKit.Util
{
    public static class ITKUtil
    {
        
        /// <summary>
        /// Does the reverse of Room.GetTilePosition()
        /// </summary>
        /// <param name="pos">Tile Vector2</param>
        /// <returns>Global World Vector2</returns>
        public static Vector2 GetWorldFromTile(Vector2 pos)
        {
            return new Vector2(((pos.x + 1) * 20) - 20, ((pos.y + 1) * 20) - 20);
        }

        /// <summary>
        /// Returns shortcut pointing to a specific room by name
        /// </summary>
        /// <param name="currentRoom">This room</param>
        /// <param name="targetRoomId">Target Room</param>
        /// <returns>Shortcut</returns>
        public static ShortcutData? GetShortcutToRoom(Room currentRoom, string targetRoomId)
        {
            foreach (ShortcutData shortcut in currentRoom.shortcuts)
            {
                IntVector2 destTile = shortcut.connection.DestTile;
                AbstractRoom destRoom = currentRoom.WhichRoomDoesThisExitLeadTo(destTile);
                if (destRoom != null)
                {
                    if (destRoom.name == targetRoomId)
                    {
                        return shortcut;
                    }
                }
            }
            IteratorKit.Log.LogInfo($"Failed to find shortcut with room to {targetRoomId} from {currentRoom.roomSettings?.name}");
            return null;
        }

        /// <summary>
        /// Retrieves save data for a specific oracle
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="session">Story Session to save to</param>
        /// <param name="oracleId">Oracle to save this data for</param>
        /// <param name="key">Unique Key</param>
        /// <param name="defaultValue">Default value if no save data exists</param>
        /// <returns>Save data value</returns>
        public static T GetSaveDataValue<T>(StoryGameSession session, Oracle.OracleID oracleId, string key, T defaultValue)
        {
            SlugBaseSaveData saveData = SaveDataExtension.GetSlugBaseData(session.saveState.miscWorldSaveData);
            if (saveData.TryGet($"{oracleId}_{key}", out T saveDataValue))
            {
                return saveDataValue;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Stores save data for a specific oracle
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="session">Story Session to save to</param>
        /// <param name="oracleId">Oracle to save this data for</param>
        /// <param name="key">Unique Key</param>
        /// <param name="value">Value to set</param>
        public static void SetSaveDataValue<T>(StoryGameSession session, Oracle.OracleID oracleId, string key, T value)
        {
            SlugBaseSaveData saveData = SaveDataExtension.GetSlugBaseData(session.saveState.miscWorldSaveData);
            saveData.Set($"{oracleId}_{key}", value);
        }

        public static void LogAllSpritesAndShaders(RainWorld rainWorld)
        {
            string sprites = String.Join(", ", Futile.atlasManager._allElementsByName.Keys);
            string shaders = String.Join(", ", rainWorld.Shaders.Keys);
            IteratorKit.Log.LogInfo($"Sprites: {sprites}\nShaders: {shaders}");
        }

        public static Vector2 DeepCopy(this  Vector2 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static Vector2[] DeepCopy(this Vector2[] vectors)
        {
            Vector2[] results = new Vector2[vectors.Length];
            for(int i = 0; i < vectors.Length; i++)
            {
                results[i] = vectors[i].DeepCopy();
            }
            return results;
        }

        public static Vector2[,] DeepCopy(this Vector2[,] vectors)
        {
            Vector2[,] results = new Vector2[vectors.GetLength(0), vectors.GetLength(1)];
            for (int i = 0; i  < vectors.GetLength(0); i++)
            {
                for (int j = 0; j < vectors.GetLength(1); j++)
                {
                    results[i, j] = vectors[i, j].DeepCopy();
                }
            }
            return results;
        }

        //public static T[,] DeepCopy<T>(this T[,] source) where T : Vector2, ICloneable
        //{
        //    T[,] newMask = new T[source.GetLength(0), source.GetLength(1)];
        //    for (int i = 0; i < newMask.GetLength(0); i++)
        //    {
        //        for (int j = 0; j < newMask.GetLength(1); i++)
        //        {
        //            newMask[i, j] = (T)source[i, j].Clone();
        //        }
        //    }
        //    return newMask;

        //}
    }
    public class ITKMultiValueDictionary<Key, Value> : Dictionary<Key, List<Value>>
    {
        public void Add(Key key, Value value)
        {
            List<Value> values;
            if (!this.TryGetValue(key, out values))
            {
                values = new List<Value>();
                this.Add(key, values);
            }
            values.Add(value);
        }

        public List<Value> AllValues(){
            return this.Values.SelectMany(x => x).ToList();
        }
    }
}
