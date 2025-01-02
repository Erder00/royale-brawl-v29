namespace Supercell.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationMessage : GameMessage
    {
        public AuthenticationMessage() : base()
        {
            AccountId = 0;
        }

        public long AccountId;
        public string PassToken;
        public string DeviceId;
        public int DeviceLang;
        public string Sha;
        public string Android;
        public int Major;
        public int Minor;
        public int Build;

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            PassToken = Stream.ReadString();

            Major = Stream.ReadInt();
            Build = Stream.ReadInt();
            Minor = Stream.ReadInt();

            Sha = Stream.ReadString(); // fingerprint sha
            DeviceId = Stream.ReadString(); // device
            Stream.ReadVInt(); // int 17956864 = ISANDROID (maybe)
            DeviceLang = Stream.ReadVInt(); // lang id (0 for US)
            Stream.ReadString(); // full lang (en-US, de-DE, etc) NOT USED

            Android = Stream.ReadString(); // android version
            Stream.ReadBoolean();

            Stream.ReadString();
            Stream.ReadString();
            Stream.ReadBoolean();
            Stream.ReadString();
            Stream.ReadInt();
            Stream.ReadVInt();
            Stream.ReadString(); // client version (not actual game version, set by apk)
        }

        public override int GetMessageType()
        {
            return 10101;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
