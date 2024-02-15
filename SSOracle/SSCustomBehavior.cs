using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.CMOracle;
using UnityEngine;

namespace IteratorKit.SSOracle
{

    /// <summary>
    /// Acts as a bridge between SSOracleBehavior and CMOracleBehavior
    /// Possibly some of the most cursed seeming code I've written. Whats worse is how well it works.
    /// We need this since rainworld will crash if we try to use CMOracleBehavior in place of SSOracleBehavior since it tries to convert this.oracleBehavior into SSOracleBehavior
    /// </summary>
    public class SSCustomBehavior : SSOracleBehavior, CMOracle.CMBehaviorInterface
    {
        CMOracleBehavior cmBehavior;
        public SSCustomBehavior(Oracle oracle) : base(oracle)
        {
            cmBehavior = new CMOracleBehavior(oracle);
        }

        public override void Update(bool eu)
        {
            cmBehavior.Update(eu);
        }

        public Vector2 ClampToRoom(Vector2 vector) { return cmBehavior.ClampToRoom(vector); }
        public Vector2 RandomRoomPoint() { return cmBehavior.RandomRoomPoint(); }

        public void NewAction(SSOracleBehavior.Action nextAction)
        {
            return; // block attemps to call this function
        }
        public void NewAction(string nextAction, string actionParam) { cmBehavior.NewAction(nextAction, actionParam); }
        public void CheckConversationEvents() { cmBehavior.CheckConversationEvents(); }
        public void ResumeConversation() { cmBehavior.ResumeConversation(); }
        public void StartItemConversation(PhysicalObject item) { cmBehavior.StartItemConversation(item); }
        public bool HasHadMainPlayerConversation() { return cmBehavior.HasHadMainPlayerConversation(); }
        public void SetHasHadMainPlayerConversation(bool hasHadPlayerConversation) { cmBehavior.SetHasHadMainPlayerConversation(hasHadPlayerConversation); }
        public void ChangePlayerScore(string operation, int amount) { cmBehavior.ChangePlayerScore(operation, amount); }

        public void CheckActions() { cmBehavior.CheckActions(); }
        public void ReactToHitByWeapon(Weapon weapon) { cmBehavior.ReactToHitByWeapon(weapon); }
        public void ShowScreens(List<OracleJSON.OracleEventsJson.OracleScreenJson> screens) { cmBehavior.ShowScreens(screens); }
        public void ShowScreenImages() { cmBehavior.ShowScreenImages(); }


    }
}
