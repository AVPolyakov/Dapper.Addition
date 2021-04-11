namespace PlainQueryExtensions
{
    public interface IConnectionHandler<out T>: IHandler<T>
    {
        string ConnectionString { get; }
    }
}