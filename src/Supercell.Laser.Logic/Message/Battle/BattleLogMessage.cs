namespace Supercell.Laser.Logic.Message.Battle
{
    public class BattleLogMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteBoolean(true);

            // FUCKING SHIT I'M IN A FUCKING WHEELCHAIR FUCK
            // OFFLINE BATTLES WHY DO YOU CARE ABT BATTLELOG FUCK OFF

            /* Stream.WriteVInt(1); // Count
            for (int y = 0; y < 1; y++)
            {
                // sub_AC0B0
                Stream.WriteVInt(0); // Order of player
                Stream.WriteVInt(0); // Time When Battle Log Entry Was Created
                Stream.WriteVInt(1); // Battle Log Type (1 = Normal, 2 = Crash, 3 = Survived for <time>)
                Stream.WriteVInt(8); // Trophies Result
                Stream.WriteVInt(120); // Battle Time
                Stream.WriteByte(0); // Type (0 = Power Play, 1 = Friendly, 2 = Championship)
                Stream.WriteVInt(5); // Map SCID SHOULD BE WRITEDATAREFERENCE FUCK
                Stream.WriteVInt(0); // win/lose/draw
                Stream.WriteVInt(0); // i don't fucking know

                Stream.WriteInt(0);
                Stream.WriteInt(0);

                Stream.WriteVInt(0);
                Stream.WriteByte(0);

                Stream.WriteVInt(6); // sub_55641C
                for (int i = 0; i < 6; i++)
                {
                    Stream.WriteVInt(i); // Order of player
                    Stream.WriteVInt(3); // PlayerID
                    Stream.WriteVInt(0); // IsStarPlayer (assuming 0 = false)
                    Stream.WriteBoolean(true); // IsStarPlayer
                    Stream.WriteVInt(4); // Player Brawler SHOULD BE DATAREFERENCE TOO
                    Stream.WriteVInt(0); // BrawlerTrophies
                    Stream.WriteVInt(0); // BrawlerTrophiesForRank
                    Stream.WriteVInt(0); // BrawlerID

                    // sub_57FBC4
                    Stream.WriteString("Brawler"); // Player Name
                    Stream.WriteVInt(100); // PlayerExperience
                    Stream.WriteVInt(28000000); // PlayerThumbnail
                    Stream.WriteVInt(43000000); // PlayerNameColor
                    Stream.WriteVInt(-1); // BrawlPassNameColor
                }

                Stream.WriteVInt(0);
                Stream.WriteByte(0);
                Stream.WriteVInt(0);
                Stream.WriteByte(0);
                Stream.WriteVInt(0);
            } */
        }

        public override int GetMessageType()
        {
            return 23458;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}