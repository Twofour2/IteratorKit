using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorMod.SLOracle;
using MoreSlugcats;
using RWCustom;
namespace IteratorMod.CustomPearls
{
    public class CustomPearls
    {
        // public static List<DataPearlJson> dataPearlsJson;
        public static List<DataPearl.AbstractDataPearl.DataPearlType> customPearls = new List<DataPearl.AbstractDataPearl.DataPearlType>();
        public static Dictionary<DataPearl.AbstractDataPearl.DataPearlType, DataPearlRelationStore> pearlJsonDict = new Dictionary<DataPearl.AbstractDataPearl.DataPearlType, DataPearlRelationStore>();

        public static void ApplyHooks()
        {
            IteratorKit.Logger.LogWarning("Apply data pearl hooks");
            On.DataPearl.ApplyPalette += CustomPearlApplyPalette;
            On.Conversation.DataPearlToConversation += CustomPearlToConversation;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += SLConversation.CustomPearlAddEvents; // this includes pebbles reading pearls for dumb reasons
        }

        public static void LoadPearlData(List<DataPearlJson> dataPearls)
        {
            //CustomPearls.dataPearls = dataPearls;
            IteratorKit.Logger.LogWarning("Register custom pearls");
            customPearls.Clear();
            pearlJsonDict.Clear();

            foreach (DataPearlJson dataPearlJson in dataPearls)
            {
                DataPearl.AbstractDataPearl.DataPearlType pearlType = new DataPearl.AbstractDataPearl.DataPearlType(dataPearlJson.pearl, true);
                Conversation.ID conv = new Conversation.ID(dataPearlJson.pearl, true);

                customPearls.Add(pearlType);
                pearlJsonDict.Add(pearlType, new DataPearlRelationStore(pearlType, dataPearlJson, conv));
            }
        }

        private static void CustomPearlApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
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
                    IteratorKit.Logger.LogError("Failed to apply pallete to pearl as it can not be found.");
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
                    IteratorKit.Logger.LogWarning($"Loading pearl convo for {pearlStore.convId}");
                    return pearlStore.convId;
                }
                else
                {
                    IteratorKit.Logger.LogError($"Failed to find conversation for pearl of type {type}");
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

            public DataPearlRelationStore(DataPearl.AbstractDataPearl.DataPearlType type, DataPearlJson dataPearlJson, Conversation.ID convId) {
                this.pearlType = type;
                this.pearlJson = dataPearlJson;
                this.convId = convId;
            }
        }
    }
}
