using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AuthApi.Infrastructure.Persistence.Connection
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}
