$(function () {
    const body = $('body');

    $('[data-sidebar-toggle]').on('click', function () {
        body.toggleClass('sidebar-open');
    });

    $('[data-sidebar-close], .erp-sidebar a[href]:not([data-bs-toggle="collapse"])').on('click', function () {
        body.removeClass('sidebar-open');
    });

    $(document).on('keyup', function (event) {
        if (event.key === 'Escape') {
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

    // Reusable DataTable filtering by date range (From Date/To Date) and Status dropdown
    if ($.fn.dataTable) {
        $.fn.dataTable.ext.search.push(
            function (settings, data, dataIndex) {
                var fromDate = $('#filterFromDate').val();
                var toDate = $('#filterToDate').val();
                var status = $('#filterStatus').val();

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

                var statusMatch = true;
                if (status) {
                    statusMatch = false;
                    for (var i = 0; i < data.length; i++) {
                        var cellVal = (data[i] || "").trim().toLowerCase();
                        if (cellVal === status.trim().toLowerCase()) {
                            statusMatch = true;
                            break;
                        }
                    }
                }

                return dateMatch && statusMatch;
            }
        );

        window.applyFilters = function () {
            $('.datatable').DataTable().draw();
        };

        window.resetFilters = function () {
            $('#filterFromDate').val('');
            $('#filterToDate').val('');
            $('#filterStatus').val('');
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

        window.initializeSelect2 = function (selectorOrElements) {
            const elements = $(selectorOrElements);
            elements.each(function () {
                const select = $(this);
                const modal = select.closest('.modal');
                select.select2({
                    width: '100%',
                    dropdownParent: modal.length ? modal : $('body')
                });
            });
            return elements;
        };

        if (!$.fn.DataTable) {
            $.fn.DataTable = function () {
                return this;
            };
        }

        $(document).ajaxError(function (_event, xhr) {
            const message = xhr.responseJSON?.message || xhr.responseText || 'Request failed. Please check required fields and try again.';
            if (window.Swal) {
                Swal.fire('Error!', message, 'error');
            } else {
                window.alert(message);
            }
        });
    }
})();
