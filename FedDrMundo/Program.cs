#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace FedDrMundo
{
    internal class Program
    {
        public const string ChampionName = "DrMundo";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static bool WActive = false;

        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 1000);
            W = new Spell(SpellSlot.W, Player.AttackRange + 25);
            E = new Spell(SpellSlot.E, Player.AttackRange + 25);
            R = new Spell(SpellSlot.R, Player.AttackRange + 25);

            Q.SetSkillshot(0.50f, 75f, 1500f, true, SkillshotType.SkillshotLine);            

            SpellList.AddRange(new[] { Q, W, E, R });

            Config = new Menu("Fed" + ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));            
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));            

            Config.AddSubMenu(new Menu("Harass", "Harass"));            
            Config.SubMenu("Harass").AddItem(new MenuItem("LifeHarass", "Dont Harass if HP < %").SetValue(new Slider(30, 100, 0)));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassToggle", "Use Harass (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));            

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E Farm").SetValue(true));            
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("KS", "Killsteal using Q").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("RangeQ", "Q Range Slider").SetValue(new Slider(1000, 1000, 0)));
            Config.SubMenu("Misc").AddItem(new MenuItem("lifesave", "Life saving Ultimate").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("percenthp", "Life Saving Ult %").SetValue(new Slider(20, 100, 0)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));            
            
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;            

            Game.PrintChat("<font color=\"#00BFFF\">Fed" + ChampionName + " -</font> <font color=\"#FFFFFF\">Loaded!</font>");

        }        

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            
            if (Player.HasBuff("BurningAgony"))
            {
                WActive = true;
            }
            else
            {
                WActive = false;
            }

            if (WActive && W.IsReady())
            {
                int inimigos = Utility.CountEnemysInRange(600);

                if (!Config.Item("LaneClearActive").GetValue<KeyBind>().Active && !Config.Item("JungleFarmActive").GetValue<KeyBind>().Active && inimigos == 0)
                {
                    W.Cast();
                }
            }
               

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();                

                if (Config.Item("harassToggle").GetValue<KeyBind>().Active)
                    ToggleHarass();

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    //LaneClear();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }

            if (Config.Item("lifesave").GetValue<bool>())
                LifeSave();
        }

        private static void LifeSave()
        {
            int inimigos = Utility.CountEnemysInRange(900);

            if (Player.Health < (Player.MaxHealth * Config.Item("percenthp").GetValue<Slider>().Value * 0.01) && R.IsReady() && inimigos >= 1)
            {
                R.Cast();                
            }
        }
        
        private static void Combo()
        {
            int qRange = Config.Item("RangeQ").GetValue<Slider>().Value;

            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (qTarget != null && Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady() && Player.Distance(qTarget) <= qRange)
            {
                    PredictionOutput qPred = Q.GetPrediction(qTarget);
                    if (qPred.Hitchance >= HitChance.High)
                        Q.Cast(qPred.CastPosition);
            }
            if (!WActive && qTarget != null && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady() && Player.Distance(qTarget) <= qRange)
            {
                W.Cast();
            }
            if (eTarget != null && Config.Item("UseECombo").GetValue<bool>() && E.IsReady() && Player.Distance(qTarget) <= qRange)
            {
                E.Cast();                
            }            
        }        

        private static void Harass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);

            int qRange = Config.Item("RangeQ").GetValue<Slider>().Value;

            var RLife = Config.Item("LifeHarass").GetValue<Slider>().Value;
            var LPercentR = Player.Health * 100 / Player.MaxHealth;

            if (qTarget != null && Q.IsReady() && LPercentR >= RLife && Player.Distance(qTarget) <= qRange)
            {
                PredictionOutput qPred = Q.GetPrediction(qTarget);
                if (qPred.Hitchance >= HitChance.High)
                    Q.Cast(qPred.CastPosition);
            }            
        }

        private static void ToggleHarass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);

            int qRange = Config.Item("RangeQ").GetValue<Slider>().Value;

            var RLife = Config.Item("LifeHarass").GetValue<Slider>().Value;
            var LPercentR = Player.Health * 100 / Player.MaxHealth;

            if (qTarget != null && Q.IsReady() && LPercentR >= RLife && Player.Distance(qTarget) <= qRange)
            {
                PredictionOutput qPred = Q.GetPrediction(qTarget);
                if (qPred.Hitchance >= HitChance.High)
                    Q.Cast(qPred.CastPosition);
            }            
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width + 30, MinionTypes.All);
            var allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range + E.Width + 30, MinionTypes.All);

            var FMana = Config.Item("ManaFarm").GetValue<Slider>().Value;
            var MPercent = Player.Mana * 100 / Player.MaxMana;

            var fle = E.GetCircularFarmLocation(allMinionsE, E.Width);
            var flq = Q.GetLineFarmLocation(allMinionsQ, Q.Width);

            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && flq.MinionsHit >= Config.Item("waveNumQ").GetValue<Slider>().Value && flq.MinionsHit >= 2 && MPercent >= FMana)
            {
                Q.Cast(flq.Position);
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && fle.MinionsHit >= Config.Item("waveNumE").GetValue<Slider>().Value && fle.MinionsHit >= 3 && MPercent >= FMana)
            {
                E.Cast(fle.Position);
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (!WActive && W.IsReady())
                {
                    W.Cast();
                }
                if (Q.IsReady() && mobs[0].IsValidTarget() && Player.Distance(mobs[0]) <= Q.Range)
                {
                    Q.Cast(mobs[0].Position);
                }
                E.Cast();
            }
        }

    }
}
