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

namespace FedMaokai
{
    internal class Program
    {
        public const string ChampionName = "Maokai";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

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

            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.R, 625);

            Q.SetSkillshot(0.50f, 110f, 1200f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(1f, 250f, 1500f, false, SkillshotType.SkillshotCircle);

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
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("MinR", "Min Enemys for (R)").SetValue<Slider>(new Slider(2, 1, 5)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ManaR", "Turn off R at % Mana").SetValue(new Slider(30, 100, 0)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Ganks", "Ganks"));
            Config.SubMenu("Ganks").AddItem(new MenuItem("UseQGank", "Use Q").SetValue(true));
            Config.SubMenu("Ganks").AddItem(new MenuItem("UseWGank", "Use W").SetValue(true));
            Config.SubMenu("Ganks").AddItem(new MenuItem("UseEGank", "Use E").SetValue(true));
            Config.SubMenu("Ganks").AddItem(new MenuItem("GanksActive", "Ganks!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));            
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("ManaHarass", "Dont Harass if mana < %").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassToggle", "Use Harass (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Killsteal", "Killsteal"));
            Config.SubMenu("Killsteal").AddItem(new MenuItem("killstealQ", "Use Q-Spell to Killsteal").SetValue(true));
            Config.SubMenu("Killsteal").AddItem(new MenuItem("killstealE", "Use E-Spell to Killsteal").SetValue(true));
            Config.SubMenu("Killsteal").AddItem(new MenuItem("killstealR", "Use R-Spell to Killsteal").SetValue(true));      

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("ManaFarm", "Min Mana").SetValue(new Slider(60, 100, 0)));
            Config.SubMenu("Farm").AddItem(new MenuItem("waveNumQ", "Minions to hit with Q").SetValue<Slider>(new Slider(3, 1, 10)));
            Config.SubMenu("Farm").AddItem(new MenuItem("waveNumE", "Minions to hit with E").SetValue<Slider>(new Slider(4, 1, 10)));            
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoW", "Auto W under Turrets").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("gapClose", "Auto-Knockback Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("stun", "Auto-Interrupt Important Spells").SetValue(true));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after a rotation").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => (float)(DamageLib.getDmg(hero, DamageLib.SpellType.Q) + DamageLib.getDmg(hero, DamageLib.SpellType.W) + DamageLib.getDmg(hero, DamageLib.SpellType.E) + DamageLib.getDmg(hero, DamageLib.SpellType.R));
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));            
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;            
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;            

            Game.PrintChat("<font color=\"#00BFFF\">Fed" + ChampionName + " -</font> <font color=\"#FFFFFF\">Loaded!</font>");

        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item("gapClose").GetValue<bool>()) return;

            if (Player.Distance(gapcloser.Sender) < Q.Range)
            {
                Q.Cast(gapcloser.Sender);
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("stun").GetValue<bool>()) return;  
          
            if (unit.IsValidTarget(600f) && (spell.DangerLevel == InterruptableDangerLevel.High || spell.DangerLevel == InterruptableDangerLevel.Medium)  && Q.IsReady())
            {
                Q.Cast(unit);                
            }
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

                if (Config.Item("GanksActive").GetValue<KeyBind>().Active)
                    Ganks();

                if (Config.Item("harassToggle").GetValue<KeyBind>().Active)
                    ToggleHarass();                

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }  
        }

        private static void AutoUlt()
        {
            //aqui
        }

        private static void Combo()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (wTarget != null && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                W.Cast(wTarget);
            }
            if (qTarget != null && Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(qTarget);
            }
            if (eTarget != null && Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }

        private static void Ganks()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (wTarget != null && Config.Item("UseWGank").GetValue<bool>() && W.IsReady())
            {
                W.Cast(wTarget);
            }
            if (eTarget != null && Config.Item("UseEGank").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget);
            }
            if (qTarget != null && Config.Item("UseQGank").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(qTarget);
            }
        }       

        private static void Harass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (qTarget != null && Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(qTarget);
            }
            if (eTarget != null && Config.Item("UseEHarass").GetValue<bool>() && E.IsReady())
            {
                    E.Cast(eTarget);                
            }
        }

        private static void ToggleHarass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (qTarget != null && Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(qTarget);
            }
            if (eTarget != null && Config.Item("UseEHarass").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width +30, MinionTypes.All);
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
                W.Cast(mob);
                Q.Cast(mob);
                E.Cast(mob);
            }
        }
    }
}
