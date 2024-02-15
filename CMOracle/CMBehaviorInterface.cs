using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using HUD;

namespace IteratorKit.CMOracle
{
    public interface CMBehaviorInterface
    {
        public void Move();
        //public new float CommunicatePosScore(Vector2 tryPos);
        //public new float BasePosScore(Vector2 tryPos);
       // public void SetNewDestination(Vector2 dst);
      //  public Vector2 ClampToRoom(Vector2 vector);
       // public Vector2 RandomRoomPoint();
        public void NewAction(string nextAction, string actionParam);
        public void CheckConversationEvents();
        public void ResumeConversation();
        public void StartItemConversation(PhysicalObject item);
        public bool HasHadMainPlayerConversation();
        public void SetHasHadMainPlayerConversation(bool hasHadPlayerConversation);
        public void ChangePlayerScore(string operation, int amount);
        
        public void CheckActions();
        public void ReactToHitByWeapon(Weapon weapon);
        public void ShowScreens(List<OracleJSON.OracleEventsJson.OracleScreenJson> screens);
        public void ShowScreenImages();

    }
}
