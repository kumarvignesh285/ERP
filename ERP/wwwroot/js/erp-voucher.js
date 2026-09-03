window.ErpVoucher = (function () {
    function editVoucher(id, ledgersList) {
        $.getJSON('/Accounts/GetVoucher/' + id, function (v) {
            $('#listView').hide();
            $('#formView').show();
            $('#Id').val(window.getVal(v, 'id'));
            $('#VoucherNumber').val(window.getVal(v, 'voucherNumber'));
            
            var rawDate = window.getVal(v, 'voucherDate');
            if (rawDate) {
                $('#VoucherDate').val(rawDate.split('T')[0]);
            }
            
            $('#ReferenceNumber').val(window.getVal(v, 'referenceNumber') || '');
            $('#Narration').val(window.getVal(v, 'narration') || '');
            $('#itemsTable tbody').empty();

            var items = window.getVal(v, 'items') || [];
            items.forEach(function (item) {
                var optionsHtml = '<option value="">Select Ledger Account</option>';
                ledgersList.forEach(function (l) {
                    var lId = window.getVal(l, 'id');
                    var lName = window.getVal(l, 'ledgerName');
                    var lCode = window.getVal(l, 'ledgerCode');
                    optionsHtml += '<option value="' + lId + '">' + lName + ' (' + lCode + ')</option>';
                });

                var debit = window.getVal(item, 'debitAmount', 0);
                var credit = window.getVal(item, 'creditAmount', 0);
                var parts = window.getVal(item, 'particulars') || '';

                var rowHtml =
                    '<tr class="grid-item-row">' +
                    '<td><select class="form-select select2-row" required>' + optionsHtml + '</select></td>' +
                    '<td><input type="number" class="form-control debit-input" value="' + debit + '" step="0.01" oninput="onDebitInput(this)" required /></td>' +
                    '<td><input type="number" class="form-control credit-input" value="' + credit + '" step="0.01" oninput="onCreditInput(this)" required /></td>' +
                    '<td><input type="text" class="form-control particulars-input" value="' + parts + '" /></td>' +
                    '<td class="text-center"><button type="button" class="btn btn-sm btn-outline-danger" onclick="removeRow(this)"><i class="fa-solid fa-trash"></i></button></td>' +
                    '</tr>';

                var $row = $(rowHtml);
                $('#itemsTable tbody').append($row);
                window.initializeSelect2($row.find('.select2-row'));
                $row.find('.select2-row').val(window.getVal(item, 'ledgerId')).trigger('change');
            });

            if (typeof calculateTotals === 'function') {
                calculateTotals();
            }
        });
    }

    return { editVoucher: editVoucher };
})();
