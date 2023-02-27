namespace Jay.Text.Extensions;

internal static class RangeExtensions
{
    public static int Length(this Range range)
    {
        
        if (!range.Start.IsFromEnd)
        {
            if (!range.End.IsFromEnd)
            {
                return range.End.Value - range.Start.Value;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            if (!range.End.IsFromEnd)
            {
                throw new NotImplementedException();
            }
            else
            {
                
                return range.Start.Value - range.End.Value;
            }
        }
    }
}