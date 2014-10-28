#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace FedCaitlyn
{
    internal class Program
    {
        public const string ChampionName = "Caitlyn";
        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;
        public static Vector2 PingLocation;
        public static int LastPingT = 0;
        public static int EQComboT = 0;

        public static Menu Config;
        private static Obj_AI_Hero Player;

        const float _spellQSpeed = 2500;
        const float _spellQSpeedMin = 400;

        public static Geometrys.Rectangle rect;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.5f, 90f, 2200f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 2000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);

            SpellList.AddRange(new[] { Q, W, E, R });

            Config = new Menu("Fed" + ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Piltover", "Piltover"));
            Config.SubMenu("Piltover").AddItem(new MenuItem("QMin", "Q Only out of range AA").SetValue(true));
            Config.SubMenu("Piltover").AddItem(new MenuItem("UseQ", "Use Q Mode: ").SetValue(new StringList(new[] { "Combo", "Harass", "Both", "No" }, 1)));
            Config.SubMenu("Piltover").AddItem(new MenuItem("KillQ", "Auto Q Kill").SetValue(true));            
            Config.SubMenu("Piltover").AddItem(new MenuItem("autoccQ", "AutoPeacemaker on CC").SetValue(true));
            Config.SubMenu("Piltover").AddItem(new MenuItem("autoQMT", "Auto Q Multi Target").SetValue(true));
            Config.SubMenu("Piltover").AddItem(new MenuItem("minAutoQMT", "Min. Targest").SetValue(new Slider(3, 2, 5)));
            Config.SubMenu("Piltover").AddItem(new MenuItem("UseQFarm", "Use Q LaneClear").SetValue(true));
            Config.SubMenu("Piltover").AddItem(new MenuItem("minMana", "Harass/Farm Mana %").SetValue(new Slider(40, 100, 0)));

            Config.AddSubMenu(new Menu("Trap", "Trap"));
            Config.SubMenu("Trap").AddItem(new MenuItem("autoccW", "AutoTrap on CC").SetValue(true));
            Config.SubMenu("Trap").AddItem(new MenuItem("autotpW", "AutoTrap on TP").SetValue(true));
            Config.SubMenu("Trap").AddItem(new MenuItem("autoRevW", "AutoTrap on Revive").SetValue(true));
            Config.SubMenu("Trap").AddItem(new MenuItem("AGCtrap", "AntiGapClose with W").SetValue(true));
            Config.SubMenu("Trap").AddItem(new MenuItem("casttrap", "Trap on Closest Enemy - ToDo").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("90 Caliber", "90 Caliber"));
            Config.SubMenu("90 Caliber").AddItem(new MenuItem("AGConoff", "AntiGapClose with E").SetValue(true));
            Config.SubMenu("90 Caliber").AddItem(new MenuItem("KillEQ", "Auto E-Q Kill").SetValue(true));
            Config.SubMenu("90 Caliber").AddItem(new MenuItem("UseEQC", "Use E-Q Combo").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("90 Caliber").AddItem(new MenuItem("PeelE", "Use E defensively").SetValue(true));
            Config.SubMenu("90 Caliber").AddItem(new MenuItem("JumpE", "Jump to Mouse").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Ace Hole", "Ace Hole"));
            Config.SubMenu("Ace Hole").AddItem(new MenuItem("rKill", "R Killshot").SetValue(new KeyBind("R".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Ace Hole").AddItem(new MenuItem("AutoRKill", "Auto R Kill").SetValue(true));
            Config.SubMenu("Ace Hole").AddItem(new MenuItem("pingkillable", "Ping Killable Heroes").SetValue(true));

            Config.AddSubMenu(new Menu("Drawing", "Drawing"));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(new Circle(true, Color.FromArgb(150, Color.DodgerBlue))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(new Circle(true, Color.FromArgb(150, Color.DodgerBlue))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(new Circle(true, Color.FromArgb(150, Color.DodgerBlue))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(new Circle(true, Color.FromArgb(150, Color.DodgerBlue))));
            Config.SubMenu("Drawing").AddItem(new MenuItem("DrawRRangeM", "Draw R Range (Minimap)").SetValue(new Circle(true, Color.FromArgb(150, Color.DodgerBlue))));
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            GameObject.OnCreate += Trap_OnCreate;

            Game.PrintChat("<font color=\"#00BFFF\">Fed" + ChampionName + " -</font> <font color=\"#FFFFFF\">Loaded!</font>");

        }

        private static void Game_OnGameUpdate(EventArgs args)
        { 
            if (ObjectManager.Player.IsDead) return;

            var Qmode = Config.Item("UseQ").GetValue<StringList>().SelectedIndex;

            switch (Program.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (Qmode == 0 || Qmode == 2)
                        Cast_BasicLineSkillshot_Enemy(Q);
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if ((Qmode == 1 || Qmode == 2) && GetManaPercent() >= Config.Item("minMana").GetValue<Slider>().Value)
                        Cast_BasicLineSkillshot_Enemy(Q);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    if (Config.Item("UseQFarm").GetValue<bool>() && GetManaPercent() >= Config.Item("minMana").GetValue<Slider>().Value)
                        Cast_BasicLineSkillshot_AOE_Farm(Q);
                    break;
            }
            
            if (Config.Item("rKill").GetValue<KeyBind>().Active || Config.Item("AutoRKill").GetValue<bool>())
            {
                AutoRKill();
            }

            if (Config.Item("autoccW").GetValue<bool>() || Config.Item("autoccQ").GetValue<bool>())
            {
                AutoCC();
            }            

            if (Config.Item("PeelE").GetValue<bool>())
            {
                PeelE();
            }

            if (Config.Item("JumpE").GetValue<KeyBind>().Active)
            {
                JumptoMouse();
            }

            if (Config.Item("UseEQC").GetValue<KeyBind>().Active)
            {
                ComboEQ();
            }

            if (Config.Item("KillQ").GetValue<bool>() || Config.Item("KillEQ").GetValue<bool>())
            {
                Killer();
            }

            if (Config.Item("autoQMT").GetValue<bool>())
            {
                AutoQMT();
            }

            if (R.IsReady() && Config.Item("pingkillable").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(GetRRange()) && (float)ObjectManager.Player.GetSpellDamage(h, SpellSlot.R) * 0.9 > h.Health))
                {
                    Ping(enemy.Position.To2D());
                }
            }
        }

        private static float GetRRange()
        {
            return 1500 + (500 * R.Level);
        }

        private static void PeelE()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsValidTarget(E.Range) && enemy.Distance(ObjectManager.Player) <= enemy.BoundingRadius + enemy.AttackRange + ObjectManager.Player.BoundingRadius && enemy.IsMelee())
                {
                    E.Cast(enemy);
                }
            }
        }

        private static void JumptoMouse()
        {
            if (E.IsReady())
            {
                var pos = ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), -300).To3D();
                E.Cast(pos, true);
            }
        }   

        private static void ComboEQ()
        {
            var vTarget = SimpleTs.GetTarget(E.Range - 30, SimpleTs.DamageType.Physical);

            if (vTarget.IsValidTarget(E.Range) && E.IsReady() && Q.IsReady())
            {
                var prediction = E.GetPrediction(vTarget);
                if (prediction.Hitchance >= HitChance.High)
                {
                    E.Cast(vTarget, true);
                    EQComboT = Environment.TickCount;
                }
            }
        }        

        private static float GetDynamicQSpeed(float distance)
        {
            float accelerationrate = Q.Range / (_spellQSpeedMin - _spellQSpeed); // = -0.476...
            return _spellQSpeed + accelerationrate * distance;
        }

        private static void Killer()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range - 50, SimpleTs.DamageType.Physical);
            var eTarget = SimpleTs.GetTarget(E.Range - 50, SimpleTs.DamageType.Physical);
            var QonlyAA = Config.Item("QMin").GetValue<bool>();

            if (QonlyAA && Orbwalking.InAutoAttackRange(qTarget)) return;

            if (Config.Item("KillQ").GetValue<bool>() && Config.Item("KillEQ").GetValue<bool>() && Q.IsReady() && E.IsReady() && eTarget.Health < (ObjectManager.Player.GetSpellDamage(eTarget, SpellSlot.E) + ObjectManager.Player.GetSpellDamage(eTarget, SpellSlot.Q)) * 0.9)
            {
                PredictionOutput ePred = E.GetPrediction(eTarget);
                if (ePred.Hitchance >= HitChance.High)
                    E.Cast(eTarget, true, true);

                Vector3 predictedPos = Prediction.GetPrediction(qTarget, Q.Delay).UnitPosition;
                Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                Q.CastIfHitchanceEquals(qTarget, HitChance.High, true);

            }
            else
            {
                if (Config.Item("KillQ").GetValue<bool>() && Q.IsReady() && qTarget.Health < (ObjectManager.Player.GetSpellDamage(qTarget, SpellSlot.Q) * 0.9))
                {
                    Vector3 predictedPos = Prediction.GetPrediction(qTarget, Q.Delay).UnitPosition;
                    Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                    Q.CastIfHitchanceEquals(qTarget, HitChance.High, true);
                }
                else
                {
                    if (Config.Item("KillEQ").GetValue<bool>() && !Q.IsReady() && E.IsReady() && eTarget.Health < (ObjectManager.Player.GetSpellDamage(qTarget, SpellSlot.Q) * 0.9))
                    {
                        Vector3 predictedPos = Prediction.GetPrediction(qTarget, Q.Delay).UnitPosition;
                        Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                        Q.CastIfHitchanceEquals(qTarget, HitChance.High, true);
                    }
                }
            }
        }

        private static void AutoRKill()
        {
            var rTarget = SimpleTs.GetTarget(GetRRange() - 100, SimpleTs.DamageType.Physical);

            if (R.IsReady() && rTarget != null && rTarget.Health < ObjectManager.Player.GetSpellDamage(rTarget, SpellSlot.R) * 0.9)
            {
                if (ObjectManager.Player.Distance(rTarget) > Q.Range)
                    R.CastOnUnit(rTarget);
            }
        }

        private static void AutoCC()
        {
            List<Obj_AI_Hero> enemBuffed = getEnemiesBuffs();
            foreach (Obj_AI_Hero enem in enemBuffed)
            {                 
                if (W.IsReady() && Config.Item("autoccW").GetValue<bool>())
                {
                    W.CastOnUnit(enem);
                }

                if (Q.IsReady() && Config.Item("autoccQ").GetValue<bool>())
                {
                    if (Q.GetPrediction(enem).Hitchance >= HitChance.High)
                        Q.Cast(enem, true);
                }
            }
        }

        public static List<Obj_AI_Hero> getEnemiesBuffs()
        {
            List<Obj_AI_Hero> enemBuffs = new List<Obj_AI_Hero>();
            foreach (Obj_AI_Hero enem in ObjectManager.Get<Obj_AI_Hero>().Where(enem => enem.IsEnemy))
            {
                foreach (BuffInstance buff in enem.Buffs)
                {
                    if (buff.Name == "zhonyasringshield" || buff.Name == "caitlynyordletrapdebuff" || buff.Name == "powerfistslow" || buff.Name == "aatroxqknockup" || buff.Name == "ahriseducedoom" ||
                        buff.Name == "CurseoftheSadMummy" || buff.Name == "braumstundebuff" || buff.Name == "braumpulselineknockup" || buff.Name == "rupturetarget" || buff.Name == "EliseHumanE" ||
                        buff.Name == "HowlingGaleSpell" || buff.Name == "jarvanivdragonstrikeph2" || buff.Name == "karmaspiritbindroot" || buff.Name == "LuxLightBindingMis" || buff.Name == "lissandrawfrozen" ||
                        buff.Name == "lissandraenemy2" || buff.Name == "unstoppableforceestun" || buff.Name == "maokaiunstablegrowthroot" || buff.Name == "monkeykingspinknockup" || buff.Name == "DarkBindingMissile" ||
                        buff.Name == "namiqdebuff" || buff.Name == "nautilusanchordragroot" || buff.Name == "RunePrison" || buff.Name == "SonaR" || buff.Name == "sejuaniglacialprison" || buff.Name == "swainshadowgrasproot" ||
                        buff.Name == "threshqfakeknockup" || buff.Name == "VeigarStun" || buff.Name == "velkozestun" || buff.Name == "virdunkstun" || buff.Name == "viktorgravitonfieldstun" || buff.Name == "yasuoq3mis" ||
                        buff.Name == "zyragraspingrootshold" || buff.Name == "zyrabramblezoneknockup" || buff.Name == "katarinarsound" || buff.Name == "lissandrarself" || buff.Name == "AlZaharNetherGrasp" || buff.Name == "Meditate" ||
                        buff.Name == "missfortunebulletsound" || buff.Name == "AbsoluteZero" || buff.Name == "pantheonesound" || buff.Name == "VelkozR" || buff.Name == "infiniteduresssound" || buff.Name == "chronorevive" ||
                        buff.Type == BuffType.Suppression || buff.Name == "aatroxpassivedeath" || buff.Name == "zacrebirthstart")
                    {
                        enemBuffs.Add(enem);
                        break;
                    }
                }
            }
            return enemBuffs;
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (R.Level == 0) return;
            var menuItem = Config.Item("DrawRRangeM").GetValue<Circle>();
            if (menuItem.Active)
                Utility.DrawCircle(ObjectManager.Player.Position, GetRRange(), menuItem.Color, 2, 30, true);

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item("Draw_" + spell.Slot).GetValue<Circle>();
                if (menuItem.Active && (spell.Slot != SpellSlot.R || R.Level > 0))
                    Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, spell.IsReady() ? menuItem.Color : Color.Red);
            }          

        }

        private static void Ping(Vector2 position)
        {
            if (Environment.TickCount - LastPingT < 30 * 1000) return;
            LastPingT = Environment.TickCount;
            PingLocation = position;
            SimplePing();
            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
            Utility.DelayAction.Add(800, SimplePing);
        }

        private static void SimplePing()
        {
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(PingLocation.X, PingLocation.Y, 0, 0, Packet.PingType.Fallback)).Process();
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                E.Cast(gapcloser.Sender);
        }

        private static void Trap_OnCreate(LeagueSharp.GameObject Trap, EventArgs args)
        {
            if (ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.W) != SpellState.Ready || 
                (!Config.Item("autotpW").GetValue<bool>() && !Config.Item("autoRevW").GetValue<bool>()))
                return;   

                // Teleport
                if (Config.Item("autotpW").GetValue<bool>())
                {
                    if (Trap.Name.Contains("GateMarker_red") || Trap.Name == "Pantheon_Base_R_indicator_red.troy" || Trap.Name.Contains("teleport_target_red") ||
                        Trap.Name == "LeBlanc_Displacement_Yellow_mis.troy" || Trap.Name == "Leblanc_displacement_blink_indicator_ult.troy" || Trap.Name.Contains("Crowstorm"))
                    {
                        if (Trap.IsEnemy)
                        {

                            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(enemy => enemy.IsEnemy && enemy.Distance(Trap.Position) < W.Range);
                            ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, target);

                        }
                    }
                }
              
                // Revive
                if (Config.Item("autoRevW").GetValue<bool>())
                {
                    if (Trap.Name == "LifeAura.troy")
                    {
                        if (Trap.IsEnemy)
                        {

                            var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(enemy => enemy.IsEnemy && enemy.Distance(Trap.Position) < W.Range);
                            ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, target);

                        }
                    }
                }
            }         

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && Environment.TickCount - EQComboT < 500 &&
                (args.SData.Name.Contains("CaitlynEntrapment")))
            {
                Q.Cast(args.End, true);
            }
        }

        private static Obj_AI_Hero GetEnemyHitByQ(Spell Q, int numHit)
        {
            int totalHit = 0;
            Obj_AI_Hero target = null;

            foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
            {

                var prediction = Q.GetPrediction(current, true);

                if (Vector3.Distance(ObjectManager.Player.Position, prediction.CastPosition) <= Q.Range - 50)
                {

                    Vector2 extended = current.Position.To2D().Extend(ObjectManager.Player.Position.To2D(), -Q.Range + Vector2.Distance(ObjectManager.Player.Position.To2D(), current.Position.To2D()));
                    rect = new Geometrys.Rectangle(ObjectManager.Player.Position.To2D(), extended, Q.Width);

                    if (!current.IsMe && current.IsEnemy)
                    {                        
                        totalHit = 1;
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                        {
                            if (enemy.IsEnemy && current.ChampionName != enemy.ChampionName && !enemy.IsDead && !rect.ToPolygon().IsOutside(enemy.Position.To2D()))
                            {
                                totalHit += 1;
                            }
                        }
                    }

                    if (totalHit >= numHit)
                    {
                        target = current;
                        break;
                    }
                }

            }
            
            return target;
        }

        private static void AutoQMT()
        {
            var minHit = GetEnemyHitByQ(Q, Config.Item("minAutoQMT").GetValue<Slider>().Value);

            if (minHit != null)
            {
                var QonlyAA = Config.Item("QMin").GetValue<bool>();
                if (QonlyAA && Orbwalking.InAutoAttackRange(minHit)) return;

                Q.Cast(minHit, true);
            }
        }

        private static Obj_AI_Hero Cast_BasicLineSkillshot_Enemy(Spell spell, SimpleTs.DamageType damageType = SimpleTs.DamageType.Physical)
        {
            var QonlyAA = Config.Item("QMin").GetValue<bool>();
            var target = SimpleTs.GetTarget(spell.Range, damageType);

            if (!spell.IsReady())
                return null;
            
            if (target == null)
                return null;

            if (QonlyAA && Orbwalking.InAutoAttackRange(target)) return null;

            if (!target.IsValidTarget(spell.Range) || spell.GetPrediction(target).Hitchance < HitChance.High)
                return null;

            spell.Cast(target, true);
            return target;
        }

        private static void Cast_BasicLineSkillshot_AOE_Farm(Spell spell)
        {
            if (!spell.IsReady()) return;

            var minions = MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minions.Count == 0) return;

            var castPostion = MinionManager.GetBestLineFarmLocation(minions.Select(minion => minion.ServerPosition.To2D()).ToList(), spell.Width - 10, spell.Range);

            spell.Cast(castPostion.Position, true);
        }

        private static float GetManaPercent()
        {
            return (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana) * 100f;
        }
    }
}
