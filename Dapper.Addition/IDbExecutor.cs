using System;
using System.Data;
using System.Threading.Tasks;

namespace Dapper.Addition
{
    public interface IDbExecutor<out TDbConnection>
    {
        Task<TResult> ExecuteAsync<TResult>(Func<TDbConnection, Task<TResult>> func);

        public async Task ExecuteAsync(Func<TDbConnection, Task> func)
        {
            await ExecuteAsync<object?>(async connection =>
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