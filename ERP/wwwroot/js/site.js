$(function () {
    const body = $('body');
    const html = $('html');

    // Mobile Sidebar Drawer Toggle
    $('[data-sidebar-toggle]').on('click', function () {
        body.toggleClass('sidebar-open');
    });

    $('[data-sidebar-close], .erp-sidebar a[href]:not([data-bs-toggle="collapse"])').on('click', function () {
        body.removeClass('sidebar-open');
    });

    // Desktop Sidebar Width Collapse Toggle via Brand Logo Icon
    $('.brand-link').on('click', function (e) {
        if (window.innerWidth >= 992) {
            e.preventDefault();
            html.toggleClass('sidebar-collapsed');
            var isCollapsed = html.hasClass('sidebar-collapsed');
            localStorage.setItem('sidebarCollapsed', isCollapsed);
            
            // Adjust DataTables responsive layout after sidebar CSS transition completes
            setTimeout(function() {
                $(window).trigger('resize');
                if ($.fn.dataTable) {
                    $('.datatable').each(function() {
                        if ($.fn.DataTable.isDataTable(this)) {
                            $(this).DataTable().columns.adjust().responsive.recalc();
                        }
                    });
                }
            }, 300);
        }
    });

    // Global Keyboard Shortcut (Ctrl + K) for Quick Search
    window.focusGlobalSearch = function() {
        var input = $('#globalHeaderSearch');
        if (input.length) {
            input.focus().select();
        }
    };

    $(document).on('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            window.focusGlobalSearch();
        } else if (e.key === 'Escape') {
            body.removeClass('sidebar-open');
        }
    });

    // Premium Theme Switcher & Dropdown Sync
    function updateActiveThemeIndicator() {
        var currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        $('[data-theme-value]').removeClass('active');
        $('[data-theme-value="' + currentTheme + '"]').addClass('active');
    }

    $('[data-theme-value]').on('click', function (e) {
        e.preventDefault();
        var theme = $(this).data('theme-value');
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        updateActiveThemeIndicator();
    });

    // Run on startup
    updateActiveThemeIndicator();

    // Fix Bootstrap modal backdrop overlay issues by appending modals to body
    $('.modal').appendTo('body');

    // Reusable DataTable filtering by date range and generic prefix inputs
    if ($.fn.dataTable) {
        $.fn.dataTable.ext.search.push(
            function (settings, data, dataIndex) {
                var fromDate = $('#filterFromDate').val();
                var toDate = $('#filterToDate').val();

                var rowDate = null;
                for (var i = 0; i < data.length; i++) {
                    var cell = (data[i] || "").trim();
                    if (/^\d{2}-\d{2}-\d{4}$/.test(cell)) {
                        var parts = cell.split('-');
                        rowDate = new Date(parts[2], parts[1] - 1, parts[0]);
                        break;
                    }
                }

                var dateMatch = true;
                if (fromDate || toDate) {
                    if (!rowDate) {
                        dateMatch = false;
                    } else {
                        if (fromDate) {
                            var from = new Date(fromDate);
                            from.setHours(0, 0, 0, 0);
                            if (rowDate < from) dateMatch = false;
                        }
                        if (toDate) {
                            var to = new Date(toDate);
                            to.setHours(23, 59, 59, 999);
                            if (rowDate > to) dateMatch = false;
                        }
                    }
                }

                var genericMatch = true;
                $('[id^="filter"]').each(function() {
                    var filterId = $(this).attr('id');
                    if (filterId === 'filterFromDate' || filterId === 'filterToDate') return;

                    var filterVal = $(this).val();
                    if (filterVal) {
                        var matchFound = false;
                        var searchVal = filterVal.trim().toLowerCase();
                        for (var i = 0; i < data.length; i++) {
                            var cellVal = (data[i] || "").trim().toLowerCase();
                            if (cellVal === searchVal || cellVal.indexOf(searchVal) !== -1) {
                                matchFound = true;
                                break;
                            }
                        }
                        if (!matchFound) {
                            genericMatch = false;
                            return false; // break loop
                        }
                    }
                });

                return dateMatch && genericMatch;
            }
        );

        window.applyFilters = function () {
            $('.datatable').DataTable().draw();
        };

        window.resetFilters = function () {
            $('[id^="filter"]').each(function() {
                var element = $(this);
                element.val('');
                if (element.hasClass('select2') || element.hasClass('select2-row') || element.hasClass('form-select')) {
                    element.trigger('change');
                }
            });
            $('.datatable').DataTable().draw();
        };
    }
});

(function () {
    window.getVal = function(obj, propName, fallback) {
        if (!obj) return fallback !== undefined ? fallback : "";
        
        // 1. Exact match
        if (obj[propName] !== undefined && obj[propName] !== null) {
            return obj[propName];
        }
        
        // 2. Capitalized (PascalCase)
        var pascal = propName.charAt(0).toUpperCase() + propName.slice(1);
        if (obj[pascal] !== undefined && obj[pascal] !== null) {
            return obj[pascal];
        }
        
        // 3. Fully uppercase (e.g. mrp -> MRP)
        var upper = propName.toUpperCase();
        if (obj[upper] !== undefined && obj[upper] !== null) {
            return obj[upper];
        }

        // 4. Special cases (GST, etc.)
        if (propName.toLowerCase().startsWith("gst")) {
            var gstProp = "GST" + propName.slice(3);
            if (obj[gstProp] !== undefined && obj[gstProp] !== null) {
                return obj[gstProp];
            }
        }
        
        return fallback !== undefined ? fallback : "";
    };

    if (!window.Swal) {
        window.Swal = {
            fire: function (title, text, icon) {
                const message = [title, text].filter(Boolean).join('\n');
                if (icon === 'warning') {
                    return Promise.resolve({ isConfirmed: window.confirm(message || 'Are you sure?') });
                }

                window.alert(message || 'Done');
                return Promise.resolve({ isConfirmed: true });
            }
        };
    }

    if (!window.Chart) {
        window.Chart = function () {
            return {
                destroy: function () { },
                update: function () { }
            };
        };
    }

    if (window.jQuery) {
        if (!$.fn.select2) {
            $.fn.select2 = function () {
                return this;
            };
        }

        function formatProductOption(state) {
            if (!state.id) return state.text;
            var parts = state.text.split('|');
            if (parts.length > 1) {
                var mainText = parts[0].trim();
                var subText = parts.slice(1).map(function(p) { return p.trim(); }).join(' • ');
                return $('<div class="d-flex flex-column">' +
                    '<span class="font-weight-semibold text-dark fs-7">' + mainText + '</span>' +
                    '<span class="text-muted fs-8">' + subText + '</span>' +
                    '</div>');
            }
            return state.text;
        }

        function formatProductSelection(state) {
            if (!state.id) return state.text;
            var parts = state.text.split('|');
            return parts[0].trim();
        }

        window.initializeSelect2 = function (selectorOrElements) {
            const elements = $(selectorOrElements);
            elements.each(function () {
                const select = $(this);
                const modal = select.closest('.modal');
                var options = {
                    width: '100%',
                    dropdownParent: modal.length ? modal : $('body')
                };
                
                if (select.hasClass('select2-row')) {
                    options.templateResult = formatProductOption;
                    options.templateSelection = formatProductSelection;
                }
                
                select.select2(options);
            });
            return elements;
        };

        // Listen for changes on product selection dropdowns to show clean badges underneath
        $(document).on('change', '.select2-row', function() {
            var select = $(this);
            var selectedVal = select.val();
            var td = select.closest('td');
            
            // Remove existing badges
            td.find('.product-details-helper').remove();
            
            if (!selectedVal) return;
            
            var selectedText = select.find('option:selected').text();
            var parts = selectedText.split('|');
            if (parts.length > 1) {
                var badgeHtml = '<div class="product-details-helper mt-1 d-flex gap-2 flex-wrap" style="font-size: 0.72rem;">';
                for (var i = 1; i < parts.length; i++) {
                    var detail = parts[i].trim();
                    var badgeClass = 'bg-light text-secondary border';
                    if (detail.toLowerCase().includes('stock')) {
                        var stockNum = parseFloat(detail.replace(/[^0-9.-]/g, '')) || 0;
                        badgeClass = stockNum <= 0 ? 'bg-danger-subtle text-danger border border-danger-subtle' : 'bg-success-subtle text-success border border-success-subtle';
                    } else if (detail.toLowerCase().includes('price')) {
                        badgeClass = 'bg-primary-subtle text-primary border border-primary-subtle';
                    } else if (detail.toLowerCase().includes('gst')) {
                        badgeClass = 'bg-info-subtle text-info border border-info-subtle';
                    }
                    badgeHtml += `<span class="badge ${badgeClass} font-weight-medium">${detail}</span>`;
                }
                badgeHtml += '</div>';
                td.append(badgeHtml);
            }
        });

        if (!$.fn.DataTable) {
            $.fn.DataTable = function () {
                return this;
            };
        }
    }
})();

// =========================================================================
// Global SmartERP Application Infrastructure (ErpApp)
// Handles structured CRUD notifications, modal management, double-submit
// protection, client-side validation, field-level error mapping, and deletion.
// =========================================================================
window.ErpApp = (function ($) {
    'use strict';

    // Safe property getter for camelCase / PascalCase objects
    function getVal(obj, prop) {
        if (!obj || typeof obj !== 'object') return '';
        if (prop in obj) return obj[prop];
        var lowerProp = prop.toLowerCase();
        for (var key in obj) {
            if (key.toLowerCase() === lowerProp) {
                return obj[key];
            }
        }
        return '';
    }
    window.getVal = getVal;

    // Toast and Alert Notification Handler
    function notify(message, type, title) {
        type = (type || 'info').toLowerCase();
        if (type === 'danger') type = 'error';

        var icons = {
            success: 'fa-solid fa-circle-check text-success',
            error: 'fa-solid fa-triangle-exclamation text-danger',
            warning: 'fa-solid fa-circle-exclamation text-warning',
            info: 'fa-solid fa-circle-info text-info'
        };

        var bgClasses = {
            success: 'border-success',
            error: 'border-danger',
            warning: 'border-warning',
            info: 'border-info'
        };

        var titles = {
            success: title || 'Success',
            error: title || 'Action Failed',
            warning: title || 'Warning',
            info: title || 'Information'
        };

        var container = $('#erpToastContainer');
        if (!container.length) {
            container = $('<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 99999;" id="erpToastContainer"></div>');
            $('body').append(container);
        }

        var toastId = 'toast-' + Date.now() + '-' + Math.floor(Math.random() * 1000);
        var iconHtml = `<i class="${icons[type] || icons.info} fs-5 me-2"></i>`;

        var toastHtml = `
            <div id="${toastId}" class="toast align-items-center shadow-lg border-2 ${bgClasses[type] || 'border-primary'} bg-white mb-2" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header bg-white border-0 py-2">
                    ${iconHtml}
                    <strong class="me-auto text-dark fs-7">${titles[type]}</strong>
                    <small class="text-muted">Just now</small>
                    <button type="button" class="btn-close ms-2" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body pt-0 pb-3 fs-7 text-dark">
                    ${message}
                </div>
            </div>
        `;

        var toastElement = $(toastHtml);
        container.append(toastElement);

        if (window.bootstrap && bootstrap.Toast) {
            var bsToast = new bootstrap.Toast(toastElement[0], { delay: type === 'error' ? 8000 : 4500 });
            bsToast.show();
            toastElement.on('hidden.bs.toast', function () {
                $(this).remove();
            });
        } else if (window.Swal) {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: type === 'error' ? 'error' : (type === 'success' ? 'success' : 'info'),
                title: message,
                showConfirmButton: false,
                timer: type === 'error' ? 6000 : 3500,
                timerProgressBar: true
            });
        } else {
            alert((type === 'error' ? 'ERROR: ' : '') + message);
        }
    }

    // Set Loading State on Action Buttons
    function setButtonLoading(button, isLoading, loadingText) {
        var $btn = $(button);
        if (!$btn.length) return;

        if (isLoading) {
            if (!$btn.data('original-html')) {
                $btn.data('original-html', $btn.html());
            }
            $btn.prop('disabled', true).addClass('btn-loading');
            var text = loadingText || 'Processing...';
            $btn.html(`<span class="spinner-border spinner-border-sm me-1.5" role="status" aria-hidden="true"></span> ${text}`);
        } else {
            var orig = $btn.data('original-html');
            if (orig) {
                $btn.html(orig);
            }
            $btn.prop('disabled', false).removeClass('btn-loading');
        }
    }

    // Clear Field-level Validation Errors
    function clearFieldErrors(formOrContainer) {
        var $context = $(formOrContainer);
        $context.find('.is-invalid').removeClass('is-invalid');
        $context.find('.invalid-feedback.dynamic-feedback').remove();
    }

    // Display Field-level Errors on Form Elements
    function showFieldErrors(formOrContainer, errors) {
        var $context = $(formOrContainer);
        if (!errors) return;

        // If errors is a dictionary/object { FieldName: "Error message" }
        if (typeof errors === 'object' && !Array.isArray(errors)) {
            $.each(errors, function (field, msg) {
                if (!msg) return;
                var $field = $context.find(`[name="${field}" i], #${field}`);
                if ($field.length) {
                    $field.addClass('is-invalid');
                    var $parent = $field.closest('.input-group');
                    var target = $parent.length ? $parent : $field;
                    target.after(`<div class="invalid-feedback dynamic-feedback d-block">${msg}</div>`);
                }
            });
        } else if (Array.isArray(errors) && errors.length > 0) {
            // Array of strings -> show in notification
            notify(errors.join('<br>'), 'error', 'Validation Errors');
        }
    }

    // Client-side Form Validation
    function validateForm(form) {
        var $form = $(form);
        clearFieldErrors($form);
        var isValid = true;
        var firstInvalid = null;

        $form.find('input[required], select[required], textarea[required]').each(function () {
            var $input = $(this);
            var val = ($input.val() || '').toString().trim();
            if (!val || val === '') {
                isValid = false;
                $input.addClass('is-invalid');
                var label = $input.closest('.mb-3, .col-12, .col-md-6, .col-md-4, .col-md-3').find('label').text().replace('*', '').trim() || 'This field';
                var $parent = $input.closest('.input-group');
                var target = $parent.length ? $parent : $input;
                target.after(`<div class="invalid-feedback dynamic-feedback d-block">${label} is required.</div>`);
                if (!firstInvalid) firstInvalid = $input;
            }
        });

        // Numeric positive checks
        $form.find('input[type="number"][min="0"]').each(function () {
            var $input = $(this);
            var val = parseFloat($input.val());
            if (!isNaN(val) && val < 0) {
                isValid = false;
                $input.addClass('is-invalid');
                var $parent = $input.closest('.input-group');
                var target = $parent.length ? $parent : $input;
                target.after(`<div class="invalid-feedback dynamic-feedback d-block">Value cannot be negative.</div>`);
                if (!firstInvalid) firstInvalid = $input;
            }
        });

        if (!isValid && firstInvalid) {
            firstInvalid.focus();
        }

        return isValid;
    }

    // Universal Form Submission Handler
    function submitForm(form, options) {
        options = options || {};
        var $form = $(form);
        if (!$form.length) return;

        // Run client-side validation
        if (!validateForm($form)) {
            notify('Please fill in all required fields marked with *.', 'warning', 'Required Information Missing');
            return;
        }

        var $btn = options.button ? $(options.button) : $form.find('[type="submit"], #submitBtn, .btn-primary').first();
        var loadingText = options.loadingText || ($form.find('[name="Id"], #Id').val() > 0 ? 'Updating...' : 'Saving...');
        setButtonLoading($btn, true, loadingText);

        var url = options.url || $form.attr('action') || window.location.href;
        var method = (options.method || $form.attr('method') || 'POST').toUpperCase();
        var isJson = options.isJson || false;

        var requestData;
        var contentType = 'application/x-www-form-urlencoded; charset=UTF-8';
        var processData = true;

        if (options.data) {
            requestData = isJson ? JSON.stringify(options.data) : options.data;
            if (isJson) contentType = 'application/json; charset=UTF-8';
        } else if (isJson) {
            var formDataObj = {};
            $form.serializeArray().forEach(function (item) {
                formDataObj[item.name] = item.value;
            });
            requestData = JSON.stringify(formDataObj);
            contentType = 'application/json; charset=UTF-8';
        } else if ($form.find('input[type="file"]').length > 0) {
            requestData = new FormData($form[0]);
            contentType = false;
            processData = false;
        } else {
            requestData = $form.serialize();
        }

        $.ajax({
            url: url,
            type: method,
            data: requestData,
            contentType: contentType,
            processData: processData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function (res) {
                setButtonLoading($btn, false);

                // Handle both ApiResponse JSON and standard redirects/strings
                var success = (res && typeof res.success === 'boolean') ? res.success : true;
                var message = (res && res.message) ? res.message : 'Operation completed successfully.';

                if (success) {
                    notify(message, 'success');

                    // If modal provided or form is inside a modal, close it safely
                    var modalTarget = options.modal;
                    if (!modalTarget) {
                        var $closestModal = $form.closest('.modal');
                        if ($closestModal.length) {
                            modalTarget = $closestModal[0];
                        }
                    }

                    if (modalTarget) {
                        try {
                            if (typeof modalTarget.hide === 'function') {
                                modalTarget.hide();
                            } else {
                                var modalEl = typeof modalTarget === 'string' ? document.querySelector(modalTarget) : (modalTarget.jquery ? modalTarget[0] : modalTarget);
                                if (modalEl && window.bootstrap && bootstrap.Modal) {
                                    var bsModal = bootstrap.Modal.getInstance(modalEl) || bootstrap.Modal.getOrCreateInstance(modalEl);
                                    if (bsModal) bsModal.hide();
                                }
                            }
                        } catch (modalErr) {
                            console.warn('ErpApp.submitForm: Error closing modal:', modalErr);
                        }
                    }

                    if (typeof options.onSuccess === 'function') {
                        options.onSuccess(res);
                    } else if (options.dataTable) {
                        if ($.fn.DataTable.isDataTable(options.dataTable)) {
                            $(options.dataTable).DataTable().ajax.reload(null, false);
                        }
                    } else if (options.reloadPage !== false) {
                        setTimeout(function () {
                            window.location.reload();
                        }, 700);
                    }

                    // Reset form on create
                    if (options.resetOnCreate !== false && $form.find('[name="Id"], #Id').val() == "0") {
                        $form[0].reset();
                    }
                } else {
                    // Business failure: keep form state, keep modal open, show exact reason
                    notify(message, 'error');
                    if (res && res.errors) {
                        showFieldErrors($form, res.errors);
                    }
                    if (typeof options.onError === 'function') {
                        options.onError(res);
                    }
                }
            },
            error: function (xhr) {
                setButtonLoading($btn, false);
                var errMessage = 'An unexpected error occurred while processing your request. Please try again.';
                var fieldErrors = null;

                if (xhr.responseJSON) {
                    errMessage = xhr.responseJSON.message || errMessage;
                    fieldErrors = xhr.responseJSON.errors;
                } else if (xhr.responseText) {
                    try {
                        var parsed = JSON.parse(xhr.responseText);
                        errMessage = parsed.message || errMessage;
                        fieldErrors = parsed.errors;
                    } catch (e) {
                        if (xhr.status === 404) errMessage = 'The requested resource was not found.';
                        else if (xhr.status === 403) errMessage = 'You do not have permission to perform this action.';
                        else if (xhr.status === 500) errMessage = 'Server error occurred. Please verify data integrity.';
                    }
                }

                // Show error message and map field errors
                notify(errMessage, 'error', 'Error Occurred');
                if (fieldErrors) {
                    showFieldErrors($form, fieldErrors);
                }

                if (typeof options.onError === 'function') {
                    options.onError(xhr);
                }
            }
        });
    }

    // Universal Delete Action with Pre-checks & Confirmation
    function confirmDelete(options) {
        if (!options || !options.url) {
            console.error('ErpApp.confirmDelete: options.url is required.');
            return;
        }

        var title = options.title || 'Are you sure?';
        var message = options.message || 'Do you want to delete this record? This action cannot be undone.';
        var confirmButtonText = options.confirmButtonText || 'Yes, delete!';
        var cancelButtonText = options.cancelButtonText || 'Cancel';

        if (window.Swal) {
            Swal.fire({
                title: title,
                text: message,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                cancelButtonColor: '#6c757d',
                confirmButtonText: confirmButtonText,
                cancelButtonText: cancelButtonText,
                reverseButtons: true,
                showLoaderOnConfirm: true,
                preConfirm: function () {
                    return $.ajax({
                        url: options.url,
                        type: 'POST',
                        data: options.data || { id: options.id },
                        headers: { 'X-Requested-With': 'XMLHttpRequest' }
                    }).then(function (response) {
                        return response;
                    }).catch(function (error) {
                        var msg = error.responseJSON?.message || 'Server error occurred while deleting.';
                        Swal.showValidationMessage(msg);
                    });
                },
                allowOutsideClick: () => !Swal.isLoading()
            }).then(function (result) {
                if (result.isConfirmed && result.value) {
                    var res = result.value;
                    if (res.success) {
                        notify(res.message || 'Record deleted successfully.', 'success');

                        // Remove row from UI smoothly
                        if (options.rowElement) {
                            var $row = $(options.rowElement);
                            if (options.dataTable && $.fn.DataTable.isDataTable(options.dataTable)) {
                                $(options.dataTable).DataTable().row($row).remove().draw(false);
                            } else {
                                $row.fadeOut(300, function () { $(this).remove(); });
                            }
                        }

                        if (typeof options.onSuccess === 'function') {
                            options.onSuccess(res);
                        } else if (options.reloadPage) {
                            setTimeout(function () { window.location.reload(); }, 600);
                        }
                    } else {
                        // Business failure: stop, do not remove row, explain exact reason
                        Swal.fire({
                            title: 'Cannot Delete Record',
                            text: res.message || 'This record cannot be deleted due to existing dependencies.',
                            icon: 'error',
                            confirmButtonColor: '#3085d6',
                            confirmButtonText: 'Understand'
                        });
                        if (typeof options.onError === 'function') {
                            options.onError(res);
                        }
                    }
                }
            });
        } else {
            if (confirm(message)) {
                $.post(options.url, options.data || { id: options.id }, function (res) {
                    if (res.success) {
                        notify(res.message || 'Deleted successfully.', 'success');
                        if (options.rowElement) $(options.rowElement).remove();
                    } else {
                        notify(res.message || 'Failed to delete.', 'error');
                    }
                });
            }
        }
    }

    function openModal(modalIdOrElement) {
        if (!modalIdOrElement) return;
        try {
            var el = typeof modalIdOrElement === 'string' ? document.querySelector(modalIdOrElement) : (modalIdOrElement.jquery ? modalIdOrElement[0] : modalIdOrElement);
            if (el && window.bootstrap && bootstrap.Modal) {
                var inst = bootstrap.Modal.getOrCreateInstance(el);
                if (inst) inst.show();
            }
        } catch (e) {
            console.warn('ErpApp.openModal error:', e);
        }
    }

    function closeModal(modalIdOrElement) {
        if (!modalIdOrElement) return;
        try {
            var el = typeof modalIdOrElement === 'string' ? document.querySelector(modalIdOrElement) : (modalIdOrElement.jquery ? modalIdOrElement[0] : modalIdOrElement);
            if (el && window.bootstrap && bootstrap.Modal) {
                var inst = bootstrap.Modal.getInstance(el) || bootstrap.Modal.getOrCreateInstance(el);
                if (inst) inst.hide();
            }
        } catch (e) {
            console.warn('ErpApp.closeModal error:', e);
        }
    }

    return {
        notify: notify,
        setButtonLoading: setButtonLoading,
        clearFieldErrors: clearFieldErrors,
        showFieldErrors: showFieldErrors,
        validateForm: validateForm,
        submitForm: submitForm,
        confirmDelete: confirmDelete,
        openModal: openModal,
        closeModal: closeModal,
        getVal: getVal
    };
})(window.jQuery);

// Universal jQuery modal bridge for Bootstrap 5
if (window.jQuery && window.bootstrap && bootstrap.Modal) {
    $.fn.modal = function (action) {
        return this.each(function () {
            try {
                var instance = bootstrap.Modal.getOrCreateInstance(this);
                if (action === 'show') instance.show();
                else if (action === 'hide') instance.hide();
                else if (action === 'toggle') instance.toggle();
                else if (action === 'dispose') instance.dispose();
            } catch (err) {
                console.warn('jQuery.fn.modal error:', err);
            }
        });
    };
}

// =========================================================================
// Global Responsive Modal, Tab, and Input Group Enhancements
// =========================================================================
$(function () {
    // Universal Password Toggle Handler
    $(document).on('click', '.btn-toggle-password, [data-toggle-password], .btn-password-toggle', function (e) {
        e.preventDefault();
        var btn = $(this);
        var inputGroup = btn.closest('.input-group');
        var input = inputGroup.find('input[type="password"], input[type="text"].password-field');
        if (!input.length) {
            input = btn.siblings('input');
        }
        if (input.length) {
            var currentType = input.attr('type');
            var newType = currentType === 'password' ? 'text' : 'password';
            input.attr('type', newType);
            var icon = btn.find('i');
            if (icon.length) {
                if (newType === 'text') {
                    icon.removeClass('fa-eye fa-eye-slash').addClass('fa-eye-slash');
                } else {
                    icon.removeClass('fa-eye fa-eye-slash').addClass('fa-eye');
                }
            }
        }
    });

    // Modal Display Adjustments: Select2 dropdown parent & DataTables recalc
    $(document).on('shown.bs.modal', function (e) {
        var modal = $(e.target);
        modal.find('.select2').each(function () {
            if ($(this).hasClass("select2-hidden-accessible")) {
                $(this).select2('destroy');
            }
            if (typeof window.initializeSelect2 === 'function') {
                window.initializeSelect2(this);
            }
        });
        if ($.fn.DataTable) {
            modal.find('.datatable').each(function () {
                if ($.fn.DataTable.isDataTable(this)) {
                    $(this).DataTable().columns.adjust().responsive.recalc();
                }
            });
        }
    });

    // Tab switch: DataTable responsive recalc
    $(document).on('shown.bs.tab', function () {
        if ($.fn.DataTable) {
            $('.datatable').each(function () {
                if ($.fn.DataTable.isDataTable(this)) {
                    $(this).DataTable().columns.adjust().responsive.recalc();
                }
            });
        }
    });
});


