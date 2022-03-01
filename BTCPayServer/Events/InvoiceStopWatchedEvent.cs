namespace BTCPayServer.Events;

public class InvoiceStopWatchedEvent : IHasInvoiceId
{
    public InvoiceStopWatchedEvent(string invoiceId)
    {
        InvoiceId = invoiceId;
    }
    public string InvoiceId { get; set; }
    public override string ToString()
    {
        return $"Invoice {InvoiceId} is not monitored anymore.";
    }
}
