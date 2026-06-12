var token = $('input[name="__RequestVerificationToken"]').val();
var allSaldosData = [];

function loadSaldos() {
    $.get('?handler=Saldos', function (data) {
        allSaldosData = data;
        renderSaldos(data);
    }).fail(function () {
        $('#saldos-table-container').html('<p class="p-3 text-danger">Error al cargar los saldos.</p>');
    });
}

function escapeHtml(str) {
    return $('<span>').text(str).html();
}

function renderSaldos(data) {
    if (!data || data.length === 0) {
        $('#saldos-table-container').html('<p class="p-3 text-muted">No hay saldos registrados.</p>');
        return;
    }
    var filterVal = $('#saldos-filter').val().toLowerCase();
    var filtered = filterVal
        ? data.filter(function (r) { return r.customerName.toLowerCase().indexOf(filterVal) >= 0; })
        : data;

    var rows = filtered.map(function (r) {
        var isNegative = r.balance < 0;
        var rowClass = isNegative ? ' class="table-success"' : '';
        var badge = isNegative
            ? '<span class="badge badge-success ml-1">Crédito a favor</span>'
            : '<span class="badge badge-warning ml-1">Saldo pendiente</span>';
        var nameDisplay = escapeHtml(r.customerName);
        if (r.isGeneric) {
            nameDisplay += ' <em class="text-muted">(Especulativo)</em>';
        }
        var absBalance = Math.abs(r.balance);
        var balanceDisplay = (isNegative ? '−' : '') + formatMoney(absBalance, localCurrencySign);
        return '<tr' + rowClass + '>' +
            '<td>' + nameDisplay + badge + '</td>' +
            '<td class="text-right">' + balanceDisplay + '</td>' +
            '</tr>';
    }).join('');

    var html = '<table class="table table-sm table-bordered mb-0">' +
        '<thead><tr><th>Cliente</th><th class="text-right">Saldo</th></tr></thead>' +
        '<tbody>' + rows + '</tbody></table>';
    $('#saldos-table-container').html(html);
}

$(function () {
    // Load payable customers only (excludes IsGeneric / Sin Cliente)
    $.get('/Customers/Index?handler=LoadPayable', function (data) {
        var sel = $('#payment-customer');
        data.forEach(function (c) {
            sel.append($('<option>').val(c.id).text(c.nickName || c.name));
        });
    });

    // Load balance on customer change
    $('#payment-customer').on('change', function () {
        var id = $(this).val();
        if (!id) { $('#balance-card').hide(); return; }

        $.get('?handler=Balance&customerId=' + id, function (r) {
            if (r.error) { alert(r.error); return; }
            $('#balance-global').text(formatMoney(r.globalBalance, localCurrencySign));
            $('#balance-org').text(formatMoney(r.orgBalance, localCurrencySign));
            $('#balance-card').show();
        });
    });

    // Register payment
    $('#btn-register-payment').on('click', function () {
        $('#payment-error').hide();
        var customerId = $('#payment-customer').val();
        var amount = parseFloat($('#payment-amount').val()) || 0;

        if (!customerId || amount <= 0) {
            $('#payment-error').text('Seleccione un cliente e ingrese un monto mayor a cero.').show();
            return;
        }

        $.ajax({
            url: '?handler=RegisterPayment', method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({ customerId: customerId, amount: amount }),
            success: function (r) {
                if (r.success) {
                    $('#payment-amount').val('');
                    $('#balance-global').text(formatMoney(r.balance.globalBalance, localCurrencySign));
                    $('#balance-org').text(formatMoney(r.balance.orgBalance, localCurrencySign));
                    loadSaldos();
                } else {
                    $('#payment-error').text(r.error).show();
                }
            }
        });
    });

    // Filter saldos table in real time
    $('#saldos-filter').on('input', function () { renderSaldos(allSaldosData); });

    // Initial load of saldos
    loadSaldos();
});
