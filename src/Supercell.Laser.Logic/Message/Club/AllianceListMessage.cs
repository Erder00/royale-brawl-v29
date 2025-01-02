namespace Supercell.Laser.Logic.Message.Club
{
    using Supercell.Laser.Logic.Club;
    using Supercell.Laser.Logic.Helper;
    using static System.Reflection.Metadata.BlobBuilder;

    public class AllianceListMessage : GameMessage // credits: tale team <3
    {
        public List<Alliance> clubs;

        public AllianceListMessage() : base()
        {
            clubs = new List<Alliance>();
        }

        public string query;

        public override void Encode()
        {
            Stream.WriteString(query);
            Stream.WriteVInt(clubs.Count);

            foreach (Alliance alliance in clubs)
            {
                Stream.WriteLong(alliance.Id); // Id
                Stream.WriteString(alliance.Name); // Name
                ByteStreamHelper.WriteDataReference(Stream, alliance.AllianceBadgeId); // Badge
                Stream.WriteVInt(alliance.Type); // Club type
                Stream.WriteVInt(alliance.Members.Count); // Members
                Stream.WriteVInt(alliance.Trophies); // Trophies
                Stream.WriteVInt(alliance.RequiredTrophies); // Required trophies
                ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(0, 0)); // unknown
                Stream.WriteString("US"); // region

                Stream.WriteVInt(0); // Members online <- TODO!
                Stream.WriteVInt(0); // Family friendly
            }
        }

        public override int GetMessageType()
        {
            return 24310;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
