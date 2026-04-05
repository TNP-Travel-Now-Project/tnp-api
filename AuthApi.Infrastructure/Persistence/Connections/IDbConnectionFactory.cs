using System.Data;

namespace AuthApi.Infrastructure.Persistence.Connection
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}
