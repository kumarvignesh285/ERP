window.ErpPrint = (function () {
    var companyCache = null;

    function loadCompany() {
        if (companyCache) {
            return $.Deferred().resolve(companyCache).promise();
        }
        return $.getJSON('/api/erp/company').then(function (data) {
            companyCache = data;
            return data;
        });
    }

    function formatMoney(value) {
        return '₹' + Number(value || 0).toFixed(2);
    }

    function formatDate(value) {
        if (!value) return '';
        return new Date(value).toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' });
    }

    function openPrintWindow(title, bodyHtml) {
        var printWindow = window.open('', '_blank');
        if (!printWindow) {
            Swal.fire('Error!', 'Please allow pop-ups to print bills.', 'error');
            return;
        }

        printWindow.document.write(
            '<html><head><title>' + title + '</title>' +
            '<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" />' +
            '<style>' +
            'body { padding: 40px; font-size: 14px; color: #333; font-family: "Segoe UI", Roboto, Helvetica, Arial, sans-serif; }' +
            '.invoice-title { color: #2563eb; font-weight: 800; font-size: 36px; letter-spacing: 2px; text-transform: uppercase; }' +
            '.top-border { border-top: 2px solid #93c5fd; margin-top: 10px; margin-bottom: 40px; }' +
            '.bottom-border { border-top: 2px solid #93c5fd; margin-top: 40px; padding-top: 15px; }' +
            '.section-title { font-weight: 600; color: #555; }' +
            '.table { border-collapse: collapse; margin-bottom: 20px; }' +
            '.table th { background-color: #2563eb !important; color: white !important; font-weight: 600; text-transform: uppercase; padding: 10px; border: none; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
            '.table td { padding: 10px; border: none; }' +
            '.table tbody tr:nth-child(even) { background-color: #dbeafe !important; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
            '.table tbody tr:nth-child(odd) { background-color: #ffffff !important; }' +
            '.blue-box { background-color: #2563eb !important; color: white !important; padding: 8px 15px; font-weight: 600; text-transform: uppercase; display: inline-block; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
            '.footer-contact { display: flex; justify-content: space-between; color: #555; font-size: 13px; }' +
            '.signature-area { text-align: right; margin-top: 20px; }' +
            '.signature-name { font-weight: bold; font-size: 16px; margin-bottom: 0; }' +
            '.signature-role { color: #555; font-size: 13px; }' +
            '</style>' +
            '</head><body>' + bodyHtml +
            '<script>window.onload=function(){setTimeout(function(){window.print();}, 500);};<\/script></body></html>'
        );
        printWindow.document.close();
    }

    function generateInvoiceHtml(company, invoice, billTitle, partyTitle, partyName) {
        var logoHtml = company.logo
            ? '<img src="' + company.logo + '" alt="Logo" style="max-height:80px;max-width:250px;" />'
            : '<h2 style="font-weight: 800;">' + (company.companyName || 'Company') + '</h2>';

        var itemRows = (invoice.items || []).map(function (item, index) {
            return '<tr>' +
                '<td class="text-center">' + (index + 1) + '</td>' +
                '<td>' + (item.productName || item.productId) + '</td>' +
                '<td class="text-center">' + Number(item.quantity || 0).toFixed(0) + '</td>' +
                '<td class="text-end">' + formatMoney(item.rate) + '</td>' +
                '<td class="text-end">' + formatMoney(item.amount || item.totalAmount) + '</td>' +
                '</tr>';
        }).join('');

        var addressParts = [company.address, company.city, company.state, company.country].filter(Boolean).join(', ');
        var invoiceDate = formatDate(invoice.invoiceDate);

        return '' +
            '<div class="d-flex justify-content-between align-items-center">' +
            '  <div>' + logoHtml + '</div>' +
            '  <div class="invoice-title">' + billTitle + '</div>' +
            '</div>' +
            '<div class="top-border"></div>' +
            
            '<div class="row mb-5">' +
            '  <div class="col-6">' +
            '    <div class="section-title mb-1">' + partyTitle + ' :</div>' +
            '    <h4 class="fw-bold mb-2">' + partyName + '</h4>' +
            '  </div>' +
            '  <div class="col-6 text-end">' +
            '    <div class="section-title mb-1">Invoice no : <span class="fw-bold text-dark">' + invoice.invoiceNumber + '</span></div>' +
            '    <div class="mb-2">' + invoiceDate + '</div>' +
            '  </div>' +
            '</div>' +

            '<table class="table">' +
            '  <thead><tr>' +
            '    <th class="text-center" style="width: 5%">NO</th>' +
            '    <th style="width: 50%">DESCRIPTION</th>' +
            '    <th class="text-center" style="width: 10%">QTY</th>' +
            '    <th class="text-end" style="width: 15%">PRICE</th>' +
            '    <th class="text-end" style="width: 20%">TOTAL</th>' +
            '  </tr></thead>' +
            '  <tbody>' + (itemRows || '<tr><td colspan="5" class="text-center">No items</td></tr>') + '</tbody>' +
            '</table>' +
            
            '<div class="row mb-5">' +
            '  <div class="col-6"></div>' +
            '  <div class="col-6">' +
            '    <div class="d-flex justify-content-end mb-2">' +
            '      <div style="width: 120px;" class="text-end section-title">Sub Total :</div>' +
            '      <div style="width: 120px;" class="text-end">' + formatMoney(invoice.subTotal) + '</div>' +
            '    </div>' +
            '    <div class="d-flex justify-content-end mb-4">' +
            '      <div style="width: 120px;" class="text-end section-title">Tax :</div>' +
            '      <div style="width: 120px;" class="text-end">' + formatMoney(invoice.taxAmount || invoice.totalTax) + '</div>' +
            '    </div>' +
            '  </div>' +
            '</div>' +

            '<div class="row mb-4 align-items-center">' +
            '  <div class="col-6">' +
            '    <div class="blue-box mb-3">PAYMENT METHOD :</div>' +
            '    <div>Bank Name : ' + (company.bankDetails ? company.bankDetails.split(',')[0] : 'N/A') + '</div>' +
            '    <div>Account Number : ' + (company.bankDetails ? company.bankDetails : 'N/A') + '</div>' +
            '  </div>' +
            '  <div class="col-6">' +
            '    <div class="d-flex justify-content-end align-items-center">' +
            '      <div class="blue-box" style="margin-right: 15px;">GRAND TOTAL :</div>' +
            '      <div class="blue-box" style="min-width: 105px; text-align: right;">' + formatMoney(invoice.grandTotal) + '</div>' +
            '    </div>' +
            '  </div>' +
            '</div>' +
            
            '<h5 class="fw-bold mt-5 mb-4">Thank you for business with us!</h5>' +
            
            '<div class="row mt-4">' +
            '  <div class="col-6">' +
            '    <div class="fw-bold mb-2">Term and Conditions :</div>' +
            '    <p class="text-muted small" style="max-width: 300px;">' + (company.billFooterNote || 'Please send payment within 30 days of receiving this invoice. There will be 10% interest charge per month on late invoice.') + '</p>' +
            '  </div>' +
            '  <div class="col-6">' +
            '    <div class="signature-area">' +
            '      <div class="mb-1" style="font-family: cursive; font-size: 20px; color: #555;">' + (company.companyName || 'Company') + '</div>' +
            '      <div class="signature-name">' + (company.companyName || 'Authorized Signatory') + '</div>' +
            '      <div class="signature-role">Administrator</div>' +
            '    </div>' +
            '  </div>' +
            '</div>' +
            
            '<div class="bottom-border"></div>' +
            '<div class="footer-contact">' +
            '  <div>' + (company.phone || 'N/A') + '</div>' +
            '  <div>' + (company.email || 'N/A') + '</div>' +
            '  <div>' + (addressParts || 'N/A') + '</div>' +
            '</div>';
    }

    function printSalesInvoice(invoice) {
        loadCompany().then(function (company) {
            var partyName = invoice.customerName || (invoice.customer && invoice.customer.customerName) || ('Customer #' + invoice.customerId);
            var bodyHtml = generateInvoiceHtml(company, invoice, "INVOICE", "Invoice to", partyName);
            openPrintWindow('Invoice ' + invoice.invoiceNumber, bodyHtml);
        });
    }

    function printPurchaseInvoice(invoice) {
        loadCompany().then(function (company) {
            var partyName = invoice.supplierName || (invoice.supplier && invoice.supplier.supplierName) || ('Supplier #' + invoice.supplierId);
            var bodyHtml = generateInvoiceHtml(company, invoice, "PURCHASE", "Supplier", partyName);
            openPrintWindow('Purchase ' + invoice.invoiceNumber, bodyHtml);
        });
    }

    function printVoucher(voucher, voucherLabel) {
        loadCompany().then(function (company) {
            var itemRows = (voucher.items || []).map(function (item) {
                return '<tr>' +
                    '<td>' + (item.ledgerName || item.ledgerId) + '</td>' +
                    '<td class="text-end">' + formatMoney(item.debitAmount) + '</td>' +
                    '<td class="text-end">' + formatMoney(item.creditAmount) + '</td>' +
                    '<td>' + (item.particulars || '') + '</td>' +
                    '</tr>';
            }).join('');

            var body = '<div class="row mb-4 mt-4">' +
                '  <div class="col-6">' +
                '    <strong>Voucher No:</strong> ' + voucher.voucherNumber + '<br/>' +
                '    <strong>Date:</strong> ' + formatDate(voucher.voucherDate) +
                '  </div>' +
                '  <div class="col-6 text-end"><h5 class="fw-bold">Total: ' + formatMoney(voucher.totalAmount) + '</h5></div>' +
                '</div>' +
                '<table class="table table-bordered">' +
                '<thead class="table-light"><tr><th>Ledger</th><th class="text-end">Debit</th><th class="text-end">Credit</th><th>Particulars</th></tr></thead>' +
                '<tbody>' + itemRows + '</tbody>' +
                '</table>';

            openPrintWindow(voucher.voucherNumber, body);
        });
    }

    function printBillById(type, id) {
        var urlMap = {
            salesInvoice: '/Sales/GetInvoice/' + id,
            purchaseInvoice: '/Purchase/GetInvoice/' + id,
            voucher: '/Accounts/GetVoucher/' + id
        };

        var url = urlMap[type];
        if (!url) {
            return $.Deferred().reject('Unknown print type').promise();
        }

        return $.getJSON(url).then(function (data) {
            if (type === 'salesInvoice') {
                printSalesInvoice(data);
            } else if (type === 'purchaseInvoice') {
                printPurchaseInvoice(data);
            } else if (type === 'voucher') {
                printVoucher(data, data.voucherType + ' Voucher');
            }
        });
    }

    function fetchNextBillNumber(type) {
        return $.getJSON('/api/erp/next-bill-number?type=' + encodeURIComponent(type));
    }

    return {
        loadCompany: loadCompany,
        printSalesInvoice: printSalesInvoice,
        printPurchaseInvoice: printPurchaseInvoice,
        printVoucher: printVoucher,
        printBillById: printBillById,
        fetchNextBillNumber: fetchNextBillNumber,
        clearCache: function () { companyCache = null; }
    };
})();
