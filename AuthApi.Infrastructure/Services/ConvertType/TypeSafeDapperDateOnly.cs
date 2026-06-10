using Dapper;
using System.Data;

namespace AuthApi.Infrastructure.Services.ConvertType
{
    public sealed class TypeSafeDapperDateOnly : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value)
        {
            return value switch
            {
                DateTime dt => DateOnly.FromDateTime(dt),
                DateOnly d => d,
                _ => throw new DataException(
                    $"Cannot convert {value.GetType()} to DateOnly")
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }
    }
}
