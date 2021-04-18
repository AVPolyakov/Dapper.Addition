using System;
using System.Threading.Tasks;

namespace PlainQuery
{
    public interface IDbExecutor<out T>
    {
        Task<TResult> ExecAsync<TResult>(Func<T, Task<TResult>> func);
    }
}