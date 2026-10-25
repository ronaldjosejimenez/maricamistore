
var token = $('input[name="__RequestVerificationToken"]').val();
var currencyItems = [];
var cfg = null;

function ajaxPost(handler, data, done, fail) {
    $.ajax({
        url: '?handler=' + handler, method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(data),
        success: done,
        error: function (xhr) {
            alert('Error: ' + xhr.responseText);
            if (fail) fail();
        }
    });
}

// jsGrid's built-in number field uses parseInt; defaults need 2 decimals
function DecimalField(config) { jsGrid.fields.number.call(this, config); }
DecimalField.prototype = new jsGrid.fields.number({
    _parse: function (val) { return val === '' || val == null ? 0 : parseFloat(val); },
    insertValue: function () { return this._parse(this.insertControl.val()); },
    editValue: function () { return this._parse(this.editControl.val()); },
    _createTextBox: function () {
        return jsGrid.fields.number.prototype._createTextBox.call(this).attr('step', '0.01');
    }
});
jsGrid.fields.decimal = DecimalField;

// Insert row starts with the Configuration value (spec 018, FR-002)
function withInsertDefault(baseField, getValue) {
    return function () {
        var $control = jsGrid.fields[baseField].prototype.insertTemplate.call(this);
        var value = getValue();
        if (value != null) $control.val(value);
        return $control;
    };
}

// The empty currency option posts '', which does not bind to Guid?
function normalize(item) {
    return $.extend({}, item, { currencyIdDefault: item.currencyIdDefault || null });
}

function currencySign(currencyId) {
    var curr = currencyItems.find(function (c) { return c.id === currencyId; });
    return curr ? curr.sign : '';
}

function initGrid() {
    $('#jsGrid').jsGrid({
        height: 'auto', width: '100%',
        filtering: true, editing: true, inserting: true, sorting: true,
        paging: true, pageSize: 20, autoload: true,

        controller: {
            loadData: function () { return $.get('?handler=Load'); },
            insertItem: function (item) {
                var d = $.Deferred();
                ajaxPost('Insert', normalize(item), function (r) { d.resolve(r); }, function () { d.reject(); });
                return d.promise();
            },
            updateItem: function (item) {
                var d = $.Deferred();
                ajaxPost('Update', normalize(item), function (r) { d.resolve(r); }, function () { d.reject(); });
                return d.promise();
            },
            deleteItem: function (item) {
                var d = $.Deferred();
                ajaxPost('Delete', { id: item.id }, function () { d.resolve(); });
                return d.promise();
            }
        },

        fields: [
            { name: 'id', type: 'text', visible: false },
            { name: 'name', title: 'Nombre', type: 'text', width: 200, validate: 'required' },
            {
                name: 'taxPercentageDefault', title: 'Impuesto %', type: 'decimal', width: 90,
                insertTemplate: withInsertDefault('number', function () { return cfg ? cfg.taxPercentage : null; })
            },
            {
                name: 'currencyIdDefault', title: 'Moneda', type: 'select', width: 90,
                items: [{ id: '', text: '' }].concat(currencyItems), valueField: 'id', textField: 'text',
                insertTemplate: withInsertDefault('select', function () { return cfg ? cfg.currencyId : null; })
            },
            {
                name: 'exchangeRateDefault', title: 'Tipo de cambio', type: 'decimal', width: 110,
                insertTemplate: withInsertDefault('number', function () { return cfg ? cfg.exchangeRate : null; })
            },
            {
                name: 'shippingCostDefault', title: 'Envío', type: 'decimal', width: 100,
                insertTemplate: withInsertDefault('number', function () { return 0; }),
                itemTemplate: function (val, item) { return formatMoney(val, currencySign(item.currencyIdDefault)); }
            },
            { name: 'isActiveForCalculator', title: 'Activo calculadora', type: 'checkbox', width: 90 },
            { type: 'control' }
        ]
    });
}

$(function () {
    $.when(
        $.get('/Currencies?handler=Load'),
        $.get('?handler=Configuration')
    ).done(function (currRes, cfgRes) {
        currencyItems = (currRes[0] || []).map(function (c) { return { id: c.id, text: c.abbreviation, sign: c.sign || '' }; });
        cfg = cfgRes[0] || null;
        initGrid();
    });
});
