var token = $('input[name="__RequestVerificationToken"]').val();

function escapeHtml(str) {
    if (str == null) return '';
    return $('<span>').text(str).html();
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    var d = new Date(dateStr);
    var day = ('0' + d.getDate()).slice(-2);
    var month = ('0' + (d.getMonth() + 1)).slice(-2);
    var year = d.getFullYear();
    return day + '/' + month + '/' + year;
}

function getFilters() {
    return {
        dateFrom: $('#filter-from').val() || null,
        dateTo: $('#filter-to').val() || null,
        customerId: $('#filter-customer').val() || null,
        transactionType: $('#filter-type').val() || null
    };
}

function buildQueryString(filters) {
    var params = [];
    if (filters.dateFrom) params.push('dateFrom=' + encodeURIComponent(filters.dateFrom));
    if (filters.dateTo) params.push('dateTo=' + encodeURIComponent(filters.dateTo));
    if (filters.customerId) params.push('customerId=' + encodeURIComponent(filters.customerId));
    if (filters.transactionType) params.push('transactionType=' + encodeURIComponent(filters.transactionType));
    return params.length ? '?' + params.join('&') + '&handler=Load' : '?handler=Load';
}

function renderTransactions(data, selectedType) {
    var container = $('#transactions-table-container');
    if (!data || data.length === 0) {
        container.html('<p class="p-3 text-muted">No hay transacciones para los filtros seleccionados.</p>');
        return;
    }

    var typeLabels = { 'Charge': 'Cargo', 'Payment': 'Pago', 'Void': 'Anulación' };

    var rows = data.map(function (r) {
        return '<tr>' +
            '<td>' + escapeHtml(r.orderName || '—') + '</td>' +
            '<td>' + escapeHtml(r.customerName || '—') + '</td>' +
            '<td>' + escapeHtml(typeLabels[r.transactionType] || r.transactionType) + '</td>' +
            '<td>' + escapeHtml(r.transactionDescription) + '</td>' +
            '<td class="text-right">' + formatMoney(r.transactionAmount, localCurrencySign) + '</td>' +
            '<td>' + formatDate(r.transactionDate) + '</td>' +
            '</tr>';
    }).join('');

    var totalRow = '';
    if (selectedType) {
        var total = data.reduce(function (sum, r) { return sum + r.transactionAmount; }, 0);
        totalRow = '<tr class="font-weight-bold table-active">' +
            '<td colspan="4">Total</td>' +
            '<td class="text-right">' + formatMoney(total, localCurrencySign) + '</td>' +
            '<td></td>' +
            '</tr>';
    }

    var html = '<table class="table table-sm table-bordered table-hover mb-0">' +
        '<thead class="thead-light">' +
        '<tr>' +
        '<th>Orden</th>' +
        '<th>Cliente</th>' +
        '<th>Tipo</th>' +
        '<th>Descripción</th>' +
        '<th class="text-right">Monto</th>' +
        '<th>Fecha</th>' +
        '</tr>' +
        '</thead>' +
        '<tbody>' + rows + totalRow + '</tbody>' +
        '</table>';

    container.html(html);
}

function loadTransactions() {
    var filters = getFilters();
    var url = buildQueryString(filters);
    $('#transactions-table-container').html('<p class="p-3 text-muted">Cargando...</p>');

    $.get(url, function (data) {
        renderTransactions(data, filters.transactionType);
    }).fail(function () {
        $('#transactions-table-container').html('<p class="p-3 text-danger">Error al cargar las transacciones.</p>');
    });
}

$(function () {
    // Load customers for both filter dropdown and modal dropdown
    $.get('/Customers/Index?handler=Load', function (data) {
        var filterSel = $('#filter-customer');
        var txSel = $('#tx-customer');
        data.forEach(function (c) {
            var text = c.nickName || c.name;
            filterSel.append($('<option>').val(c.id).text(text));
            txSel.append($('<option>').val(c.id).text(text));
        });
    });

    // Filter button
    $('#btn-filter').on('click', function () {
        loadTransactions();
    });

    // Open modal
    $('#btn-nueva-tx').on('click', function () {
        $('#tx-customer').val('');
        $('#tx-type').val('');
        $('#tx-amount').val('');
        $('#tx-description').val('');
        $('#tx-error').hide().text('');
        $('#modal-new-tx').modal('show');
    });

    // Save manual transaction
    $('#btn-guardar-tx').on('click', function () {
        $('#tx-error').hide().text('');
        var customerId = $('#tx-customer').val();
        var transactionType = $('#tx-type').val();
        var amount = parseFloat($('#tx-amount').val()) || 0;
        var description = $('#tx-description').val().trim();

        if (!customerId) {
            $('#tx-error').text('Seleccione un cliente.').show();
            return;
        }
        if (!transactionType) {
            $('#tx-error').text('Seleccione un tipo de transacción.').show();
            return;
        }
        if (amount <= 0) {
            $('#tx-error').text('El monto debe ser mayor a cero.').show();
            return;
        }

        $('#btn-guardar-tx').prop('disabled', true);

        $.ajax({
            url: '?handler=CreateManual',
            method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({
                customerId: customerId,
                transactionType: transactionType,
                amount: amount,
                description: description || null
            }),
            success: function (r) {
                $('#btn-guardar-tx').prop('disabled', false);
                if (r.success) {
                    $('#modal-new-tx').modal('hide');
                    loadTransactions();
                } else {
                    $('#tx-error').text(r.error || 'Error al guardar la transacción.').show();
                }
            },
            error: function () {
                $('#btn-guardar-tx').prop('disabled', false);
                $('#tx-error').text('Error de conexión al guardar la transacción.').show();
            }
        });
    });

    // Initial load
    loadTransactions();
});
