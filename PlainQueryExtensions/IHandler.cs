using System;
using System.Threading.Tasks;

namespace PlainQueryExtensions
{
    public interface IHandler<out T>
    {
        Task<TResult> Handle<TResult>(Func<T, Task<TResult>> func);
    }
}