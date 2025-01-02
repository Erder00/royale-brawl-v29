namespace Supercell.Laser.Server.Message
{
    using System.Diagnostics;
    using System.Linq;
    using MySql.Data.MySqlClient;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Avatar.Structures;
    using Supercell.Laser.Logic.Battle;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Club;
    using Supercell.Laser.Logic.Command;
    using Supercell.Laser.Logic.Command.Avatar;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Friends;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Quest;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message;
    using Supercell.Laser.Logic.Message.Account;
    using Supercell.Laser.Logic.Message.Account.Auth;
    using Supercell.Laser.Logic.Message.Battle;
    using Supercell.Laser.Logic.Message.Club;
    using Supercell.Laser.Logic.Message.Friends;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Logic.Message.Latency;
    using Supercell.Laser.Logic.Message.Ranking;
    using Supercell.Laser.Logic.Message.Security;
    using Supercell.Laser.Logic.Message.Team;
    using Supercell.Laser.Logic.Message.Team.Stream;
    using Supercell.Laser.Logic.Message.Udp;
    using Supercell.Laser.Logic.Stream.Entry;
    using Supercell.Laser.Logic.Team;
    using Supercell.Laser.Logic.Team.Stream;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Server.Database;
    using Supercell.Laser.Server.Database.Models;
    using Supercell.Laser.Server.Logic;
    using Supercell.Laser.Server.Logic.Game;
    using Supercell.Laser.Server.Networking;
    using Supercell.Laser.Server.Networking.Security;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Server.Networking.UDP.Game;
    using Supercell.Laser.Server.Settings;

    public class MessageManager
    {
        public Connection Connection { get; }

        public HomeMode HomeMode;

        public CommandManager CommandManager;

        private DateTime LastKeepAlive;

        public MessageManager(Connection connection)
        {
            Connection = connection;
            LastKeepAlive = DateTime.UtcNow;
        }


        public bool IsAlive()
        {
            return (int)(DateTime.UtcNow - LastKeepAlive).TotalSeconds < 30;
        }
        public string GetPingIconByMs(int ms)
        {
            string str = "▂   ";
            if (ms <= 75)
            {
                str = "▂▄▆█";
            }
            else if (ms <= 125)
            {
                str = "▂▄▆ ";
            }
            else if (ms <= 300)
            {
                str = "▂▄  ";
            }
            return str;
        }

        public void ShowLobbyInfo()
        {
            DateTime startTime = Process.GetCurrentProcess().StartTime;
            DateTime now = DateTime.Now;
            TimeSpan uptime = now - startTime;
            string formattedUptime = string.Format(
                "{0}{1}{2}{3}",
                uptime.Days > 0 ? $"{uptime.Days} Days, " : string.Empty,
                uptime.Hours > 0 || uptime.Days > 0 ? $"{uptime.Hours} Hours, " : string.Empty,
                uptime.Minutes > 0 || uptime.Hours > 0 ? $"{uptime.Minutes} Minutes, " : string.Empty,
                uptime.Seconds > 0 ? $"{uptime.Seconds} Seconds" : string.Empty
            );
            string abd = $"Connection: {GetPingIconByMs(0)} (---ms)\n";
            if (Connection.Ping != 0)
            {
                abd = $"Connection: {GetPingIconByMs(Connection.Ping)} ({Connection.Ping}ms)\n";
            }

            bool hasPremium = HomeMode.Home.PremiumEndTime >= DateTime.UtcNow;

            LobbyInfoMessage b = new()
            {
                LobbyData = $"<cff001f>R<cff003f>o<cff005f>y<cff007f>a<cff009f>l<cff00bf>e<cff00df> <cff00ff>B<cdf00ff>r<cbf00ff>a<c9f00ff>w<c7f00ff>l<c5f00ff> <c3f00ff>v<c1f00ff>2<c0000ff>9</c>\n<c001cff>g<c0038ff>i<c0055ff>t<c0071ff>h<c008dff>u<c00aaff>b<c00c6ff>.<c00e2ff>c<c00ffff>o<c00ffe2>m<c00ffc6>/<c00ffa9>e<c00ff8d>r<c00ff71>d<c00ff54>e<c00ff38>r<c00ff1c>0<c00ff00>0</c>\n{abd}Players Online: {Sessions.Count}\nUptime: {formattedUptime}\nPremium: {hasPremium}\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\nhi'",
                PlayersCount = 0
            };
            Connection.Send(b);
        }

        public void ReceiveMessage(GameMessage message)
        {
            switch (message.GetMessageType())
            {
                case 10100:
                    ClientHelloReceived((ClientHelloMessage)message);
                    break;
                case 10101:
                    LoginReceived((AuthenticationMessage)message);
                    break;
                case 10107:
                    ClientCapabilitesReceived((ClientCapabilitiesMessage)message);
                    //Connection.Send(new StartLatencyTestRequestMessage());
                    break;
                case 10108:
                    LastKeepAlive = DateTime.UtcNow;
                    Connection.Send(new KeepAliveServerMessage());
                    ShowLobbyInfo();
                    if (!Sessions.IsSessionActive(HomeMode.Avatar.AccountId))
                    {
                        Sessions.Create(HomeMode, Connection);
                    }
                    break;
                case 10110:
                    ShowLobbyInfo();
                    break;
                case 10119:
                    ReportAllianceStreamReceived((ReportAllianceStreamMessage)message);
                    break;
                case 10212:
                    ChangeName((ChangeAvatarNameMessage)message);
                    break;
                case 10177:
                    ClientInfoReceived((ClientInfoMessage)message);
                    break;
                case 10501:
                    AcceptFriendReceived((AcceptFriendMessage)message);
                    break;
                case 10502:
                    AddFriendReceived((AddFriendMessage)message);
                    break;
                case 10504:
                    AskForFriendListReceived((AskForFriendListMessage)message);
                    break;
                case 10506:
                    RemoveFriendReceived((RemoveFriendMessage)message);
                    break;
                case 10576:
                    SetBlockFriendRequestsReceived((SetBlockFriendRequestsMessage)message);
                    break;
                case 10555:
                    break;
                case 14101:
                    GoHomeReceived((GoHomeMessage)message);
                    break;
                case 14102:
                    EndClientTurnReceived((EndClientTurnMessage)message);
                    break;
                case 14103:
                    MatchmakeRequestReceived((MatchmakeRequestMessage)message);
                    break;
                case 14104:
                    StartSpectateReceived((StartSpectateMessage)message);
                    break;
                case 14106:
                    CancelMatchMaking((CancelMatchmakingMessage)message);
                    break;
                case 14107:
                    StopSpectateReceived((StopSpectateMessage)message);
                    break;
                case 14109:
                    GoHomeFromOfflinePractiseReceived((GoHomeFromOfflinePractiseMessage)message);
                    break;
                case 14110:
                    AskForBattleEndReceived((AskForBattleEndMessage)message);
                    break;
                case 14113:
                    GetPlayerProfile((GetPlayerProfileMessage)message);
                    break;
                case 14166:
                    break;
                case 14277:
                    GetSeasonRewardsReceived((GetSeasonRewardsMessage)message);
                    break;
                case 14301:
                    CreateAllianceReceived((CreateAllianceMessage)message);
                    break;
                case 14302:
                    AskForAllianceDataReceived((AskForAllianceDataMessage)message);
                    break;
                case 14303:
                    AskForJoinableAllianceListReceived((AskForJoinableAllianceListMessage)message);
                    break;
                case 14305:
                    JoinAllianceReceived((JoinAllianceMessage)message);
                    break;
                case 14306:
                    ChangeAllianceMemberRoleReceived((ChangeAllianceMemberRoleMessage)message);
                    break;
                case 14307:
                    KickAllianceMemberReceived((KickAllianceMemberMessage)message);
                    break;
                case 14308:
                    LeaveAllianceReceived((LeaveAllianceMessage)message);
                    break;
                case 14315:
                    ChatToAllianceStreamReceived((ChatToAllianceStreamMessage)message);
                    break;
                case 14316:
                    ChangeAllianceSettingsReceived((ChangeAllianceSettingsMessage)message);
                    break;
                case 14330:
                    SendAllianceMailMessage((SendAllianceMailMessage)message);
                    break;
                case 14350:
                    TeamCreateReceived((TeamCreateMessage)message);
                    break;
                case 14353:
                    TeamLeaveReceived((TeamLeaveMessage)message);
                    break;
                case 14354:
                    TeamChangeMemberSettingsReceived((TeamChangeMemberSettingsMessage)message);
                    break;
                case 14355:
                    TeamSetMemberReadyReceived((TeamSetMemberReadyMessage)message);
                    break;
                case 14358:
                    TeamSpectateMessageReceived((TeamSpectateMessage)message);
                    break;
                case 14359:
                    TeamChatReceived((TeamChatMessage)message);
                    break;
                case 14361:
                    TeamMemberStatusReceived((TeamMemberStatusMessage)message);
                    ShowLobbyInfo();
                    break;
                case 14362:
                    TeamSetEventReceived((TeamSetEventMessage)message);
                    break;
                case 14363:
                    TeamSetLocationReceived((TeamSetLocationMessage)message);
                    break;
                case 14365:
                    TeamInviteReceived((TeamInviteMessage)message);
                    break;
                case 14366:
                    PlayerStatusReceived((PlayerStatusMessage)message);
                    ShowLobbyInfo();
                    break;
                case 14367:
                    TeamClearInviteMessageReceived((TeamClearInviteMessage)message);
                    break;
                case 14369:
                    TeamPremadeChatReceived((TeamPremadeChatMessage)message);
                    break;
                case 14370:
                    TeamInviteReceived((TeamInviteMessage)message);
                    break;
                case 14403:
                    GetLeaderboardReceived((GetLeaderboardMessage)message);
                    break;
                case 14479:
                    TeamInvitationResponseReceived((TeamInvitationResponseMessage)message);
                    break;
                case 14600:
                    AvatarNameCheckRequestReceived((AvatarNameCheckRequestMessage)message);
                    break;
                case 14777:
                    SetInvitesBlockedReceived((SetInvitesBlockedMessage)message);
                    break;
                case 18686:
                    SetSupportedCreator((SetSupportedCreatorMessage)message);
                    break;
                case 19001:
                    LatencyTestResultReceived((LatencyTestResultMessage)message);
                    break;
                    //default:
                    //    Logger.Print($"MessageManager::ReceiveMessage - no case for {message.GetType().Name} ({message.GetMessageType()})");
                    //    break;
            }
        }

        private void TeamSpectateMessageReceived(TeamSpectateMessage message)
        {
            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;
            HomeMode.Avatar.TeamId = team.Id;
            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.HasPremiumPass, HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);

            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.HeroLevel = hero.PowerLevel;
            if (hero.HasStarpower)
            {
                CardData card = null;
                CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                card = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique");
                CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_2");
                if (HomeMode.Avatar.SelectedStarpowers.Contains(card.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card.GetGlobalId();
                }
                else if (HomeMode.Avatar.SelectedStarpowers.Contains(card2.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card2.GetGlobalId();
                }
                else if (HomeMode.Avatar.Starpowers.Contains(card.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card.GetGlobalId();
                }
                else if (HomeMode.Avatar.Starpowers.Contains(card2.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card2.GetGlobalId();
                }
            }
            else
            {
                member.Starpower = 0;
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
                    if (WildCard != null)
                    {
                        if (HomeMode.Avatar.Starpowers.Contains(WildCard.GetGlobalId()))
                        {
                            member.Gadget = WildCard.GetGlobalId();
                            break;
                        }

                    }
                }
            }
            else
            {
                member.Gadget = 0;
            }
            member.SkinId = GlobalId.CreateGlobalId(29, HomeMode.Home.SelectedSkins[GlobalId.GetInstanceId(HomeMode.Home.CharacterId)]);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;

            member.IsOwner = false;
            member.State = 2;
            team.Members.Add(member);
            team.TeamUpdated();
        }

        private void SetBlockFriendRequestsReceived(SetBlockFriendRequestsMessage message)
        {
            //HomeMode.Home.BlockFriendRequests = message.State;
        }

        private void ReportAllianceStreamReceived(ReportAllianceStreamMessage message)
        {
            if (HomeMode.Avatar.AllianceId < 0) return;
            Alliance myAlliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (myAlliance == null) return;
            if (HomeMode.Home.ReportsIds.Count > 5)
            {
                Connection.Send(new ReportUserStatusMessage()
                {
                    Status = 2
                });
                return;
            }
            long index = 0;
            foreach (AllianceStreamEntry e in myAlliance.Stream.GetEntries())
            {
                index++;
                if (e.Id == message.MessageIndex)
                {
                    if (HomeMode.Home.ReportsIds.Contains(e.AuthorId))
                    {
                        Connection.Send(new ReportUserStatusMessage()
                        {
                            Status = 3
                        });
                        return;
                    }
                    string reporterTag = LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId);
                    string reporterName = HomeMode.Avatar.Name;
                    string susTag = LogicLongCodeGenerator.ToCode(e.AuthorId);
                    string susName = e.AuthorName;
                    HomeMode.Home.ReportsIds.Add(e.AuthorId);
                    string text = "";
                    try { text += myAlliance.Stream.GetEntries()[index - 5].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index - 5].AuthorId) + " " + myAlliance.Stream.GetEntries()[index - 5].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index - 5].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index - 4].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index - 4].AuthorId) + " " + myAlliance.Stream.GetEntries()[index - 4].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index - 4].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index - 3].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index - 3].AuthorId) + " " + myAlliance.Stream.GetEntries()[index - 3].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index - 3].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index - 2].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index - 2].AuthorId) + " " + myAlliance.Stream.GetEntries()[index - 2].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index - 2].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index - 1].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index - 1].AuthorId) + " " + myAlliance.Stream.GetEntries()[index - 1].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index - 1].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index - 0].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index - 0].AuthorId) + " " + myAlliance.Stream.GetEntries()[index - 0].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index - 0].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index + 1].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index + 1].AuthorId) + " " + myAlliance.Stream.GetEntries()[index + 1].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index + 1].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index + 2].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index + 2].AuthorId) + " " + myAlliance.Stream.GetEntries()[index + 2].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index + 2].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index + 3].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index + 3].AuthorId) + " " + myAlliance.Stream.GetEntries()[index + 3].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index + 3].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index + 4].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index + 4].AuthorId) + " " + myAlliance.Stream.GetEntries()[index + 4].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index + 4].Message + "\n"; } catch { }
                    try { text += myAlliance.Stream.GetEntries()[index + 5].SendTime + " " + LogicLongCodeGenerator.ToCode(myAlliance.Stream.GetEntries()[index + 5].AuthorId) + " " + myAlliance.Stream.GetEntries()[index + 5].AuthorName + ">>> " + myAlliance.Stream.GetEntries()[index + 5].Message + "\n"; } catch { }
                    Logger.HandleReport($"Player {reporterName}, {reporterTag} reported player {susName}, {susTag}, in this msgs:\n`\n{text}`");
                    Connection.Send(new ReportUserStatusMessage()
                    {
                        Status = 1
                    });
                    break;
                }
            }
        }
        private void GetSeasonRewardsReceived(GetSeasonRewardsMessage message)
        {
            Connection.Send(new SeasonRewardsMessage());
        }
        private void addNotifToAllAccounts(string message, long club)
        {
            var allAccounts = Accounts.GetRankingList();
            foreach (var account in allAccounts)
            {
                string accountId = LogicLongCodeGenerator.ToCode(account.AccountId);
                addNotif(accountId, message, club);
            }
        }

        private void addNotif(string id, string message, long club)
        {
            long player = LogicLongCodeGenerator.ToId(id);
            Account targetAccount = Accounts.Load(player);
            if (targetAccount.Avatar.AllianceId == club)
            {
                if (targetAccount == null)
                {
                    Logger.Error($"Fail: account not found for ID {id}!");
                    return;
                }

                Account acc = new Account();

                Notification nGems = new Notification
                {
                    //Sender = Accounts.Load(HomeMode.Avatar.AccountId).Avatar.Name,
                    Id = 81,
                    MessageEntry = $"{message}"
                };
                targetAccount.Home.NotificationFactory.Add(nGems);
                LogicAddNotificationCommand acmGems = new()
                {
                    Notification = nGems
                };
                AvailableServerCommandMessage asmGems = new AvailableServerCommandMessage();
                asmGems.Command = acmGems;
                if (Sessions.IsSessionActive(player))
                {
                    var sessionGems = Sessions.GetSession(player);
                    sessionGems.GameListener.SendTCPMessage(asmGems);
                }
            }
        }
        private void SetInvitesBlockedReceived(SetInvitesBlockedMessage message)
        {
            HomeMode.Avatar.DoNotDisturb = message.State;
            LogicInviteBlockingChangedCommand command = new LogicInviteBlockingChangedCommand();
            command.State = message.State;
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command;
            Connection.Send(serverCommandMessage);
            if (HomeMode.Avatar.AllianceId > 0)
            {
                Alliance a = Alliances.Load(HomeMode.Avatar.AllianceId);
                AllianceMember m = a.GetMemberById(HomeMode.Avatar.AccountId);
                m.DoNotDisturb = message.State;
                AllianceDataMessage dataMessage = new AllianceDataMessage()
                {
                    Alliance = a,
                    IsMyAlliance = true,
                };
                a.SendToAlliance(dataMessage);
            }
        }

        private void SetSupportedCreator(SetSupportedCreatorMessage message) // credits: tale brawl team
        {
            string[] validCreators = Configuration.Instance.CreatorCodes.Split(',');

            if (validCreators.Contains(message.Creator))
            {
                HomeMode.Avatar.SupportedCreator = message.Creator;
                LogicSetSupportedCreatorCommand response = new()
                {
                    Name = message.Creator
                };
                AvailableServerCommandMessage msg = new AvailableServerCommandMessage();
                msg.Command = response;

                Connection.Send(msg);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(message.Creator))
                {
                    HomeMode.Avatar.SupportedCreator = message.Creator;
                    LogicSetSupportedCreatorCommand response = new()
                    {
                        Name = message.Creator
                    };
                    //Console.WriteLine(message.Creator);
                    AvailableServerCommandMessage msg = new AvailableServerCommandMessage();
                    msg.Command = response;

                    Connection.Send(msg);
                }
                else
                {
                    SetSupportedCreatorResponse response = new SetSupportedCreatorResponse();
                    Connection.Send(response);
                }
            }
        }
        private void LatencyTestResultReceived(LatencyTestResultMessage message)
        {
            LatencyTestStatusMessage l = new()
            {
                Ping = Connection.Ping
            };
            Connection.Send(l);
        }
        private void TeamPremadeChatReceived(TeamPremadeChatMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            QuickChatStreamEntry entry = new QuickChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.TargetId = message.TargetId;
            entry.Name = HomeMode.Avatar.Name;

            if (message.TargetId > 0)
            {
                TeamMember member = team.GetMember(message.TargetId);
                if (member != null)
                {
                    entry.TargetPlayerName = member.DisplayData.Name;
                }
            }

            entry.MessageDataId = message.MessageDataId;
            entry.Unknown1 = message.Unknown1;
            entry.Unknown2 = message.Unknown2;

            team.AddStreamEntry(entry);
        }

        private void TeamChatReceived(TeamChatMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;
            if (HomeMode.Avatar.IsCommunityBanned) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            ChatStreamEntry entry = new ChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.Name = HomeMode.Avatar.Name;
            entry.Message = message.Message;

            team.AddStreamEntry(entry);
        }

        private void AvatarNameCheckRequestReceived(AvatarNameCheckRequestMessage message)
        {
            LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
            command.Name = message.Name;
            command.ChangeNameCost = 0;
            command.Execute(HomeMode);
            if (HomeMode.Avatar.AllianceId >= 0)
            {
                Alliance a = Alliances.Load(HomeMode.Avatar.AllianceId);
                if (a == null) return;
                AllianceMember m = a.GetMemberById(HomeMode.Avatar.AccountId);
                m.DisplayData.Name = message.Name;
            }
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command;
            Connection.Send(serverCommandMessage);
        }

        private void TeamSetEventReceived(TeamSetEventMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            if (message.EventSlot == 2) return;

            EventData data = Events.GetEvent(message.EventSlot);
            if (data == null) return;

            team.EventSlot = message.EventSlot;
            team.LocationId = data.LocationId;
            team.TeamUpdated();
        }

        private BattleMode SpectatedBattle;
        private void StopSpectateReceived(StopSpectateMessage message)
        {
            if (SpectatedBattle != null)
            {
                SpectatedBattle.RemoveSpectator(Connection.UdpSessionId);
                SpectatedBattle = null;
            }

            if (Connection.Home != null && Connection.Avatar != null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void StartSpectateReceived(StartSpectateMessage message)
        {
            Account data = Accounts.Load(message.AccountId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;
            long battleId = avatar.BattleId;

            BattleMode battle = Battles.Get(battleId);
            if (battle == null) return;

            SpectatedBattle = battle;
            UDPSocket socket = UDPGateway.CreateSocket();
            socket.Battle = battle;
            socket.IsSpectator = true;
            socket.TCPConnection = Connection;
            Connection.UdpSessionId = socket.SessionId;
            battle.AddSpectator(socket.SessionId, new UDPGameListener(socket, Connection));

            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.LocationId = battle.Location.GetGlobalId();
            startLoading.TeamIndex = 0;
            startLoading.OwnIndex = 0;
            startLoading.GameMode = battle.GetGameModeVariation() == 6 ? 6 : 1;
            startLoading.Players.AddRange(battle.GetPlayers());
            startLoading.SpectateMode = 1;

            Connection.Send(startLoading);

            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerPort = Configuration.Instance.Port;
            Connection.Send(info);
        }

        private void GoHomeFromOfflinePractiseReceived(GoHomeFromOfflinePractiseMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                if (Connection.Avatar.IsTutorialState())
                {
                    Connection.Avatar.SkipTutorial();
                }
                Connection.Home.Events = Events.GetEventsById(HomeMode.Home.PowerPlayGamesPlayed, Connection.Avatar.AccountId);

                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                ShowLobbyInfo();
                Connection.Send(ohd);
            }
        }

        private void TeamSetLocationReceived(TeamSetLocationMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            team.Type = 1;
            team.TeamUpdated();
        }

        private void ChangeAllianceSettingsReceived(ChangeAllianceSettingsMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            if (HomeMode.Avatar.AllianceRole != AllianceRole.Leader) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
            {
                alliance.AllianceBadgeId = message.BadgeId;
            }
            else
            {
                alliance.AllianceBadgeId = 8000000;
            }

            alliance.Description = message.Description;
            alliance.RequiredTrophies = message.RequiredTrophies;

            Connection.Send(new AllianceResponseMessage()
            {
                ResponseType = 10
            });

            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);
        }
        private void SendAllianceMailMessage(SendAllianceMailMessage message)
        {
            SendAllianceMailMessage sendAllianceMailMessage = message;

            if (HomeMode.Avatar.AllianceRole != AllianceRole.Leader && HomeMode.Avatar.AllianceRole != AllianceRole.CoLeader) return;
            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                if (HomeMode.Avatar.AllianceRole != AllianceRole.Leader && HomeMode.Avatar.AllianceRole != AllianceRole.CoLeader) return;
                if (message.Text.Length > 450) return;
                addNotifToAllAccounts(message.Text, HomeMode.Avatar.AllianceId);

                AllianceResponseMessage responseMessages = new AllianceResponseMessage();
                responseMessages.ResponseType = 113;
                Connection.Send(responseMessages);
            }

            AllianceResponseMessage responseMessage = new AllianceResponseMessage();
            responseMessage.ResponseType = 114;
            Connection.Send(responseMessage);

            Connection.Send(sendAllianceMailMessage);
        }
        private void KickAllianceMemberReceived(KickAllianceMemberMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            AllianceMember member = alliance.GetMemberById(message.AccountId);
            if (member == null) return;

            ClientAvatar avatar = Accounts.Load(message.AccountId).Avatar;

            if (HomeMode.Avatar.AllianceRole <= avatar.AllianceRole) return;

            alliance.Members.Remove(member);
            avatar.AllianceId = -1;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = avatar.AccountId;
            entry.PlayerName = avatar.Name;
            entry.Type = 4;
            entry.Event = 1; // kicked
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 70;
            Connection.Send(response);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new AllianceResponseMessage()
                {
                    ResponseType = 100
                });
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new MyAllianceMessage());
            }
        }

        private void TeamSetMemberReadyReceived(TeamSetMemberReadyMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.IsReady = message.IsReady;

            team.TeamUpdated();

            //if (team.IsEveryoneReady())
            // {
            //Teams.StartGame(team);
            //}
        }

        private void TeamChangeMemberSettingsReceived(TeamChangeMemberSettingsMessage message)
        {
            ;
        }

        private void TeamMemberStatusReceived(TeamMemberStatusMessage message)
        {
            if (HomeMode == null) return;
            if (message.Status < 0) return;
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.State = message.Status;
            team.TeamUpdated();
        }

        private void TeamInvitationResponseReceived(TeamInvitationResponseMessage message)
        {
            bool isAccept = message.Response == 1;

            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;

            TeamInviteEntry invite = team.GetInviteById(HomeMode.Avatar.AccountId);
            if (invite == null) return;

            team.Invites.Remove(invite);

            if (isAccept)
            {
                TeamMember member = new TeamMember();
                member.AccountId = HomeMode.Avatar.AccountId;
                member.CharacterId = HomeMode.Home.CharacterId;
                member.DisplayData = new PlayerDisplayData(HomeMode.Home.HasPremiumPass, HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);

                Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                member.HeroTrophies = hero.Trophies;
                member.HeroHighestTrophies = hero.HighestTrophies;
                member.HeroLevel = hero.PowerLevel;
                member.IsOwner = false;
                member.State = 0;
                team.Members.Add(member);

                HomeMode.Avatar.TeamId = team.Id;
            }

            team.TeamUpdated();
        }

        private TeamEntry GetTeam()
        {
            return Teams.Get(HomeMode.Avatar.TeamId);
        }

        private void TeamInviteReceived(TeamInviteMessage message)
        {
            TeamEntry team = GetTeam();
            if (team == null) return;

            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            TeamInviteEntry entry = new TeamInviteEntry();
            entry.Slot = message.Team;
            entry.Name = data.Avatar.Name;
            entry.Id = message.AvatarId;
            entry.InviterId = HomeMode.Avatar.AccountId;

            team.Invites.Add(entry);

            team.TeamUpdated();

            LogicGameListener gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);
            if (gameListener != null)
            {
                TeamInvitationMessage teamInvitationMessage = new TeamInvitationMessage();
                teamInvitationMessage.TeamId = team.Id;

                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.HasPremiumPass, HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                friendEntry.Trophies = HomeMode.Avatar.Trophies;
                teamInvitationMessage.Unknown = 1;
                teamInvitationMessage.FriendEntry = friendEntry;

                gameListener.SendTCPMessage(teamInvitationMessage);
            }
        }

        private void TeamClearInviteMessageReceived(TeamClearInviteMessage message)
        {
            TeamEntry team = GetTeam();
            if (team == null) return;

            TeamInviteEntry inviteToRemove = team.Invites.FirstOrDefault(invite => invite.Slot == message.Slot);
            if (inviteToRemove != null)
            {
                team.Invites.Remove(inviteToRemove);
                team.TeamUpdated();
            }
        }

        private void TeamLeaveReceived(TeamLeaveMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);

            if (team == null)
            {
                Logger.Print("TeamLeave - Team is NULL!");
                HomeMode.Avatar.TeamId = -1;
                Connection.Send(new TeamLeftMessage());
                return;
            }

            TeamMember entry = team.GetMember(HomeMode.Avatar.AccountId);

            if (entry == null) return;
            HomeMode.Avatar.TeamId = -1;

            team.Members.Remove(entry);

            Connection.Send(new TeamLeftMessage());
            team.TeamUpdated();

            if (team.Members.Count == 0)
            {
                Teams.Remove(team.Id);
            }
        }

        private void TeamCreateReceived(TeamCreateMessage message)
        {
            TeamEntry team = Teams.Create();

            team.Type = message.TeamType;
            team.LocationId = Events.GetEvents()[0].LocationId;

            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.HasPremiumPass, HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);

            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.HeroLevel = hero.PowerLevel;
            if (hero.HasStarpower)
            {
                CardData card = null;
                CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                card = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique");
                CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unique_2");
                if (HomeMode.Avatar.SelectedStarpowers.Contains(card.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card.GetGlobalId();
                }
                else if (HomeMode.Avatar.SelectedStarpowers.Contains(card2.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card2.GetGlobalId();
                }
                else if (HomeMode.Avatar.Starpowers.Contains(card.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card.GetGlobalId();
                }
                else if (HomeMode.Avatar.Starpowers.Contains(card2.GetGlobalId()))
                {
                    member.HeroLevel = 9;
                    member.Starpower = card2.GetGlobalId();
                }
            }
            else
            {
                member.Starpower = 0;
            }
            if (hero.PowerLevel > 5)
            {
                string[] cards = { "GrowBush", "Shield", "Heal", "Jump", "ShootAround", "DestroyPet", "PetSlam", "Slow", "Push", "Dash", "SpeedBoost", "BurstHeal", "Spin", "Teleport", "Immunity", "Trail", "Totem", "Grab", "Swing", "Vision", "Regen", "HandGun", "Promote", "Sleep", "Slow", "Reload", "Reload", "Fake", "Trampoline", "Explode" };
                CharacterData cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId);
                CardData WildCard = null;
                foreach (string cardname in cards)
                {
                    string n = char.ToUpper(cd.Name[0]) + cd.Name.Substring(1);
                    WildCard = DataTables.Get(DataType.Card).GetData<CardData>(n + "_" + cardname);
                    if (WildCard != null)
                    {
                        break;
                    }
                }
                if (HomeMode.Avatar.Starpowers.Contains(WildCard.GetGlobalId()))
                {
                    member.Gadget = WildCard.GetGlobalId();
                }
            }
            else
            {
                member.Gadget = 0;
            }
            //member.SkinId = HomeMode.Home.SelectedSkins[GlobalId.GetInstanceId(HomeMode.Home.CharacterId)];
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;

            member.HeroLevel = hero.PowerLevel;
            member.IsOwner = true;
            member.State = 0;
            team.Members.Add(member);

            TeamMessage teamMessage = new TeamMessage();
            teamMessage.Team = team;
            HomeMode.Avatar.TeamId = team.Id;
            Connection.Send(teamMessage);
        }

        private void AcceptFriendReceived(AcceptFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            {
                Friend entry = HomeMode.Avatar.GetRequestFriendById(message.AvatarId);
                if (entry == null) return;

                Friend oldFriend = HomeMode.Avatar.GetAcceptedFriendById(message.AvatarId);
                if (oldFriend != null)
                {
                    HomeMode.Avatar.Friends.Remove(entry);
                    Connection.Send(new OutOfSyncMessage());
                    return;
                }

                entry.FriendReason = 0;
                entry.FriendState = 4;

                FriendListUpdateMessage update = new FriendListUpdateMessage();
                update.Entry = entry;
                Connection.Send(update);
            }

            {
                ClientAvatar avatar = data.Avatar;
                Friend entry = avatar.GetFriendById(HomeMode.Avatar.AccountId);
                if (entry == null) return;

                entry.FriendState = 4;
                entry.FriendReason = 0;

                if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
                {
                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = entry;
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update);
                }
            }
        }

        private void RemoveFriendReceived(RemoveFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;

            Friend MyEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (MyEntry == null) return;

            MyEntry.FriendState = 0;

            HomeMode.Avatar.Friends.Remove(MyEntry);

            FriendListUpdateMessage update = new FriendListUpdateMessage();
            update.Entry = MyEntry;
            Connection.Send(update);

            Friend OtherEntry = avatar.GetFriendById(HomeMode.Avatar.AccountId);

            if (OtherEntry == null) return;

            OtherEntry.FriendState = 0;

            avatar.Friends.Remove(OtherEntry);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = OtherEntry;
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update2);
            }
        }

        private void AddFriendReceived(AddFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null)
            {
                Connection.Send(new AddFriendFailedMessage
                {
                    Reason = 5
                });
                return;
            }
            if (data.Avatar.AccountId == HomeMode.Avatar.AccountId)
            {
                // 2 - too many invites
                // 4 - invite urself
                // 5 doesnt exist
                // 7 - u have too many friends, rm
                // 8 - u have too many friends
                Connection.Send(new AddFriendFailedMessage
                {
                    Reason = 4
                });
                return;
            }
            if (data.Home.BlockFriendRequests)
            {
                Connection.Send(new AddFriendFailedMessage
                {
                    Reason = 0
                });
                return;
            }

            ClientAvatar avatar = data.Avatar;

            Friend requestEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (requestEntry != null)
            {
                AcceptFriendReceived(new AcceptFriendMessage()
                {
                    AvatarId = message.AvatarId
                });
                return;
            }
            else
            {
                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.HasPremiumPass, HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                friendEntry.FriendReason = message.Reason;
                friendEntry.FriendState = 3;
                avatar.Friends.Add(friendEntry);

                Friend request = new Friend();
                request.AccountId = avatar.AccountId;
                request.DisplayData = new PlayerDisplayData(data.Home.HasPremiumPass, data.Home.ThumbnailId, data.Home.NameColorId, data.Avatar.Name);
                request.FriendReason = 0;
                request.FriendState = 2;
                HomeMode.Avatar.Friends.Add(request);

                if (LogicServerListener.Instance.IsPlayerOnline(message.AvatarId))
                {
                    var gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);

                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = friendEntry;

                    gameListener.SendTCPMessage(update);
                }

                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = request;
                Connection.Send(update2);
            }
        }

        private void AskForFriendListReceived(AskForFriendListMessage message)
        {
            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);
        }

        private void PlayerStatusReceived(PlayerStatusMessage message)
        {
            if (HomeMode == null) return;
            if (message.Status < 0) return;
            int oldstatus = HomeMode.Avatar.PlayerStatus;
            int newstatus = message.Status;
            /*
             * practice:
             * 10
             * -1
             * 8
             *
             * battle:
             * 3
             * -1
             * 8
             */

            HomeMode.Avatar.PlayerStatus = message.Status;
            if (oldstatus == 3 && newstatus == 8)
            {
                HomeMode.Avatar.BattleStartTime = DateTime.UtcNow;
            }
            if (oldstatus == 8 && newstatus == 3)
            {
                Hero h = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                int lose = 0;
                int brawlerTrophies = h.Trophies;
                if (brawlerTrophies <= 49)
                {
                    lose = 0;
                }
                else if (50 <= brawlerTrophies && brawlerTrophies <= 99)
                {
                    lose = -1;
                }
                else if (100 <= brawlerTrophies && brawlerTrophies <= 199)
                {
                    lose = -2;
                }
                else if (200 <= brawlerTrophies && brawlerTrophies <= 299)
                {
                    lose = -3;
                }
                else if (300 <= brawlerTrophies && brawlerTrophies <= 399)
                {
                    lose = -4;
                }
                else if (400 <= brawlerTrophies && brawlerTrophies <= 499)
                {
                    lose = -5;
                }
                else if (500 <= brawlerTrophies && brawlerTrophies <= 599)
                {
                    lose = -6;
                }
                else if (600 <= brawlerTrophies && brawlerTrophies <= 699)
                {
                    lose = -7;
                }
                else if (700 <= brawlerTrophies && brawlerTrophies <= 799)
                {
                    lose = -8;
                }
                else if (800 <= brawlerTrophies && brawlerTrophies <= 899)
                {
                    lose = -9;
                }
                else if (900 <= brawlerTrophies && brawlerTrophies <= 999)
                {
                    lose = -10;
                }
                else if (1000 <= brawlerTrophies && brawlerTrophies <= 1099)
                {
                    lose = -11;
                }
                else if (1100 <= brawlerTrophies && brawlerTrophies <= 1199)
                {
                    lose = -12;
                }
                else if (brawlerTrophies >= 1200)
                {
                    lose = -12;
                }
                h.AddTrophies(lose);
                HomeMode.Home.PowerPlayGamesPlayed = Math.Max(0, HomeMode.Home.PowerPlayGamesPlayed - 1);
                HomeMode.Avatar.BattleStartTime = new DateTime();
                HomeMode.Home.TrophiesReward = 0;
                Logger.BLog($"Player {LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId)} left battle!");
                HomeMode.Avatar.BattleStartTime = DateTime.MinValue;
                if (HomeMode.Home.NotificationFactory.NotificationList.Count < 5)
                {
                    HomeMode.Home.NotificationFactory.Add(
                    new Notification()
                    {
                        Id = 81,
                        MessageEntry = $"Because of leaving match, you recieved leave penalty: {lose} trophies!"
                    });
                }
            }

            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
        }

        private void SendMyAllianceData(Alliance alliance)
        {
            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);

            AllianceStreamMessage stream = new AllianceStreamMessage();
            stream.Entries = alliance.Stream.GetEntries();
            Connection.Send(stream);
        }

        private void ChatToAllianceStreamReceived(ChatToAllianceStreamMessage message)
        {
            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (message.Message.StartsWith("/"))
            {
                string[] cmd = message.Message.Substring(1).Split(' ');
                if (cmd.Length == 0) return;

                AllianceStreamEntryMessage response = new()
                {
                    Entry = new AllianceStreamEntry
                    {
                        AuthorName = "Alliance Bot",
                        AuthorId = HomeMode.Avatar.AccountId + 1,
                        Id = alliance.Stream.EntryIdCounter + 1,
                        AuthorRole = AllianceRole.Member,
                        Type = 2
                    }
                };

                long accountId = HomeMode.Avatar.AccountId;

                switch (cmd[0])
                {
                    case "status":
                        long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                        DateTime now = Process.GetCurrentProcess().StartTime;
                        DateTime futureDate = DateTime.Now;

                        TimeSpan timeDifference = futureDate - now;

                        string formattedTime = string.Format("{0}{1}{2}{3}",
                        timeDifference.Days > 0 ? $"{timeDifference.Days} Days, " : string.Empty,
                        timeDifference.Hours > 0 || timeDifference.Days > 0 ? $"{timeDifference.Hours} Hours, " : string.Empty,
                        timeDifference.Minutes > 0 || timeDifference.Hours > 0 ? $"{timeDifference.Minutes} Minutes, " : string.Empty,
                        timeDifference.Seconds > 0 ? $"{timeDifference.Seconds} Seconds" : string.Empty);

                        response.Entry.Message = $"Server Status:\n" +
                            $"Server Game Version: v29.258\n" +
                            $"Server Build: v5.0 from 18.05.2024\n" +
                            $"Resources Sha: {Fingerprint.Sha}\n" +
                            $"Environment: Prod\n" +
                            $"Server Time: {DateTime.Now} EEST\n" +
                            $"Players Online: {Sessions.Count}\n" +
                            $"Memory Used: {megabytesUsed} MB\n" +
                            $"Uptime: {formattedTime}\n";
                        Connection.Send(response);
                        break;
                    case "help":
                        response.Entry.Message = $"Avaliable commands:\n/help - show all server commands list\n/status - show server status\n\n                               --- royale ID ---\n/register [username] [password] - register an account\n/login [username] [password] - login to an account";
                        Connection.Send(response);
                        break;
                    default:
                        response.Entry.Message = $"Unknown command \"{cmd[0]}\" - type \"/help\" to get command list!";
                        Connection.Send(response);
                        break;
                    // ACCOUNT SYSTEM HERE
                    case "register":
                        if (cmd.Length != 3)
                        {
                            response.Entry.Message = $"Usage: /register [username] [password]";
                            Connection.Send(response);
                            return;
                        }

                        string username = cmd[1];
                        string password = cmd[2];

                        bool registrationSuccess = RegisterUserToDatabase(username, password, accountId);

                        if (!registrationSuccess)
                        {
                            response.Entry.Message = $"Registration was unsuccessful. Username is already being used.";
                            Connection.Send(response);
                            return;
                        }
                        Account plreaccount = Accounts.Load(accountId);
                        Notification brlyn = new()
                        {
                            Id = 89,
                            DonationCount = 200,
                            MessageEntry = "<c6>royale ID Thank you for connecting!</c>"
                        };
                        plreaccount.Home.NotificationFactory.Add(brlyn);
                        response.Entry.Message = $"Registration is successful! You can log into your account now. You can also get your registration gift by restarting the game.";
                        Connection.Send(response);
                        break;
                    case "login":
                        if (cmd.Length != 3)
                        {
                            response.Entry.Message = $"Usage: /login [username] [password]";
                            Connection.Send(response);
                            return;
                        }

                        string loginUsername = cmd[1];
                        string loginPassword = cmd[2];


                        string accountIdS = LoginUserFromDatabase(loginUsername, loginPassword);

                        if (string.IsNullOrEmpty(accountIdS))
                        {
                            response.Entry.Message = $"Username or password incorrect.";
                            Connection.Send(response);
                            return;
                        }

                        Account account = Accounts.Load((long)Convert.ToDouble(accountIdS));

                        if (account == null)
                        {
                            response.Entry.Message = $"Account not found.";
                            Connection.Send(response);
                            return;
                        }

                        Connection.Send(new CreateAccountOkMessage
                        {
                            AccountId = account.AccountId,
                            PassToken = account.PassToken
                        });

                        Connection.Send(new AuthenticationFailedMessage
                        {
                            ErrorCode = 8,
                            Message = "Logged in successfully."
                        });
                        break;
                }
                return;
            }
            if (!HomeMode.Avatar.IsCommunityBanned && message.Message.Length < 100)
            {
                alliance.SendChatMessage(HomeMode.Avatar.AccountId, message.Message);
            }
            else if (HomeMode.Avatar.IsCommunityBanned)
            {
                AllianceStreamEntryMessage response = new()
                {
                    Entry = new AllianceStreamEntry
                    {
                        AuthorName = "Console",
                        AuthorId = 0,
                        Id = alliance.Stream.EntryIdCounter + 1,
                        AuthorRole = AllianceRole.Member,
                        Message = "This message is not visible, wait until the end of the blocking period!",
                        Type = 2
                    }
                };
                Connection.Send(response);
            }
            else
            {
                AllianceStreamEntryMessage response = new()
                {
                    Entry = new AllianceStreamEntry
                    {
                        AuthorName = "Console",
                        AuthorId = 0,
                        Id = alliance.Stream.EntryIdCounter + 1,
                        AuthorRole = AllianceRole.Member,
                        Message = "Unknown error occured. Contact an administrator.",
                        Type = 2
                    }
                };
                Connection.Send(response);
            }
        }

        private void JoinAllianceReceived(JoinAllianceMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (HomeMode.Avatar.AllianceId > 0) return;
            if (alliance == null) return;
            if (alliance.Members.Count >= 100) return;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 3;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            HomeMode.Avatar.AllianceRole = AllianceRole.Member;
            HomeMode.Avatar.AllianceId = alliance.Id;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar));

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 40;
            Connection.Send(response);

            SendMyAllianceData(alliance);
        }

        private void LeaveAllianceReceived(LeaveAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId < 0 || HomeMode.Avatar.AllianceRole == AllianceRole.None) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;
            if (HomeMode.Avatar.AllianceRole == AllianceRole.Leader)
            {
                AllianceMember nextLeader = alliance.GetNextRoleMember();
                if (nextLeader == null)
                {
                    alliance.RemoveMemberById(HomeMode.Avatar.AccountId);
                    if (alliance.Members.Count < 1)
                    {
                        Alliances.Delete(HomeMode.Avatar.AllianceId);
                    }
                    HomeMode.Avatar.AllianceId = -1;
                    HomeMode.Avatar.AllianceRole = AllianceRole.None;

                    Connection.Send(new AllianceResponseMessage
                    {
                        ResponseType = 80
                    });

                    Connection.Send(new MyAllianceMessage());

                    return;
                };
                Account target = Accounts.Load(nextLeader.AccountId);
                if (target == null) return;
                target.Avatar.AllianceRole = AllianceRole.Leader;
                nextLeader.Role = AllianceRole.Leader;
                if (LogicServerListener.Instance.IsPlayerOnline(target.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(target.AccountId).SendTCPMessage(new AllianceResponseMessage()
                    {
                        ResponseType = 101
                    });
                    MyAllianceMessage targetAlliance = new()
                    {
                        AllianceHeader = alliance.Header,
                        Role = HomeMode.Avatar.AllianceRole
                    };
                    LogicServerListener.Instance.GetGameListener(target.AccountId).SendTCPMessage(targetAlliance);
                }
            }
            alliance.RemoveMemberById(HomeMode.Avatar.AccountId);
            if (alliance.Members.Count < 1)
            {
                Alliances.Delete(HomeMode.Avatar.AllianceId);
            }
            else
            {
                AllianceStreamEntry allianceentry = new()
                {
                    AuthorId = HomeMode.Avatar.AccountId,
                    AuthorName = HomeMode.Avatar.Name,
                    Id = ++alliance.Stream.EntryIdCounter,
                    PlayerId = HomeMode.Avatar.AccountId,
                    PlayerName = HomeMode.Avatar.Name,
                    Type = 4,
                    Event = 4,
                    AuthorRole = HomeMode.Avatar.AllianceRole
                };
                alliance.AddStreamEntry(allianceentry);
            }
            HomeMode.Avatar.AllianceId = -1;
            HomeMode.Avatar.AllianceRole = AllianceRole.None;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 4;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 80;
            Connection.Send(response);

            MyAllianceMessage myAlliance = new MyAllianceMessage();
            Connection.Send(myAlliance);
        }

        private void CreateAllianceReceived(CreateAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId >= 0) return;

            Alliance alliance = new Alliance();
            alliance.Name = message.Name;
            alliance.Description = message.Description;
            alliance.RequiredTrophies = message.RequiredTrophies;

            if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
            {
                alliance.AllianceBadgeId = message.BadgeId;
            }
            else
            {
                alliance.AllianceBadgeId = 8000000;
            }

            HomeMode.Avatar.AllianceRole = AllianceRole.Leader;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar));

            Alliances.Create(alliance);

            HomeMode.Avatar.AllianceId = alliance.Id;

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 20;
            Connection.Send(response);

            SendMyAllianceData(alliance);
        }

        private void AskForAllianceDataReceived(AskForAllianceDataMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (alliance == null) return;

            AllianceDataMessage data = new AllianceDataMessage();
            data.Alliance = alliance;
            data.IsMyAlliance = message.AllianceId == HomeMode.Avatar.AllianceId;
            Connection.Send(data);
        }
        private void ChangeAllianceMemberRoleReceived(ChangeAllianceMemberRoleMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;
            if (HomeMode.Avatar.AllianceRole == AllianceRole.Member || HomeMode.Avatar.AllianceRole == AllianceRole.None) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            AllianceMember member = alliance.GetMemberById(message.AccountId);
            if (member == null) return;

            ClientAvatar avatar = Accounts.Load(message.AccountId).Avatar;

            AllianceRole
                    Member = (AllianceRole)1,
                    Leader = (AllianceRole)2,
                    Elder = (AllianceRole)3,
                    CoLeader = (AllianceRole)4;
            if (HomeMode.Avatar.AllianceRole == (AllianceRole)Member) return;
            //if (member.Role == Leader) return;
            if (alliance.getRoleVector(member.Role, (AllianceRole)message.Role))
            {
                if (avatar.AllianceRole == (AllianceRole)Member)
                {
                    avatar.AllianceRole = (AllianceRole)Elder;
                }
                else if (avatar.AllianceRole == (AllianceRole)Elder)
                {
                    avatar.AllianceRole = (AllianceRole)CoLeader;
                }
                else if (avatar.AllianceRole == (AllianceRole)CoLeader)
                {
                    HomeMode.Avatar.AllianceRole = (AllianceRole)CoLeader;
                    avatar.AllianceRole = (AllianceRole)Leader;
                    AllianceStreamEntry entry2 = new()
                    {
                        AuthorId = HomeMode.Avatar.AccountId,
                        AuthorName = HomeMode.Avatar.Name,
                        Id = ++alliance.Stream.EntryIdCounter,
                        PlayerId = HomeMode.Avatar.AccountId,
                        PlayerName = HomeMode.Avatar.Name,
                        Type = 4,
                        Event = 6,
                        AuthorRole = HomeMode.Avatar.AllianceRole
                    };
                    alliance.AddStreamEntry(entry2);

                    AllianceMember me = alliance.GetMemberById(HomeMode.Avatar.AccountId);
                    me.Role = HomeMode.Avatar.AllianceRole;

                }
                member.Role = avatar.AllianceRole;

                AllianceStreamEntry entry = new()
                {
                    AuthorId = HomeMode.Avatar.AccountId,
                    AuthorName = HomeMode.Avatar.Name,
                    Id = ++alliance.Stream.EntryIdCounter,
                    PlayerId = avatar.AccountId,
                    PlayerName = avatar.Name,
                    Type = 4,
                    Event = 5,
                    AuthorRole = HomeMode.Avatar.AllianceRole
                };
                alliance.AddStreamEntry(entry);

                AllianceResponseMessage response = new()
                {
                    ResponseType = 81
                };
                Connection.Send(response);
                MyAllianceMessage myAlliance = new()
                {
                    AllianceHeader = alliance.Header,
                    Role = HomeMode.Avatar.AllianceRole
                };
                Connection.Send(myAlliance);
                if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new AllianceResponseMessage()
                    {
                        ResponseType = 101
                    });
                    MyAllianceMessage targetAlliance = new()
                    {
                        AllianceHeader = alliance.Header,
                        Role = avatar.AllianceRole
                    };
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(targetAlliance);
                }
            }
            else
            {
                if (avatar.AllianceRole == (AllianceRole)Elder)
                {
                    avatar.AllianceRole = (AllianceRole)Member;
                }
                else if (avatar.AllianceRole == (AllianceRole)CoLeader)
                {
                    avatar.AllianceRole = (AllianceRole)Elder;
                }
                member.Role = avatar.AllianceRole;

                AllianceStreamEntry entry = new()
                {
                    AuthorId = HomeMode.Avatar.AccountId,
                    AuthorName = HomeMode.Avatar.Name,
                    Id = ++alliance.Stream.EntryIdCounter,
                    PlayerId = avatar.AccountId,
                    PlayerName = avatar.Name,
                    Type = 4,
                    Event = 6,
                    AuthorRole = HomeMode.Avatar.AllianceRole
                };
                alliance.AddStreamEntry(entry);

                AllianceResponseMessage response = new()
                {
                    ResponseType = 82
                };
                Connection.Send(response);
                MyAllianceMessage myAlliance = new()
                {
                    AllianceHeader = alliance.Header,
                    Role = HomeMode.Avatar.AllianceRole
                };
                Connection.Send(myAlliance);
                if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new AllianceResponseMessage()
                    {
                        ResponseType = 102
                    });
                    MyAllianceMessage targetAlliance = new()
                    {
                        AllianceHeader = alliance.Header,
                        Role = avatar.AllianceRole
                    };
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(targetAlliance);
                }
            }
        }

        private void AskForJoinableAllianceListReceived(AskForJoinableAllianceListMessage message)
        {
            JoinableAllianceListMessage list = new JoinableAllianceListMessage();
            List<Alliance> alliances = Alliances.GetRandomAlliances(10)
                                            .Distinct()
                                            .ToList();

            if (alliances.Count == 0) // if there are no clubs, don't send anything
            {
                return;
            }

            foreach (Alliance alliance in alliances)
            {
                list.JoinableAlliances.Add(alliance.Header);
            }

            Connection.Send(list);
        }

        private void ClientCapabilitesReceived(ClientCapabilitiesMessage message)
        {
            Connection.PingUpdated(message.Ping);
            ShowLobbyInfo();
        }

        private bool RegisterUserToDatabase(string username, string password, long id)
        {
            bool success = false;

            try
            {
                string connectionString = $"server={Configuration.Instance.MysqlHost};" +
                                          $"user={Configuration.Instance.MysqlUsername};" +
                                          $"database={Configuration.Instance.MysqlDatabase};" +
                                          $"port={Configuration.Instance.MysqlPort};" +
                                          $"password={Configuration.Instance.MysqlPassword}";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO users (username, password, id) VALUES (@username, @password, @id)";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@id", id);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return success;
        }

        private string LoginUserFromDatabase(string username, string password)
        {
            string accountId = null;

            try
            {
                string connectionString = $"server={Configuration.Instance.MysqlHost};" +
                                          $"user={Configuration.Instance.MysqlUsername};" +
                                          $"database={Configuration.Instance.MysqlDatabase};" +
                                          $"port={Configuration.Instance.MysqlPort};" +
                                          $"password={Configuration.Instance.MysqlPassword}";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT id FROM users WHERE username = @username AND password = @password";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        accountId = result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return accountId;
        }

        private void AskForBattleEndReceived(AskForBattleEndMessage message)
        {
            bool isPvP;
            BattlePlayer OwnPlayer = null;

            LocationData location = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(message.LocationId);
            if (location == null || location.Disabled)
            {
                return;
            }
            if (DateTime.UtcNow > HomeMode.Home.PremiumEndTime && HomeMode.Avatar.PremiumLevel > 1)
            {
                HomeMode.Avatar.PremiumLevel = 0;
                HomeMode.Home.NotificationFactory.Add(new Notification
                {
                    Id = 81,
                    //TimePassed =
                    MessageEntry = $"Hello!\nYour Premium status has expired."
                });
            }

            isPvP = true;//Events.HasLocation(message.LocationId);

            for (int x = 0; x < message.BattlePlayersCount; x++)
            {
                BattlePlayer battlePlayer = message.BattlePlayers[x];
                if (battlePlayer.DisplayData.Name == HomeMode.Avatar.Name)
                {
                    battlePlayer.AccountId = HomeMode.Avatar.AccountId;
                    OwnPlayer = battlePlayer;

                    Hero hero = HomeMode.Avatar.GetHero(OwnPlayer.CharacterId);
                    if (hero == null)
                    {
                        return;
                    }
                    message.BattlePlayers[x].HeroPowerLevel = hero.PowerLevel + (hero.HasStarpower ? 1 : 0);
                    OwnPlayer.HeroPowerLevel = hero.PowerLevel + (hero.HasStarpower ? 1 : 0);
                    OwnPlayer.Trophies = hero.Trophies;
                    OwnPlayer.HighestTrophies = hero.HighestTrophies;
                    OwnPlayer.PowerPlayScore = HomeMode.Home.PowerPlayScore;

                    battlePlayer.DisplayData = new PlayerDisplayData(HomeMode.Home.HasPremiumPass, HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                }
                else
                {
                    battlePlayer.DisplayData = new PlayerDisplayData(false, 28000000, 43000000, "Bot " + battlePlayer.DisplayData.Name);
                }
            }

            if (OwnPlayer == null)
            {
                return;
            }

            int StartExperience = HomeMode.Home.Experience;
            int[] ExperienceRewards = new[] { 8, 4, 6 };
            int[] TokensRewards = new[] { 14, 7, 10 };

            bool starToken = false;
            int gameMode = 0;
            int[] Trophies = new int[10];
            int trophiesResult = 0;
            int underdogTrophiesResult = 0;
            int experienceResult = 0;
            int totalTokensResult = 0;
            int tokensResult = 0;
            int doubledTokensResult = 0;
            int MilestoneReward = 0;
            int starExperienceResult = 0;
            List<int> MilestoneRewards = new List<int>();
            int powerPlayScoreGained = 0;
            int powerPlayEpicScoreGained = 0;
            bool isPowerPlay = false;
            bool HasNoTokens = false;
            List<Quest> q = new();

            if (isPvP)
            {
                Hero hero = HomeMode.Avatar.GetHero(OwnPlayer.CharacterId);
                if (hero == null)
                {
                    return;
                }
                int slot = Events.SlotsLocations[message.LocationId];

                int brawlerTrophies = hero.Trophies;
                if (slot == 9)
                {
                    if (HomeMode.Home.PowerPlayGamesPlayed < 3)
                    {
                        isPvP = false;
                        isPowerPlay = true;
                        int[] powerPlayAwards = { 30, 5, 15 };
                        powerPlayScoreGained = powerPlayAwards[message.BattleResult];
                        HomeMode.Home.PowerPlayTrophiesReward = powerPlayScoreGained;
                        HomeMode.Home.PowerPlayGamesPlayed++;
                        HomeMode.Home.PowerPlayScore += powerPlayScoreGained;

                        HomeMode.Avatar.TrioWins++;
                        if (location.GameMode == "CoinRush")
                        {
                            if (DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds <= 90)
                            {
                                HomeMode.Home.PowerPlayTrophiesReward += 3;
                                HomeMode.Home.PowerPlayScore += 3;
                                powerPlayEpicScoreGained = 3;
                            }
                        }
                        else if (location.GameMode == "LaserBall")
                        {
                            if (DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds <= 30)
                            {
                                HomeMode.Home.PowerPlayTrophiesReward += 3;
                                HomeMode.Home.PowerPlayScore += 3;
                                powerPlayEpicScoreGained = 3;
                            }
                        }
                        else if (location.GameMode == "AttackDefend")
                        {
                            if (DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds <= 45)
                            {
                                HomeMode.Home.PowerPlayTrophiesReward += 3;
                                HomeMode.Home.PowerPlayScore += 3;
                                powerPlayEpicScoreGained = 3;
                            }
                        }
                        else if (location.GameMode == "RoboWars")
                        {
                            if (DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds <= 45)
                            {
                                HomeMode.Home.PowerPlayTrophiesReward += 3;
                                HomeMode.Home.PowerPlayScore += 3;
                                powerPlayEpicScoreGained = 3;
                            }
                        }
                        if (HomeMode.Home.PowerPlayScore >= HomeMode.Home.PowerPlayHighestScore)
                        {
                            HomeMode.Home.PowerPlayHighestScore = HomeMode.Home.PowerPlayScore;
                        }
                        if (HomeMode.Home.Quests != null)
                        {
                            if (location.GameMode == "BountyHunter")
                            {
                                q = HomeMode.Home.Quests.UpdateQuestsProgress(3, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                            }
                            else if (location.GameMode == "CoinRush")
                            {
                                q = HomeMode.Home.Quests.UpdateQuestsProgress(0, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                            }
                            else if (location.GameMode == "AttackDefend")
                            {
                                q = HomeMode.Home.Quests.UpdateQuestsProgress(2, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                            }
                            else if (location.GameMode == "LaserBall")
                            {
                                q = HomeMode.Home.Quests.UpdateQuestsProgress(5, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                            }
                            else if (location.GameMode == "RoboWars")
                            {
                                q = HomeMode.Home.Quests.UpdateQuestsProgress(11, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                            }
                        }
                    }


                }
                else if (location.GameMode == "BountyHunter" || location.GameMode == "CoinRush" || location.GameMode == "AttackDefend" || location.GameMode == "LaserBall" || location.GameMode == "RoboWars" || location.GameMode == "KingOfHill")
                {
                    if (message.BattleResult == 0)
                    {
                        // star player
                        OwnPlayer.isStarplayer = true;
                        starExperienceResult = 4;
                        HomeMode.Home.Experience += starExperienceResult;

                        // Commented out code for adding star tokens
                        /*
                        if (Events.PlaySlot(HomeMode.Avatar.AccountId, slot))
                        {
                            starToken = true;
                            HomeMode.Avatar.AddStarTokens(1);
                            HomeMode.Home.StarTokensReward = 1;
                        }
                        */
                    }
                    else
                    {
                        Random r = new();
                        message.BattlePlayers[r.Next(1, 5)].isStarplayer = true;
                    }

                    if (brawlerTrophies <= 49)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = 0;
                    }
                    else if (brawlerTrophies <= 99)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -1;
                    }
                    else if (brawlerTrophies <= 199)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -2;
                    }
                    else if (brawlerTrophies <= 299)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -3;
                    }
                    else if (brawlerTrophies <= 399)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -4;
                    }
                    else if (brawlerTrophies <= 499)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -5;
                    }
                    else if (brawlerTrophies <= 599)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -6;
                    }
                    else if (brawlerTrophies <= 699)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -7;
                    }
                    else if (brawlerTrophies <= 799)
                    {
                        Trophies[0] = 8;
                        Trophies[1] = -8;
                    }
                    else if (brawlerTrophies <= 899)
                    {
                        Trophies[0] = 7;
                        Trophies[1] = -9;
                    }
                    else if (brawlerTrophies <= 999)
                    {
                        Trophies[0] = 6;
                        Trophies[1] = -10;
                    }
                    else if (brawlerTrophies <= 1099)
                    {
                        Trophies[0] = 5;
                        Trophies[1] = -11;
                    }
                    else if (brawlerTrophies <= 1199)
                    {
                        Trophies[0] = 4;
                        Trophies[1] = -12;
                    }
                    else if (brawlerTrophies >= 1200)
                    {
                        Trophies[0] = 3;
                        Trophies[1] = -12;
                    }

                    gameMode = 1;

                    trophiesResult = Trophies[message.BattleResult];
                    HomeMode.Home.TrophiesReward = Math.Max(trophiesResult, 0);

                    if (message.BattleResult == 0) // Win
                    {
                        HomeMode.Avatar.TrioWins++;

                        if (location.GameMode == "CoinRush")
                        {
                            if (DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds <= 90)
                            {
                                if (HomeMode.Avatar.PremiumLevel > 0)
                                {
                                    underdogTrophiesResult += (int)Math.Round((double)Trophies[message.BattleResult] / 4);
                                    trophiesResult += underdogTrophiesResult;
                                    HomeMode.Home.TrophiesReward = Math.Max(trophiesResult, 0);
                                }
                            }
                        }
                        else if (location.GameMode == "LaserBall")
                        {
                            if (DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds <= 30)
                            {
                                if (HomeMode.Avatar.PremiumLevel > 0)
                                {
                                    underdogTrophiesResult += (int)Math.Round((double)Trophies[message.BattleResult] / 4);
                                    trophiesResult += underdogTrophiesResult;
                                    HomeMode.Home.TrophiesReward = Math.Max(trophiesResult, 0);
                                }
                            }
                        }

                        // quests
                        if (HomeMode.Home.Quests != null)
                        {
                            switch (location.GameMode)
                            {
                                case "BountyHunter":
                                    q = HomeMode.Home.Quests.UpdateQuestsProgress(3, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                                    break;
                                case "CoinRush":
                                    q = HomeMode.Home.Quests.UpdateQuestsProgress(0, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                                    break;
                                case "AttackDefend":
                                    q = HomeMode.Home.Quests.UpdateQuestsProgress(2, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                                    break;
                                case "LaserBall":
                                    q = HomeMode.Home.Quests.UpdateQuestsProgress(5, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                                    break;
                                case "RoboWars":
                                    q = HomeMode.Home.Quests.UpdateQuestsProgress(11, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                                    break;
                            }
                        }
                    }
                    tokensResult = TokensRewards[message.BattleResult];
                    totalTokensResult = tokensResult;

                    experienceResult = ExperienceRewards[message.BattleResult];
                    HomeMode.Home.Experience += experienceResult;
                }
                else if (location.GameMode == "BattleRoyale")
                {
                    // star token logic
                    if (message.Rank < 5)
                    {
                        /*
                        if (Events.PlaySlot(HomeMode.Avatar.AccountId, slot))
                        {
                            starToken = true;
                            HomeMode.Avatar.AddStarTokens(1);
                            HomeMode.Home.StarTokensReward = 1;
                        }
                        */
                    }

                    if (brawlerTrophies >= 0 && brawlerTrophies <= 49)
                    {
                        Trophies = new[] { 10, 8, 7, 6, 4, 2, 2, 1, 0, 0 };
                    }
                    else if (brawlerTrophies <= 99)
                    {
                        Trophies = new[] { 10, 8, 7, 6, 3, 2, 2, 0, -1, -2 };
                    }
                    else if (brawlerTrophies <= 199)
                    {
                        Trophies = new[] { 10, 8, 7, 6, 3, 1, 0, -1, -2, -2 };
                    }
                    else if (brawlerTrophies <= 299)
                    {
                        Trophies = new[] { 10, 8, 6, 5, 3, 1, 0, -2, -3, -3 };
                    }
                    else if (brawlerTrophies <= 399)
                    {
                        Trophies = new[] { 10, 8, 6, 5, 2, 0, 0, -3, -4, -4 };
                    }
                    else if (brawlerTrophies <= 499)
                    {
                        Trophies = new[] { 10, 8, 6, 5, 2, -1, -2, -3, -5, -5 };
                    }
                    else if (brawlerTrophies <= 599)
                    {
                        Trophies = new[] { 10, 8, 6, 4, 2, -1, -2, -5, -6, -6 };
                    }
                    else if (brawlerTrophies <= 699)
                    {
                        Trophies = new[] { 10, 8, 6, 4, 1, -2, -2, -5, -7, -8 };
                    }
                    else if (brawlerTrophies <= 799)
                    {
                        Trophies = new[] { 10, 8, 6, 4, 1, -3, -4, -5, -8, -9 };
                    }
                    else if (brawlerTrophies <= 899)
                    {
                        Trophies = new[] { 9, 7, 5, 2, 0, -3, -4, -7, -9, -10 };
                    }
                    else if (brawlerTrophies <= 999)
                    {
                        Trophies = new[] { 8, 6, 4, 1, -1, -3, -6, -8, -10, -11 };
                    }
                    else if (brawlerTrophies <= 1099)
                    {
                        Trophies = new[] { 6, 5, 3, 1, -2, -5, -6, -9, -11, -12 };
                    }
                    else if (brawlerTrophies <= 1199)
                    {
                        Trophies = new[] { 5, 4, 1, 0, -2, -6, -7, -10, -12, -13 };
                    }
                    else if (brawlerTrophies >= 1200)
                    {
                        Trophies = new[] { 5, 3, 0, -1, -2, -6, -8, -11, -12, -13 };
                    }

                    gameMode = 2;
                    trophiesResult = Trophies[message.Rank - 1];

                    ExperienceRewards = new[] { 15, 12, 9, 6, 5, 4, 3, 2, 1, 0 };
                    TokensRewards = new[] { 30, 24, 21, 15, 12, 8, 6, 4, 2, 0 };

                    HomeMode.Home.TrophiesReward = Math.Max(trophiesResult, 0);

                    if (message.Rank == 1)
                    {
                        HomeMode.Avatar.SoloWins++;
                    }

                    if (message.Rank < 5 && HomeMode.Home.Quests != null)
                    {
                        q = HomeMode.Home.Quests.UpdateQuestsProgress(6, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                    }

                    tokensResult = TokensRewards[message.Rank - 1];
                    totalTokensResult = tokensResult;

                    experienceResult = ExperienceRewards[message.Rank - 1];
                    HomeMode.Home.Experience += experienceResult;
                }
                else if (location.GameMode == "BattleRoyaleTeam")
                {
                    // star token logic
                    if (message.Rank < 3)
                    {
                        /*
                        if (Events.PlaySlot(HomeMode.Avatar.AccountId, slot))
                        {
                            starToken = true;
                            HomeMode.Avatar.AddStarTokens(1);
                            HomeMode.Home.StarTokensReward = 1;
                        }
                        */
                    }

                    if (brawlerTrophies >= 0 && brawlerTrophies <= 49)
                    {
                        Trophies[0] = 9;
                        Trophies[1] = 7;
                        Trophies[2] = 4;
                        Trophies[3] = 0;
                        Trophies[4] = 0;
                    }
                    else if (brawlerTrophies <= 999)
                    {
                        Trophies[0] = 9;
                        Trophies[1] = 7;
                        int rankDiff = (brawlerTrophies - 100) / 100;
                        Trophies[2] = Math.Max(3 - rankDiff, 0);
                        Trophies[3] = Math.Max(-1 - rankDiff, -3);
                        Trophies[4] = Math.Max(-2 - rankDiff, -4);
                    }
                    else if (brawlerTrophies <= 1099)
                    {
                        Trophies[0] = 5;
                        Trophies[1] = 4;
                        int rankDiff = (brawlerTrophies - 1000) / 100;
                        Trophies[2] = Math.Max(-4 - rankDiff, -6);
                        Trophies[3] = Math.Max(-9 - rankDiff, -10);
                        Trophies[4] = Math.Max(-11 - rankDiff, -12);
                    }
                    else
                    {
                        Trophies[0] = 4;
                        Trophies[1] = 2;
                        Trophies[2] = -6;
                        Trophies[3] = -10;
                        Trophies[4] = -12;
                    }

                    gameMode = 5;
                    trophiesResult = Trophies[message.Rank - 1];

                    ExperienceRewards = new[] { 14, 8, 4, 2, 0 };
                    TokensRewards = new[] { 32, 20, 8, 4, 0 };

                    HomeMode.Home.TrophiesReward = Math.Max(trophiesResult, 0);

                    if (message.Rank < 3)
                    {
                        HomeMode.Avatar.DuoWins++;
                    }

                    if (message.Rank < 3 && HomeMode.Home.Quests != null)
                    {
                        q = HomeMode.Home.Quests.UpdateQuestsProgress(9, OwnPlayer.CharacterId, 0, 0, 0, HomeMode.Home);
                    }

                    tokensResult = TokensRewards[message.Rank - 1];
                    totalTokensResult = tokensResult;

                    experienceResult = ExperienceRewards[message.Rank - 1];
                    HomeMode.Home.Experience += experienceResult;
                }
                else if (location.GameMode == "BossFight")
                {
                    gameMode = 4;
                }
                else if (location.GameMode == "Raid_TownCrush")
                {
                    isPvP = false;
                    message.BattleResult = 0;
                    gameMode = 6;
                }
                else if (location.GameMode == "Raid")
                {
                    isPvP = false;
                    message.BattleResult = 0;
                    gameMode = 6;
                }

                if (HomeMode.Avatar.PremiumLevel > 0)
                {
                    switch (HomeMode.Avatar.PremiumLevel)
                    {
                        case 1:
                            if (location.GameMode == "BountyHunter" || location.GameMode == "CoinRush" || location.GameMode == "AttackDefend" || location.GameMode == "LaserBall" || location.GameMode == "RoboWars" || location.GameMode == "KingOfHill")
                            {
                                underdogTrophiesResult += (int)Math.Floor((double)Trophies[0] / 2);
                                trophiesResult += (int)Math.Floor((double)Trophies[0] / 2);
                            }
                            else if (location.GameMode == "BattleRoyale" || location.GameMode == "BattleRoyaleTeam")
                            {
                                underdogTrophiesResult += (int)Math.Floor((double)Math.Abs(Trophies[message.Rank - 1]) / 2);
                                trophiesResult += (int)Math.Floor((double)Math.Abs(Trophies[message.Rank - 1]) / 2);
                            }
                            break;
                        case 3:
                            if (location.GameMode == "BountyHunter" || location.GameMode == "CoinRush" || location.GameMode == "AttackDefend" || location.GameMode == "LaserBall" || location.GameMode == "RoboWars" || location.GameMode == "KingOfHill")
                            {
                                underdogTrophiesResult += (int)(Math.Floor((double)Trophies[0] * 2 * 1.5));
                                trophiesResult += (int)(Math.Floor((double)Trophies[0] * 2 * 1.5));
                            }
                            else if (location.GameMode == "BattleRoyale" || location.GameMode == "BattleRoyaleTeam")
                            {
                                underdogTrophiesResult += (int)(Math.Floor((double)Math.Abs(Trophies[message.Rank - 1]) * 2 * 1.5));
                                trophiesResult += (int)(Math.Floor((double)Math.Abs(Trophies[message.Rank - 1]) * 2 * 1.5));
                            }
                            break;
                        case 2:
                            if (location.GameMode == "BountyHunter" || location.GameMode == "CoinRush" || location.GameMode == "AttackDefend" || location.GameMode == "LaserBall" || location.GameMode == "RoboWars" || location.GameMode == "KingOfHill")
                            {
                                underdogTrophiesResult += (int)(Math.Floor((double)Trophies[0] / 2) + (Math.Round((double)Trophies[0] / 4)));
                                trophiesResult += (int)(Math.Floor((double)Trophies[0] / 2) + (Math.Round((double)Trophies[0] / 4)));
                            }
                            else if (location.GameMode == "BattleRoyale" || location.GameMode == "BattleRoyaleTeam")
                            {
                                underdogTrophiesResult += (int)(Math.Floor((double)Math.Abs(Trophies[message.Rank - 1]) / 2) + (Math.Round((double)Math.Abs(Trophies[message.Rank - 1]) / 4)));
                                trophiesResult += (int)(Math.Floor((double)Math.Abs(Trophies[message.Rank - 1]) / 2) + (Math.Round((double)Math.Abs(Trophies[message.Rank - 1]) / 4)));
                            }
                            break;
                    }
                    HomeMode.Home.TrophiesReward = Math.Max(trophiesResult, 0);
                }

                if (HomeMode.Home.BattleTokens > 0)
                {
                    if (HomeMode.Home.BattleTokens - tokensResult < 0)
                    {
                        tokensResult = HomeMode.Home.BattleTokens;
                        HomeMode.Home.BattleTokens = 0;
                    }
                    else
                    {
                        HomeMode.Home.BattleTokens -= tokensResult;
                    }
                    totalTokensResult += tokensResult;

                    if (HomeMode.Home.BattleTokensRefreshStart == new DateTime())
                    {
                        HomeMode.Home.BattleTokensRefreshStart = DateTime.UtcNow;
                    }
                }
                else
                {
                    tokensResult = 0;
                    totalTokensResult = 0;
                    HasNoTokens = true;
                }

                int startExperience = HomeMode.Home.Experience;
                HomeMode.Home.Experience += experienceResult + starExperienceResult;
                int endExperience = HomeMode.Home.Experience;

                for (int i = 34; i < 500; i++)
                {
                    MilestoneData milestone = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId(39, i));
                    int milestoneThreshold = milestone.ProgressStart + milestone.Progress;

                    if (startExperience < milestoneThreshold && endExperience >= milestoneThreshold)
                    {
                        MilestoneReward = GlobalId.CreateGlobalId(39, i);
                        MilestoneRewards.Add(MilestoneReward);
                        HomeMode.Avatar.StarPoints += milestone.SecondaryLvlUpRewardCount;
                        HomeMode.Home.StarPointsGained += milestone.SecondaryLvlUpRewardCount;
                        totalTokensResult += milestone.PrimaryLvlUpRewardCount;
                        break;
                    }
                }

                int startTrophies = hero.HighestTrophies;
                HomeMode.Avatar.AddTrophies(trophiesResult);
                hero.AddTrophies(trophiesResult);
                int endTrophies = hero.HighestTrophies;

                for (int i = 0; i < 34; i++)
                {
                    MilestoneData milestone = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId(39, i));
                    int milestoneThreshold = milestone.ProgressStart + milestone.Progress;

                    if (startTrophies < milestoneThreshold && endTrophies >= milestoneThreshold)
                    {
                        MilestoneReward = GlobalId.CreateGlobalId(39, i);
                        MilestoneRewards.Add(MilestoneReward);
                        HomeMode.Avatar.StarPoints += milestone.SecondaryLvlUpRewardCount;
                        HomeMode.Home.StarPointsGained += milestone.SecondaryLvlUpRewardCount;
                        totalTokensResult += milestone.PrimaryLvlUpRewardCount;
                        break;
                    }
                }

                // token doublers
                if (HomeMode.Home.TokenDoublers > 0)
                {
                    doubledTokensResult = Math.Min(totalTokensResult, HomeMode.Home.TokenDoublers);
                    HomeMode.Home.TokenDoublers -= doubledTokensResult;
                    totalTokensResult += doubledTokensResult;
                }

                HomeMode.Home.BrawlPassTokens += totalTokensResult;
                HomeMode.Home.TokenReward += totalTokensResult;

                if (location.GameMode == "BountyHunter" || location.GameMode == "CoinRush" || location.GameMode == "AttackDefend" ||
                    location.GameMode == "LaserBall" || location.GameMode == "RoboWars")
                {
                    gameMode = 1;
                }
                else if (location.GameMode == "BattleRoyale")
                {
                    gameMode = 2;
                }
                else if (location.GameMode == "BattleRoyaleTeam")
                {
                    gameMode = 5;
                }
                else if (location.GameMode == "BossFight")
                {
                    gameMode = 4;
                }

                // battle log
                string[] battleResults = { "win", "lose", "draw" };
                string logMessage = location.GameMode.StartsWith("BattleRoyale")
                    ? $"Player {LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId)} ended battle! Battle Rank: {message.BattleResult} in {DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds}s gamemode: {location.GameMode}!"
                    : $"Player {LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId)} ended battle! Battle Result: {battleResults[message.BattleResult]} in {DateTime.UtcNow.Subtract(HomeMode.Avatar.BattleStartTime).TotalSeconds}s gamemode: {location.GameMode}!";

                Logger.BLog(logMessage);
                HomeMode.Avatar.BattleStartTime = new DateTime();

                BattleEndMessage battleend = new()
                {
                    GameMode = gameMode,
                    Result = message.BattleResult,
                    StarToken = starToken,
                    IsPowerPlay = isPowerPlay,
                    IsPvP = isPvP,
                    pp = message.BattlePlayers,
                    OwnPlayer = OwnPlayer,
                    TrophiesReward = trophiesResult,
                    ExperienceReward = experienceResult,
                    StarExperienceReward = starExperienceResult,
                    DoubledTokensReward = doubledTokensResult,
                    TokenDoublersLeft = HomeMode.Home.TokenDoublers,
                    TokensReward = tokensResult,
                    Experience = StartExperience,
                    MilestoneReward = MilestoneReward,
                    ProgressiveQuests = q,
                    UnderdogTrophies = underdogTrophiesResult,
                    PowerPlayScoreGained = powerPlayScoreGained,
                    PowerPlayEpicScoreGained = powerPlayEpicScoreGained,
                    HasNoTokens = HasNoTokens,
                    MilestoneRewards = MilestoneRewards,
                };

                Connection.Send(battleend);
            }
        }

        private void GetLeaderboardReceived(GetLeaderboardMessage message)
        {
            LeaderboardMessage leaderboard = new()
            {
                LeaderboardType = message.LeaderboardType,
                Region = message.IsRegional ? "US" : null
            };

            switch (message.LeaderboardType)
            {
                case 1: // main leaderboard
                    ProcessAvatarLeaderboard(leaderboard, Leaderboards.GetAvatarRankingList());
                    break;

                case 2: // club leaderboard
                    ProcessAllianceLeaderboard(leaderboard, Leaderboards.GetAllianceRankingList());
                    break;

                case 0: // brawl leaderboard
                    ProcessBrawlersLeaderboard(leaderboard, Leaderboards.GetBrawlersRankingList(), message.CharachterId);
                    break;
            }

            leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
            Connection.Send(leaderboard);
        }

        private void ProcessAvatarLeaderboard(LeaderboardMessage leaderboard, Account[] rankingList)
        {
            foreach (Account data in rankingList)
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
            }
        }

        private void ProcessAllianceLeaderboard(LeaderboardMessage leaderboard, Alliance[] rankingList)
        {
            leaderboard.AllianceList.AddRange(rankingList);
        }

        private void ProcessBrawlersLeaderboard(LeaderboardMessage leaderboard, Dictionary<int, List<Account>> rankingList, int characterId)
        {
            var brawlersData = rankingList
                .Where(data => data.Key == characterId)
                .SelectMany(data => data.Value)
                .ToDictionary(account => account.Home, account => account.Avatar);

            var sortedBrawlers = brawlersData
                .OrderByDescending(x => x.Value.GetHero(characterId).Trophies)
                .Take(Math.Min(brawlersData.Count, 200))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            leaderboard.CharachterId = characterId;
            leaderboard.Brawlers = sortedBrawlers;
        }

        private void GoHomeReceived(GoHomeMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                Connection.Home.Events = Events.GetEventsById(HomeMode.Home.PowerPlayGamesPlayed, Connection.Avatar.AccountId);
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
                ShowLobbyInfo();
            }
        }

        private void ClientInfoReceived(ClientInfoMessage message)
        {
            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerPort = Configuration.Instance.Port;
            Connection.Send(info);
        }

        private void CancelMatchMaking(CancelMatchmakingMessage message)
        {
            Matchmaking.CancelMatchmake(Connection);
            Connection.Send(new MatchMakingCancelledMessage());
        }

        private void MatchmakeRequestReceived(MatchmakeRequestMessage message)
        {
            int slot = message.EventSlot;

            if (HomeMode.Home.Character.Disabled)
            {
                Connection.Send(new OutOfSyncMessage());
                return;
            }

            if (!Events.HasSlot(slot))
            {
                slot = 1;
            }

            Matchmaking.RequestMatchmake(Connection, slot);
        }

        private void EndClientTurnReceived(EndClientTurnMessage message)
        {
            foreach (Command command in message.Commands)
            {
                if (!CommandManager.ReceiveCommand(command))
                {
                    OutOfSyncMessage outOfSync = new();
                    Connection.Send(outOfSync);
                }
            }
            HomeMode.ClientTurnReceived(message.Tick, message.Checksum, message.Commands);
        }

        private void GetPlayerProfile(GetPlayerProfileMessage message)
        {
            if (message.AccountId == 0)
            {
                Profile p = Profile.CreateConsole();
                PlayerProfileMessage a = new PlayerProfileMessage();
                a.Profile = p;

                Connection.Send(a);
                return;
            }
            Account data = Accounts.Load(message.AccountId);
            if (data == null) return;

            Profile profile = Profile.Create(data.Home, data.Avatar);

            PlayerProfileMessage profileMessage = new PlayerProfileMessage();
            profileMessage.Profile = profile;
            if (data.Avatar.AllianceId >= 0)
            {
                Alliance alliance = Alliances.Load(data.Avatar.AllianceId);
                if (alliance != null)
                {
                    profileMessage.AllianceHeader = alliance.Header;
                    profileMessage.AllianceRole = data.Avatar.AllianceRole;
                }
            }
            Connection.Send(profileMessage);
        }

        private void ChangeName(ChangeAvatarNameMessage message)
        {
            LogicChangeAvatarNameCommand command = new()
            {
                Name = message.Name,
                ChangeNameCost = 0
            };
            if (HomeMode.Avatar.AllianceId >= 0)
            {
                Alliance a = Alliances.Load(HomeMode.Avatar.AllianceId);
                if (a == null) return;
                AllianceMember m = a.GetMemberById(HomeMode.Avatar.AccountId);
                m.DisplayData.Name = message.Name;
            }
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommandMessage = new()
            {
                Command = command
            };
            Connection.Send(serverCommandMessage);
        }

        private void OnChangeCharacter(int characterId)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            Hero hero = HomeMode.Avatar.GetHero(characterId);
            if (hero == null) return;
            member.CharacterId = characterId;
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;

            team.TeamUpdated();
        }

        private void LoginReceived(AuthenticationMessage message)
        {
            Account account = GetAccount(message);

            if (account == null)
            {
                SendAuthenticationFailed(1, "Unknown Error occurred while loading account");
                return;
            }

            if (message.AccountId == 0)
            {
                account = Accounts.Create();
            }
            else
            {
                account = Accounts.Load(message.AccountId);
                if (account.PassToken != message.PassToken)
                {
                    account = null;
                }
            }

            if (account == null)
            {

                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "clear appdata.";
                Connection.Send(loginFailed);

                return;
            }

            string combinedVersion = $"{message.Major}.{message.Minor}";
            string gameVersion = "29.258";

            if (combinedVersion != gameVersion)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage
                {
                    ErrorCode = 8,
                    Message = $"Wrong game version. Expected: {gameVersion}, Received: {combinedVersion}",
                    Redirect = "https://github.com/erder00/royale-brawl-v29" // redirect
                };
                Connection.Send(loginFailed);
                return;
            }

            string[] androidVersionParts = message.Android.Split('.'); // check if android version is between 0 and 100 (if not then it's not an android device and login will be prevented)
            if (androidVersionParts.Length == 0 || !int.TryParse(androidVersionParts[0], out int androidMajorVersion) || androidMajorVersion < 0 || androidMajorVersion > 100)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage
                {
                    ErrorCode = 8,
                    Message = "Invalid Android version deteced, contact an admin if you think this is a mistake."
                };
                Connection.Send(loginFailed);
                return;
            }

            if (IsSessionActive((int)account.Avatar.AccountIdRedirect))
            {
                HandleActiveSession((int)account.Avatar.AccountIdRedirect);
            }

            if (IsSessionActive((int)message.AccountId))
            {
                HandleActiveSession((int)message.AccountId);
            }

            if (account.Avatar.AccountIdRedirect != 0)
            {
                account = Accounts.Load((int)account.Avatar.AccountIdRedirect);
            }

            if (account.Avatar.Banned)
            {
                SendAuthenticationFailed(11, "Account is banned");
                return;
            }

            SendAuthenticationSuccess(account, message);

            InitializeHomeMode(account, message);

            HandleBattleState(account);

            UpdateLastOnline(account);

            InitializeSession(account);

            SendFriendAndAllianceData(account);
        }

        private Account GetAccount(AuthenticationMessage message)
        {
            if (message.AccountId == 0)
            {
                return Accounts.Create();
            }

            Account account = Accounts.Load((int)message.AccountId);
            return account.PassToken == message.PassToken ? account : null;
        }

        private bool IsSessionActive(int accountId)
        {
            return Sessions.IsSessionActive(accountId);
        }

        private void HandleActiveSession(int accountId)
        {
            var session = Sessions.GetSession(accountId);
            session.GameListener.SendTCPMessage(new AuthenticationFailedMessage
            {
                Message = "Another device has connected to this game!"
            });
            Sessions.Remove(accountId);
        }


        private void SendAuthenticationFailed(int errorCode, string message)
        {
            Connection.Send(new AuthenticationFailedMessage
            {
                ErrorCode = errorCode,
                Message = message
            });
        }

        private void SendAuthenticationSuccess(Account account, AuthenticationMessage message)
        {
            var loginOk = new AuthenticationOkMessage
            {
                AccountId = account.AccountId,
                PassToken = account.PassToken,
                Major = message.Major,
                Minor = message.Minor,
                Build = message.Build,
                ServerEnvironment = "prod"
            };
            Connection.Send(loginOk);
        }

        private void InitializeHomeMode(Account account, AuthenticationMessage message)
        {
            HomeMode = HomeMode.LoadHomeState(new HomeGameListener(Connection), account.Home, account.Avatar, Events.GetEventsById(account.Home.PowerPlayGamesPlayed, account.Avatar.AccountId));
            HomeMode.CharacterChanged += OnChangeCharacter;
            HomeMode.Home.IpAddress = Connection.Socket.RemoteEndPoint.ToString().Split(" ")[0];
            HomeMode.Home.Device = message.DeviceId;

            if (HomeMode.Avatar.HighestTrophies == 0 && HomeMode.Avatar.Trophies != 0)
            {
                HomeMode.Avatar.HighestTrophies = HomeMode.Avatar.Trophies;
            }

            CommandManager = new(HomeMode, Connection);
        }

        private void HandleBattleState(Account account)
        {
            if (HomeMode.Avatar.BattleStartTime != new DateTime())
            {
                Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                int trophiesLost = CalculateTrophiesLost(hero.Trophies);
                hero.AddTrophies(trophiesLost);
                HomeMode.Home.PowerPlayGamesPlayed = Math.Max(0, HomeMode.Home.PowerPlayGamesPlayed - 1);
                Connection.Home.Events = Events.GetEventsById(HomeMode.Home.PowerPlayGamesPlayed, Connection.Avatar.AccountId);
                HomeMode.Avatar.BattleStartTime = new DateTime();
            }

            BattleMode battle = HomeMode.Avatar.BattleId > 0 ? Battles.Get((int)HomeMode.Avatar.BattleId) : null;

            if (battle == null)
            {
                Connection.Send(new OwnHomeDataMessage { Home = HomeMode.Home, Avatar = HomeMode.Avatar });
            }
            else
            {
                StartLoadingMessage startLoading = new StartLoadingMessage
                {
                    LocationId = battle.Location.GetGlobalId(),
                    TeamIndex = HomeMode.Avatar.TeamIndex,
                    OwnIndex = HomeMode.Avatar.OwnIndex,
                    GameMode = battle.GetGameModeVariation() == 6 ? 6 : 1,
                    Players = new List<Supercell.Laser.Logic.Battle.Structures.BattlePlayer>(battle.GetPlayers())
                };

                UDPSocket socket = UDPGateway.CreateSocket();
                socket.TCPConnection = Connection;
                socket.Battle = battle;
                Connection.UdpSessionId = socket.SessionId;
                battle.ChangePlayerSessionId(HomeMode.Avatar.UdpSessionId, socket.SessionId);
                HomeMode.Avatar.UdpSessionId = socket.SessionId;
                Connection.Send(startLoading);
            }
        }

        private int CalculateTrophiesLost(int brawlerTrophies)
        {
            if (brawlerTrophies <= 49) return 0;
            if (brawlerTrophies <= 99) return -1;
            if (brawlerTrophies <= 199) return -2;
            if (brawlerTrophies <= 299) return -3;
            if (brawlerTrophies <= 399) return -4;
            if (brawlerTrophies <= 499) return -5;
            if (brawlerTrophies <= 599) return -6;
            if (brawlerTrophies <= 699) return -7;
            if (brawlerTrophies <= 799) return -8;
            if (brawlerTrophies <= 899) return -9;
            if (brawlerTrophies <= 999) return -10;
            if (brawlerTrophies <= 1099) return -11;
            return -12;
        }

        private void UpdateLastOnline(Account account)
        {
            Connection.Avatar.LastOnline = DateTime.UtcNow;
        }

        private void InitializeSession(Account account)
        {
            Sessions.Create(HomeMode, Connection);
        }

        private void SendFriendAndAllianceData(Account account)
        {
            Connection.Send(new FriendListMessage { Friends = HomeMode.Avatar.Friends.ToArray() });

            if (HomeMode.Avatar.AllianceRole != AllianceRole.None && HomeMode.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load((int)HomeMode.Avatar.AllianceId);
                if (alliance != null)
                {
                    SendMyAllianceData(alliance);
                    Connection.Send(new AllianceDataMessage { Alliance = alliance, IsMyAlliance = true });
                }
            }

            foreach (Friend entry in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(entry.AccountId))
                {
                    Connection.Send(new FriendOnlineStatusEntryMessage
                    {
                        AvatarId = entry.AccountId,
                        PlayerStatus = entry.Avatar.PlayerStatus
                    });
                }
            }

            if (HomeMode.Avatar.TeamId > 0)
            {
                TeamMessage teamMessage = new TeamMessage { Team = Teams.Get((int)HomeMode.Avatar.TeamId) };
                if (teamMessage.Team != null)
                {
                    Connection.Send(teamMessage);
                    TeamMember member = teamMessage.Team.GetMember(HomeMode.Avatar.AccountId);
                    member.State = 0;
                    teamMessage.Team.TeamUpdated();
                }
            }
        }

        private void ClientHelloReceived(ClientHelloMessage message)
        {
            if (message.KeyVersion != PepperKey.VERSION)
            {
                //return;
            }

            Connection.Messaging.Seed = message.ClientSeed;
            Random r = new();

            Connection.Messaging.serversc = (byte)r.Next(1, 256);
            ServerHelloMessage hello = new ServerHelloMessage();
            hello.serversc = Connection.Messaging.serversc;
            hello.SetServerHelloToken(Connection.Messaging.SessionToken);
            Connection.Send(hello);
        }
    }
}
