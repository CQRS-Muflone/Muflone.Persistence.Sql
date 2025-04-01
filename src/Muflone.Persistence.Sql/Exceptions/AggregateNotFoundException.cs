using System.Runtime.CompilerServices;
using Muflone.Core;

namespace Muflone.Persistence.Sql.Exceptions;

public class AggregateNotFoundException : Exception
{
    public readonly IDomainId Id;
    public readonly Type Type;

    public AggregateNotFoundException(IDomainId id, Type type)
    {
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
        interpolatedStringHandler.AppendLiteral("Aggregate '");
        interpolatedStringHandler.AppendFormatted(id.Value);
        interpolatedStringHandler.AppendLiteral("' (type ");
        interpolatedStringHandler.AppendFormatted(type.Name);
        interpolatedStringHandler.AppendLiteral(") was not found.");

        Id = id;
        Type = type;
    }
}