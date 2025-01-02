namespace Supercell.Laser.Logic.Message.Club
{
    public class AllianceSearchMessage : GameMessage
    {
        public string SearchValue;

        public override void Decode()
        {
            SearchValue = Stream.ReadString();
        }

        public override int GetMessageType()
        {
            return 14324;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
