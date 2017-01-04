using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hermes
{
    public class Inbox<TIn> : IInbox<TIn>
    {
        private readonly Dictionary<RuntimeTypeHandle, Func<TIn, Task>> _handlers = new Dictionary<RuntimeTypeHandle, Func<TIn, Task>>();
        private readonly Queue<TIn> _queue = new Queue<TIn>();

        public void Register<TOut>(Func<IInbox<TIn>, TOut, Task> func) where TOut : class, TIn
            => _handlers.Add(typeof(TOut).TypeHandle, async m => await func(this, m as TOut));

        public void Register<TOut>(Action<IInbox<TIn>, TOut> func) where TOut : class, TIn
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

    public class MultiplexInbox<TIn> : IInbox<TIn>
    {
        /// <summary>
        /// Time allocated per handler in milliseconds
        /// </summary>
        private const int _HANDLER_TIME_MS = 100;

        private readonly ConcurrentDictionary<RuntimeTypeHandle, List<Func<TIn, Task>>> _handlers =
            new ConcurrentDictionary<RuntimeTypeHandle, List<Func<TIn, Task>>>();

        private readonly Queue<TIn> _queue = new Queue<TIn>();

        /// <inheritdoc/>
        public void Register<TOut>(Func<IInbox<TIn>, TOut, Task> func) where TOut : class, TIn
            => _handlers.AddOrUpdate(typeof(TOut).TypeHandle,
                handle => new List<Func<TIn, Task>> {async m => await func(this, m as TOut)},
                (handle, list) => list.Concat(new List<Func<TIn, Task>> {async m => await func(this, m as TOut)}).ToList());

        /// <inheritdoc/>
        public void Register<TOut>(Action<IInbox<TIn>, TOut> func) where TOut : class, TIn
            => _handlers.AddOrUpdate(typeof(TOut).TypeHandle,
                handle => new List<Func<TIn, Task>> {(m => Task.Run(() => func(this, m as TOut)))},
                (handle, list) => list.Concat(new List<Func<TIn, Task>> {m => Task.Run(() => func(this, m as TOut))}).ToList());

        /// <inheritdoc/>
        public void Push(TIn message) => _queue.Enqueue(message);

        /// <inheritdoc/>
        public async Task<bool> TryProcessNext()
        {
            var nextMessage = PeekOrDefault(_queue);

            if (nextMessage != null)
            {
                var handlerType = Type.GetTypeHandle(nextMessage);
                // ReSharper disable once CollectionNeverUpdated.Local
                if (_handlers.TryGetValue(handlerType, out var handlers))
                {
                    var waitBarrier = new Barrier(handlers.Count);

                    foreach (var handler in handlers)
                    {
                        await Task.Run(() => handler.BeginInvoke(_queue.Dequeue(), ar => waitBarrier.RemoveParticipant(), null));
                    }

                    waitBarrier.SignalAndWait(TimeSpan.FromMilliseconds(handlers.Count * _HANDLER_TIME_MS));
                    return true;
                }
            }

            return false;
        }

        private static TIn PeekOrDefault(Queue<TIn> queue) => queue.Any() ? queue.Peek() : default(TIn);
    }
}
