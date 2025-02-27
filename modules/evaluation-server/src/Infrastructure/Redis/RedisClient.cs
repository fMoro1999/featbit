using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Redis;

public class RedisClient : IRedisClient
{
    private readonly Lazy<ConnectionMultiplexer> _lazyConnection;
    private IConnectionMultiplexer ConnectionMultiplexer => _lazyConnection.Value;

    public RedisClient(IOptions<RedisOptions> options)
    {
        var value = options.Value;

        _lazyConnection = new Lazy<ConnectionMultiplexer>(
            () => StackExchange.Redis.ConnectionMultiplexer.Connect(value.ConnectionString)
        );
    }

    public bool IsConnected => ConnectionMultiplexer.IsConnected;

    public IDatabase GetDatabase() => ConnectionMultiplexer.GetDatabase();

    public ISubscriber GetSubscriber() => ConnectionMultiplexer.GetSubscriber();
}