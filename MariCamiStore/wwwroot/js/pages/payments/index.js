var token = $('input[name="__RequestVerificationToken"]').val();
var allSaldosData = [];
var saldosRequest = null;
var balanceRequest = null;

// Saldos de Clientes scope: unchecked (default) = only the session's organization; checked = all organizations.
// This does not affect the "Saldo del Cliente" card (Saldo Global / Saldo Esta Org. are always both shown),
// nor which organization a registered payment is posted to (always the session's).
function saldosOrganizationId() {
    return $('#saldos-full-balance').is(':checked') ? null : sessionOrganizationId;
}

function loadSaldos() {
    if (saldosRequest) saldosRequest.abort();
    var orgId = saldosOrganizationId();
    var url = '?handler=Saldos' + (orgId ? '&organizationId=' + encodeURIComponent(orgId) : '');
    saldosRequest = $.get(url, function (data) {
        saldosRequest = null;
        allSaldosData = data;
        renderSaldos(data);
    }).fail(function (xhr) {
        if (xhr.statusText === 'abort') return;
        $('#saldos-table-container').html('<p class="p-3 text-danger">Error al cargar los saldos.</p>');
    });
}

function loadBalance(customerId) {
    if (!customerId) { $('#balance-card').hide(); return; }
    if (balanceRequest) balanceRequest.abort();
    var url = '?handler=Balance&customerId=' + customerId;
    balanceRequest = $.get(url, function (r) {
        balanceRequest = null;
        if (r.error) { alert(r.error); return; }
        $('#balance-global').text(formatMoney(r.globalBalance, localCurrencySign));
        $('#balance-org').text(formatMoney(r.orgBalance, localCurrencySign));
        $('#balance-card').show();
    }).fail(function (xhr) {
        if (xhr.statusText === 'abort') return;
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

    if (filtered.length === 0) {
        rows = '<tr><td colspan="2" class="text-muted">Ningún cliente coincide con el filtro</td></tr>';
    }

    // Net total of the visible rows (rounded to avoid floating point noise)
    var total = Math.round(filtered.reduce(function (s, r) { return s + r.balance; }, 0) * 100) / 100;
    var totalDisplay = (total < 0 ? '−' : '') + formatMoney(Math.abs(total), localCurrencySign);

    var html = '<table class="table table-sm table-bordered mb-0">' +
        '<thead><tr><th>Cliente</th><th class="text-right">Saldo</th></tr></thead>' +
        '<tbody>' + rows + '</tbody>' +
        '<tfoot><tr class="font-weight-bold"><td>Total</td><td class="text-right">' + totalDisplay + '</td></tr></tfoot>' +
        '</table>';
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
        loadBalance($(this).val());
    });

    // Register payment (always posted to the session's organization)
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

    // "Ver saldo completo del cliente" toggles Saldos de Clientes between session-org-only and all organizations
    $('#saldos-full-balance').on('change', function () {
        loadSaldos();
    });

    // Initial load
    loadSaldos();
});
