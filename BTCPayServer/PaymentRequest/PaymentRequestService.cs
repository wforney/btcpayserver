using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using BTCPayServer.Models.PaymentRequestViewModels;
using BTCPayServer.Payments;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.PaymentRequests;
using BTCPayServer.Services.Rates;
using PaymentRequestData = BTCPayServer.Data.PaymentRequestData;

namespace BTCPayServer.PaymentRequest;

public class PaymentRequestService
{
    private readonly PaymentRequestRepository _PaymentRequestRepository;
    private readonly BTCPayNetworkProvider _BtcPayNetworkProvider;
    private readonly AppService _AppService;
    private readonly CurrencyNameTable _currencies;

    public PaymentRequestService(
        PaymentRequestRepository paymentRequestRepository,
        BTCPayNetworkProvider btcPayNetworkProvider,
        AppService appService,
        CurrencyNameTable currencies)
    {
        _PaymentRequestRepository = paymentRequestRepository;
        _BtcPayNetworkProvider = btcPayNetworkProvider;
        _AppService = appService;
        _currencies = currencies;
    }

    public async Task UpdatePaymentRequestStateIfNeeded(string id)
    {
        PaymentRequestData pr = await _PaymentRequestRepository.FindPaymentRequest(id, null);
        await UpdatePaymentRequestStateIfNeeded(pr);
    }

    public async Task UpdatePaymentRequestStateIfNeeded(PaymentRequestData pr)
    {
        PaymentRequestBaseData blob = pr.GetBlob();
        Client.Models.PaymentRequestData.PaymentRequestStatus currentStatus = pr.Status;
        if (blob.ExpiryDate.HasValue)
        {
            if (blob.ExpiryDate.Value <= DateTimeOffset.UtcNow)
            {
                currentStatus = Client.Models.PaymentRequestData.PaymentRequestStatus.Expired;
            }
        }
        else if (currentStatus != Client.Models.PaymentRequestData.PaymentRequestStatus.Completed)
        {
            currentStatus = Client.Models.PaymentRequestData.PaymentRequestStatus.Pending;
        }

        if (currentStatus != Client.Models.PaymentRequestData.PaymentRequestStatus.Expired)
        {
            InvoiceEntity[] invoices = await _PaymentRequestRepository.GetInvoicesForPaymentRequest(pr.Id);
            Models.AppViewModels.ViewCrowdfundViewModel.Contributions contributions = _AppService.GetContributionsByPaymentMethodId(blob.Currency, invoices, true);

            currentStatus = contributions.TotalCurrency >= blob.Amount
                ? Client.Models.PaymentRequestData.PaymentRequestStatus.Completed
                : Client.Models.PaymentRequestData.PaymentRequestStatus.Pending;
        }

        if (currentStatus != pr.Status)
        {
            pr.Status = currentStatus;
            await _PaymentRequestRepository.UpdatePaymentRequestStatus(pr.Id, currentStatus);
        }
    }

    public async Task<ViewPaymentRequestViewModel> GetPaymentRequest(string id, string userId = null)
    {
        PaymentRequestData pr = await _PaymentRequestRepository.FindPaymentRequest(id, null);
        if (pr == null)
        {
            return null;
        }

        PaymentRequestBaseData blob = pr.GetBlob();

        InvoiceEntity[] invoices = await _PaymentRequestRepository.GetInvoicesForPaymentRequest(id);

        Models.AppViewModels.ViewCrowdfundViewModel.Contributions paymentStats = _AppService.GetContributionsByPaymentMethodId(blob.Currency, invoices, true);
        var amountDue = blob.Amount - paymentStats.TotalCurrency;
        InvoiceEntity pendingInvoice = invoices.OrderByDescending(entity => entity.InvoiceTime)
            .FirstOrDefault(entity => entity.Status == InvoiceStatusLegacy.New);

        return new ViewPaymentRequestViewModel(pr)
        {
            Archived = pr.Archived,
            AmountFormatted = _currencies.FormatCurrency(blob.Amount, blob.Currency),
            AmountCollected = paymentStats.TotalCurrency,
            AmountCollectedFormatted = _currencies.FormatCurrency(paymentStats.TotalCurrency, blob.Currency),
            AmountDue = amountDue,
            AmountDueFormatted = _currencies.FormatCurrency(amountDue, blob.Currency),
            CurrencyData = _currencies.GetCurrencyData(blob.Currency, true),
            LastUpdated = DateTime.UtcNow,
            AnyPendingInvoice = pendingInvoice != null,
            PendingInvoiceHasPayments = pendingInvoice != null &&
                                        pendingInvoice.ExceptionStatus != InvoiceExceptionStatus.None,
            Invoices = invoices.Select(entity =>
            {
                InvoiceState state = entity.GetInvoiceState();
                var payments = entity
                    .GetPayments(true)
                    .Select(paymentEntity =>
                    {
                        CryptoPaymentData paymentData = paymentEntity.GetCryptoPaymentData();
                        PaymentMethodId paymentMethodId = paymentEntity.GetPaymentMethodId();
                        if (paymentData is null || paymentMethodId is null)
                        {
                            return null;
                        }

                        string txId = paymentData.GetPaymentId();
                        string link = GetTransactionLink(paymentMethodId, txId);
                        PaymentMethod paymentMethod = entity.GetPaymentMethod(paymentMethodId);
                        var amount = paymentData.GetValue();
                        var rate = paymentMethod.Rate;
                        var paid = (amount - paymentEntity.NetworkFee) * rate;

                        return new ViewPaymentRequestViewModel.PaymentRequestInvoicePayment
                        {
                            Amount = amount,
                            Paid = paid,
                            ReceivedDate = paymentEntity.ReceivedTime.DateTime,
                            PaidFormatted = _currencies.FormatCurrency(paid, blob.Currency),
                            RateFormatted = _currencies.FormatCurrency(rate, blob.Currency),
                            PaymentMethod = paymentMethodId.ToPrettyString(),
                            Link = link,
                            Id = txId,
                            Destination = paymentData.GetDestination()
                        };
                    })
                    .Where(payment => payment != null)
                    .ToList();

                if (state.Status == InvoiceStatusLegacy.Invalid ||
                    state.Status == InvoiceStatusLegacy.Expired && !payments.Any())
                {
                    return null;
                }

                return new ViewPaymentRequestViewModel.PaymentRequestInvoice
                {
                    Id = entity.Id,
                    Amount = entity.Price,
                    AmountFormatted = _currencies.FormatCurrency(entity.Price, blob.Currency),
                    Currency = entity.Currency,
                    ExpiryDate = entity.ExpirationTime.DateTime,
                    State = state,
                    StateFormatted = state.ToString(),
                    Payments = payments
                };
            })
            .Where(invoice => invoice != null)
            .ToList()
        };
    }

    private string GetTransactionLink(PaymentMethodId paymentMethodId, string txId)
    {
        BTCPayNetworkBase network = _BtcPayNetworkProvider.GetNetwork(paymentMethodId.CryptoCode);
        if (network == null)
        {
            return null;
        }

        return paymentMethodId.PaymentType.GetTransactionLink(network, txId);
    }
}
