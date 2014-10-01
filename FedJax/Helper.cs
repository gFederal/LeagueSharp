using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace FedJax
{
    class EnemyInfo
    {
        public Obj_AI_Hero Player;
        public int LastSeen;        

        //public RecallInfo RecallInfo;

        public EnemyInfo(Obj_AI_Hero player)
        {
            Player = player;
        }
    }

    class Helper
    {
        public IEnumerable<Obj_AI_Hero> EnemyTeam;
        public IEnumerable<Obj_AI_Hero> OwnTeam;
        public List<EnemyInfo> EnemyInfo = new List<EnemyInfo>();

        public Helper()
        {
            var champions = ObjectManager.Get<Obj_AI_Hero>().ToList();

            OwnTeam = champions.Where(x => x.IsAlly);
            EnemyTeam = champions.Where(x => x.IsEnemy);

            EnemyInfo = EnemyTeam.Select(x => new EnemyInfo(x)).ToList();

            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            var time = Environment.TickCount;

            foreach (EnemyInfo enemyInfo in EnemyInfo.Where(x => x.Player.IsVisible))
                enemyInfo.LastSeen = time;
        }  
        
    }
}
