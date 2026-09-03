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
            '<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=Great+Vibes&display=swap" rel="stylesheet" />' +
            '<style>' +
            'body { padding: 0; font-size: 14px; color: #334155; font-family: "Inter", "Segoe UI", Roboto, Helvetica, Arial, sans-serif; background-color: #ffffff; min-height: 100vh; position: relative; }' +
            '.invoice-wrapper { padding: 150px 40px 120px 40px; }' +
            '.table-custom { width: 100%; border-collapse: collapse; margin-top: 25px; margin-bottom: 25px; }' +
            '.table-custom th { background-color: #1e3a8a !important; color: #ffffff !important; font-weight: 700; text-transform: uppercase; font-size: 11px; letter-spacing: 0.5px; padding: 12px 20px; border: none; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
            '.table-custom td { padding: 14px 20px; border-bottom: 1px solid #e2e8f0; color: #475569; }' +
            '.table-custom tbody tr:nth-child(even) { background-color: #f8fafc !important; }' +
            '.address-col-title { font-size: 13px; font-weight: 800; color: #1e3a8a; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }' +
            '.address-col-name { font-size: 16px; font-weight: 700; color: #0f172a; margin-bottom: 4px; }' +
            '.address-col-text { font-size: 13px; color: #475569; line-height: 1.4; margin-bottom: 1px; }' +
            '.total-badge-solid { background-color: #1e3a8a !important; color: #ffffff !important; font-weight: 800; padding: 10px 20px; border-radius: 4px; font-size: 16px; -webkit-print-color-adjust: exact; print-color-adjust: exact; }' +
            '.signature-font { font-family: "Great Vibes", cursive; font-size: 32px; color: #0f172a; line-height: 1; }' +
            '.header-overlay-text, .header-overlay-text * { color: #ffffff !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }' +
            '.footer-overlay-text, .footer-overlay-text * { color: #ffffff !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }' +
            '@media print {' +
            '  @page { margin: 0; }' +
            '  body { margin: 0; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }' +
            '  .header-overlay-text, .header-overlay-text * { color: #ffffff !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }' +
            '  .footer-overlay-text, .footer-overlay-text * { color: #ffffff !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }' +
            '}' +
            '</style>' +
            '</head><body>' + bodyHtml +
            '<script>window.onload=function(){setTimeout(function(){window.print();}, 500);};<\/script></body></html>'
        );
        printWindow.document.close();
    }

    function generateInvoiceHtml(company, invoice, billTitle, partyTitle, partyName) {
        var clientDetails = {
            name: partyName,
            address: '',
            city: '',
            state: '',
            country: '',
            email: '',
            phone: '',
            gst: ''
        };

        if (invoice.customer) {
            clientDetails.name = invoice.customer.customerName || partyName;
            clientDetails.address = invoice.customer.address || '';
            clientDetails.city = invoice.customer.city || '';
            clientDetails.state = invoice.customer.state || '';
            clientDetails.country = invoice.customer.country || '';
            clientDetails.email = invoice.customer.email || '';
            clientDetails.phone = invoice.customer.phone || '';
            clientDetails.gst = invoice.customer.gstNumber || '';
        } else if (invoice.supplier) {
            clientDetails.name = invoice.supplier.supplierName || partyName;
            clientDetails.address = invoice.supplier.address || '';
            clientDetails.city = invoice.supplier.city || '';
            clientDetails.state = invoice.supplier.state || '';
            clientDetails.country = invoice.supplier.country || '';
            clientDetails.email = invoice.supplier.email || '';
            clientDetails.phone = invoice.supplier.phone || '';
            clientDetails.gst = invoice.supplier.gstNumber || '';
        }

        var companyCityState = [company.city, company.state].filter(Boolean).join(', ');
        if (company.pincode) {
            companyCityState += ' - ' + company.pincode;
        }
        var companyAddress = [company.address, companyCityState, company.country].filter(Boolean).join(', ');
        var clientAddress = [clientDetails.address, clientDetails.city, clientDetails.state, clientDetails.country].filter(Boolean).join(', ');

        var showGST = invoice.withGST || false;

        var itemRows = (invoice.items || []).map(function (item, index) {
            var gstCol = showGST
                ? '<td class="text-end">' + (item.taxPercentage || 0) + '%</td>'
                : '';

            return '<tr>' +
                '<td class="text-center">' + (index + 1) + '</td>' +
                '<td>' +
                '  <div class="fw-semibold text-dark">' + (item.productName || 'N/A') + '</div>' +
                '</td>' +
                '<td class="text-center">' + Number(item.quantity || 0).toFixed(0) + '</td>' +
                '<td class="text-end">' + formatMoney(item.rate) + '</td>' +
                gstCol +
                '<td class="text-end fw-semibold text-dark">' + formatMoney(item.quantity * item.rate) + '</td>' +
                '</tr>';
        }).join('');

        var tableHeaderGST = showGST ? '<th class="text-end" style="width: 12%">GST %</th>' : '';
        var tableWidthSL = showGST ? '5%' : '8%';
        var tableWidthDesc = showGST ? '43%' : '50%';
        var tableWidthQty = showGST ? '10%' : '12%';
        var tableWidthPrice = showGST ? '12%' : '15%';
        var tableWidthTotal = showGST ? '15%' : '15%';

        var topWaveSvg = 
            '<svg viewBox="0 0 800 120" preserveAspectRatio="none" style="position: absolute; top: 0; left: 0; width: 100%; height: 120px; z-index: 1;">' +
            '  <polygon points="0,0 440,0 390,110 0,110" fill="#3b82f6" opacity="0.4" />' +
            '  <polygon points="0,0 400,0 350,100 0,100" fill="#1e3a8a" />' +
            '</svg>';

        return topWaveSvg +
            
            // High-resolution logo overlay inside top blue polygon
            '<div class="header-overlay-text" style="position: absolute; top: 15px; left: 30px; z-index: 10; display: flex; align-items: center; gap: 15px;">' +
            '  <img src="/images/company-logo.png" alt="Company Logo" style="height: 55px; width: auto; object-fit: contain; filter: drop-shadow(0px 2px 4px rgba(0,0,0,0.3));" />' +
            '  <div>' +
            '    <div style="font-weight: 800; font-size: 22px; color: #ffffff !important; letter-spacing: 0.5px; line-height: 1.1;">VMR POWER TOOLS</div>' +
            '    <div style="font-weight: 500; font-size: 11px; color: #ffffff !important; opacity: 0.8; letter-spacing: 1px; text-transform: uppercase;">Quality & Reliability</div>' +
            '  </div>' +
            '</div>' +
            
            '<div class="text-end" style="position: absolute; top: 15px; right: 40px; z-index: 10;">' +
            '  <div style="font-size: 38px; font-weight: 800; color: #1e3a8a; letter-spacing: 1.5px; text-transform: uppercase; margin-bottom: 2px;">' + billTitle + '</div>' +
            '  <div style="font-size: 13px; color: #475569;"><strong>Invoice Number:</strong> #' + invoice.invoiceNumber + '</div>' +
            '  <div style="font-size: 13px; color: #475569;"><strong>Invoice Date:</strong> ' + formatDate(invoice.invoiceDate) + '</div>' +
            '</div>' +

            '<div class="invoice-wrapper" style="padding-top: 170px; padding-bottom: 30px;">' +
            '  <div class="row" style="margin-top: 15px; margin-bottom: 45px; padding: 0 10px;">' +
            '    <div class="col-6">' +
            '      <div class="text-uppercase small text-muted font-weight-bold mb-2" style="color: #1e3a8a !important; letter-spacing: 0.5px;">Invoice To:</div>' +
            '      <div class="fw-bold text-dark fs-5 mb-2">' + clientDetails.name + '</div>' +
            '      <div class="text-muted" style="line-height: 1.6; font-size: 13px;">' +
            '        ' + clientAddress + '<br/>' +
            (clientDetails.phone ? '        Phone: ' + clientDetails.phone + '<br/>' : '') +
            (clientDetails.email ? '        Email: ' + clientDetails.email + '<br/>' : '') +
            (clientDetails.gst ? '        GSTIN: ' + clientDetails.gst : '') +
            '      </div>' +
            '    </div>' +
            '    <div class="col-6 text-end">' +
            '      <div class="text-uppercase small text-muted font-weight-bold mb-2" style="color: #1e3a8a !important; letter-spacing: 0.5px;">Invoice From:</div>' +
            '      <div class="fw-bold text-dark fs-5 mb-2">VMR Power Tools</div>' +
            '      <div class="text-muted" style="line-height: 1.6; font-size: 13px;">' +
            '        ' + companyAddress + '<br/>' +
            (company.phone ? '        Phone: ' + company.phone + '<br/>' : '') +
            (company.email ? '        Email: ' + company.email + '<br/>' : '') +
            (company.gstNumber ? '        GSTIN: ' + company.gstNumber : '') +
            '      </div>' +
            '    </div>' +
            '  </div>' +

            '  <table class="table-custom" style="margin-top: 35px; margin-bottom: 35px;">' +
            '    <thead><tr>' +
            '      <th class="text-center" style="width: ' + tableWidthSL + '">NO.</th>' +
            '      <th style="width: ' + tableWidthDesc + '">PRODUCT DESCRIPTION</th>' +
            '      <th class="text-center" style="width: ' + tableWidthQty + '">QUANTITY</th>' +
            '      <th class="text-end" style="width: ' + tableWidthPrice + '">PRICE</th>' +
            tableHeaderGST +
            '      <th class="text-end" style="width: ' + tableWidthTotal + '">TOTAL</th>' +
            '    </tr></thead>' +
            '    <tbody>' + (itemRows || '<tr><td colspan="' + (showGST ? 6 : 5) + '" class="text-center">No items</td></tr>') + '</tbody>' +
            '  </table>' +

            '  <div class="row mt-5" style="padding-top: 15px;">' +
            '    <div class="col-7">' +
            '    </div>' +
            '    <div class="col-5">' +
            '      <div class="d-flex justify-content-between mb-2">' +
            '        <span class="text-muted fw-bold small">Subtotal:</span>' +
            '        <span class="fw-semibold text-dark">' + formatMoney(invoice.subTotal) + '</span>' +
            '      </div>' +
            (showGST ?
            ('      <div class="d-flex justify-content-between mb-3">' +
            '        <span class="text-muted fw-bold small">Tax (GST):</span>' +
            '        <span class="fw-semibold text-dark">' + formatMoney(invoice.taxAmount) + '</span>' +
            '      </div>') : '') +
            '      <div class="d-flex justify-content-between align-items-center total-badge-solid">' +
            '        <span>Total:</span>' +
            '        <span>' + formatMoney(invoice.grandTotal) + '</span>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>' +

            // Dynamic footer container with terms on left, signature on right
            '<div class="invoice-footer" style="position: relative; min-height: 120px; width: 100%; page-break-inside: avoid; margin-top: 50px; border-top: 1px solid #cbd5e1; padding-top: 20px;">' +
            '  <div style="position: absolute; top: 20px; left: 40px; width: 60%;">' +
            '    <div class="fw-bold text-dark mb-1" style="font-size: 13px; color: #1e3a8a !important;">Terms & Conditions:</div>' +
            '    <div class="text-muted" style="font-size: 12px; line-height: 1.4; margin-bottom: 12px;">Payment Terms: Spot Payment (Due Immediately upon Receipt)</div>' +
            '    <div class="fw-bold" style="font-size: 15px; color: #475569; letter-spacing: 0.5px;">Thank You For Your Business</div>' +
            '  </div>' +
            '  <div style="position: absolute; top: 20px; right: 40px; text-align: right; width: 30%;">' +
            '    <div class="signature-font" style="margin-bottom: 5px;">Vinoth Kumar R</div>' +
            '    <div style="display: inline-block; width: 150px; border-top: 1px solid #cbd5e1; padding-top: 5px;" class="small text-muted font-weight-bold text-uppercase text-center">' +
            '      Authorized Signatory' +
            '    </div>' +
            '  </div>' +
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
