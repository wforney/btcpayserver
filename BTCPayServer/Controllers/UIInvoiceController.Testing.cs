using BTCPayServer.Data;
using BTCPayServer.Filters;
using BTCPayServer.Payments;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace BTCPayServer.Controllers;

public partial class UIInvoiceController
{
    public class FakePaymentRequest
    {
        public decimal Amount { get; set; }
        public string CryptoCode { get; set; } = "BTC";
    }

    public class MineBlocksRequest
    {
        public int BlockCount { get; set; } = 1;
        public string CryptoCode { get; set; } = "BTC";
    }

    [HttpPost]
    [Route("i/{invoiceId}/test-payment")]
    [CheatModeRoute]
    public async Task<IActionResult> TestPayment(string invoiceId, FakePaymentRequest request, [FromServices] Cheater cheater)
    {
        Services.Invoices.InvoiceEntity invoice = await _InvoiceRepository.GetInvoice(invoiceId);
        StoreData store = await _StoreRepository.FindStore(invoice.StoreId);

        // TODO support altcoins, not just bitcoin
        BTCPayNetwork network = _NetworkProvider.GetNetwork<BTCPayNetwork>(request.CryptoCode);
        PaymentMethodId paymentMethodId = store.GetDefaultPaymentId() ?? store.GetEnabledPaymentIds(_NetworkProvider).FirstOrDefault(p => p.CryptoCode == request.CryptoCode && p.PaymentType == PaymentTypes.BTCLike);
        var bitcoinAddressString = invoice.GetPaymentMethod(paymentMethodId).GetPaymentMethodDetails().GetPaymentDestination();
        var bitcoinAddressObj = BitcoinAddress.Create(bitcoinAddressString, network.NBitcoinNetwork);
        var BtcAmount = request.Amount;

        try
        {
            Services.Invoices.PaymentMethod paymentMethod = invoice.GetPaymentMethod(paymentMethodId);
            var rate = paymentMethod.Rate;
            var txid = cheater.CashCow.SendToAddress(bitcoinAddressObj, new Money(BtcAmount, MoneyUnit.BTC)).ToString();

            // TODO The value of totalDue is wrong. How can we get the real total due? invoice.Price is only correct if this is the 2nd payment, not for a 3rd or 4th payment. 
            var totalDue = invoice.Price;
            return Ok(new
            {
                Txid = txid,
                AmountRemaining = (totalDue - (BtcAmount * rate)) / rate,
                SuccessMessage = "Created transaction " + txid
            });
        }
        catch (Exception e)
        {
            return BadRequest(new
            {
                ErrorMessage = e.Message,
                AmountRemaining = invoice.Price
            });
        }
    }

    [HttpPost]
    [Route("i/{invoiceId}/mine-blocks")]
    [CheatModeRoute]
    public IActionResult MineBlock(string invoiceId, MineBlocksRequest request, [FromServices] Cheater cheater)
    {
        // TODO support altcoins, not just bitcoin
        BitcoinAddress blockRewardBitcoinAddress = cheater.CashCow.GetNewAddress();
        try
        {
            if (request.BlockCount > 0)
            {
                cheater.CashCow.GenerateToAddress(request.BlockCount, blockRewardBitcoinAddress);
                return Ok(new
                {
                    SuccessMessage = "Mined " + request.BlockCount + " blocks"
                });
            }
            return BadRequest(new
            {
                ErrorMessage = "Number of blocks should be > 0"
            });
        }
        catch (Exception e)
        {
            return BadRequest(new
            {
                ErrorMessage = e.Message
            });
        }
    }

    [HttpPost]
    [Route("i/{invoiceId}/expire")]
    [CheatModeRoute]
    public async Task<IActionResult> TestExpireNow(string invoiceId, [FromServices] Cheater cheater)
    {
        try
        {
            await cheater.UpdateInvoiceExpiry(invoiceId, DateTimeOffset.Now);
            return Ok(new { SuccessMessage = "Invoice is now expired." });
        }
        catch (Exception e)
        {
            return BadRequest(new { ErrorMessage = e.Message });
        }
    }
}
