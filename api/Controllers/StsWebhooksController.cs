using Microsoft.AspNetCore.Mvc;

namespace OmsApi;

[ApiController]
[Route("api/webhooks/sts")]
public class StsWebhooksController(
    AbbTaxInvoiceHandler abbTaxInvoice,
    StsCreditNoteHandler creditNote,
    AbbTaxInvoiceReceivedHandler abbTaxInvoiceReceived,
    CreditNoteReceivedHandler creditNoteReceived) : ControllerBase
{
    [HttpPost("abb-tax-invoice")]
    public IResult AbbTaxInvoice([FromBody] AbbTaxInvoiceRequest req) => abbTaxInvoice.Handle(req);

    [HttpPost("credit-note")]
    public IResult CreditNote([FromBody] StsCreditNoteRequest req) => creditNote.Handle(req);

    [HttpPost("abb-tax-invoice-received")]
    public IResult AbbTaxInvoiceReceived([FromBody] AbbTaxInvoiceReceivedRequest req) => abbTaxInvoiceReceived.Handle(req);

    [HttpPost("credit-note-received")]
    public IResult CreditNoteReceived([FromBody] CreditNoteReceivedRequest req) => creditNoteReceived.Handle(req);
}
