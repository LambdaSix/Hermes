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

            async Task Process()
            {
                // Register our interest in UserRequest messages
                inbox.Register<UserRequest>((i, m) =>
                    i.Push(new UserResponse(m.Message, expectedGuid))
                );
                // Process the next message, invoking the above
                await inbox.TryProcessNext();
            }

            // Register our interest in UserResponse messages
            inbox.Register<UserResponse>(async (i,m) =>
            {
                await Task.Run(() => Assert.That(m.Response, Is.EqualTo(expectedGuid)));
            });

            // Push a UserRequest into the inbox before any handlers are registered for it.
            inbox.Push(new UserRequest("Hello World"));

            // Wait for the Request to process and be sent.
            await Process();

            // Process the next message, invoking our UserResponse handler above.
            Assert.That(inbox.TryProcessNext().Result, Is.True);
        }
    }
}