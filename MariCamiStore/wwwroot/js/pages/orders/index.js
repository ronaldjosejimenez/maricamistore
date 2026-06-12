var token = $('input[name="__RequestVerificationToken"]').val();
var userEditedName = false;
var currencyItems = [];

function ajaxPost(handler, data, done, fail) {
    $.ajax({
        url: '?handler=' + handler, method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(data),
        success: done,
        error: function (xhr) {
            var msg = xhr.responseJSON?.error || xhr.responseText || 'Error';
            if (fail) fail(msg); else alert(msg);
        }
    });
}

var grid;

function loadGrid() {
    var filter = $('#statusFilter').val();

    if (grid) {
        $('#jsGrid').jsGrid('option', 'controller', {
            loadData: function () { return $.get('?handler=Load&statusFilter=' + encodeURIComponent(filter)); }
        });
        $('#jsGrid').jsGrid('loadData');
        return;
    }

    grid = $('#jsGrid').jsGrid({
        height: 'auto', width: '100%',
        sorting: true, paging: true, pageSize: 20, autoload: true,

        controller: {
            loadData: function () { return $.get('?handler=Load&statusFilter=' + encodeURIComponent(filter)); }
        },

        fields: [
            { name: 'id', type: 'text', visible: false },
            { name: 'nameOfOrder', title: 'Nombre', type: 'text', width: 200 },
            { name: 'statusLabel', title: 'Estado', type: 'text', width: 110 },
            { name: 'totalOfTheOrder', title: 'Total Orden', width: 120, itemTemplate: function (val, item) { var curr = currencyItems.find(function (c) { return c.id === item.currencyId; }); return formatMoney(val, curr ? (curr.sign || '') : ''); } },
            { name: 'estimatedProfitInLocal', title: 'Ganancia Est.', width: 120, itemTemplate: function (val) { return formatMoney(val, localCurrencySign); } },
            {
                title: 'Acciones', width: 260, itemTemplate: function (val, item) {
                    var btns = $('<span>');
                    // Edit + Delete (Pending only)
                    if (item.canEdit) {
                        $('<button class="btn btn-xs btn-info mr-1">Editar</button>').on('click', function () { openEditOrder(item); }).appendTo(btns);
                        $('<button class="btn btn-xs btn-danger mr-1">Eliminar</button>').on('click', function () {
                            if (!confirm('¿Eliminar esta orden y todos sus artículos?')) return;
                            ajaxPost('Delete', { id: item.id }, function (r) {
                                if (r.success) $('#jsGrid').jsGrid('loadData');
                                else alert(r.error);
                            });
                        }).appendTo(btns);
                    }
                    // Items link
                    $('<a class="btn btn-xs btn-secondary mr-1">Items</a>')
                        .attr('href', '/Orders/Items?orderId=' + item.id).appendTo(btns);
                    // Transition buttons
                    (item.nextStatuses || []).forEach(function (s) {
                        var labels = { Active: 'Activar', Delivering: 'Enviar', Delivered: 'Entregada', Completed: 'Completar', Voided: 'Anular' };
                        var btnClass = s === 'Voided' ? 'btn-danger' : 'btn-warning';
                        $('<button class="btn btn-xs ' + btnClass + ' mr-1">' + (labels[s] || s) + '</button>')
                            .on('click', (function (status) { return function () { openTransitionModal(item.id, status, item.shippingAmountToCR); }; })(s))
                            .appendTo(btns);
                    });
                    return btns;
                }
            }
        ]
    });
}

// ── New/Edit Order Modal ──────────────────────────────────────────────────────

function openNewOrder() {
    userEditedName = false;
    $('#order-id').val('');
    $('#order-name').val('');
    $('#order-supplier').val('');
    $('#order-exchange-rate').val('');
    $('#order-tax').val('');
    $('#order-shipping-intern').val('0');
    $('#order-discount').val('0');
    $('#order-currency').prop('disabled', false);
    $('.edit-only-field').hide();
    $.get('?handler=Configuration', function (cfg) {
        if (cfg) {
            if (cfg.exchangeRate != null) $('#order-exchange-rate').val(cfg.exchangeRate);
            if (cfg.taxPercentage != null) $('#order-tax').val(cfg.taxPercentage);
            if (cfg.currencyId) $('#order-currency').val(cfg.currencyId);
        }
    });
    $('#orderModal').modal('show');
}

function openEditOrder(item) {
    $('.edit-only-field').show();
    $('#order-id').val(item.id);
    $('#order-name').val(item.nameOfOrder);
    $('#order-supplier').val(item.supplierId);
    $('#order-currency').val(item.currencyId);
    $('#order-exchange-rate').val(item.exchangeRate);
    $('#order-tax').val(item.taxPercentage);
    $('#order-shipping-intern').val(item.shippingAmountIntern);
    $('#order-discount').val(item.discountAmount);
    // Currency is read-only if order has items AND is not Pending
    var readOnly = item.itemCount > 0 && item.status !== 'Pending';
    $('#order-currency').prop('disabled', readOnly);
    $('#orderModal').modal('show');
}

$('#btn-new-order').on('click', openNewOrder);

$('#order-name').on('input', function () { userEditedName = true; });

$('#order-supplier').on('change', function () {
    if ($('#order-id').val() || userEditedName) return;
    var supplierName = $(this).find('option:selected').text();
    if (!supplierName) return;
    var d = new Date();
    var dd = String(d.getDate()).padStart(2, '0');
    var mm = String(d.getMonth() + 1).padStart(2, '0');
    var yyyy = d.getFullYear();
    $('#order-name').val(supplierName + '-' + dd + '-' + mm + '-' + yyyy);
});

$('#btn-save-order').on('click', function () {
    var id = $('#order-id').val();
    var payload = {
        id: id || '00000000-0000-0000-0000-000000000000',
        nameOfOrder: $('#order-name').val(),
        supplierId: $('#order-supplier').val(),
        exchangeRate: parseFloat($('#order-exchange-rate').val()) || 0,
        taxPercentage: parseFloat($('#order-tax').val()) || 0,
        shippingAmountIntern: parseFloat($('#order-shipping-intern').val()) || 0,
        discountAmount: parseFloat($('#order-discount').val()) || 0
    };

    payload.currencyId = $('#order-currency').val() || '00000000-0000-0000-0000-000000000000';

    var handler = id ? 'Update' : 'Create';
    ajaxPost(handler, payload, function () {
        $('#orderModal').modal('hide');
        $('#jsGrid').jsGrid('loadData');
    });
});

// ── Transition Modal ──────────────────────────────────────────────────────────

function openTransitionModal(orderId, toStatus, shippingAmountToCR) {
    var today = new Date().toISOString().split('T')[0];
    $('#transition-order-id').val(orderId);
    $('#transition-to-status').val(toStatus);
    $('#transition-date').val(today);
    $('#transition-notes').val('');
    $('#transition-justification').val('');
    $('#transition-error').hide();
    $('#justification-group').toggle(toStatus === 'Voided');
    if (toStatus === 'Delivered') {
        $('#actual-shipping-amount').val(shippingAmountToCR || 0);
        $('#actual-shipping-group').show();
    } else {
        $('#actual-shipping-group').hide();
    }
    $('#transitionModal').modal('show');
}

$('#btn-confirm-transition').on('click', function () {
    $('#transition-error').hide();
    var toStatus = $('#transition-to-status').val();
    var payload = {
        orderId: $('#transition-order-id').val(),
        toStatus: toStatus,
        transitionDate: $('#transition-date').val(),
        notes: $('#transition-notes').val(),
        justification: $('#transition-justification').val()
    };
    if (toStatus === 'Delivered') {
        payload.actualShippingAmountToCR = parseFloat($('#actual-shipping-amount').val() || '0');
    }

    ajaxPost('Transition', payload,
        function (r) {
            if (r.success) {
                $('#transitionModal').modal('hide');
                $('#jsGrid').jsGrid('loadData');
            } else {
                $('#transition-error').text(r.error).show();
            }
        },
        function (msg) { $('#transition-error').text(msg).show(); }
    );
});

// ── Init ──────────────────────────────────────────────────────────────────────

$(function () {
    // Load suppliers into dropdown
    $.get('/Suppliers/Index?handler=Load', function (data) {
        var sel = $('#order-supplier');
        sel.empty();
        data.forEach(function (s) { sel.append($('<option>').val(s.id).text(s.name)); });
    });

    // Load currencies into order-currency dropdown (T040)
    $.get('/Currencies/Index?handler=Load', function (data) {
        currencyItems = data || [];
        var sel = $('#order-currency');
        sel.empty().append($('<option>').val('').text('-- Seleccione --'));
        currencyItems.forEach(function (c) { sel.append($('<option>').val(c.id).text(c.abbreviation || c.name)); });
    });

    $('#statusFilter').on('change', loadGrid);
    loadGrid();
});
