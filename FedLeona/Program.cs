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

namespace FedLeona
{
    internal class Program
    {
        public const string ChampionName = "Leona";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        private static SpellSlot ExaustSlot;

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

            Q = new Spell(SpellSlot.Q, Player.AttackRange + 25);
            W = new Spell(SpellSlot.W, 275f);
            E = new Spell(SpellSlot.E, 850f);
            R = new Spell(SpellSlot.R, 1200f);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            ExaustSlot = Player.GetSpellSlot("SummonerExaust");
            
            E.SetSkillshot(0.25f, 85f, 2000f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.625f, 315f, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("ManaHarass", "Dont Harass if mana < %").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassToggle", "Use Harass (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("laugh", "Troll laugh?").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoI", "Auto Ignite").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoEx", "Auto Exaust").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoUnderT", "Combo Under MyTower").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("gapClose", "Auto-Knockback Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("stun", "Auto-Interrupt Important Spells").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoR", "Auto R when: ").SetValue(new StringList(new[] { "No", "Target Stuned", "Min Enemys", "Both" }, 3)));            
            Config.SubMenu("Misc").AddItem(new MenuItem("MinR", "Min Enemys for (R)").SetValue<Slider>(new Slider(3, 1, 5)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

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

                if (Config.Item("AutoUnderT").GetValue<bool>())
                    AutoUnderTower();                

                if (Config.Item("AutoI").GetValue<bool>())
                    AutoIgnite();

                if (Config.Item("AutoEx").GetValue<bool>())
                    AutoIgnite();

                if (Config.Item("AutoR").GetValue<StringList>().SelectedIndex > 0)
                    AutoUlt();
            }
        }

        private static void AutoIgnite()
        {
            var iTarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.True);
            var Idamage = ObjectManager.Player.GetSummonerSpellDamage(iTarget, Damage.SummonerSpell.Ignite) * 0.9;

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && iTarget.Health < Idamage)
            {
                Player.SummonerSpellbook.CastSpell(IgniteSlot, iTarget);
                if (Config.Item("laugh").GetValue<bool>())
                {
                    Game.Say("/l");
                }
            }
        }       

        private static void AutoUlt()
        {
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            var Rmode = Config.Item("AutoR").GetValue<StringList>().SelectedIndex;

            if (R.IsReady() && rTarget != null)
            {
                if (Rmode != 0 && Rmode != 1)
                {
                    var Rmin = Config.Item("MinR").GetValue<Slider>().Value;

                    R.CastIfWillHit(rTarget, Rmin);
                }

                if (Rmode != 0 && Rmode != 2)
                {
                    PredictionOutput rPred = R.GetPrediction(rTarget);
                    if (rPred.Hitchance == HitChance.Immobile)
                        R.Cast(rPred.CastPosition);
                }
            }

        }

        private static void AutoUnderTower()
        {
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);

            if (Utility.UnderTurret(wTarget, false) && W.IsReady())
            {
                W.Cast(wTarget);
                if (Config.Item("laugh").GetValue<bool>())
                {
                    Game.Say("/l");
                }
            }
        }

        private static void Combo()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
                        
            if (Config.Item("UseWCombo").GetValue<bool>() && (W.IsReady() && wTarget != null || eTarget != null && W.IsReady() && E.IsReady()))
            {
                W.Cast();
            }
            if (eTarget != null && Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                PredictionOutput ePred = E.GetPrediction(eTarget);
                if (ePred.Hitchance >= HitChance.High)
                    E.Cast(eTarget);
            }
            if (qTarget != null && Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast();
            }            
        }        

        private static void Harass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (eTarget != null && Config.Item("UseWHarass").GetValue<bool>() && W.IsReady() && E.IsReady())
            {                
                    W.Cast();
            }
            if (eTarget != null && Config.Item("UseEHarass").GetValue<bool>() && E.IsReady())
            {
                PredictionOutput ePred = E.GetPrediction(eTarget);
                if (ePred.Hitchance >= HitChance.Medium)
                E.Cast(eTarget);
            }
            if (qTarget != null && Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast();
            }
        }

        private static void ToggleHarass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (qTarget != null && Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
            {
                if (!qTarget.IsVisible)
                    Q.Cast(qTarget);
            }
            if (eTarget != null && Config.Item("UseEHarass").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }        

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item("gapClose").GetValue<bool>()) return;

            if (gapcloser.Sender.IsValidTarget(400f))
            {
                Q.Cast(gapcloser.Sender);
            }
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("stun").GetValue<bool>()) return;

            if (unit.IsValidTarget(600f) && (spell.DangerLevel != InterruptableDangerLevel.Low) && Q.IsReady())
            {
                Q.Cast(unit);
                if (Q.IsReady() && unit.IsValidTarget(W.Range))
                {
                    W.Cast(unit);
                }
                if (Config.Item("laugh").GetValue<bool>())
                {
                    Game.Say("/l");
                }
            }
        }
    }
}
