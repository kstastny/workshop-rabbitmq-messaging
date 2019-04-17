namespace OrcVillage.Messaging
{
    public static class MessagingConstants
    {
        public const string HEADER_SENDER = "x-sender";

        public const string EXCHANGE_EVENTS = "orcvillage.events";
        public const string ROUTINGKEY_ORC_EVENTS = "orcevent";

        public const string EVENT_TYPE_ORCEVENT = "event.orc";
    }
}