
using System.Data;
using Npgsql;

namespace BookingSystem.Infrastructure.Data;

public class DapperDbContext
{
    private readonly NpgsqlDataSource _dataSource;

    public DapperDbContext(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public IDbConnection CreateConnection() => _dataSource.CreateConnection();
}
