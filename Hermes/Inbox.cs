using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hermes
{
    public class Inbox<TIn>
    {
        private readonly Dictionary<RuntimeTypeHandle, Func<TIn, Task>> _handlers = new Dictionary<RuntimeTypeHandle, Func<TIn, Task>>();
        private readonly Queue<TIn> _queue = new Queue<TIn>();

        public Func<TIn, Task> Fetch(TIn message)
            => _handlers.TryGetValue(Type.GetTypeHandle(message), out var outVal) ? outVal : null;

        public void Register<TOut>(Func<Inbox<TIn>, TOut, Task> func) where TOut : class, TIn
            => _handlers.Add(typeof(TOut).TypeHandle, async m => await func(this, m as TOut));

        public void Register<TOut>(Action<Inbox<TIn>, TOut> func) where TOut : class, TIn
            => _handlers.Add(typeof(TOut).TypeHandle, m => Task.Run(() => func(this, m as TOut)));

        public void Push(TIn message) => _queue.Enqueue(message);

        public async Task<bool> TryProcessNext()
        {
            var nextMessage = PeekOrDefault(_queue);

            if (nextMessage != null)
            {
                var handlerType = Type.GetTypeHandle(nextMessage);
                if (_handlers.TryGetValue(handlerType, out var val))
                {
                    await val.Invoke(_queue.Dequeue());
                    return true;
                }
            }

            return false;
        }

        private static TIn PeekOrDefault(Queue<TIn> queue) => queue.Any() ? queue.Peek() : default(TIn);
    }
}
