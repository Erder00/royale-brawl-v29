namespace Supercell.Laser.Logic.Message
{
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

    public class MessageFactory
    {
        public static MessageFactory Instance;

        static MessageFactory()
        {
            Instance = new MessageFactory();
        }

        private Dictionary<int, Type> Definitions;

        public MessageFactory()
        {
            Definitions = new Dictionary<int, Type>()
            {
                {10100, typeof(ClientHelloMessage)},
                {10101, typeof(AuthenticationMessage)},
                {10107, typeof(ClientCapabilitiesMessage)},
                {10108, typeof(KeepAliveMessage)},
                {10110, typeof(AnalyticEventMessage)},
                {10119, typeof(ReportAllianceStreamMessage)},

                //{10177, typeof(ClientInfoMessage)},
                {10212, typeof(ChangeAvatarNameMessage)},

                {10501, typeof(AcceptFriendMessage)},
                {10502, typeof(AddFriendMessage)},
                {10504, typeof(AskForFriendListMessage)},
                {10506, typeof(RemoveFriendMessage)},
                {10576, typeof(SetBlockFriendRequestsMessage)},
                {14101, typeof(GoHomeMessage)},
                {14102, typeof(EndClientTurnMessage)},
                ///{14103, typeof(MatchmakeRequestMessage)},
                ///{14104, typeof(StartSpectateMessage)},
                {14106, typeof(CancelMatchmakingMessage)},
                ///{14107, typeof(StopSpectateMessage)},
                {14109, typeof(GoHomeFromOfflinePractiseMessage)},
                {14110, typeof(AskForBattleEndMessage)},
                {14113, typeof(GetPlayerProfileMessage)},
                {14114, typeof(BattleLogMessage)},

                {14166, typeof(ChronosEventSeenMessage)},
                {14277, typeof(GetSeasonRewardsMessage)},

                {14301, typeof(CreateAllianceMessage)},
                {14302, typeof(AskForAllianceDataMessage)},
                {14303, typeof(AskForJoinableAllianceListMessage)},
                {14305, typeof(JoinAllianceMessage)},
                {14306, typeof(ChangeAllianceMemberRoleMessage)},
                {14307, typeof(KickAllianceMemberMessage)},
                {14308, typeof(LeaveAllianceMessage)},
                {14315, typeof(ChatToAllianceStreamMessage)},
                {14316, typeof(ChangeAllianceSettingsMessage)},
                {14324, typeof(AllianceSearchMessage)},
                {14330, typeof(SendAllianceMailMessage)},

                {14350, typeof(TeamCreateMessage)},
                {14353, typeof(TeamLeaveMessage)},
                {14354, typeof(TeamChangeMemberSettingsMessage)},
                {14355, typeof(TeamSetMemberReadyMessage)},
                {14358, typeof(TeamSpectateMessage)},
                {14359, typeof(TeamChatMessage)},
                {14361, typeof(TeamMemberStatusMessage)},
                {14362, typeof(TeamSetEventMessage)},
                {14363, typeof(TeamSetLocationMessage)},
                {14365, typeof(TeamInviteMessage)},
                {14366, typeof(PlayerStatusMessage)},
                {14367, typeof(TeamClearInviteMessage)},
                {14369, typeof(TeamPremadeChatMessage)},
                {14370, typeof(TeamInviteMessage)},

                {14403, typeof(GetLeaderboardMessage)},

                {14479, typeof(TeamInvitationResponseMessage)},

                {14600, typeof(AvatarNameCheckRequestMessage)},

                {14777, typeof(SetInvitesBlockedMessage)},

                {18686, typeof(SetSupportedCreatorMessage)},

                {19001, typeof(LatencyTestResultMessage)},

                //{14330, typeof(SendAllianceMailMessage)},
            };
        }

        public GameMessage CreateMessageByType(int type)
        {
            if (Definitions.ContainsKey(type))
            {
                return (GameMessage)Activator.CreateInstance(Definitions[type]);
            }
            return null;
        }
    }
}
