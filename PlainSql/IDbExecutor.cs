using System;
using System.Data;
using System.Threading.Tasks;

namespace PlainSql
{
    public interface IDbExecutor<out TDbConnection>
    {
        Task<TResult> ExecAsync<TResult>(Func<TDbConnection, Task<TResult>> func);

        public async Task ExecAsync(Func<TDbConnection, Task> func)
        {
            await ExecAsync<object?>(async connection =>
            {
                await func(connection);
                return null;
            });
        }
    }
    
    public interface IDbExecutor: IDbExecutor<IDbConnection>
    {
    }
}