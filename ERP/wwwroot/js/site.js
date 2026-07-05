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
