using System;

namespace OrcVillage.Messaging
{
    public class TransientFailure : Exception
    {
        public TransientFailure(string message) : base(message)
        {
        }
    }
}