using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;


namespace IteratorKit.CMOracle
{
    /// <summary>
    /// Stores Oracle JSON so it can be mapped to any oracle instead of just CMOracle
    /// </summary>
    public class CMOracleData
    {
        public Oracle owner;
        public OracleJData oracleJson;
        public CMOracleData(Oracle oracle)
        {
            this.owner = oracle;
        }
    }

    /// <summary>
    /// Stores Oracle Events so they can be used for any oracle instead of just CMOracle
    /// </summary>
    public class CMOracleEvents
    {
        public delegate void OnEventStart(CMOracleBehavior cmBehavior, string eventId, Conversation.DialogueEvent dialogueEvent, OracleEventObjectJData eventData);
        public delegate void OnEventEnd(CMOracleBehavior cmBehavior, string eventId);
        public OnEventStart OnCMEventStart;
        public OnEventEnd OnCMEventEnd;
        public Oracle owner;
        public CMOracleEvents(Oracle oracle)
        {
            this.owner = oracle;
        }
    }

    /// <summary>
    /// This is where we store all the ConditionalWeakTables so we can retrieve player info in other code.
    /// Do not use TryGetValue, instead call Player object (self) method GetPlayerData or GetPlayerGraphics
    /// Both of these can be used either directly (no out) or with an out to get a bool result if nothing is retrieved
    /// </summary>
    public static class CMOracleModule
    {
        private static readonly ConditionalWeakTable<Oracle, CMOracleData> _cwt = new();
        public static CMOracleData OracleData(this Oracle oracle) => _cwt.GetValue(oracle, _ => new(oracle));
        public static bool OracleData(this Oracle oracle, out CMOracleData oracleData)
        {
            if (_cwt.TryGetValue(oracle, out CMOracleData oracleDataActual))
            {
                oracleData = oracleDataActual;
                return true;
            }
            else
            {
                oracleData = oracleDataActual;
                _cwt.Add(oracle, oracleData);
                return true;
            }
        }

        /// <summary>
        /// CWT Shortcut for oracle JSON
        /// </summary>
        /// <param name="oracle">this</param>
        /// <returns></returns>
        public static OracleJData OracleJson(this Oracle oracle)
        {
            return oracle.OracleData().oracleJson;
        }

        /// <summary>
        /// CWT storage for event delegates
        /// </summary>
        private static readonly ConditionalWeakTable<Oracle, CMOracleEvents> _eventsCwt = new ConditionalWeakTable<Oracle, CMOracleEvents>();
        public static CMOracleEvents OracleEvents(this Oracle oracle) => _eventsCwt.GetValue(oracle, _ => new CMOracleEvents(oracle));
        public static bool OracleEvents(this Oracle oracle, out CMOracleEvents oracleEvents)
        {
            if (_eventsCwt.TryGetValue(oracle, out CMOracleEvents oracleEventsActual))
            {
                oracleEvents = oracleEventsActual;
                return true;
            }
            else
            {
                oracleEvents = oracleEventsActual;
                _eventsCwt.Add(oracle, oracleEvents);
                return true;
            }
        }



    }

}
