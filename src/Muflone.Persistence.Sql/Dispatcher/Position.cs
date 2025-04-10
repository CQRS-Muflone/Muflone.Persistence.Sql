namespace Muflone.Persistence.Sql.Dispatcher;

public class Position : 
    IEquatable<Position>,
    IComparable<Position>,
    IComparable
{
    public static readonly Position Start = new Position(0, 0);
    public static readonly Position End = new Position(long.MaxValue, long.MaxValue);
    
    public readonly long CommitPosition;
    public readonly long PreparePosition;
    
    public Position(long commitPosition, long preparePosition)
    {
        if (commitPosition < preparePosition)
            throw new ArgumentOutOfRangeException(nameof (commitPosition), "The commit position cannot be less than the prepare position");
        if (commitPosition > long.MaxValue && commitPosition != long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof (commitPosition));
        if (preparePosition > long.MaxValue && preparePosition != long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof (preparePosition));
        
        CommitPosition = commitPosition;
        PreparePosition = preparePosition;
    }
    
    public static bool operator <(Position p1, Position p2)
    {
        if (p1.CommitPosition < p2.CommitPosition)
            return true;
        return (long) p1.CommitPosition == (long) p2.CommitPosition && p1.PreparePosition < p2.PreparePosition;
    }
    
    public static bool operator >(Position p1, Position p2)
    {
        if (p1.CommitPosition > p2.CommitPosition)
            return true;
        return (long) p1.CommitPosition == (long) p2.CommitPosition && p1.PreparePosition > p2.PreparePosition;
    }
    
    public static bool operator >=(Position p1, Position p2) => p1 > p2 || p1 == p2;

    /// <summary>Compares whether p1 &lt;= p2.</summary>
    /// <param name="p1">A <see cref="T:Muflone.Persistence.Sql.Dispatcher.Position" />.</param>
    /// <param name="p2">A <see cref="T:Muflone.Persistence.Sql.Dispatcher.Position" />.</param>
    /// <returns>True if p1 &lt;= p2.</returns>
    public static bool operator <=(Position p1, Position p2) => p1 < p2 || p1 == p2;
    
    public static bool operator ==(Position p1, Position p2)
    {
        return Equals((object) p1, (object) p2);
    }
    
    public static bool operator !=(Position p1, Position p2) => !(p1 == p2);
    
    public int CompareTo(Position other)
    {
        if (this == other)
            return 0;
        return !(this > other) ? -1 : 1;
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj == null)
            return 1;
        if (obj is Position other)
            return CompareTo(other);
        throw new ArgumentException("Object is not a Position");
    }
    
    public override bool Equals(object? obj) => obj is Position other && Equals(other);

    /// <summary>
    /// Compares this instance of <see cref="T:Muflone.Persistence.Sql.Dispatcher.Position" /> for equality
    /// with another instance.
    /// </summary>
    /// <param name="other">A <see cref="T:Muflone.Persistence.Sql.Dispatcher.Position" /></param>
    /// <returns>True if this instance is equal to the other instance.</returns>
    public bool Equals(Position other)
    {
        return (long) CommitPosition == (long) other.CommitPosition && (long) PreparePosition == (long) other.PreparePosition;
    }
    
    // public override int GetHashCode()
    // {
    //     return (int) HashCode.Hash.Combine<ulong>(CommitPosition).Combine<ulong>(PreparePosition);
    // }

    public static bool TryParse(string value, out Position? position)
    {
        position = new Position(0, 0);
        string[] strArray = value.Split('/');
        long p1;
        long p2;
        if (strArray.Length != 2 || !TryParsePosition("C", strArray[0], out p1) ||
            !TryParsePosition("P", strArray[1], out p2))
            return false;
        position = new Position(p1, p2);
        return true;

        static bool TryParsePosition(string expectedPrefix, string v, out long p)
        {
            p = 0;
            string[] strArray = v.Split(':');
            return strArray.Length == 2 && !(strArray[0] != expectedPrefix) && long.TryParse(strArray[1], out p);
        }
    }
}