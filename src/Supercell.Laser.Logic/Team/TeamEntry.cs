namespace Supercell.Laser.Logic.Team
{
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message.Team;
    using Supercell.Laser.Logic.Message.Team.Stream;
    using Supercell.Laser.Logic.Team.Stream;
    using Supercell.Laser.Titan.DataStream;

    public class TeamEntry
    {
        public long Id;
        public int Type;

        public int LocationId;

        public List<TeamMember> Members { get; set; }
        public List<TeamInviteEntry> Invites { get; set; }
        public List<TeamJoinRequest> JoinRequests { get; set; }
        public List<int> DisabledBots { get; set; }
        public int EventSlot { get; set; }

        private long EntryCounter;
        public List<TeamStreamEntry> Stream { get; private set; }

        public TeamEntry()
        {
            Members = new List<TeamMember>();
            Invites = new List<TeamInviteEntry>();
            JoinRequests = new List<TeamJoinRequest>();
            DisabledBots = new List<int>();
            Stream = new List<TeamStreamEntry>();

            EventSlot = 1;
            EntryCounter = 0;
        }

        public void AddStreamEntry(TeamStreamEntry entry)
        {
            if (entry == null) return;

            entry.Id = ++EntryCounter;
            Stream.Add(entry);
            TeamStreamMessage message = new TeamStreamMessage();
            message.TeamId = Id;
            message.Entries = new TeamStreamEntry[] { entry };

            foreach (var member in Members)
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
        }

        public void StreamUpdated()
        {

        }

        public TeamMember GetMember(long id)
        {
            return Members.Find(member => member.AccountId == id);
        }

        public TeamInviteEntry GetInviteById(long id)
        {
            return Invites.Find(invite => invite.Id == id);
        }

        public bool IsEveryoneReady()
        {
            foreach (TeamMember member in Members)
            {
                if (!member.IsReady) return false;
            }
            return true;
        }

        public void TeamUpdated()
        {
            TeamMessage message = new TeamMessage();
            message.Team = this;

            foreach (var member in Members)
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Type);
            stream.WriteBoolean(Type == 1);
            stream.WriteVInt(3); // Team capacity
            stream.WriteLong(Id);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            ByteStreamHelper.WriteDataReference(stream, LocationId);

            stream.WriteVInt(Members.Count);
            foreach (TeamMember entry in Members.ToArray())
            {
                entry.Encode(stream);
            }

            stream.WriteVInt(Invites.Count);
            foreach (TeamInviteEntry entry in Invites.ToArray())
            {
                entry.Encode(stream);
            }

            stream.WriteVInt(JoinRequests.Count);
            foreach (TeamJoinRequest request in JoinRequests.ToArray())
            {
                request.Encode(stream);
            }

            stream.WriteVInt(0); // Disabled Team Slots

            stream.WriteBoolean(false);
            stream.WriteBoolean(true); // Room Chat enabled
            stream.WriteBoolean(false); // Accessories enabled
        }
    }
}
