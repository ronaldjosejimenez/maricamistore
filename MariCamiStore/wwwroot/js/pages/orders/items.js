var token = $('input[name="__RequestVerificationToken"]').val();

// T029: file-scope state
var userEditedAgreed = false;
var userEditedRealPrice = false;
var orderDirty = false;
var selectedImageBase64 = null;
var customerItems = [];
var currentItems = [];
var reasignarCustomers = [];
var itemViewMode = false;

// Spanish labels for order status keys (includes the legacy 'Delivered' for old history rows).
var statusLabels = { Pending: 'Pendiente', Active: 'Activa', Delivering: 'Entregando', Delivered: 'Entregada', Completed: 'Completada', Voided: 'Anulada' };

function ajaxPost(handler, data, done, fail) {
    $.ajax({
        url: '?handler=' + handler + '&orderId=' + orderId, method: 'POST',
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

// ── Calculated field helpers ──────────────────────────────────────────────────

// Half away from zero like the server (MidpointRounding.AwayFromZero); EPSILON absorbs binary float noise
function round2(v) { return Math.sign(v) * Math.round((Math.abs(v) + Number.EPSILON) * 100) / 100; }

// T027: updated recalcOrderHeader with TotalAgreedPriceInLocal + fixed formula
function recalcOrderHeader(allItems) {
    var totalAgreedPriceInLocal = round2(allItems.reduce(function (s, i) { return s + (parseFloat(i.agreedPriceInLocal) || 0); }, 0));
    var shippingToCR = round2(allItems.reduce(function (s, i) { return s + (parseFloat(i.estimateShipping) || 0); }, 0));
    // Spec 018, FR-021: realPrice now includes tax and shipping, so the subtotal uses list prices
    var totalWithoutTaxes = round2(allItems.reduce(function (s, i) { return s + (parseFloat(i.listPrice) || 0); }, 0));
    var taxesAmount = round2((totalWithoutTaxes - discountAmount) * (taxPercentage / 100));
    var totalToSupplier = round2(shippingAmountIntern + totalWithoutTaxes + taxesAmount - discountAmount);
    var totalOrder = round2(totalToSupplier + shippingToCR);
    var profit = round2(totalAgreedPriceInLocal - totalOrder * exchangeRate);

    var totals = { totalAgreedPriceInLocal, shippingToCR, totalWithoutTaxes, taxesAmount, totalToSupplier, totalOrder, profit };
    renderHeader(totals);
    return totals;
}

function renderHeader(totals) {
    $('#order-total-agreed').text(formatMoney(totals.totalAgreedPriceInLocal, localCurrencySign));
    $('#h-subtotal').text(formatMoney(totals.totalWithoutTaxes, orderCurrencySign));
    $('#h-taxes').text(formatMoney(totals.taxesAmount, orderCurrencySign));
    $('#h-ship-cr').text(formatMoney(totals.shippingToCR, orderCurrencySign));
    $('#h-total-supplier').text(formatMoney(totals.totalToSupplier, orderCurrencySign));
    $('#h-total').text(formatMoney(totals.totalOrder, orderCurrencySign));
    $('#h-profit').text(formatMoney(totals.profit, localCurrencySign));
}

// T028: persistTotals with totalAgreedPriceInLocal
function persistTotals(totals) {
    $.ajax({
        url: '?handler=UpdateTotals&orderId=' + orderId, method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify({
            orderId: orderId,
            totalAgreedPriceInLocal: totals.totalAgreedPriceInLocal,
            shippingAmountToCR: totals.shippingToCR,
            totalWithoutTaxes: totals.totalWithoutTaxes,
            taxesAmount: totals.taxesAmount,
            totalToPayToSupplier: totals.totalToSupplier,
            totalOfTheOrder: totals.totalOrder,
            estimatedProfitInLocal: totals.profit
        })
    });
}

function refreshTotals() {
    // Spec 018, FR-022: only pending orders recalculate and persist; the rest show the saved totals
    if (!isPending) { renderHeader(savedTotals); return; }
    var totals = recalcOrderHeader(currentItems);
    persistTotals(totals);
}

// ── Modal helpers ─────────────────────────────────────────────────────────────

// Spec 018: recalculates the modal chain. source = 'list' | 'lptwt' | 'shipping' | 'real' | 'fee'
function recalcModal(source) {
    var lp = parseFloat($('#item-list-price').val()) || 0;
    if (source === 'list') {
        $('#item-list-price-tax-with-tax').val(round2(lp + lp * (taxPercentage / 100)).toFixed(2));
    }
    var lptwt = parseFloat($('#item-list-price-tax-with-tax').val()) || 0;
    var shipping = parseFloat($('#item-estimate-shipping').val()) || 0;
    if (!userEditedRealPrice && source !== 'real') {
        $('#item-real-price').val(round2(lptwt + shipping).toFixed(2));
    }
    var real = parseFloat($('#item-real-price').val()) || 0;
    var realLocal = round2(real * exchangeRate);
    var total = round2(realLocal + (parseFloat($('#item-service-fee-in-local').val()) || 0));
    $('#item-real-price-in-local').val(realLocal.toFixed(2));
    $('#item-total').val(total.toFixed(2));
    if (!userEditedAgreed) $('#item-agreed-price-in-local').val(total.toFixed(2));
}

// Spec 018, FR-020: price fields stay locked until a product type is chosen
function setPriceFieldsEnabled(enabled) {
    $('#item-list-price, #item-list-price-tax-with-tax, #item-estimate-shipping, #item-real-price, #item-service-fee-in-local, #item-agreed-price-in-local')
        .prop('disabled', !enabled);
}

// Items flagged by the 018 migration still carry the old price defaults (see OrderItem.HasLegacyPriceDefaults)
function isLegacyItem(item) {
    return !!item.hasLegacyPriceDefaults;
}

function hasProductType(id) {
    return !!id && id !== '00000000-0000-0000-0000-000000000000';
}

// T029: openAddItem
function openAddItem() {
    userEditedAgreed = false;
    userEditedRealPrice = false;
    selectedImageBase64 = null;
    $('#item-id').val('');
    $('#item-modal-title').text('Nuevo Ítem');
    $('#item-customer').val('').trigger('change');
    $('#item-product-description').val('');
    $('#item-product-link').val('');
    $('#item-product-source-code').val('');
    $('#item-size').val('N/A');
    $('#item-product-image').val('');
    $('#item-product-image-preview').hide();
    $('#item-image-error').hide();
    $('#item-list-price').val('0');
    $('#item-list-price-tax-with-tax').val('0');
    $('#item-real-price').val('0');
    $('#item-estimate-shipping').val('0');
    $('#item-real-price-in-local').val('0');
    $('#item-service-fee-in-local').val('0');
    $('#item-total').val('0');
    $('#item-agreed-price-in-local').val('0');
    $('#item-modal-error').hide();
    setPriceFieldsEnabled(false);

    // T031: load ProductTypes filtered by order currency
    var ptSel = $('#item-product-type').empty();
    if (orderCurrencyId) {
        $.get('?handler=ProductTypesByCurrency&currencyId=' + orderCurrencyId + '&orderId=' + orderId, function (data) {
            if (!data || data.length === 0) {
                ptSel.append($('<option>').val('').text('No hay tipos disponibles para esta moneda').prop('disabled', true));
                ptSel.prop('disabled', true);
            } else {
                ptSel.prop('disabled', false);
                ptSel.append($('<option>').val('').text('-- Seleccione --'));
                data.forEach(function (p) { ptSel.append($('<option>').val(p.id).text(p.name)); });
            }
        });
    }

    $('#itemModal').modal('show');
}

// T029: openEditItem
function openEditItem(item) {
    // Spec 018, FR-020a: manual-edit flags compare against the new formulas
    // Items saved before 018 used RealPrice = ListPrice and Agreed = (Price + Tax) x rate + fee as defaults;
    // those still count as automatic so the item keeps recalculating.
    var realLocal = parseFloat(item.realPriceInLocal) || 0;
    var fee = parseFloat(item.serviceFeeInLocal) || 0;
    var lptwt = parseFloat(item.listPriceTaxWithTax) || 0;
    var real = parseFloat(item.realPrice) || 0;
    var agreed = parseFloat(item.agreedPriceInLocal) || 0;
    var computed = round2(realLocal + fee);
    var legacyAgreed = round2(lptwt * exchangeRate + fee);
    var tolerance = 0.011; // 1 cent plus float noise
    var isLegacy = isLegacyItem(item);
    userEditedAgreed = Math.abs(agreed - computed) > tolerance &&
        !(isLegacy && Math.abs(agreed - legacyAgreed) <= tolerance);
    userEditedRealPrice = Math.abs(real - (lptwt + (parseFloat(item.estimateShipping) || 0))) > tolerance &&
        !(isLegacy && Math.abs(real - (parseFloat(item.listPrice) || 0)) <= tolerance);
    selectedImageBase64 = null;

    $('#item-id').val(item.id);
    $('#item-modal-title').text('Editar Ítem');
    $('#item-customer').val(item.customerId).trigger('change');
    $('#item-product-description').val(item.productDescription);
    $('#item-product-link').val(item.productLink || '');
    $('#item-product-source-code').val(item.productSourceCode || '');
    $('#item-size').val(item.size || 'N/A');
    $('#item-product-image').val('');
    if (item.hasImage) {
        $('#item-product-image-preview').attr('src', '?handler=ItemImage&itemId=' + item.id + '&orderId=' + orderId).show();
    } else {
        $('#item-product-image-preview').hide();
    }
    $('#item-image-error').hide();

    // T031: load ProductTypes filtered by currency, then set selected value
    var ptSel = $('#item-product-type').empty();
    if (orderCurrencyId) {
        $.get('?handler=ProductTypesByCurrency&currencyId=' + orderCurrencyId + '&orderId=' + orderId, function (data) {
            if (!data || data.length === 0) {
                ptSel.append($('<option>').val('').text('No hay tipos disponibles para esta moneda').prop('disabled', true));
                ptSel.prop('disabled', true);
            } else {
                ptSel.prop('disabled', false);
                ptSel.append($('<option>').val('').text('-- Seleccione --'));
                data.forEach(function (p) { ptSel.append($('<option>').val(p.id).text(p.name)); });
                ptSel.val(item.productTypeId);
            }
            if (itemViewMode) ptSel.prop('disabled', true);
        });
    }

    $('#item-list-price').val((parseFloat(item.listPrice) || 0).toFixed(2));
    $('#item-list-price-tax-with-tax').val((parseFloat(item.listPriceTaxWithTax) || 0).toFixed(2));
    $('#item-real-price').val((parseFloat(item.realPrice) || 0).toFixed(2));
    $('#item-estimate-shipping').val((parseFloat(item.estimateShipping) || 0).toFixed(2));
    $('#item-real-price-in-local').val(realLocal.toFixed(2));
    $('#item-service-fee-in-local').val((parseFloat(item.serviceFeeInLocal) || 0).toFixed(2));
    $('#item-total').val(computed.toFixed(2));
    setPriceFieldsEnabled(hasProductType(item.productTypeId));
    $('#item-agreed-price-in-local').val((parseFloat(item.agreedPriceInLocal) || 0).toFixed(2));
    $('#item-modal-error').hide();

    $('#itemModal').modal('show');
}

// openViewItem: shows the item modal in read-only mode (orders outside Pending)
function openViewItem(item) {
    itemViewMode = true;
    openEditItem(item);
    $('#item-modal-title').text('Ver Ítem');
    $('#itemModal .modal-body').find('input, select, textarea').prop('disabled', true);
    $('#btn-save-item').hide();
    $('#btn-cancel-item').text('Cerrar');
}

// Restores the item modal to its editable state after a read-only view
function restoreItemModalEditable() {
    itemViewMode = false;
    $('#itemModal .modal-body').find('input, select, textarea').not('#item-product-type').prop('disabled', false);
    // Keep the product type disabled when it only holds the "no types for this currency" option
    var ptSel = $('#item-product-type');
    var noTypes = ptSel.children('option').length > 0 && ptSel.children('option:not(:disabled)').length === 0;
    ptSel.prop('disabled', noTypes);
    setPriceFieldsEnabled(hasProductType(ptSel.val()));
    $('#btn-save-item').show();
    $('#btn-cancel-item').text('Cancelar');
}

// ── Custom Table Rendering ────────────────────────────────────────────────────

var isDelivering = orderStatus === 'Delivering';

function renderItemsTable(items) {
    currentItems = items || [];
    $('#item-count-badge').text(currentItems.length);

    var $container = $('#items-table-container');
    if (!currentItems.length) {
        $container.html('<div class="alert alert-info">No hay artículos en esta orden.</div>');
        refreshTotals();
        return;
    }

    var groups = {};
    var groupOrder = [];
    currentItems.forEach(function (item) {
        var key = item.customerDisplayName || item.customerId;
        if (!groups[key]) { groups[key] = []; groupOrder.push(key); }
        groups[key].push(item);
    });

    var headerCols = ['Cliente', 'Descripción', 'Cód.', 'Link', 'P.Lista', 'P+Imp', 'Servicio', 'Total', 'P.Acordado', 'P.Real', 'Envío Est.'];
    if (isDelivering) headerCols.push('Recibido');
    headerCols.push('Acciones');

    var $table = $('<table class="table table-sm table-bordered table-hover">');
    var $thead = $('<thead class="thead-light">');
    var $htr = $('<tr>');
    headerCols.forEach(function (h) { $htr.append($('<th>').text(h)); });
    $thead.append($htr);
    $table.append($thead);

    var $tbody = $('<tbody>');
    var grandTotal = 0;

    groupOrder.forEach(function (groupName) {
        var groupItems = groups[groupName];
        var groupSubtotal = 0;

        $tbody.append(
            $('<tr class="table-dark">').append(
                $('<td>').attr('colspan', headerCols.length).html('<strong>' + groupName + '</strong>')
            )
        );

        groupItems.forEach(function (item) {
            var total = round2((parseFloat(item.realPriceInLocal) || 0) + (parseFloat(item.serviceFeeInLocal) || 0));
            groupSubtotal += parseFloat(item.agreedPriceInLocal) || 0;

            var $tr = $('<tr>');
            if (item.isReceived) $tr.addClass('table-success');

            $tr.append($('<td>').text(item.customerDisplayName || ''));
            $tr.append($('<td>').text(item.productDescription));
            $tr.append($('<td>').text(item.productSourceCode || ''));

            var $linkTd = $('<td style="min-width:250px;">');
            if (item.productLink) {
                $linkTd.append($('<a>').attr('href', item.productLink).attr('target', '_blank').text('Ver link'));
            }
            $tr.append($linkTd);

            $tr.append($('<td>').text(formatMoney(item.listPrice, orderCurrencySign)));
            $tr.append($('<td>').text(formatMoney(item.listPriceTaxWithTax, orderCurrencySign)));
            $tr.append($('<td>').text(formatMoney(item.serviceFeeInLocal, localCurrencySign)));
            $tr.append($('<td>').text(formatMoney(total, orderCurrencySign)));
            $tr.append($('<td>').text(formatMoney(item.agreedPriceInLocal, localCurrencySign)));
            $tr.append($('<td>').text(formatMoney(item.realPrice, orderCurrencySign)));
            $tr.append($('<td>').text(formatMoney(item.estimateShipping, orderCurrencySign)));

            if (isDelivering) {
                var $chk = $('<input type="checkbox" class="is-received-chk">').attr('data-item-id', item.id);
                if (item.isReceived) $chk.prop('checked', true);
                $tr.append($('<td class="text-center">').append($chk));
            }

            var $btnTd = $('<td>');
            if (isPending) {
                $('<button class="btn btn-xs btn-info mr-1">Editar</button>')
                    .on('click', function () { openEditItem(item); }).appendTo($btnTd);
            } else {
                $('<button class="btn btn-xs btn-info mr-1">Ver</button>')
                    .on('click', function () { openViewItem(item); }).appendTo($btnTd);
            }
            if (item.hasImage) {
                $('<button class="btn btn-xs btn-secondary mr-1">Ver Img</button>')
                    .on('click', function () {
                        $('#preview-img').attr('src', '?handler=ItemImage&itemId=' + item.id + '&orderId=' + orderId);
                        $('#imagePreviewModal').modal('show');
                    }).appendTo($btnTd);
            }
            if (!isPending && orderStatus !== 'Voided') {
                $('<button class="btn btn-xs btn-warning mr-1">Reasignar</button>')
                    .on('click', function () { openReasignar(item); }).appendTo($btnTd);
            }
            if (isPending) {
                $('<button class="btn btn-xs btn-danger">Eliminar</button>')
                    .on('click', function () {
                        if (!confirm('¿Eliminar este ítem?')) return;
                        ajaxPost('Delete', { id: item.id }, function (r) {
                            if (r.success) { loadItems(); refreshTotals(); }
                            else alert(r.error);
                        });
                    }).appendTo($btnTd);
            }
            $tr.append($btnTd);
            $tbody.append($tr);
        });

        grandTotal += groupSubtotal;
        $tbody.append(
            $('<tr class="table-light font-weight-bold">').append(
                $('<td>').attr('colspan', headerCols.length - 1).html('Subtotal <em>' + groupName + '</em>'),
                $('<td>').text(formatMoney(round2(groupSubtotal), localCurrencySign))
            )
        );
    });

    $tbody.append(
        $('<tr class="table-info font-weight-bold">').append(
            $('<td>').attr('colspan', headerCols.length - 1).text('Total Precio Acordado'),
            $('<td>').text(formatMoney(round2(grandTotal), localCurrencySign))
        )
    );

    $table.append($tbody);
    $container.html('').append($table);
    refreshTotals();
}

function loadItems() {
    $.get('?handler=Load&orderId=' + orderId, function (data) {
        renderItemsTable(data || []);
    });
}

// ── Reassignment ──────────────────────────────────────────────────────────────

function loadCustomersForReasignar(selectedCustomerId) {
    $.get('/Customers/Index?handler=Load', function (data) {
        reasignarCustomers = data || [];
        var sel = $('#reasignar-customer');
        sel.empty();
        reasignarCustomers.forEach(function (c) {
            sel.append($('<option>').val(c.id).text(c.nickName || c.name));
        });
        if (selectedCustomerId) sel.val(selectedCustomerId);
    });
}

function openReasignar(item) {
    $('#reasignar-item-id').val(item.id);
    $('#reasignar-precio').val(item.agreedPriceInLocal);
    $('#reasignar-error').text('').hide();
    $('#reasignar-precio-warning').hide();
    $('#modalReasignar').removeData('zero-confirmed');
    loadCustomersForReasignar(item.customerId);
    $('#modalReasignar').modal('show');
}

// ── Shipping packages ─────────────────────────────────────────────────────────

function todayIso() {
    var d = new Date();
    var m = String(d.getMonth() + 1).padStart(2, '0');
    var day = String(d.getDate()).padStart(2, '0');
    return d.getFullYear() + '-' + m + '-' + day;
}

function loadPackages() {
    $.get('?handler=Packages&orderId=' + orderId, function (data) {
        if (!data) return;
        $('#pkg-estimated').text(formatMoney(data.estimatedShipping, orderCurrencySign));
        $('#pkg-total').text(formatMoney(data.totalPackages, orderCurrencySign));
        $('#pkg-pending').text(formatMoney(data.pending, orderCurrencySign));
        renderPackagesTable(data.packages || []);
    });
}

function renderPackagesTable(packages) {
    var $container = $('#packages-table-container').empty();
    if (packages.length === 0) {
        $container.append($('<div class="text-center text-muted py-3">').text('Sin paquetes registrados.'));
        return;
    }
    var $table = $('<table class="table table-sm table-bordered">');
    var $htr = $('<tr>');
    ['Fecha de entrega', 'Número de tracking', 'Descripción', 'Monto'].forEach(function (h) { $htr.append($('<th>').text(h)); });
    if (canManagePackages) $htr.append($('<th>'));
    $table.append($('<thead>').append($htr));
    var $tbody = $('<tbody>');
    packages.forEach(function (p) {
        var $tr = $('<tr>');
        $tr.append($('<td>').text((p.deliveryDate || '').substr(0, 10)));
        $tr.append($('<td>').text(p.trackingNumber || ''));
        $tr.append($('<td>').text(p.description || ''));
        $tr.append($('<td>').text(formatMoney(p.amount, orderCurrencySign)));
        if (canManagePackages) {
            var $btn = $('<button type="button" class="btn btn-xs btn-danger" title="Eliminar"><i class="fas fa-trash"></i></button>')
                .on('click', function () { deletePackage(p.id, $(this)); });
            $tr.append($('<td>').append($btn));
        }
        $tbody.append($tr);
    });
    $container.append($table.append($tbody));
}

function addPackage() {
    var $btn = $('#btn-add-package');
    $('#pkg-error').hide();
    var amount = parseFloat($('#pkg-amount').val());
    var deliveryDate = $('#pkg-date').val();
    if (!deliveryDate) {
        $('#pkg-error').text('La fecha de entrega es requerida.').show();
        return;
    }
    if (!(amount > 0)) {
        $('#pkg-error').text('El monto debe ser mayor a cero.').show();
        return;
    }
    $btn.prop('disabled', true);
    ajaxPost('AddPackage', {
        orderId: orderId,
        deliveryDate: deliveryDate,
        amount: amount,
        description: $('#pkg-description').val(),
        trackingNumber: $('#pkg-tracking-number').val()
    }, function (r) {
        $btn.prop('disabled', false);
        if (r && r.success) {
            $('#pkg-amount').val('');
            $('#pkg-description').val('');
            $('#pkg-tracking-number').val('');
            $('#pkg-date').val(todayIso());
            loadPackages();
        } else {
            $('#pkg-error').text((r && r.error) || 'Error al agregar el paquete.').show();
        }
    }, function (msg) {
        $btn.prop('disabled', false);
        $('#pkg-error').text(msg).show();
    });
}

function deletePackage(packageId, $btn) {
    if (!confirm('¿Eliminar este paquete? Se registrará un reverso en CxP.')) return;
    $('#pkg-error').hide();
    $btn.prop('disabled', true);
    ajaxPost('DeletePackage', { packageId: packageId }, function (r) {
        if (r && r.success) {
            loadPackages();
        } else {
            $btn.prop('disabled', false);
            var msg = (r && r.error) || 'Error al eliminar el paquete.';
            if ($('#pkg-error').length) $('#pkg-error').text(msg).show(); else alert(msg);
        }
    }, function (msg) {
        $btn.prop('disabled', false);
        if ($('#pkg-error').length) $('#pkg-error').text(msg).show(); else alert(msg);
    });
}

// ── Initialization ────────────────────────────────────────────────────────────

$(function () {
    // Initialize select2 for customer dropdown in modal
    $('#item-customer').select2({
        theme: 'bootstrap4',
        dropdownParent: $('#itemModal'),
        placeholder: '-- Seleccione cliente --',
        allowClear: true
    });

    // Load customers for modal dropdown
    $.get('/Customers/Index?handler=Load', function (data) {
        customerItems = data || [];
        var sel = $('#item-customer');
        sel.empty().append($('<option>').val('').text(''));
        customerItems.forEach(function (c) {
            sel.append($('<option>').val(c.id).text(c.nickName || c.name));
        });
    });

    // Read-only "Ver" mode must not leak into later add/edit flows
    $('#itemModal').on('hidden.bs.modal', function () {
        if (itemViewMode) restoreItemModalEditable();
    });

    // T020: Agregar Ítem button
    $('#btn-add-item').on('click', openAddItem);

    // T030: Reactive handlers in modal
    $('#item-list-price').on('input', function () { recalcModal('list'); });

    $('#item-real-price').on('input', function () {
        userEditedRealPrice = true;
        recalcModal('real');
    });

    $('#item-list-price-tax-with-tax').on('input', function () { recalcModal('lptwt'); });

    $('#item-estimate-shipping').on('input', function () { recalcModal('shipping'); });

    $('#item-service-fee-in-local').on('input', function () { recalcModal('fee'); });

    $('#item-agreed-price-in-local').on('input', function () {
        userEditedAgreed = true;
    });

    // T031: ProductType change handler
    $('#item-product-type').on('change', function () {
        var id = $(this).val();
        setPriceFieldsEnabled(hasProductType(id));
        if (!id) return;
        $.get('?handler=ProductType&id=' + id + '&orderId=' + orderId, function (pt) {
            if (!pt) return;
            $('#item-service-fee-in-local').val((parseFloat(pt.serviceFeeInLocal) || 0).toFixed(2));
            $('#item-estimate-shipping').val((parseFloat(pt.estimateShipping) || 0).toFixed(2));
            recalcModal('shipping');
        });
    });

    // T032: Image file validation and preview
    $('#item-product-image').on('change', function () {
        var file = this.files[0];
        $('#item-image-error').hide();
        selectedImageBase64 = null;
        $('#item-product-image-preview').hide();

        if (!file) return;
        if (file.size > 2097152) {
            $('#item-image-error').text('La imagen supera el límite de 2 MB.').show();
            $(this).val('');
            return;
        }
        var reader = new FileReader();
        reader.onload = function (e) {
            var dataUrl = e.target.result;
            // Extract base64 part after the comma
            selectedImageBase64 = dataUrl.split(',')[1];
            $('#item-product-image-preview').attr('src', dataUrl).show();
        };
        reader.readAsDataURL(file);
    });

    // T033: Save item handler
    $('#btn-save-item').on('click', function () {
        $('#item-modal-error').hide();
        var id = $('#item-id').val();
        var payload = {
            id: id || '00000000-0000-0000-0000-000000000000',
            orderId: orderId,
            customerId: $('#item-customer').val() || '00000000-0000-0000-0000-000000000000',
            productDescription: $('#item-product-description').val(),
            productLink: $('#item-product-link').val() || null,
            productSourceCode: $('#item-product-source-code').val() || null,
            size: $('#item-size').val() || '',
            productImageBase64: selectedImageBase64 !== null ? selectedImageBase64 : (id ? null : null),
            productTypeId: $('#item-product-type').val() || '00000000-0000-0000-0000-000000000000',
            listPrice: parseFloat($('#item-list-price').val()) || 0,
            listPriceTaxWithTax: parseFloat($('#item-list-price-tax-with-tax').val()) || 0,
            realPrice: parseFloat($('#item-real-price').val()) || 0,
            estimateShipping: parseFloat($('#item-estimate-shipping').val()) || 0,
            serviceFeeInLocal: parseFloat($('#item-service-fee-in-local').val()) || 0,
            agreedPriceInLocal: parseFloat($('#item-agreed-price-in-local').val()) || 0
        };

        var handler = id ? 'Update' : 'Insert';
        ajaxPost(handler, payload,
            function (r) {
                if (r && r.error) {
                    $('#item-modal-error').text(r.error).show();
                    return;
                }
                $('#itemModal').modal('hide');
                loadItems();
            },
            function (msg) {
                $('#item-modal-error').text(msg || 'Error al guardar el ítem. Por favor intente de nuevo.').show();
            }
        );
    });

    // T034: Order header dirty state + save
    if (isPending) {
        $('#order-exchange-rate, #order-tax, #order-shipping-intern, #order-discount').on('change input', function () {
            orderDirty = true;
            $('#order-dirty-warning').show();
        });

        $('#btn-save-order').on('click', function () {
            var newExchangeRate = parseFloat($('#order-exchange-rate').val()) || 0;
            var newTaxPercentage = parseFloat($('#order-tax').val()) || 0;
            var rateChanged = newExchangeRate !== exchangeRate;
            var taxChanged = newTaxPercentage !== taxPercentage;

            $.ajax({
                url: '?handler=UpdateOrder&orderId=' + orderId, method: 'POST',
                contentType: 'application/json',
                headers: { 'RequestVerificationToken': token },
                data: JSON.stringify({
                    orderId: orderId,
                    exchangeRate: newExchangeRate,
                    taxPercentage: newTaxPercentage,
                    shippingAmountIntern: parseFloat($('#order-shipping-intern').val()) || 0,
                    discountAmount: parseFloat($('#order-discount').val()) || 0
                }),
                success: function (r) {
                    if (!r.success) { alert(r.error || 'Error al guardar la orden.'); return; }
                    orderDirty = false;
                    $('#order-dirty-warning').hide();

                    // Update local variables
                    exchangeRate = newExchangeRate;
                    taxPercentage = newTaxPercentage;
                    shippingAmountIntern = parseFloat($('#order-shipping-intern').val()) || 0;
                    discountAmount = parseFloat($('#order-discount').val()) || 0;

                    // Trigger A: reload grid and recalculate if rate or tax changed
                    if (rateChanged || taxChanged) {
                        loadItems();
                    }
                    refreshTotals();
                },
                error: function () { alert('Error al guardar la orden.'); }
            });
        });
    }

    // IsReceived checkbox handler (delegated)
    $(document).on('change', '.is-received-chk', function () {
        var $chk = $(this);
        var itemId = $chk.data('item-id');
        var isReceived = $chk.is(':checked');
        ajaxPost('ToggleReceived', { itemId: itemId, isReceived: isReceived },
            function (r) {
                if (r.success) {
                    $chk.closest('tr').toggleClass('table-success', isReceived);
                } else {
                    $chk.prop('checked', !isReceived);
                    alert(r.error || 'Error al actualizar.');
                }
            },
            function (msg) {
                $chk.prop('checked', !isReceived);
                alert(msg);
            }
        );
    });

    // ── Reasignar modal handlers ──────────────────────────────────────────────

    $('#reasignar-precio').on('input', function () {
        var v = parseFloat($(this).val()) || 0;
        if (v === 0) {
            $('#reasignar-precio-warning').show();
            $('#modalReasignar').removeData('zero-confirmed');
        } else {
            $('#reasignar-precio-warning').hide();
        }
    });

    $('#btn-confirmar-reasignar').on('click', function () {
        $('#reasignar-error').hide();
        var itemId = $('#reasignar-item-id').val();
        var newCustomerId = $('#reasignar-customer').val();
        var newPrecio = parseFloat($('#reasignar-precio').val()) || 0;

        if (newPrecio === 0 && !$('#modalReasignar').data('zero-confirmed')) {
            $('#reasignar-precio-warning').show();
            $('#modalReasignar').data('zero-confirmed', true);
            return;
        }

        $.ajax({
            url: '?handler=Reasignar&orderId=' + orderId, method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({
                itemId: itemId,
                newCustomerId: newCustomerId,
                newAgreedPriceInLocal: newPrecio
            }),
            success: function (r) {
                if (r.success) {
                    $('#modalReasignar').modal('hide');
                    // Spec 018, FR-023: only these two header totals change on reassignment
                    if (r.totalAgreedPriceInLocal != null && r.estimatedProfitInLocal != null) {
                        savedTotals.totalAgreedPriceInLocal = r.totalAgreedPriceInLocal;
                        savedTotals.profit = r.estimatedProfitInLocal;
                    }
                    loadItems();
                } else {
                    $('#reasignar-error').text(r.error || 'Error al reasignar.').show();
                }
            },
            error: function (xhr) {
                $('#reasignar-error').text((xhr.responseJSON && xhr.responseJSON.error) || 'Error al reasignar.').show();
            }
        });
    });

    loadItems();

    // Shipping packages (card only rendered when the order is not Pending)
    if ($('#packages-table-container').length) {
        $('#pkg-date').val(todayIso());
        $('#btn-add-package').on('click', addPackage);
        loadPackages();
    }

    // Load status history
    $.get('?handler=History&orderId=' + orderId, function (data) {
        var tbody = $('#history-table tbody');
        tbody.empty();
        data.forEach(function (h) {
            // Build cells as text nodes: notes/justification are free user text (avoid stored XSS)
            tbody.append($('<tr>').append(
                $('<td>').text((h.transitionDate || '').substr(0, 10)),
                $('<td>').text(statusLabels[h.fromStatus] || h.fromStatus),
                $('<td>').text(statusLabels[h.toStatus] || h.toStatus),
                $('<td>').text(h.notes || ''),
                $('<td>').text(h.justification || '')));
        });
    });
});
