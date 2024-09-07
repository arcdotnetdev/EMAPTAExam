using RefactorThis.Persistence;
using System;
using System.Linq;

namespace RefactorThis.Domain
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceService(InvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public string ProcessPayment(Payment payment)
        {
            var inv = _invoiceRepository.GetInvoice(payment.Reference);

            var responseMessage = string.Empty;

            //validation

            if (inv is null)
            {
                throw new InvalidOperationException("There is no invoice matching this payment");
            }

            if (inv.Amount == 0)
            {
                if (inv.Payments == null || !inv.Payments.Any()) return "no payment needed";
                throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");
            }

            if (payment.Amount > inv.Amount)
            {
                return "the payment is greater than the invoice amount";
            }

            //Process
            bool isFullPaid = (inv.Payments?.Sum(x => x.Amount) != 0
            && inv.Amount == inv.Payments?.Sum(x => x.Amount));

            if (isFullPaid) return "invoice was already fully paid";

            bool isPaymentGreaterThanPartialAmount = (inv.Payments?.Sum(x => x.Amount) != 0
                && payment.Amount > (inv.Amount - inv.AmountPaid));

            if (isPaymentGreaterThanPartialAmount) return "the payment is greater than the partial amount remaining";

            inv.AmountPaid += payment.Amount;
            inv.Payments.Add(payment);

            if (inv.Type == InvoiceType.Commercial
                || payment.Amount == inv.Amount)
            {
                inv.TaxAmount += payment.Amount * 0.14m;
            }

            if ((inv.Amount - inv.AmountPaid) == payment.Amount)
            {
                responseMessage = "final partial payment received, invoice is now fully paid";
            }
            else
            {
                responseMessage = "another partial payment received, still not fully paid";
            }

            if (inv.Amount == payment.Amount
                && inv.Type == InvoiceType.Standard)
            {
                responseMessage = "invoice is now fully paid";
            }
            else
            {
                responseMessage = "invoice is now partially paid";
            }


            inv.Save();

            return responseMessage;
        }


    }
}