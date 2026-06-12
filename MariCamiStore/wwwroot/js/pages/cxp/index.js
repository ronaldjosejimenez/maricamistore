var token = $('input[name="__RequestVerificationToken"]').val();
var currentPeriodId = null;
var periodIsClosed = false;

var TYPE_LABELS = {
    'AutoActiva': 'Auto-Activa',
    'AutoDelivered': 'Auto-Entregada',
    'SaldoAnterior': 'Saldo Anterior',
    'Manual': 'Manual'
};

function ajaxPost(handler, data, success, error) {
    $.ajax({
        url: '?handler=' + handler,
        method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(data),
        success: success,
        error: function (xhr) { if (error) error(xhr.statusText || 'Error'); }
    });
}

// ── Load period indicators ────────────────────────────────────────────────────

function loadPeriod() {
    $.get('?handler=Period', function (data) {
        if (data.noPeriod) {
            $('#cxp-init-section').show();
            $('#cxp-panel').hide();
            return;
        }
        if (data.error) {
            alert('Error al cargar el período: ' + data.error);
            return;
        }

        currentPeriodId = data.periodId;
        periodIsClosed = data.isClosed;

        $('#cxp-init-section').hide();
        $('#cxp-panel').show();

        $('#period-title').text('Control del Mes — ' + data.transactionMonth + '/' + data.transactionYear);

        if (data.exchangeRateWarning) {
            $('#tc-warning').show();
        } else {
            $('#tc-warning').hide();
        }

        // Fill indicators
        $('#por-pagar-colones').text(formatMoney(data.porPagarEnColones, '₡'));
        $('#saldos-cobrar').text(formatMoney(data.saldosPorCobrar, '₡'));
        $('#deuda-pagar').text(formatMoney(data.deudaAPagar, '₡'));
        $('#pendiente-recoger').text(formatMoney(data.pendienteDeRecoger, '₡'));
        $('#shipping-pendientes').text(formatMoney(data.shippingCRPendientesDeAplicar, '₡'));

        var posicion = data.posicion;
        $('#posicion-value')
            .text(formatMoney(posicion, '₡'))
            .removeClass('text-success text-danger')
            .addClass(posicion >= 0 ? 'text-success' : 'text-danger');

        // Per-currency "Por pagar" badges
        var $monedas = $('#por-pagar-monedas-container').empty();
        if (data.porPagarPorMoneda) {
            $.each(data.porPagarPorMoneda, function (_, bal) {
                $monedas.append(
                    '<div class="col-md-4 col-sm-6">' +
                    '<div class="info-box bg-light">' +
                    '<div class="info-box-content">' +
                    '<span class="info-box-text">Por pagar en ' + escHtml(bal.currencyName) + '</span>' +
                    '<span class="info-box-number">' + formatMoney(bal.amount, bal.sign) + '</span>' +
                    '</div></div></div>'
                );
            });
        }

        // Editable fields
        $('#tc-input').val(data.exchangeRate);
        $('#pagos-input').val(data.pagosRealizados);
        $('#en-cuenta-input').val(data.enCuenta);

        // Toggle controls based on closed state
        var isOpen = !data.isClosed;
        $('#tc-input, #pagos-input, #en-cuenta-input').prop('disabled', !isOpen);
        $('#btn-save-fields').toggle(isOpen);
        $('#btn-add-entry').toggle(isOpen);
        $('#btn-close-period').toggle(isOpen);
    });
}

// ── Load entries ──────────────────────────────────────────────────────────────

function loadEntries() {
    $.get('?handler=Entries', function (groups) {
        var $container = $('#cxp-tables-container').empty();

        if (!groups || groups.length === 0) {
            $container.html('<p class="text-muted">Sin entradas en este período.</p>');
            return;
        }

        $.each(groups, function (_, group) {
            var rows = '';
            $.each(group.entries, function (_, e) {
                var typeLabel = TYPE_LABELS[e.type] || e.type;
                var dateStr = e.createdAt ? e.createdAt.substr(0, 10) : '';
                var deleteBtn = periodIsClosed ? '' :
                    '<button class="btn btn-xs btn-danger btn-delete-entry" data-entry-id="' + e.id + '">' +
                    '<i class="fas fa-trash"></i></button>';
                rows += '<tr>' +
                    '<td>' + escHtml(e.reference) + '</td>' +
                    '<td>' + typeLabel + '</td>' +
                    '<td class="text-right">' + formatMoney(e.amount, group.sign) + '</td>' +
                    '<td>' + dateStr + '</td>' +
                    '<td class="text-center">' + deleteBtn + '</td>' +
                    '</tr>';
            });

            var card =
                '<div class="card card-secondary mb-3">' +
                '<div class="card-header"><h3 class="card-title">' + escHtml(group.currencyName) + '</h3></div>' +
                '<div class="card-body p-0">' +
                '<table class="table table-sm table-bordered mb-0">' +
                '<thead><tr>' +
                '<th>Referencia</th><th>Tipo</th><th class="text-right">Monto</th><th>Fecha</th><th></th>' +
                '</tr></thead><tbody>' + rows + '</tbody>' +
                '<tfoot><tr>' +
                '<td colspan="2" class="text-right font-weight-bold">Subtotal</td>' +
                '<td class="text-right font-weight-bold">' + formatMoney(group.total, group.sign) + '</td>' +
                '<td colspan="2"></td>' +
                '</tr></tfoot>' +
                '</table></div></div>';
            $container.append(card);
        });
    });
}

// ── Delete entry ──────────────────────────────────────────────────────────────

$(document).on('click', '.btn-delete-entry', function () {
    var entryId = $(this).data('entry-id');
    if (!confirm('¿Eliminar esta entrada?')) return;
    deleteEntry(entryId);
});

function deleteEntry(entryId) {
    ajaxPost('DeleteEntry', { entryId: entryId }, function (r) {
        if (r.success) {
            loadPeriod();
            loadEntries();
        } else {
            alert(r.error || 'Error al eliminar la entrada.');
        }
    });
}

// ── Add entry modal ───────────────────────────────────────────────────────────

$('#btn-add-entry').on('click', function () {
    $('#entry-reference').val('');
    $('#entry-currency').val('');
    $('#entry-amount').val('');
    $('#entry-error').hide();
    $('#modal-add-entry').modal('show');
});

$('#btn-confirm-add-entry').on('click', function () {
    var ref = $('#entry-reference').val().trim();
    var currencyId = $('#entry-currency').val();
    var amount = parseFloat($('#entry-amount').val());

    if (!ref) { $('#entry-error').text('La referencia es requerida.').show(); return; }
    if (!currencyId) { $('#entry-error').text('Seleccione una moneda.').show(); return; }
    if (!amount || amount <= 0) { $('#entry-error').text('El monto debe ser mayor a cero.').show(); return; }

    addEntry(currencyId, amount, ref);
});

function addEntry(currencyId, amount, reference) {
    ajaxPost('AddEntry', { currencyId: currencyId, amount: amount, reference: reference }, function (r) {
        if (r.success) {
            $('#modal-add-entry').modal('hide');
            loadPeriod();
            loadEntries();
        } else {
            $('#entry-error').text(r.error || 'Error al agregar la entrada.').show();
        }
    });
}

// ── Save period fields ────────────────────────────────────────────────────────

$('#btn-save-fields').on('click', function () {
    savePeriodFields();
});

function savePeriodFields() {
    var tc = parseFloat($('#tc-input').val());
    var pagos = parseFloat($('#pagos-input').val());
    var enCuenta = parseFloat($('#en-cuenta-input').val());

    if (isNaN(tc) || tc < 0) { $('#panel-error').text('El tipo de cambio no puede ser negativo.').show(); return; }
    if (isNaN(pagos) || pagos < 0) { $('#panel-error').text('Pagos realizados no puede ser negativo.').show(); return; }
    if (isNaN(enCuenta) || enCuenta < 0) { $('#panel-error').text('En cuenta no puede ser negativo.').show(); return; }

    $('#panel-error').hide();
    ajaxPost('UpdatePeriod', { exchangeRate: tc, pagosRealizados: pagos, enCuenta: enCuenta }, function (r) {
        if (r.success) {
            loadPeriod();
        } else {
            $('#panel-error').text(r.error || 'Error al guardar.').show();
        }
    });
}

// ── Close period ──────────────────────────────────────────────────────────────

$('#btn-close-period').on('click', function () {
    $('#close-error').hide();
    $('#modal-close-period').modal('show');
});

$('#btn-confirm-close').on('click', function () {
    closePeriod();
});

function closePeriod() {
    ajaxPost('ClosePeriod', {}, function (r) {
        if (r.success) {
            $('#modal-close-period').modal('hide');
            window.location.reload();
        } else {
            $('#close-error').text(r.error || 'Error al cerrar el período.').show();
        }
    });
}

// ── Init period form ──────────────────────────────────────────────────────────

$('#btn-init-period').on('click', function () {
    var month = parseInt($('#init-month').val());
    var year = parseInt($('#init-year').val());
    var tc = parseFloat($('#init-tc').val());

    if (!month || month < 1 || month > 12) { $('#init-error').text('El mes debe estar entre 1 y 12.').show(); return; }
    if (!year || year < 2020) { $('#init-error').text('El año debe ser mayor o igual a 2020.').show(); return; }
    if (!tc || tc <= 0) { $('#init-error').text('El tipo de cambio debe ser mayor a cero.').show(); return; }

    $('#init-error').hide();
    ajaxPost('InitPeriod', { month: month, year: year, exchangeRate: tc }, function (r) {
        if (r.success) {
            loadPeriod();
            loadEntries();
        } else {
            $('#init-error').text(r.error || 'Error al inicializar.').show();
        }
    });
});

// ── Helpers ───────────────────────────────────────────────────────────────────

function escHtml(str) {
    return $('<div>').text(str || '').html();
}

// ── Init ──────────────────────────────────────────────────────────────────────

$(function () {
    loadPeriod();
    loadEntries();
});
