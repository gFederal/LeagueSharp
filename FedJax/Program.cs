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

namespace FedJax
{
    internal class Program
    {
        public const string ChampionName = "Jax";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;

        public static bool Eactive = false;        

        public static Map map;
        public static Helper Helper;
        public static Menu Config;
        public static Menu TargetedItems;
        public static Menu NoTargetedItems;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            map = new Map();
            Helper = new Helper();
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 700f);
            W = new Spell(SpellSlot.W, 200f);
            E = new Spell(SpellSlot.E, 187f);
            R = new Spell(SpellSlot.R, 200f);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");           

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
            Config.SubMenu("Combo").AddItem(new MenuItem("UnderTurretCombo", "Q Under Turret?").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItensCombo", "Use Items in Combo").SetValue(true)); 
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q in Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W in Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E in Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UnderTurretHarass", "Q Under Turret?").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("ModeHarass", "Harass Mode: ").SetValue(new StringList(new[] { "Q+W+E", "Q+W", "Q+E", "E+Q", "Default" }, 4)));
            Config.SubMenu("Harass").AddItem(new MenuItem("ManaHarass", "Dont Harass if Mana < %").SetValue(new Slider(50, 100, 0)));            
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E Farm").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("ManaFarm", "Min. Mana Percent").SetValue(new Slider(50, 100, 0))); 
            Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("WardJumpSmite", "WardJump + Smite").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "Auto Smite!").SetValue<KeyBind>(new KeyBind('J', KeyBindType.Toggle)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("laugh", "Troll laugh?").SetValue(true));            
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoI", "Auto Ignite").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("stun", "Interrupt Spells").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoEWQTower", "Auto Attack my tower").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ward", "Ward Jump!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("JumpSmiteRange", "wJumper+Smite Range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));            

            Config.AddSubMenu(new Menu("Spells", "Spells"));
            Config.SubMenu("Spells").AddItem(new MenuItem("setQ", "Use Q: ").SetValue(new StringList(new[] { "Out of Range", "Melee Range", "Both" }, 2)));
            Config.SubMenu("Spells").AddItem(new MenuItem("setW", "Use W: ").SetValue(new StringList(new[] { "Every AA", "After third AA" }, 1)));
            Config.SubMenu("Spells").AddItem(new MenuItem("setE", "Use E: ").SetValue(new StringList(new[] { "Q Range", "Melee Range", "Both" }, 0)));            
            Config.SubMenu("Spells").AddItem(new MenuItem("AutoUlt", "Enable Auto Ult").SetValue(true));
            Config.SubMenu("Spells").AddItem(new MenuItem("minEnemies", "Min. Enemies in Range").SetValue(new Slider(2, 5, 0)));
                        
            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Spells").AddSubMenu(menuUseItems);
            
            TargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(TargetedItems);
            TargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));

            // Extras -> Use Items -> AOE Items
            NoTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(NoTargetedItems);
            NoTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true)); 

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            Game.PrintChat("<font color=\"#00BFFF\">Fed" + ChampionName + " -</font> <font color=\"#FFFFFF\">Loaded!</font>");

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var menuItem = Config.Item("JumpSmiteRange").GetValue<Circle>();
            if (menuItem.Active) Utility.DrawCircle(Player.Position, 1100, menuItem.Color);
            
            foreach (var spell in SpellList)
            {
                 menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                {
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
                }
            }            
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Config.Item("setW").GetValue<StringList>().SelectedIndex == 1)
            {
                if (Config.Item("ComboActive").GetValue<KeyBind>().Active && Player.HasBuff("jaxrelentlessassaultas", true) && W.IsReady())
                {
                    W.Cast();
                }
            }

            if (Config.Item("AutoI").GetValue<bool>())
            {
                AutoIgnite();
            }

            if (!Config.Item("ComboActive").GetValue<KeyBind>().Active && !Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Eactive = false;
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }

            if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
            {
                FreezeFarm();
            }
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                JungleFarm();
            }            

            if (Config.Item("Ward").GetValue<KeyBind>().Active)
            {
                Jumper.wardJump(Game.CursorPos.To2D());
            }

            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                AutoSmite();
            }            

            if (Config.Item("AutoEWQTower").GetValue<KeyBind>().Active)
            {
                AutoUnderTower();
            }
        }                      
        

        private static void AutoUnderTower()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (Utility.UnderTurret(qTarget, false) && Q.IsReady() && E.IsReady())
            {
                E.Cast();
                W.Cast();
                Q.Cast(qTarget);
            }
        }

        private static void AutoIgnite()
        {
            if (IgniteSlot == SpellSlot.Unknown || ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) != SpellState.Ready) return;            

            const int range = 600;

            foreach (var enemy in Program.Helper.EnemyTeam.Where(hero => hero.IsValidTarget(range) && ObjectManager.Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite) * 0.9 >= hero.Health))
            {
                ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, enemy);
                if (Config.Item("laugh").GetValue<bool>())
                {
                    Game.Say("/l");
                }
                return;
            }
        }

        private static void AutoUlt()
        {
            int inimigos = Utility.CountEnemysInRange(650);

            if (Config.Item("minEnemies").GetValue<Slider>().Value <= inimigos)
            {
                R.Cast();
            }
        }
        
        private static void CastSpellQ()
        {
            var useQi = Config.Item("setQ").GetValue<StringList>().SelectedIndex;
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (!Config.Item("UnderTurretCombo").GetValue<bool>() && Utility.UnderTurret(qTarget) && Config.Item("ComboActive").GetValue<KeyBind>().Active) return;
            if (!Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(qTarget) && Config.Item("HarassActive").GetValue<KeyBind>().Active) return;

            switch (useQi)
            {                
                case 0:
                    if (qTarget.Distance(ObjectManager.Player) >= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                        Q.CastOnUnit(qTarget);
                    break;
                case 1:
                    if (qTarget.Distance(ObjectManager.Player) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                        Q.CastOnUnit(qTarget);
                    break;
                case 2:                    
                    Q.CastOnUnit(qTarget);
                    break;
            }         
        }

        private static void CastSpellE()
        { 
            var useEi = Config.Item("setE").GetValue<StringList>().SelectedIndex;

            if (!Eactive && ((useEi == 0 || useEi == 2)  && Q.IsReady() && (Utility.CountEnemysInRange((int)Q.Range + 200) >= 1)) ||
                 (useEi >= 1 && (Utility.CountEnemysInRange((int)E.Range + 100) >= 1)))
            {
                E.Cast();
                E.LastCastAttemptT = Environment.TickCount;
                Eactive = true;
            }
        }

        private static void ActivateE()
        {
            if (!E.IsReady() || !Eactive) return;
            
            if (E.IsReady() && Environment.TickCount - E.LastCastAttemptT <= 5000)
            {
                if (Utility.CountEnemysInRange((int)E.Range) >= 1)
                {
                    E.Cast();
                    Eactive = false;
                }
            }
        }
        
        private static void Combo()
        {
            var iTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (iTarget != null && Config.Item("UseItensCombo").GetValue<bool>())
            {                
                UseItems(iTarget);
            }
            if (Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                CastSpellE();
            }
            if (iTarget != null && Config.Item("setW").GetValue<StringList>().SelectedIndex == 0 && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                W.Cast();
            }
            if (Config.Item("UseQCombo").GetValue<bool>() && Q.IsReady())
            {
                CastSpellQ();
            }
            if (Config.Item("UseECombo").GetValue<bool>())
            {
                ActivateE();
            }

            if (Config.Item("AutoUlt").GetValue<bool>() && R.IsReady())
            {
                AutoUlt();
            }
              
        }

        private static void Harass()
        {
            var hTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);            

            var HMana = Config.Item("ManaHarass").GetValue<Slider>().Value;
            var MPercentH = Player.Mana * 100 / Player.MaxMana;

            int HarassMode = Config.Item("ModeHarass").GetValue<StringList>().SelectedIndex;

            if (MPercentH >= HMana && hTarget != null)
            {
                switch (HarassMode)
                {
                    case 0:
                        {
                            if (!Q.IsReady() || !W.IsReady() || !E.IsReady()) return;
                            
                                if ((Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) ||
                                   (!Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)) ||
                                   (Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)))
                                {
                                    if (!Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) return;

                                    Q.CastOnUnit(hTarget);
                                    W.Cast();
                                    E.Cast();   
                                }
                            break;
                        }
                    case 1:
                        {
                            if (Q.IsReady() && W.IsReady())
                            {
                                if ((Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) ||
                                   (!Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)) ||
                                   (Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)))
                                {
                                    if (!Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) return;

                                    Q.CastOnUnit(hTarget);
                                    W.Cast();
                                }                                
                            }
                            break;
                        }
                    case 2:
                        {
                            if (!Q.IsReady() || !E.IsReady()) return;
                            
                                if ((Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) ||
                                   (!Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)) ||
                                   (Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)))
                                {
                                    if (!Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) return;

                                    Q.CastOnUnit(hTarget);
                                    E.Cast();
                                } 
                            break;
                        }
                    case 3:
                        {
                            if (!Q.IsReady() || !E.IsReady()) return;
                            

                                if ((Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) ||
                                   (!Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)) ||
                                   (Config.Item("UnderTurretHarass").GetValue<bool>() && !Utility.UnderTurret(hTarget)))
                                {
                                    if (!Config.Item("UnderTurretHarass").GetValue<bool>() && Utility.UnderTurret(hTarget)) return;

                                    CastSpellE();
                                    CastSpellQ();
                                    ActivateE();
                                }
                            break;
                        }
                    case 4:
                        {
                            if (Config.Item("UseWHarass").GetValue<bool>() && W.IsReady())
                            {
                                W.Cast();
                            }
                            if (Config.Item("UseEHarass").GetValue<bool>() && E.IsReady())
                            {
                                CastSpellE();
                            }
                            if (Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
                            {
                                CastSpellQ();
                            }
                            if (Config.Item("UseEHarass").GetValue<bool>())
                            {
                                ActivateE();
                            }
                            break;
                        }                       
                }                              
                
            }
        }        

        private static void FreezeFarm()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);

            foreach (var vMinion in allMinionsQ)
            {
                var Qdamage = ObjectManager.Player.GetSpellDamage(vMinion, SpellSlot.Q) * 0.85;

                if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && Qdamage >= Q.GetHealthPrediction(vMinion))
                {
                    Q.CastOnUnit(vMinion);
                }
            }
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health); 
           
            var FMana = Config.Item("ManaFarm").GetValue<Slider>().Value;
            var MPercent = Player.Mana * 100 / Player.MaxMana;

            foreach (var vMinion in allMinionsQ)
            {
                var Qdamage = ObjectManager.Player.GetSpellDamage(vMinion, SpellSlot.Q) * 0.85;

                if (Config.Item("setW").GetValue<StringList>().SelectedIndex == 0 && W.IsReady() && Config.Item("UseWFarm").GetValue<bool>() && allMinionsQ.Count > 0)
                {
                    W.Cast();
                }
                if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady() && Qdamage >= Q.GetHealthPrediction(vMinion) && MPercent >= FMana)
                {
                    Q.CastOnUnit(vMinion);
                }

                if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady() && Q.IsReady() && allMinionsQ.Count > 2 && MPercent >= FMana)
                {
                    E.Cast();
                    Q.CastOnUnit(vMinion);
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                if (Config.Item("setW").GetValue<StringList>().SelectedIndex == 0 && W.IsReady() && Config.Item("UseWJFarm").GetValue<bool>())
                {
                    W.Cast();
                }
                if (Q.IsReady() && Config.Item("UseQJFarm").GetValue<bool>())
                {
                    Q.CastOnUnit(mobs[0]);
                }
                if (E.IsReady() && Config.Item("UseEJFarm").GetValue<bool>())
                {
                    E.Cast();
                }
            } 
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {            
            if (!Config.Item("stun").GetValue<bool>())
                return;

            if (Player.Distance(vTarget) < Q.Range-50)
            {
                E.Cast();
                Q.Cast(vTarget);
            }
        }

        private static InventorySlot GetInventorySlot(int ID)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(item => (item.Id == (ItemId)ID && item.Stacks >= 1) || (item.Id == (ItemId)ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget != null)
            {
                foreach (MenuItem menuItem in TargetedItems.Items)
                {
                    var useItem = TargetedItems.Item(menuItem.Name).GetValue<bool>();
                    if (useItem)
                    {
                        var itemID = Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4));
                        if (Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null)
                            Items.UseItem(itemID, vTarget);
                    }
                }

                foreach (MenuItem menuItem in NoTargetedItems.Items)
                {
                    var useItem = NoTargetedItems.Item(menuItem.Name).GetValue<bool>();
                    if (useItem)
                    {
                        var itemID = Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4));
                        if (Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null)
                            Items.UseItem(itemID);
                    }
                }
            }
        }

        private static void AutoSmite()
        {
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                float[] SmiteDmg = { 20 * Player.Level + 370, 30 * Player.Level + 330, 40 * Player.Level + 240, 50 * Player.Level + 100 };
                string[] MonsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
                string[] Monstersteal = { "Worm", "Dragon" };
                var vMinions = MinionManager.GetMinions(Player.ServerPosition, 350+Player.SummonerSpellbook.Spells.FirstOrDefault(
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
                            if (Config.Item("WardJumpSmite").GetValue<bool>())
                            {
                                if (Player.Distance(vMinion) > 720 && (Monstersteal.Any(name => vMinion.BaseSkinName.StartsWith(name))))
                                {
                                    Jumper.wardJump(Game.CursorPos.To2D());
                                }
                            }

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

        private static void OnProcessSpell(LeagueSharp.Obj_AI_Base obj, LeagueSharp.GameObjectProcessSpellCastEventArgs arg)
        {
            if (Jumper.testSpells.ToList().Contains(arg.SData.Name))
            {
                Jumper.testSpellCast = arg.End.To2D();
                Polygon pol;
                if ((pol = Program.map.getInWhichPolygon(arg.End.To2D())) != null)
                {
                    Jumper.testSpellProj = pol.getProjOnPolygon(arg.End.To2D());
                }
            }
        }

        

    }
}
