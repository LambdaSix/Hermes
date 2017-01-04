using System;
using System.Threading.Tasks;

namespace Hermes
{
    public interface IInbox<TIn>
    {
        void Push(TIn message);
        void Register<TOut>(Func<IInbox<TIn>, TOut, Task> func) where TOut : class, TIn;
        void Register<TOut>(Action<IInbox<TIn>, TOut> func) where TOut : class, TIn;
        Task<bool> TryProcessNext();
    }
}