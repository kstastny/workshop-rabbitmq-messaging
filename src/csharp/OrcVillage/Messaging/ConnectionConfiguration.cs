namespace OrcVillage.Messaging
{
    public class ConnectionConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string VHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        public string ConnectionName { get; set; }
    }
}