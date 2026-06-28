window.ErpVoucher = (function () {
    function editVoucher(id, ledgersList) {
        $.getJSON('/Accounts/GetVoucher/' + id, function (v) {
            $('#listView').hide();
            $('#formView').show();
            $('#Id').val(v.id);
            $('#VoucherNumber').val(v.voucherNumber);
            $('#VoucherDate').val(v.voucherDate.split('T')[0]);
            $('#ReferenceNumber').val(v.referenceNumber || '');
            $('#Narration').val(v.narration || '');
            $('#itemsTable tbody').empty();

            (v.items || []).forEach(function (item) {
                var optionsHtml = '<option value="">Select Ledger Account</option>';
                ledgersList.forEach(function (l) {
                    optionsHtml += '<option value="' + l.id + '">' + l.ledgerName + ' (' + l.ledgerCode + ')</option>';
                });

                var rowHtml =
                    '<tr class="grid-item-row">' +
                    '<td><select class="form-select select2-row" required>' + optionsHtml + '</select></td>' +
                    '<td><input type="number" class="form-control debit-input" value="' + item.debitAmount + '" step="0.01" oninput="onDebitInput(this)" required /></td>' +
                    '<td><input type="number" class="form-control credit-input" value="' + item.creditAmount + '" step="0.01" oninput="onCreditInput(this)" required /></td>' +
                    '<td><input type="text" class="form-control particulars-input" value="' + (item.particulars || '') + '" /></td>' +
                    '<td class="text-center"><button type="button" class="btn btn-sm btn-outline-danger" onclick="removeRow(this)"><i class="fa-solid fa-trash"></i></button></td>' +
                    '</tr>';

                var $row = $(rowHtml);
                $('#itemsTable tbody').append($row);
                window.initializeSelect2($row.find('.select2-row'));
                $row.find('.select2-row').val(item.ledgerId).trigger('change');
            });

            if (typeof calculateTotals === 'function') {
                calculateTotals();
            }
        });
    }

    return { editVoucher: editVoucher };
})();
