using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Hermes.Test
{
    [TestFixture]
    public class InboxTests
    {
        [Test]
        public async Task CanRegister()
        {
            var expectedGuid = new Guid(1024, 1024, 1024, 255, 255, 255, 255, 255, 255, 255, 255);
            var inbox = new Inbox<Message>();

            void Process()
            {
                // Register our interest in UserRequest messages
                inbox.Register<UserRequest>((i,m) => i.Push(new UserResponse(m.Message, expectedGuid)));
                // Process the next message, invoking the above
                inbox.TryProcessNext();
            }

            // Register our interest in UserResponse messages
            inbox.Register<UserResponse>((i,m) =>
            {
                // Look ma, no casting!
                Assert.That(m.Response, Is.EqualTo(expectedGuid));
            });

            // Push a UserRequest into the inbox before any handlers are registered for it.
            inbox.Push(new UserRequest("Hello World"));

            // Wait for the Request to process and be sent.
            await Task.Run(() => Process());

            // Process the next message, invoking our UserResponse handler above.
            Assert.That(inbox.TryProcessNext(), Is.True);
        }
    }
}