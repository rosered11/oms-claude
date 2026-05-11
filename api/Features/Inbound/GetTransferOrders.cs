namespace OmsApi;

public class GetTransferOrdersHandler(InMemoryStore store)
{
    public IResult Handle() => Results.Ok(store.TransferOrders);
}
