namespace Supercell.Laser.Logic.Message.Team
{
    public class TeamSetLocationMessage : GameMessage
    {
        public int RequestedMap { get; set; }

        public override void Decode()
        {
            base.Decode();

            Stream.ReadVInt();
            RequestedMap = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 14363;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
