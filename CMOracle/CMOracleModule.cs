using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace IteratorKit.CMOracle
{
    public class CMOracleData
    {
        public Oracle owner;
        public OracleJSON oracleJson;
        public CMOracleData(Oracle oracle)
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
        public static CMOracleData GetOracleData(this Oracle oracle) => _cwt.GetValue(oracle, _ => new(oracle));

        public static bool GetOracleData(this Oracle oracle, out CMOracleData oracleData)
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
        public static OracleJSON OracleJson(this Oracle oracle)
        {
            return oracle.GetOracleData().oracleJson;
        }


    }

}
