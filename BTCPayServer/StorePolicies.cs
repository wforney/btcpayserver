namespace BTCPayServer;

public class StoreRoles
{
    public const string Owner = "Owner";
    public const string Guest = "Guest";
    public static IEnumerable<string> AllRoles
    {
        get
        {
            yield return Owner;
            yield return Guest;
        }
    }
}
