namespace Supercell.Laser.Logic.Command
{
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Titan.DataStream;

    public class CommandManager
    {
        private static Dictionary<int, Type> CommandTypes;

        static CommandManager()
        {
            CommandTypes = new Dictionary<int, Type>()
            {
                {215, typeof(LogicSetSupportedCreatorCommand)},
                {500, typeof(LogicGatchaCommand)},
                {503, typeof(LogicClaimDailyRewardCommand)},
                {505, typeof(LogicSetPlayerThumbnailCommand)},
                {506, typeof(LogicSelectSkinCommand)},
                {509, typeof(LogicPurchaseDoubleCoinsCommand)},
                {514, typeof(LogicDeleteNotificationCommand)},
                {515, typeof(LogicClearShopTickersCommand)},
                {517, typeof(LogicClaimRankUpRewardCommand)},
                {519, typeof(LogicPurchaseOfferCommand)},
                {520, typeof(LogicLevelUpCommand)},
                {521, typeof(LogicPurchaseHeroLvlUpMaterialCommand)},
                {525, typeof(LogicSelectCharacterCommand)},
                {527, typeof(LogicSetPlayerNameColorCommand)},
                {528, typeof(LogicViewInboxNotificationCommand)},
                {529, typeof(LogicSelectStarPowerCommand)},
                {533, typeof(LogicQuestsSeenCommand)},
                {534, typeof(LogicPurchaseBrawlPassCommand)},
                {535, typeof(LogicClaimTailRewardCommand)},
                {536, typeof(LogicPurchaseBrawlPassProgressCommand)}
            };
        }

        public static Command DecodeCommand(ByteStream stream)
        {
            int type = stream.ReadVInt();
            Command command = CommandManager.CreateCommand(type);
            if (command == null)
            {
                //Debugger.Warning("Command is unhandled: " + type);
                return null;
            }

            command.Decode(stream);
            return command;
        }

        public static Command CreateCommand(int type)
        {
            if (CommandTypes.ContainsKey(type))
            {
                return (Command)Activator.CreateInstance(CommandTypes[type]);
            }
            return null;
        }
    }
}
