using System;
using System.Threading.Tasks;

namespace PlainQuery
{
    public interface IHandler<out T>
    {
        Task<TResult> Handle<TResult>(Func<T, Task<TResult>> func);
    }
}