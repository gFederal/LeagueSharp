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

namespace FedNocturne
{
    internal class Program
    {
        public const string ChampionName = "Nocturne";
        public static Orbwalking.Orbwalker Orbwalker;
        
        public static Spell Q, W, E, R;
        public static Vector2 PingLocation;
        public static int LastPingT = 0;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;

        public static Menu Config;
        public static Menu TargetedItems;
        public static Menu NoTargetedItems;
        private static Obj_AI_Hero Player;
        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, 0f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 25000f);

            Q.SetSkillshot(0.25f, 60f, 1600f, false, SkillshotType.SkillshotLine);       

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");            

            Config = new Menu("Fed" + ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("TeamFight", "TeamFight"));
            Config.SubMenu("TeamFight").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("TeamFight").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("TeamFight").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("TeamFight").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("TeamFight").AddItem(new MenuItem("UseRHP", "% HP to R Combo: ").SetValue<Slider>(new Slider(50, 100, 10)));
            Config.SubMenu("TeamFight").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));            
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("useQHit", "Q Min hit").SetValue(new Slider(3, 6, 1)));            
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "Auto Smite!").SetValue<KeyBind>(new KeyBind('J', KeyBindType.Toggle)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoI", "Auto Ignite").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("InterruptSpells", "Interrupt spells").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoRHP", "Auto R LowHP").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Misc").AddItem(new MenuItem("HPR", "% Low HP: ").SetValue<Slider>(new Slider(30, 100, 10)));
            Config.SubMenu("Misc").AddItem(new MenuItem("useR_Killableping", "Ping if is Low HP").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoRStrong", "R Most AD/AP in Range").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Misc").AddItem(new MenuItem("MostR", "R Most: ").SetValue(new StringList(new[] { "AD", "AP", "Easy" }, 0)));            

            Config.AddSubMenu(new Menu("Itens", "Itens"));
            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            Config.SubMenu("Itens").AddSubMenu(menuUseItems);

            TargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(TargetedItems);
            TargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            TargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));

            NoTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(NoTargetedItems);
            NoTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
            NoTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));

            Config.AddSubMenu(new Menu("Drawing", "Drawing"));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
            Config.SubMenu("Drawing").AddItem(new MenuItem("DrawRRangeM", "Draw R Range (Minimap)").SetValue(new Circle(false, Color.FromArgb(150, Color.DodgerBlue))));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            Game.PrintChat("<font color=\"#00BFFF\">Fed" + ChampionName + " -</font> <font color=\"#FFFFFF\">Loaded!</font>");

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {

            if (ObjectManager.Player.IsDead) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {

                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                {
                    Harass();
                }

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                {
                    LaneClear();
                }

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                {
                    JungleFarm();
                }
            }

            if (Config.Item("AutoRHP").GetValue<KeyBind>().Active)
            {
                AutoRLowHP();
            }

            if (Config.Item("AutoRStrong").GetValue<KeyBind>().Active)
            {
                AutoRMode();
            }

            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                AutoSmite();
            }

            if (Config.Item("AutoI").GetValue<bool>())
            {
                AutoIgnite();
            }

            if (R.IsReady() && Config.Item("useR_Killableping").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(GetRRange()) && EnemmylowHP(Config.Item("HPR").GetValue<Slider>().Value, GetRRange())))
                {
                    Ping(enemy.Position.To2D());
                }
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

        private static void AutoIgnite()
        {
            var iTarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.True);
            var Idamage = ObjectManager.Player.GetSummonerSpellDamage(iTarget, Damage.SummonerSpell.Ignite) * 0.90;

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && iTarget.Health < Idamage)
            {
                Player.SummonerSpellbook.CastSpell(IgniteSlot, iTarget);                
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
                        }
                    }
                }
            }
        }
        private static float GetRRange()
        {
            return 1250 + (750 * R.Level);
        }
        private static void AutoRMode()
        {
            Obj_AI_Hero newtarget = null;
            var RMode = Config.Item("MostR").GetValue<StringList>().SelectedIndex;

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget() && Geometry.Distance(enemy) <= GetRRange()))
            {
                if (newtarget == null)
                {
                    newtarget = enemy;
                }
                else
                {
                    switch (RMode)
                    {
                        case 0:
                            if (enemy.BaseAttackDamage + enemy.FlatPhysicalDamageMod <
                                        newtarget.BaseAttackDamage + newtarget.FlatPhysicalDamageMod)
                            {
                                newtarget = enemy;
                            }
                            break;
                        case 1:
                            if (enemy.FlatMagicDamageMod < newtarget.FlatMagicDamageMod)
                            {
                                newtarget = enemy;
                            }
                            break;
                        case 2:
                            if ((enemy.Health - Damage.CalcDamage(ObjectManager.Player, enemy, Damage.DamageType.Magical, enemy.Health)) <
                                        (enemy.Health - Damage.CalcDamage(ObjectManager.Player, newtarget, Damage.DamageType.Magical, newtarget.Health)))
                            {
                                newtarget = enemy;
                            }
                            break;
                    }
                }
            }

            if (R.IsReady() && newtarget != null)
            {
                R.Cast();
                R.CastOnUnit(newtarget, true);
            }
        }
        private static void AutoRLowHP()
        {
            var rTarget = SimpleTs.GetTarget(GetRRange(), SimpleTs.DamageType.Physical);
            if (R.IsReady() && rTarget != null)
            {
                if (ObjectManager.Player.Distance(rTarget) > 1100 && EnemmylowHP(Config.Item("HPR").GetValue<Slider>().Value, GetRRange()))
                {
                    R.Cast();
                    R.CastOnUnit(rTarget, true);
                }
            }
        }
        private static void Combo()
        {
            var rTarget = SimpleTs.GetTarget(GetRRange(), SimpleTs.DamageType.Physical);
            if (R.IsReady() && Config.Item("UseRCombo").GetValue<bool>())
            {
                if (ObjectManager.Player.Distance(rTarget) > 1100 && EnemmylowHP(Config.Item("UseRHP").GetValue<Slider>().Value, GetRRange()))
                {
                    R.Cast();
                    R.CastOnUnit(rTarget, true);
                }
            }            

            var qTarget = SimpleTs.GetTarget(Q.Range - 50, SimpleTs.DamageType.Physical);

            if (qTarget != null && Config.Item("UseItensCombo").GetValue<bool>())
            {
                UseItems(qTarget);
            }

            if (Q.IsReady() && Config.Item("UseQCombo").GetValue<bool>())
            {
                if (Q.GetPrediction(qTarget).Hitchance >= HitChance.High)
                    Q.Cast(qTarget, true);
            }

            var eTarget = SimpleTs.GetTarget(E.Range - 30, SimpleTs.DamageType.Physical);
            if (E.IsReady() && Config.Item("UseECombo").GetValue<bool>())
            {
                E.CastOnUnit(qTarget, true);
            }

            if (W.IsReady() && Config.Item("UseWCombo").GetValue<bool>())
            {
                if (ObjectManager.Player.Distance(eTarget) < 425)
                {
                    W.Cast();
                }
            }
        }
        private static void Harass()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range - 50, SimpleTs.DamageType.Physical);
            if (Q.IsReady() && Config.Item("UseQHarass").GetValue<bool>())
            {
                if (Q.GetPrediction(qTarget).Hitchance >= HitChance.High)
                    Q.Cast(qTarget, true);
            }

            var eTarget = SimpleTs.GetTarget(E.Range - 30, SimpleTs.DamageType.Physical);
            if (E.IsReady() && Config.Item("UseEHarass").GetValue<bool>())
            {
                E.CastOnUnit(qTarget, true);
            }
        }
        private static void LaneClear()
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (Q.IsReady() && Config.Item("UseQFarm").GetValue<bool>())
            {
                List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.25f, 60f, 1600f, ObjectManager.Player.ServerPosition, 1200f, false, SkillshotType.SkillshotLine);
                MinionManager.FarmLocation farm = Q.GetLineFarmLocation(minionPs);
                if (farm.MinionsHit >= Config.Item("useQHit").GetValue<Slider>().Value)
                {
                    Q.Cast(farm.Position, true);
                }
            }
        }
        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];

                if (Q.IsReady() && Config.Item("UseQJFarm").GetValue<bool>())
                {
                    Q.Cast(mob);
                }
                if (W.IsReady() && Config.Item("UseWJFarm").GetValue<bool>())
                {
                    W.Cast();
                }
                if (E.IsReady() && Config.Item("UseEJFarm").GetValue<bool>())
                {
                    E.Cast(mob);
                }
            }
        }
        private static bool EnemmylowHP(int percentHP, float range)
        {
            foreach (var enemmy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemmy.IsEnemy && !enemmy.IsDead)
                {
                    if (Vector3.Distance(ObjectManager.Player.Position, enemmy.Position) < range && ((enemmy.Health / enemmy.MaxHealth) * 100) < percentHP)
                    {
                        return true;
                    }
                }
            }

            return false;
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
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(PingLocation.X, PingLocation.Y, 0, 0, Packet.PingType.FallbackSound)).Process();
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

            if (Config.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (Config.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (Config.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, GetRRange(), R.IsReady() ? Color.Green : Color.Red);
            
        }
        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;
            if (ObjectManager.Player.Distance(unit) < E.Range && E.IsReady() && unit.IsEnemy)
                E.CastOnUnit(unit, true);
        }
    }
}
