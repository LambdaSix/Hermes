using System;

namespace Hermes.Test
{
    public abstract class Message { }

    class UserRequest : Message
    {
        public string Message { get; }

        public UserRequest(string message)
        {
            Message = message;
        }
    }

    public class UserResponse : Message
    {
        public string Message { get; }
        public Guid Response { get; }

        public UserResponse(string message, Guid response)
        {
            Message = message;
            Response = response;
        }
    }
}
