using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RWCustom;
using IteratorKit.CMOracle;
using System.Reflection;

namespace IteratorKit.CustomPearls
{
    public class CustomPearls
    {
        public static List<DataPearl.AbstractDataPearl.DataPearlType> customPearls = new List<DataPearl.AbstractDataPearl.DataPearlType>();
        public static Dictionary<DataPearl.AbstractDataPearl.DataPearlType, DataPearlRelationStore> pearlJsonDict = new Dictionary<DataPearl.AbstractDataPearl.DataPearlType, DataPearlRelationStore>();

        public static void ApplyHooks()
        {
            IteratorKit.Log.LogInfo("Apply data pearl hooks");
            On.DataPearl.ApplyPalette += CustomPearlApplyPalette;
            On.Conversation.DataPearlToConversation += CustomPearlToConversation;
            //On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += SLConversation.CustomPearlAddEvents; // this includes pebbles reading pearls for dumb reasons
        }

        public static void RemoveHooks()
        {
            On.DataPearl.ApplyPalette -= CustomPearlApplyPalette;
            On.Conversation.DataPearlToConversation -= CustomPearlToConversation;
        }

        public static void LoadPearlData(List<DataPearlJson> dataPearls)
        {
            //CustomPearls.dataPearls = dataPearls;
            IteratorKit.Log.LogInfo("Register custom pearls");

            foreach (DataPearlJson dataPearlJson in dataPearls)
            {

                DataPearl.AbstractDataPearl.DataPearlType pearlType = new DataPearl.AbstractDataPearl.DataPearlType(dataPearlJson.pearl, true);
                Conversation.ID conv = new Conversation.ID(dataPearlJson.pearl, true);
                if (customPearls.Contains(pearlType))
                {
                    IteratorKit.Log.LogWarning($"Skipping pearl {dataPearlJson.pearl} as it is already registered");
                    continue;
                }

                customPearls.Add(pearlType);
                pearlJsonDict.Add(pearlType, new DataPearlRelationStore(pearlType, dataPearlJson, conv));
            }
        }

        public static void CustomPearlApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            DataPearl.AbstractDataPearl abstractDataPearl = self.abstractPhysicalObject as DataPearl.AbstractDataPearl;
            if (CustomPearls.customPearls.Contains(abstractDataPearl.dataPearlType))
            {
                if (CustomPearls.pearlJsonDict.TryGetValue(abstractDataPearl.dataPearlType, out DataPearlRelationStore pearlStore))
                {
                    DataPearlJson pearlJson = pearlStore.pearlJson;
                    self.color = pearlJson.color.color;
                    self.highlightColor = pearlJson.highlight.color;
                }
                else
                {
                    IteratorKit.Log.LogError("Failed to apply pallete to pearl as it can not be found.");
                }

            }
            else
            {
                orig(self, sLeaser, rCam, palette);
            }
        }

        private static Conversation.ID CustomPearlToConversation(On.Conversation.orig_DataPearlToConversation orig, DataPearl.AbstractDataPearl.DataPearlType type)
        {
            if (customPearls.Contains(type))
            {
                if (CustomPearls.pearlJsonDict.TryGetValue(type, out DataPearlRelationStore pearlStore))
                {
                    IteratorKit.Log.LogInfo($"Loading pearl convo for {pearlStore.convId}");
                    return pearlStore.convId;
                }
                else
                {
                    IteratorKit.Log.LogError($"Failed to find conversation for pearl of type {type}");
                    return Conversation.ID.None;
                }
            }
            else
            {
                return orig(type);
            }
        }

        public class DataPearlRelationStore
        {
            public DataPearl.AbstractDataPearl.DataPearlType pearlType;
            public DataPearlJson pearlJson;
            public Conversation.ID convId;

            public DataPearlRelationStore(DataPearl.AbstractDataPearl.DataPearlType type, DataPearlJson dataPearlJson, Conversation.ID convId)
            {
                this.pearlType = type;
                this.pearlJson = dataPearlJson;
                this.convId = convId;
            }
        }
    }

}
