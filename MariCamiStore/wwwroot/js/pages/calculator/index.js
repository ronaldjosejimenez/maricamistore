
var suppliers = [];
var defaults = { serviceCost: 3000 };

function round2(v) { return Math.round(v * 100) / 100; }

function currentSupplier() {
    var id = $('#SupplierId').val();
    return suppliers.find(function (s) { return s.id === id; }) || null;
}

// Loads tax, exchange rate and shipping from the selected supplier (spec 018, FR-006/FR-007).
// Overwrites manual edits on purpose.
function applySupplier() {
    var s = currentSupplier();
    if (!s) {
        $('#Vat, #ExchangeRate, #Shipping').val('');
        return;
    }
    var factor = $('#FastShipping').is(':checked') ? 2 : 1;
    $('#Vat').val(Number(s.taxPercentageDefault).toFixed(2));
    $('#ExchangeRate').val(Number(s.exchangeRateDefault).toFixed(2));
    $('#Shipping').val((Number(s.shippingCostDefault) * factor).toFixed(2));
}

function loadSuppliers() {
    $.get('?handler=Suppliers', function (data) {
        suppliers = data || [];
        var sel = $('#SupplierId').empty();
        if (!suppliers.length) {
            sel.append($('<option>').val('').text(''));
            $('#NoSuppliersWarning').show();
        } else {
            suppliers.forEach(function (s) { sel.append($('<option>').val(s.id).text(s.name)); });
            $('#NoSuppliersWarning').hide();
        }
        Clean();
    }).fail(function (xhr) { alert('Error cargando proveedores: ' + xhr.status); });
}

function Clean() {
    $('#ListPrice').val((0).toFixed(2));
    $('#ServiceCost').val(defaults.serviceCost.toFixed(2));
    $('#FastShipping').prop('checked', false);
    applySupplier();
    $('#PriceWithVAT').text((0).toFixed(2));
    $('#PriceCOL').text((0).toFixed(2));
    $('#Total').text((0).toFixed(2));
}

// FR-010: total is always rounded up to the next hundred
function Calculate() {
    if (!Validate()) {
        return;
    }

    var listPrice = Number($('#ListPrice').val());
    var vat = Number($('#Vat').val());
    var exchangeRate = Number($('#ExchangeRate').val());
    var shipping = Number($('#Shipping').val());
    var serviceCost = Number($('#ServiceCost').val());
    // Same rounding steps as the order item chain (FR-010a); cents first so float noise never jumps a hundred
    var priceWithVAT = round2(round2(listPrice + listPrice * (vat / 100)) + shipping);
    var priceCOL = round2(priceWithVAT * exchangeRate);
    var subtotal = round2(priceCOL + serviceCost);
    var total = Math.ceil(subtotal / 100) * 100;
    $('#PriceWithVAT').text(priceWithVAT.toFixed(2));
    $('#PriceCOL').text(priceCOL.toFixed(2));
    $('#Total').text(total.toFixed(0));
}

function Validate() {
    var exchangeRate = Number($('#ExchangeRate').val());
    if (!exchangeRate || exchangeRate <= 0) {
        alert('Digita el tipo de cambio.');
        return false;
    }

    var serviceCost = Number($('#ServiceCost').val());
    if (!serviceCost || serviceCost <= 0) {
        alert('Digita el Costo servicio.');
        return false;
    }

    var listPrice = Number($('#ListPrice').val());
    if (!listPrice || listPrice <= 0) {
        alert('Digita el Precio de lista.');
        return false;
    }

    var vat = Number($('#Vat').val());
    if (!vat || vat <= 0) {
        alert('Digita el impuesto.');
        return false;
    }

    var shipping = Number($('#Shipping').val());
    if (isNaN(shipping) || shipping < 0) {
        alert('Digita el Costo envío.');
        return false;
    }

    return true;
}

$(function () {
    $('#SupplierId').on('change', applySupplier);
    $('#FastShipping').on('change', applySupplier);
    $('#btnCalculate').on('click', Calculate);
    $('#btnClean').on('click', Clean);
    loadSuppliers();
});
