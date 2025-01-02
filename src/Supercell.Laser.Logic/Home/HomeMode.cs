namespace Supercell.Laser.Logic.Home
{
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Command;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;

    public class HomeMode
    {
        public const int UNLOCKABLE_HEROES_COUNT = 38;

        public readonly LogicGameListener GameListener;

        public ClientHome Home;
        public ClientAvatar Avatar;

        public Action<int> CharacterChanged;

        public HomeMode(ClientHome home, ClientAvatar avatar, LogicGameListener gameListener)
        {
            Home = home;
            Avatar = avatar;

            Home.HomeMode = this;
            Avatar.HomeMode = this;

            GameListener = gameListener;
        }

        public static HomeMode LoadHomeState(LogicGameListener gameListener, ClientHome home, ClientAvatar avatar, EventData[] events)
        {
            home.Events = events;

            HomeMode homeMode = new HomeMode(home, avatar, gameListener);
            homeMode.Enter(DateTime.UtcNow);

            return homeMode;
        }

        private bool GetRandomBrawlerForGatcha(Random rand, DeliveryUnit unit)
        {
            List<int> Brawlers = new() { 4, 5, 6, 10, 11, 12, 13, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 28, 29, 31, 32, 34, 36, 37, 38 };

            List<int> UnlockedBrawlers = new List<int>();
            foreach (Hero hero in Avatar.Heroes)
            {
                UnlockedBrawlers.Add(GlobalId.GetInstanceId(hero.CharacterId));
            }
            List<int> UnlockableBrawlers = Brawlers.Where(x => !UnlockedBrawlers.Contains(x)).ToList();
            if (UnlockableBrawlers.Count == 0) return false;
            int Brawler = GlobalId.CreateGlobalId(16, UnlockableBrawlers[rand.Next(UnlockableBrawlers.Count)]);

            CharacterData Character = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(Brawler);
            //CardData Card = DataTables.Get(DataType.Card).GetData<CardData>(Character.Name + "_unlock");
            GatchaDrop drop = new(1)
            {
                DataGlobalId = Character.GetGlobalId(),
                Count = 1
            };
            unit.AddDrop(drop);
            return true;
        }

        public bool HasHeroUnlocked(int Brawler)
        {
            return Avatar.HasHero(Brawler);
        }

        public bool ProcChance(Random r, double chance, string type, double forcedDrops)
        {
            double v1 = r.Next(0, 1000) / 10;
            switch (type)
            {
                case "bonus":
                    chance += forcedDrops * 2;
                    break;
                case "starpower":
                    chance += forcedDrops * 1.25;
                    break;
                case "character":
                    chance += forcedDrops * 0.5;
                    break;
            }
            return v1 < chance;
        }
        public double GetMultByRarity(string rarity)
        {
            double result = 0;
            switch (rarity)
            {
                case "bonus":
                    result = 0.5;
                    break;
                case "character":
                    result = 2;
                    break;
                case "card_notchar":
                    result = 4;
                    break;
                case "rare":
                    result = 2;
                    break;
                case "super_rare":
                    result = 3;
                    break;
                case "epic":
                    result = 5;
                    break;
                case "super_epic":
                    result = 7;
                    break;
                case "legendary":
                    result = 10;
                    break;
            }
            return result;
        }
        public double CalcForcedDrop(bool proc, string rarity, string type, double forcedDrops, string box)
        {
            double result = 0.0;
            double v1 = 0.0;
            double v2 = 0.0;
            double v3 = 0.0;

            double v4 = 0.0;
            double v5 = 0.0;
            double v6 = 0.0;

            double v7 = GetMultByRarity(rarity);
            double v8 = forcedDrops;
            switch (box)
            {
                case "mega":
                    v1 = 0.66;
                    v2 = 0.33;
                    v3 = 0.125;

                    v4 = 0.66 * v7;
                    v5 = 0.66 * v7;
                    v6 = 0.66 * v7;
                    break;
                case "big":
                    v1 = 0.33;
                    v2 = 0.125;
                    v3 = 0.065;

                    v4 = 0.55 * v7;
                    v5 = 0.55 * v7;
                    v6 = 0.55 * v7;
                    break;
                case "box":
                    v1 = 0.22;
                    v2 = 0.10;
                    v3 = 0.045;

                    v4 = 0.44 * v7;
                    v5 = 0.44 * v7;
                    v6 = 0.44 * v7;
                    break;
            }
            if (!proc)
            {
                switch (type)
                {
                    case "bonus":
                        v8 += v3;
                        break;
                    case "character":
                        v8 += v1;
                        break;
                    case "starpower":
                        v8 += v2;
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case "bonus":
                        v8 = Math.Max(0, v8 - v6);
                        break;
                    case "character":
                        v8 += Math.Max(0, v8 - v4);
                        break;
                    case "starpower":
                        v8 = Math.Max(0, v8 - v5);
                        break;
                }

            }
            if (v8 > 10)
            {
                result = 10;
            }
            else
            {
                result = v8;
            }
            return result;
        }
        public void SimulateGatcha(DeliveryUnit unit, bool lowluck = false)
        {
            List<int> Brawlers = new() { 4, 5, 6, 10, 11, 12, 13, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 28, 29, 31, 32, 34, 35, 36, 37, 38 };

            List<int> UnlockedBrawlers = new List<int>();
            foreach (Hero hero in Avatar.Heroes)
            {
                UnlockedBrawlers.Add(GlobalId.GetInstanceId(hero.CharacterId));
            }
            List<int> UnlockableBrawlers = Brawlers.Where(x => !UnlockedBrawlers.Contains(x)).ToList();
            bool isBr = false;

            bool canDropSp = false;
            List<int> sps = new();

            Avatar.RollsSinceGoodDrop++;
            Random rand = new();

            List<int> DiamondsReward = new() { 3, 5, 7, 12, 15, 18, 21, 24, 27 };
            int unlockedBrawlersCount = Avatar.GetUnlockedHeroesCount();
            List<int> possibleBonuses = new() { 0, 1 };

            List<int> powerPoints = new();
            if (unit.Type == 10)
            {
                List<int> TokenDoublersReward = new() { 200 };
                List<int> TicketsReward = new() { 1, 2, 3 };

                List<Hero> CanHavePowerPoints = new List<Hero>();

                foreach (Hero hero in Avatar.Heroes)
                {
                    if (hero.PowerLevel != 8)
                    {
                        if (hero.PowerPoints < 1410)
                        {
                            CanHavePowerPoints.Add(hero);
                        }
                        continue;
                    }
                }

                int count = CanHavePowerPoints.Count;
                Dictionary<int, int> Avards = new();
                int bonusChance = 0;
                if (UnlockedBrawlers.Count < 3)
                {
                    bonusChance = 6;
                }
                else if (UnlockedBrawlers.Count < 5)
                {
                    bonusChance = 4;
                }
                else if (UnlockedBrawlers.Count < 7)
                {
                    bonusChance = 2;
                }

                if (UnlockableBrawlers.Count > 0 && !isBr)
                {
                    Avatar.ForcedDrops = CalcForcedDrop(false, "character", "starpower", Avatar.ForcedDrops, "box");
                }


                foreach (Hero hero in Avatar.Heroes)
                {
                    if (hero.PowerLevel >= 6) // == 8)
                    {
                        canDropSp = true;
                        break;
                    }
                }
                if (ProcChance(rand, 5 * 1 + bonusChance, "character", Avatar.ForcedDrops) && UnlockableBrawlers.Count > 0)
                {
                    isBr = true;
                    int Brawler = GlobalId.CreateGlobalId(16, UnlockableBrawlers[rand.Next(UnlockableBrawlers.Count)]);
                    UnlockableBrawlers.Remove(GlobalId.GetInstanceId(Brawler));
                    CharacterData Character = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(Brawler);
                    CardData Card = DataTables.Get(DataType.Card).GetData<CardData>(Character.Name + "_unlock");
                    GatchaDrop drop = new(1)
                    {
                        DataGlobalId = Character.GetGlobalId(),
                        Count = 1
                    };
                    unit.AddDrop(drop);
                    Avatar.ForcedDrops = CalcForcedDrop(true, Card.Rarity, "starpower", Avatar.ForcedDrops, "box");
                    //UnlockableBrawlers.Remove(Brawler);
                }
                if (!isBr && canDropSp)
                {
                    foreach (Hero hero in Avatar.Heroes)
                    {
                        if (hero.PowerLevel == 8)
                        {
                            CardData card = null;
                            CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                            card = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique");
                            CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_2");
                            CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_3");
                            if (card3 != null)
                            {
                                if (card3.LockedForChronos)
                                {
                                    card3 = null;
                                }
                            }
                            if (card3 == null)
                            {
                                if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                {
                                    sps.Add(card2.GetGlobalId());
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                {
                                    sps.Add(card.GetGlobalId());
                                }
                            }
                            else
                            {
                                if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    int r = rand.Next(0, 2);
                                    if (r == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else if (r == 1)
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    sps.Add(card3.GetGlobalId());
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    sps.Add(card.GetGlobalId());
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    sps.Add(card2.GetGlobalId());
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                }
                            }

                        }
                        if (hero.PowerLevel > 5)
                        {
                            string[] cards = { "GrowBush", "Shield", "Heal", "Jump", "ShootAround", "DestroyPet", "PetSlam", "Slow", "Push", "Dash", "SpeedBoost", "BurstHeal", "Spin", "Teleport", "Immunity", "Trail", "Totem", "Grab", "Swing", "Vision", "Regen", "HandGun", "Promote", "Sleep", "Slow", "Reload", "Fake", "Trampoline", "Explode", "Blink", "PoisonTrigger", "Barrage", "Focus", "MineTrigger", "Reload", "Seeker", "Meteor", "HealPotion", "Stun", "TurretBuff", "StaticDamage" };
                            CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                            CardData WildCard = null;
                            foreach (string cardname in cards)
                            {
                                string n = char.ToUpper(cd.Name[0]) + cd.Name.Substring(1);
                                WildCard = DataTables.Get(DataType.Card).GetData<CardData>(n + "_" + cardname);
                                if (WildCard != null && !Avatar.Starpowers.Contains(WildCard.GetGlobalId()))
                                {
                                    sps.Add(WildCard.GetGlobalId());
                                }
                            }
                        }
                    }

                    if (ProcChance(rand, 11 * 1, "starpower", Avatar.ForcedDrops) && sps.Count > 0)
                    {
                        int sp = sps[rand.Next(sps.Count)];
                        GatchaDrop drop = new(4)
                        {
                            CardGlobalId = sp,
                            Count = 1
                        };
                        unit.AddDrop(drop);
                        CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(sp);
                        CharacterData hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
                        Hero h = Avatar.GetHero(hero.GetGlobalId());
                        h.HasStarpower = true;
                        Avatar.ForcedDrops = CalcForcedDrop(true, "card_notchar", "starpower", Avatar.ForcedDrops, "box");
                        sps.Remove(sp);
                    }
                    else
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(false, "card_notchar", "starpower", Avatar.ForcedDrops, "box");
                        canDropSp = false;
                    }
                }
                if (!canDropSp && !isBr)
                {
                    GatchaDrop coins = new(7)
                    {
                        Count = rand.Next(25, 75)
                    };
                    unit.AddDrop(coins);
                    for (int x = 0; Math.Min(count, 2) > x; x++)
                    {
                        Hero Brawler = CanHavePowerPoints[rand.Next(CanHavePowerPoints.Count)];
                        CanHavePowerPoints.Remove(Brawler);
                        int oldpp = Brawler.PowerPoints;
                        int PowerPoints = rand.Next(5, 45);
                        if (oldpp + PowerPoints > 1410)
                        {
                            PowerPoints = 1410 - oldpp;
                        }
                        Avards.Add(Brawler.CharacterId, PowerPoints);
                        //Brawler.PowerPoints += PowerPoints;
                    }
                    List<KeyValuePair<int, int>> list = Avards.ToList();
                    list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    Dictionary<int, int> FinalAwards = new Dictionary<int, int>();
                    foreach (KeyValuePair<int, int> pair in list)
                    {
                        FinalAwards.Add(pair.Key, pair.Value);
                    }
                    foreach (KeyValuePair<int, int> avard in FinalAwards)
                    {
                        GatchaDrop drop = new(6)
                        {
                            Count = avard.Value,
                            DataGlobalId = avard.Key
                        };
                        unit.AddDrop(drop);
                    }

                    while (ProcChance(rand, 10, "bonus", Avatar.ForcedDrops) && possibleBonuses.Count > 0) // add gems bonus
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(true, "bonus", "bonus", Avatar.ForcedDrops, "box");
                        int bonusType = possibleBonuses[rand.Next(0, possibleBonuses.Count)];
                        switch (bonusType)
                        {
                            case 0:
                                GatchaDrop gems = new(8)
                                {
                                    Count = DiamondsReward[rand.Next(DiamondsReward.Count)]
                                };
                                unit.AddDrop(gems);
                                break;
                            case 1:
                                GatchaDrop tokens = new(2)
                                {
                                    Count = TokenDoublersReward[rand.Next(TokenDoublersReward.Count)]
                                };
                                unit.AddDrop(tokens);
                                break;
                            case 2:
                                GatchaDrop tickets = new(3)
                                {
                                    Count = TicketsReward[rand.Next(TicketsReward.Count)]
                                };
                                unit.AddDrop(tickets);
                                break;
                        }
                        possibleBonuses.Remove(bonusType);
                    }
                    if (possibleBonuses.Count == 3)
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(false, "bonus", "bonus", Avatar.ForcedDrops, "box");
                    }
                }
            }
            else if (unit.Type == 12)
            {
                List<int> TokenDoublersReward = new() { 200, 400 };
                List<int> TicketsReward = new() { 1, 2, 3, 5 };

                GatchaDrop coins = new(7)
                {
                    Count = rand.Next(50, 200)
                };
                unit.AddDrop(coins);

                List<Hero> CanHavePowerPoints = new List<Hero>();

                foreach (Hero hero in Avatar.Heroes)
                {
                    if (hero.PowerLevel != 8)
                    {
                        if (hero.PowerPoints < 1410)
                        {
                            CanHavePowerPoints.Add(hero);
                        }
                        continue;
                    }
                }

                int count = CanHavePowerPoints.Count;
                Dictionary<int, int> Avards = new();
                for (int x = 0; Math.Min(count, 3) > x; x++)
                {
                    Hero Brawler = CanHavePowerPoints[rand.Next(CanHavePowerPoints.Count)];
                    CanHavePowerPoints.Remove(Brawler);
                    int oldpp = Brawler.PowerPoints;
                    int PowerPoints = rand.Next(15, 125);
                    if (oldpp + PowerPoints > 1410)
                    {
                        PowerPoints = 1410 - oldpp;
                    }
                    Avards.Add(Brawler.CharacterId, PowerPoints);
                    //Brawler.PowerPoints += PowerPoints;
                }
                List<KeyValuePair<int, int>> list = Avards.ToList();
                list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                Dictionary<int, int> FinalAwards = new Dictionary<int, int>();
                foreach (KeyValuePair<int, int> pair in list)
                {
                    FinalAwards.Add(pair.Key, pair.Value);
                }
                foreach (KeyValuePair<int, int> avard in FinalAwards)
                {
                    GatchaDrop drop = new(6)
                    {
                        Count = avard.Value,
                        DataGlobalId = avard.Key
                    };
                    unit.AddDrop(drop);
                }
                int bonusChance = 0;
                if (UnlockedBrawlers.Count < 3)
                {
                    bonusChance = 6;
                }
                else if (UnlockedBrawlers.Count < 5)
                {
                    bonusChance = 4;
                }
                else if (UnlockedBrawlers.Count < 7)
                {
                    bonusChance = 2;
                }
                while (ProcChance(rand, 5 * 2 + bonusChance, "character", Avatar.ForcedDrops) && UnlockableBrawlers.Count > 0)
                {
                    isBr = true;
                    int Brawler = GlobalId.CreateGlobalId(16, UnlockableBrawlers[rand.Next(UnlockableBrawlers.Count)]);
                    UnlockableBrawlers.Remove(GlobalId.GetInstanceId(Brawler));
                    CharacterData Character = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(Brawler);
                    CardData Card = DataTables.Get(DataType.Card).GetData<CardData>(Character.Name + "_unlock");
                    GatchaDrop drop = new(1)
                    {
                        DataGlobalId = Character.GetGlobalId(),
                        Count = 1
                    };
                    unit.AddDrop(drop);
                    Avatar.ForcedDrops = CalcForcedDrop(true, Card.Rarity, "starpower", Avatar.ForcedDrops, "big");
                    //UnlockableBrawlers.Remove(Brawler);
                }

                if (UnlockableBrawlers.Count > 0 && !isBr)
                {
                    Avatar.ForcedDrops = CalcForcedDrop(false, "character", "starpower", Avatar.ForcedDrops, "big");
                }


                foreach (Hero hero in Avatar.Heroes)
                {
                    if (hero.PowerLevel >= 6)//== 8)
                    {
                        canDropSp = true;
                        break;
                    }
                }
                if (canDropSp)
                {
                    foreach (Hero hero in Avatar.Heroes)
                    {
                        if (hero.PowerLevel == 8)
                        {
                            CardData card = null;
                            CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                            card = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique");
                            CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_2");
                            CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_3");
                            if (card3 != null)
                            {
                                if (card3.LockedForChronos)
                                {
                                    card3 = null;
                                }
                            }
                            if (card3 == null)
                            {
                                if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                {
                                    sps.Add(card2.GetGlobalId());
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                {
                                    sps.Add(card.GetGlobalId());
                                }
                            }
                            else
                            {
                                if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    int r = rand.Next(0, 2);
                                    if (r == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else if (r == 1)
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    sps.Add(card3.GetGlobalId());
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    sps.Add(card.GetGlobalId());
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    sps.Add(card2.GetGlobalId());
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                }
                                else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                }
                                else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                {
                                    if (rand.Next(0, 1) == 0)
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                    else
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                }
                            }

                        }
                        if (hero.PowerLevel > 5)
                        {
                            string[] cards = { "GrowBush", "Shield", "Heal", "Jump", "ShootAround", "DestroyPet", "PetSlam", "Slow", "Push", "Dash", "SpeedBoost", "BurstHeal", "Spin", "Teleport", "Immunity", "Trail", "Totem", "Grab", "Swing", "Vision", "Regen", "HandGun", "Promote", "Sleep", "Slow", "Reload", "Reload", "Fake", "Trampoline", "Explode", "Blink", "PoisonTrigger", "Barrage", "Focus", "MineTrigger", "Reload", "Seeker", "Meteor", "HealPotion", "Stun", "TurretBuff", "StaticDamage" };
                            CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                            CardData WildCard = null;
                            foreach (string cardname in cards)
                            {
                                string n = char.ToUpper(cd.Name[0]) + cd.Name.Substring(1);
                                WildCard = DataTables.Get(DataType.Card).GetData<CardData>(n + "_" + cardname);
                                if (WildCard != null && !Avatar.Starpowers.Contains(WildCard.GetGlobalId()))
                                {
                                    sps.Add(WildCard.GetGlobalId());
                                }
                            }
                        }
                    }
                }
                if (canDropSp && sps.Count > 0)
                {
                    bool spdrop = false;
                    while (ProcChance(rand, 11 * 2, "starpower", Avatar.ForcedDrops) && sps.Count > 0)
                    {
                        spdrop = true;
                        int sp = sps[rand.Next(sps.Count)];
                        GatchaDrop drop = new(4)
                        {
                            CardGlobalId = sp,
                            Count = 1
                        };
                        unit.AddDrop(drop);
                        CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(sp);
                        CharacterData hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
                        Hero h = Avatar.GetHero(hero.GetGlobalId());
                        h.HasStarpower = true;
                        Avatar.ForcedDrops = CalcForcedDrop(true, "card_notchar", "starpower", Avatar.ForcedDrops, "big");
                        sps.Remove(sp);
                    }
                    if (sps.Count > 0 && !spdrop)
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(false, "card_notchar", "starpower", Avatar.ForcedDrops, "big");
                    }
                }

                while (ProcChance(rand, 20, "bonus", Avatar.ForcedDrops) && possibleBonuses.Count > 0) // add gems bonus
                {
                    Avatar.ForcedDrops = CalcForcedDrop(true, "bonus", "bonus", Avatar.ForcedDrops, "big");
                    int bonusType = possibleBonuses[rand.Next(0, possibleBonuses.Count)];
                    switch (bonusType)
                    {
                        case 0:
                            GatchaDrop gems = new(8)
                            {
                                Count = DiamondsReward[rand.Next(DiamondsReward.Count)]
                            };
                            unit.AddDrop(gems);
                            break;
                        case 1:
                            GatchaDrop tokens = new(2)
                            {
                                Count = TokenDoublersReward[rand.Next(TokenDoublersReward.Count)]
                            };
                            unit.AddDrop(tokens);
                            break;
                        case 2:
                            GatchaDrop tickets = new(3)
                            {
                                Count = TicketsReward[rand.Next(TicketsReward.Count)]
                            };
                            unit.AddDrop(tickets);
                            break;
                    }
                    possibleBonuses.Remove(bonusType);
                }
                if (possibleBonuses.Count == 3)
                {
                    Avatar.ForcedDrops = CalcForcedDrop(false, "bonus", "bonus", Avatar.ForcedDrops, "mega");
                }
            }
            else if (unit.Type == 11)
            {
                List<int> TokenDoublersReward = new() { 200, 400, 600 };
                List<int> TicketsReward = new() { 1, 2, 3, 5 };

                int s = 0;
                if (!lowluck)
                {
                    s = rand.Next(175, 750);
                }
                else
                {
                    s = rand.Next(20, 30);
                }

                GatchaDrop coins = new(7)
                {
                    Count = s
                };
                unit.AddDrop(coins);

                List<Hero> CanHavePowerPoints = new List<Hero>();

                foreach (Hero hero in Avatar.Heroes)
                {
                    if (hero.PowerLevel != 8)
                    {
                        if (hero.PowerPoints < 1410)
                        {
                            CanHavePowerPoints.Add(hero);
                        }
                        continue;
                    }
                }

                if (!lowluck)
                {
                    int count = CanHavePowerPoints.Count;
                    Dictionary<int, int> Avards = new();
                    for (int x = 0; Math.Min(count, 5) > x; x++)
                    {
                        Hero Brawler = CanHavePowerPoints[rand.Next(CanHavePowerPoints.Count)];
                        CanHavePowerPoints.Remove(Brawler);
                        int oldpp = Brawler.PowerPoints;
                        int PowerPoints = rand.Next(25, 250);
                        if (oldpp + PowerPoints > 1410)
                        {
                            PowerPoints = 1410 - oldpp;
                        }
                        Avards.Add(Brawler.CharacterId, PowerPoints);
                        //Brawler.PowerPoints += PowerPoints;
                    }
                    List<KeyValuePair<int, int>> list = Avards.ToList();
                    list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    Dictionary<int, int> FinalAwards = new Dictionary<int, int>();
                    foreach (KeyValuePair<int, int> pair in list)
                    {
                        FinalAwards.Add(pair.Key, pair.Value);
                    }
                    foreach (KeyValuePair<int, int> avard in FinalAwards)
                    {
                        GatchaDrop drop = new(6)
                        {
                            Count = avard.Value,
                            DataGlobalId = avard.Key
                        };
                        unit.AddDrop(drop);
                    }
                    int bonusChance = 0;
                    if (UnlockedBrawlers.Count < 3)
                    {
                        bonusChance = 7;
                    }
                    else if (UnlockedBrawlers.Count < 5)
                    {
                        bonusChance = 5;
                    }
                    else if (UnlockedBrawlers.Count < 7)
                    {
                        bonusChance = 3;
                    }
                    while (ProcChance(rand, 5 * 3 + bonusChance, "character", Avatar.ForcedDrops) && UnlockableBrawlers.Count > 0)
                    {
                        isBr = true;
                        int Brawler = GlobalId.CreateGlobalId(16, UnlockableBrawlers[rand.Next(UnlockableBrawlers.Count)]);
                        UnlockableBrawlers.Remove(GlobalId.GetInstanceId(Brawler));
                        CharacterData Character = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(Brawler);
                        CardData Card = DataTables.Get(DataType.Card).GetData<CardData>(Character.Name + "_unlock");
                        GatchaDrop drop = new(1)
                        {
                            DataGlobalId = Character.GetGlobalId(),
                            Count = 1
                        };
                        unit.AddDrop(drop);
                        Avatar.ForcedDrops = CalcForcedDrop(true, Card.Rarity, "starpower", Avatar.ForcedDrops, "mega");
                        //UnlockableBrawlers.Remove(Brawler);
                    }

                    if (UnlockableBrawlers.Count > 0 && !isBr)
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(false, "character", "starpower", Avatar.ForcedDrops, "mega");
                    }


                    foreach (Hero hero in Avatar.Heroes)
                    {
                        if (hero.PowerLevel >= 6)//== 8)
                        {
                            canDropSp = true;
                            break;
                        }
                    }
                    if (canDropSp)
                    {
                        foreach (Hero hero in Avatar.Heroes)
                        {
                            if (hero.PowerLevel == 8)
                            {
                                CardData card = null;
                                CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                                card = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique");
                                CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_2");
                                CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_3");
                                if (card3 != null)
                                {
                                    if (card3.LockedForChronos)
                                    {
                                        card3 = null;
                                    }
                                }
                                if (card3 == null)
                                {
                                    if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                    {
                                        if (rand.Next(0, 1) == 0)
                                        {
                                            sps.Add(card.GetGlobalId());
                                        }
                                        else
                                        {
                                            sps.Add(card2.GetGlobalId());
                                        }
                                    }
                                    else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                    else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()))
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                }
                                else
                                {
                                    if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        int r = rand.Next(0, 2);
                                        if (r == 0)
                                        {
                                            sps.Add(card.GetGlobalId());
                                        }
                                        else if (r == 1)
                                        {
                                            sps.Add(card2.GetGlobalId());
                                        }
                                        else
                                        {
                                            sps.Add(card3.GetGlobalId());
                                        }
                                    }
                                    else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        sps.Add(card3.GetGlobalId());
                                    }
                                    else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        sps.Add(card.GetGlobalId());
                                    }
                                    else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        sps.Add(card2.GetGlobalId());
                                    }
                                    else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        if (rand.Next(0, 1) == 0)
                                        {
                                            sps.Add(card.GetGlobalId());
                                        }
                                        else
                                        {
                                            sps.Add(card2.GetGlobalId());
                                        }
                                    }
                                    else if (!Avatar.Starpowers.Contains(card.GetGlobalId()) && Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        if (rand.Next(0, 1) == 0)
                                        {
                                            sps.Add(card.GetGlobalId());
                                        }
                                        else
                                        {
                                            sps.Add(card3.GetGlobalId());
                                        }
                                    }
                                    else if (Avatar.Starpowers.Contains(card.GetGlobalId()) && !Avatar.Starpowers.Contains(card2.GetGlobalId()) && !Avatar.Starpowers.Contains(card3.GetGlobalId()))
                                    {
                                        if (rand.Next(0, 1) == 0)
                                        {
                                            sps.Add(card2.GetGlobalId());
                                        }
                                        else
                                        {
                                            sps.Add(card3.GetGlobalId());
                                        }
                                    }
                                }

                            }
                            if (hero.PowerLevel > 5)
                            {
                                string[] cards = { "GrowBush", "Shield", "Heal", "Jump", "ShootAround", "DestroyPet", "PetSlam", "Slow", "Push", "Dash", "SpeedBoost", "BurstHeal", "Spin", "Teleport", "Immunity", "Trail", "Totem", "Grab", "Swing", "Vision", "Regen", "HandGun", "Promote", "Sleep", "Slow", "Reload", "Reload", "Fake", "Trampoline", "Explode", "Blink", "PoisonTrigger", "Barrage", "Focus", "MineTrigger", "Reload", "Seeker", "Meteor", "HealPotion", "Stun", "TurretBuff", "StaticDamage" };
                                CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                                CardData WildCard = null;
                                foreach (string cardname in cards)
                                {
                                    string n = char.ToUpper(cd.Name[0]) + cd.Name.Substring(1);
                                    WildCard = DataTables.Get(DataType.Card).GetData<CardData>(n + "_" + cardname);
                                    if (WildCard != null && !Avatar.Starpowers.Contains(WildCard.GetGlobalId()))
                                    {
                                        sps.Add(WildCard.GetGlobalId());
                                    }
                                }
                            }
                        }
                    }
                    if (canDropSp && sps.Count > 0)
                    {
                        bool spdrop = false;
                        while (ProcChance(rand, 11 * 3, "starpower", Avatar.ForcedDrops) && sps.Count > 0)
                        {
                            spdrop = true;
                            int sp = sps[rand.Next(sps.Count)];
                            GatchaDrop drop = new(4)
                            {
                                CardGlobalId = sp,
                                Count = 1
                            };
                            unit.AddDrop(drop);
                            CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(sp);
                            CharacterData hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
                            Hero h = Avatar.GetHero(hero.GetGlobalId());
                            h.HasStarpower = true;
                            Avatar.ForcedDrops = CalcForcedDrop(true, "card_notchar", "starpower", Avatar.ForcedDrops, "mega");
                            sps.Remove(sp);
                        }
                        if (sps.Count > 0 && !spdrop)
                        {
                            Avatar.ForcedDrops = CalcForcedDrop(false, "card_notchar", "starpower", Avatar.ForcedDrops, "mega");
                        }
                    }

                    while (ProcChance(rand, 30, "bonus", Avatar.ForcedDrops) && possibleBonuses.Count > 0) // add gems bonus
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(true, "bonus", "bonus", Avatar.ForcedDrops, "mega");
                        int bonusType = possibleBonuses[rand.Next(0, possibleBonuses.Count)];
                        switch (bonusType)
                        {
                            case 0:
                                GatchaDrop gems = new(8)
                                {
                                    Count = DiamondsReward[rand.Next(DiamondsReward.Count)]
                                };
                                unit.AddDrop(gems);
                                break;
                            case 1:
                                GatchaDrop tokens = new(2)
                                {
                                    Count = TokenDoublersReward[rand.Next(TokenDoublersReward.Count)]
                                };
                                unit.AddDrop(tokens);
                                break;
                            case 2:
                                GatchaDrop tickets = new(3)
                                {
                                    Count = TicketsReward[rand.Next(TicketsReward.Count)]
                                };
                                unit.AddDrop(tickets);
                                break;
                        }
                        possibleBonuses.Remove(bonusType);
                    }
                    if (possibleBonuses.Count == 3)
                    {
                        Avatar.ForcedDrops = CalcForcedDrop(false, "bonus", "bonus", Avatar.ForcedDrops, "mega");
                    }
                }
                else
                {
                    int count = CanHavePowerPoints.Count;
                    Dictionary<int, int> Avards = new();
                    for (int x = 0; Math.Min(count, 5) > x; x++)
                    {
                        Hero Brawler = CanHavePowerPoints[rand.Next(CanHavePowerPoints.Count)];
                        CanHavePowerPoints.Remove(Brawler);
                        int oldpp = Brawler.PowerPoints;
                        int PowerPoints = rand.Next(5, 20);
                        if (oldpp + PowerPoints > 1410)
                        {
                            PowerPoints = 1410 - oldpp;
                        }
                        Avards.Add(Brawler.CharacterId, PowerPoints);
                        //Brawler.PowerPoints += PowerPoints;
                    }
                    List<KeyValuePair<int, int>> list = Avards.ToList();
                    list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    Dictionary<int, int> FinalAwards = new Dictionary<int, int>();
                    foreach (KeyValuePair<int, int> pair in list)
                    {
                        FinalAwards.Add(pair.Key, pair.Value);
                    }
                    foreach (KeyValuePair<int, int> avard in FinalAwards)
                    {
                        GatchaDrop drop = new(6)
                        {
                            Count = avard.Value,
                            DataGlobalId = avard.Key
                        };
                        unit.AddDrop(drop);
                    }
                }
            }

        }

        public void Enter(DateTime dateTime)
        {
            Home.HomeVisited();
            Home.Tick();
        }

        public void ClientTurnReceived(int tick, int checksum, List<Command> commands)
        {
            Home.Tick();
        }
    }
}
