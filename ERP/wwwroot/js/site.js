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
});

(function () {
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
