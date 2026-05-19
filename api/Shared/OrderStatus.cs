namespace OmsApi;

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string PickStarted = "PickStarted";
    public const string PickConfirmed = "PickConfirmed";
    public const string Packed = "Packed";
    public const string ReadyForCollection = "ReadyForCollection";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";
    public const string Collected = "Collected";
    public const string OnHold = "OnHold";
    public const string Cancelled = "Cancelled";
    public const string Returned = "Returned";

    private static readonly HashSet<string> TerminalOrLate =
    [
        OutForDelivery, Delivered, Collected, Cancelled, Returned
    ];

    public static bool CanCancel(string status) =>
        status is Pending or OnHold;

    public static bool CanHold(string status) =>
        status is not (Cancelled or Delivered or Collected or Returned);

    public static bool CanUpdateSlot(string status) =>
        !TerminalOrLate.Contains(status);
}
