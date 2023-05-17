namespace System.Collections.Generic;

public static class EnumerableExtensions
{
    public static bool SequenceIgnoreEqual<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2)
    {
        if (sequence1 == sequence2)
        {
            return true;
        }
        else if (sequence1 == null || sequence2 == null)
        {
            return false;
        }
        else
        {
            return sequence1.OrderBy(e => e).SequenceEqual(sequence2.OrderBy(e => e));
        }
    }
}
