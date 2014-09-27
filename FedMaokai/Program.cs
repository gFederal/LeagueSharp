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

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;

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

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");

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
            Config.SubMenu("Combo").AddItem(new MenuItem("MinR", "Min Enemys for (R)").SetValue<Slider>(new Slider(3, 1, 5)));
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
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "Auto Smite!").SetValue<KeyBind>(new KeyBind('J', KeyBindType.Toggle)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("laugh", "Troll laugh?").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoI", "Auto Ignite").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoW", "Auto W under Turrets").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("gapClose", "Auto-Knockback Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("stun", "Auto-Interrupt Important Spells").SetValue(true));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after a rotation").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => (float)(ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.W) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R));
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

                if (Config.Item("GanksActive").GetValue<KeyBind>().Active)
                    Ganks();

                if (Config.Item("harassToggle").GetValue<KeyBind>().Active)
                    ToggleHarass();                

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();

                if (Config.Item("AutoW").GetValue<bool>())
                    AutoUnderTower();

                if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
                    AutoSmite();

                if (Config.Item("AutoI").GetValue<bool>())
                    AutoIgnite();  
            }  
        }

        private static void AutoIgnite()
        {
            var iTarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.True);
            var Idamage = ObjectManager.Player.GetSummonerSpellDamage(iTarget, Damage.SummonerSpell.Ignite) * 0.90;

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && iTarget.Health < Idamage)
            {
                Player.SummonerSpellbook.CastSpell(IgniteSlot, iTarget);
                if (Config.Item("laugh").GetValue<bool>())
                {
                    Game.Say("/l");
                }
            }
        }

        private static void AutoSmite()
        {
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                float[] SmiteDmg = { 20 * Player.Level + 370, 30 * Player.Level + 330, 40 * Player.Level + 240, 50 * Player.Level + 100 };
                string[] MonsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
                var vMinions = MinionManager.GetMinions(Player.ServerPosition, Player.SummonerSpellbook.Spells.FirstOrDefault(
                    spell => spell.Name.Contains("smite")).SData.CastRange[0], MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    if (vMinion != null
                        && !vMinion.IsDead
                        && !Player.IsDead
                        && !Player.IsStunned
                        && SmiteSlot != SpellSlot.Unknown
                        && Player.SummonerSpellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                    {
                        if ((vMinion.Health < SmiteDmg.Max()) && (MonsterNames.Any(name => vMinion.BaseSkinName.StartsWith(name))))
                        {
                            Player.SummonerSpellbook.CastSpell(SmiteSlot, vMinion);

                            if (Config.Item("laugh").GetValue<bool>())
                            {
                                Game.Say("/l");
                            }

                        }
                    }
                }
            }
        }
        
        private static void AutoUlt()
        {
            int inimigos = Utility.CountEnemysInRange(650);

            var RMana = Config.Item("ManaR").GetValue<Slider>().Value;
            var MPercentR = Player.Mana * 100 / Player.MaxMana;

            if (Config.Item("MinR").GetValue<Slider>().Value <= inimigos && MPercentR >= RMana)
            {
                R.Cast();
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
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (wTarget != null && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                    W.Cast(wTarget);
            }
            if (qTarget != null && Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
            {
                if (!qTarget.IsVisible)
                Q.Cast(qTarget);
            }
            if (eTarget != null && Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget);
            }
            if (Config.Item("UseRCombo").GetValue<bool>() && R.IsReady())
            {
                AutoUlt();
            }
        }

        private static void Ganks()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range + E.Width, SimpleTs.DamageType.Magical);

            if (qTarget != null && Config.Item("UseQGank").GetValue<bool>() && Q.IsReady())
            {
                Q.Cast(qTarget);
            }
            if (wTarget != null && Config.Item("UseWGank").GetValue<bool>() && W.IsReady())
            {
                W.Cast(wTarget);
            }
            if (eTarget != null && Config.Item("UseEGank").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget);
            }           
        }       

        private static void Harass()
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
