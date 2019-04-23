namespace OrcVillage.Messaging
{
    public static class MessagingConstants
    {
        public const int QUEST_TIMEOUT_MS = 60000;
        
        public const string HEADER_SENDER = "x-sender";

        public const string EXCHANGE_EVENTS = "orcvillage.events";
        public const string EXCHANGE_COMMANDS = "orcvillage.commands";
        public const string EXCHANGE_DLX = "orcvillage.dlx";
        public const string QUEUE_QUESTS = "orcvillage.quests";
        public const string QUEUE_PREPARATION = "orcvillage.preparationtasks";

        public const string ROUTINGKEY_CHIEFTAIN_QUESTS = "quest";
        public const string ROUTINGKEY_CHIEFTAIN_PREPARATION = "preparationtask";
        public const string ROUTINGKEY_ORC_EVENTS = "orcevent";

        public const string EVENT_TYPE_ORCEVENT = "event.orc";
    }
}