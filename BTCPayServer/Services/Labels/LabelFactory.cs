using System.Drawing;
using BTCPayServer.Client.Models;
using BTCPayServer.Data;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Services.Labels;

public class LabelFactory
{
    private readonly LinkGenerator _linkGenerator;

    public LabelFactory(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public IEnumerable<ColoredLabel> ColorizeTransactionLabels(WalletBlobInfo walletBlobInfo, WalletTransactionInfo transactionInfo,
        HttpRequest request)
    {
        foreach (KeyValuePair<string, LabelData> label in transactionInfo.Labels)
        {
            walletBlobInfo.LabelColors.TryGetValue(label.Value.Text, out var color);
            yield return CreateLabel(label.Value, color, request);
        }
    }

    public IEnumerable<ColoredLabel> GetWalletColoredLabels(WalletBlobInfo walletBlobInfo, HttpRequest request)
    {
        foreach (KeyValuePair<string, string> kv in walletBlobInfo.LabelColors)
        {
            yield return CreateLabel(new RawLabel() { Text = kv.Key }, kv.Value, request);
        }
    }

    private const string DefaultColor = "#000";
    private ColoredLabel CreateLabel(LabelData uncoloredLabel, string color, HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(uncoloredLabel);
        color ??= DefaultColor;

        ColoredLabel coloredLabel = new ColoredLabel
        {
            Text = uncoloredLabel.Text,
            Color = color,
            TextColor = TextColor(color)
        };
        if (uncoloredLabel is ReferenceLabel refLabel)
        {
            var refInLabel = string.IsNullOrEmpty(refLabel.Reference) ? string.Empty : $"({refLabel.Reference})";
            switch (uncoloredLabel.Type)
            {
                case "invoice":
                    coloredLabel.Tooltip = $"Received through an invoice {refInLabel}";
                    coloredLabel.Link = string.IsNullOrEmpty(refLabel.Reference)
                            ? null
                            : _linkGenerator.InvoiceLink(refLabel.Reference, request.Scheme, request.Host, request.PathBase);
                    break;
                case "payment-request":
                    coloredLabel.Tooltip = $"Received through a payment request {refInLabel}";
                    coloredLabel.Link = string.IsNullOrEmpty(refLabel.Reference)
                            ? null
                            : _linkGenerator.PaymentRequestLink(refLabel.Reference, request.Scheme, request.Host, request.PathBase);
                    break;
                case "app":
                    coloredLabel.Tooltip = $"Received through an app {refInLabel}";
                    coloredLabel.Link = string.IsNullOrEmpty(refLabel.Reference)
                        ? null
                        : _linkGenerator.AppLink(refLabel.Reference, request.Scheme, request.Host, request.PathBase);
                    break;
                case "pj-exposed":
                    coloredLabel.Tooltip = $"This UTXO was exposed through a PayJoin proposal for an invoice {refInLabel}";
                    coloredLabel.Link = string.IsNullOrEmpty(refLabel.Reference)
                        ? null
                        : _linkGenerator.InvoiceLink(refLabel.Reference, request.Scheme, request.Host, request.PathBase);
                    break;
            }
        }
        else if (uncoloredLabel is PayoutLabel payoutLabel)
        {
            coloredLabel.Tooltip = $"Paid a payout of a pull payment ({payoutLabel.PullPaymentId})";
            coloredLabel.Link = string.IsNullOrEmpty(payoutLabel.PullPaymentId) || string.IsNullOrEmpty(payoutLabel.WalletId)
                ? null
                : _linkGenerator.PayoutLink(payoutLabel.WalletId,
                    payoutLabel.PullPaymentId, request.Scheme, request.Host,
                    request.PathBase);
        }
        return coloredLabel;
    }

    private string TextColor(string bgColor)
    {
        int nThreshold = 105;
        Color bg = ColorTranslator.FromHtml(bgColor);
        int bgDelta = Convert.ToInt32((bg.R * 0.299) + (bg.G * 0.587) + (bg.B * 0.114));
        Color color = (255 - bgDelta < nThreshold) ? Color.Black : Color.White;
        return ColorTranslator.ToHtml(color);
    }
}
