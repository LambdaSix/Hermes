using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Hermes
{
    public class Inbox<TIn>
    {
        private readonly Dictionary<RuntimeTypeHandle, Action<TIn>> _handlers = new Dictionary<RuntimeTypeHandle, Action<TIn>>();
        private readonly Queue<TIn> _queue = new Queue<TIn>();
        private readonly Object _lockObject = new object();

        public Action<TIn> Fetch(TIn message) => _handlers.TryGetValue(Type.GetTypeHandle(message), out var outVal) ? outVal : null;

        public void Register<TOut>(Action<Inbox<TIn>,TOut> func) where TOut : class, TIn => _handlers.Add(typeof(TOut).TypeHandle, m => func(this, m as TOut));

        public void Push(TIn message) => _queue.Enqueue(message);

        public bool TryProcessNext()
        {
            if (Monitor.TryEnter(_lockObject))
            {
                var nextMessage = PeekOrDefault(_queue);

                if (nextMessage != null)
                {
                    var handlerType = Type.GetTypeHandle(nextMessage);
                    if (_handlers.TryGetValue(handlerType, out Action<TIn> val))
                    {
                        val?.Invoke(_queue.Dequeue());
                        return true;
                    }
                }
            }

            return false;
        }

        private static TIn PeekOrDefault(Queue<TIn> queue) => queue.Any() ? queue.Peek() : default(TIn);
    }
}
