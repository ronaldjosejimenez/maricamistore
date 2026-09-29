var token = $('input[name="__RequestVerificationToken"]').val();
var currentPeriodId = null;
var periodIsClosed = false;

var TYPE_LABELS = {
    'AutoActiva': 'Auto-Activa',
    'AutoDelivered': 'Auto-Entregada',
    'SaldoAnterior': 'Saldo Anterior',
    'Manual': 'Manual',
    'AutoPaquete': 'Auto-Paquete',
    'ReversoPaquete': 'Reverso Paquete'
};

// Negative amounts (e.g. package reversals) are shown in red with a leading minus sign.
function formatSignedMoneyHtml(amount, sign) {
    var value = parseFloat(amount) || 0;
    if (value < 0)
        return '<span class="text-danger">−' + formatMoney(Math.abs(value), sign) + '</span>';
    return formatMoney(value, sign);
}

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
        $('#entradas-total-colonizado').text(formatMoney(data.porPagarEnColones, '₡'));
        $('#saldos-cobrar').text(formatMoney(data.saldosPorCobrar, '₡'));
        $('#deuda-pagar').text(formatMoney(data.deudaAPagar, '₡'));
        $('#en-cuenta-indicator').text(formatMoney(data.enCuenta, '₡'));
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
                    '<div class="info-box bg-light">' +
                    '<div class="info-box-content">' +
                    '<span class="info-box-text">Por pagar en ' + escHtml(bal.currencyName) + '</span>' +
                    '<span class="info-box-number">' + formatSignedMoneyHtml(bal.amount, bal.sign) + '</span>' +
                    '</div></div>'
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
                    '<td class="text-right">' + formatSignedMoneyHtml(e.amount, group.sign) + '</td>' +
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
                '<td class="text-right font-weight-bold">' + formatSignedMoneyHtml(group.total, group.sign) + '</td>' +
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

    if (isNaN(tc) || tc <= 0) { $('#panel-error').text(MSG_TC_POSITIVE).show(); return; }
    if (isNaN(pagos) || pagos < 0 || isNaN(enCuenta) || enCuenta < 0) { $('#panel-error').text(MSG_NOT_NEGATIVE).show(); return; }

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

var MSG_TC_POSITIVE = 'El tipo de cambio debe ser mayor a cero.';
var MSG_NOT_NEGATIVE = 'El valor no puede ser negativo.';
var closePreviewTimer = null;
var closePreviewRequest = null;
var closePreviewErrors = [];

$('#btn-close-period').on('click', function () {
    $('#close-error').hide();
    $('#close-preview-body').hide();
    $('#close-loading').show();
    $('#close-new-tc, #close-new-encuenta').val('');
    $('#btn-confirm-close').prop('disabled', true);
    $('#modal-close-period').modal('show');
    loadClosePreview(null, null, true);
});

$('#close-new-tc, #close-new-encuenta').on('input', function () {
    $('#btn-confirm-close').prop('disabled', true);
    clearTimeout(closePreviewTimer);
    closePreviewTimer = setTimeout(function () {
        loadClosePreview($('#close-new-tc').val(), $('#close-new-encuenta').val(), false);
    }, 400);
});

// Server-computed preview; no data is written until the user confirms.
function loadClosePreview(tc, enCuenta, firstLoad) {
    if (closePreviewRequest) closePreviewRequest.abort();
    var url = '?handler=ClosePreview&periodId=' + encodeURIComponent(currentPeriodId);
    if (tc !== null && tc !== '') url += '&exchangeRate=' + encodeURIComponent(tc);
    if (enCuenta !== null && enCuenta !== '') url += '&enCuenta=' + encodeURIComponent(enCuenta);

    closePreviewRequest = $.get(url, function (data) {
        closePreviewRequest = null;
        if (data.error) {
            showCloseError(data.error, data.alreadyClosed);
            return;
        }
        renderClosePreview(data, firstLoad);
    }).fail(function (xhr) {
        if (xhr.statusText !== 'abort') showCloseError('Error al calcular la vista previa.', false);
    });
}

function renderClosePreview(data, firstLoad) {
    var c = data.closing, n = data.newPeriod;
    $('#close-closing-title').text('Mes que cierra (' + c.transactionMonth + '/' + c.transactionYear + ')');
    $('#close-new-title').text('Mes nuevo (' + n.month + '/' + n.year + ')');

    var closingRows = indicatorRows(c);
    closingRows.push(['Tipo de Cambio', formatMoney(c.exchangeRate, '')]);
    closingRows.push(['Pagos Realizados', formatMoney(c.pagosRealizados, '₡')]);
    $('#close-closing-table').html(rowsHtml(closingRows));

    if (firstLoad) {
        $('#close-new-tc').val(n.exchangeRate);
        $('#close-new-encuenta').val(n.enCuenta);
    }
    $('#close-new-tc-hint').text('Propuesto: ' + formatMoney(n.proposedExchangeRate, ''));
    $('#close-new-encuenta-hint').text('Propuesto: ' + formatMoney(n.proposedEnCuenta, '₡'));
    $('#close-new-entries').text(n.saldoAnterior != null
        ? 'Saldo anterior: ' + formatMoney(n.saldoAnterior, '₡')
        : 'Sin saldo anterior');
    $('#close-new-table').html(rowsHtml(indicatorRows(n.indicators)));

    closePreviewErrors = data.errors || [];
    if (closePreviewErrors.length) {
        $('#close-error').text(closePreviewErrors.join(' ')).show();
    } else {
        $('#close-error').hide();
    }
    $('#close-loading').hide();
    $('#close-preview-body').show();
    $('#btn-confirm-close').prop('disabled', closePreviewErrors.length > 0);
}

function indicatorRows(ind) {
    var rows = [['Total por pagar colonizado', formatSignedMoneyHtml(ind.porPagarEnColones, '₡')]];
    $.each(ind.porPagarPorMoneda || {}, function (_, bal) {
        rows.push(['Por pagar en ' + escHtml(bal.currencyName), formatSignedMoneyHtml(bal.amount, bal.sign)]);
    });
    rows.push(['Saldos por Cobrar a Clientes', formatSignedMoneyHtml(ind.saldosPorCobrar, '₡')]);
    rows.push(['Shipping CR Pendientes', formatSignedMoneyHtml(ind.shippingCRPendientesDeAplicar, '₡')]);
    rows.push(['Deuda a Pagar', formatSignedMoneyHtml(ind.deudaAPagar, '₡')]);
    rows.push(['En Cuenta', formatSignedMoneyHtml(ind.enCuenta, '₡')]);
    rows.push(['Pendiente de Recoger', formatSignedMoneyHtml(ind.pendienteDeRecoger, '₡')]);
    rows.push(['<strong>Posición</strong>', '<strong>' + formatSignedMoneyHtml(ind.posicion, '₡') + '</strong>']);
    return rows;
}

function rowsHtml(rows) {
    return rows.map(function (r) {
        return '<tr><td>' + r[0] + '</td><td class="text-right">' + r[1] + '</td></tr>';
    }).join('');
}

function showCloseError(msg, alreadyClosed) {
    $('#close-loading').hide();
    $('#close-error').text(msg).show();
    $('#btn-confirm-close').prop('disabled', true);
    if (alreadyClosed) setTimeout(function () { window.location.reload(); }, 1500);
}

$('#btn-confirm-close').on('click', function () {
    closePeriod();
});

function closePeriod() {
    var tc = parseFloat($('#close-new-tc').val());
    var enCuenta = parseFloat($('#close-new-encuenta').val());
    if (isNaN(tc) || tc <= 0) { $('#close-error').text(MSG_TC_POSITIVE).show(); return; }
    if (isNaN(enCuenta) || enCuenta < 0) { $('#close-error').text(MSG_NOT_NEGATIVE).show(); return; }

    var $btn = $('#btn-confirm-close').prop('disabled', true);
    ajaxPost('ClosePeriod', { periodId: currentPeriodId, exchangeRate: tc, enCuenta: enCuenta }, function (r) {
        if (r.success) {
            $('#modal-close-period').modal('hide');
            window.location.reload();
        } else if (r.alreadyClosed) {
            showCloseError(r.error, true);
        } else {
            $('#close-error').text(r.error || 'Error al cerrar el período.').show();
            $btn.prop('disabled', false);
        }
    }, function (msg) {
        $('#close-error').text(msg || 'Error al cerrar el período.').show();
        $btn.prop('disabled', false);
    });
}

// ── Init period form ──────────────────────────────────────────────────────────

$('#btn-init-period').on('click', function () {
    var month = parseInt($('#init-month').val());
    var year = parseInt($('#init-year').val());
    var tc = parseFloat($('#init-tc').val());

    if (!month || month < 1 || month > 12) { $('#init-error').text('El mes debe estar entre 1 y 12.').show(); return; }
    if (!year || year < 2020) { $('#init-error').text('El año debe ser mayor o igual a 2020.').show(); return; }
    if (!tc || tc <= 0) { $('#init-error').text(MSG_TC_POSITIVE).show(); return; }

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
