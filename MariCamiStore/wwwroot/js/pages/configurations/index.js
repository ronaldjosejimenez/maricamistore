var token = $('input[name="__RequestVerificationToken"]').val();
var currencyItems = [];

function initGrid() {
    $('#jsGrid').jsGrid({
        height: 'auto',
        width: '100%',
        editing: true,
        inserting: true,
        sorting: false,
        paging: false,
        autoload: false,

        controller: {
            loadData: function () {
                var d = $.Deferred();
                $.get('?handler=Load', function (data) {
                    if (data && data.length > 0) {
                        $('#jsGrid').jsGrid('option', 'inserting', false);
                        $('#config-banner').show();
                    } else {
                        $('#jsGrid').jsGrid('option', 'inserting', true);
                        $('#config-banner').hide();
                    }
                    d.resolve(data);
                });
                return d.promise();
            },
            insertItem: function (item) {
                return $.ajax({
                    url: '?handler=Upsert', method: 'POST',
                    contentType: 'application/json',
                    headers: { 'RequestVerificationToken': token },
                    data: JSON.stringify(item)
                });
            },
            updateItem: function (item) {
                return $.ajax({
                    url: '?handler=Upsert', method: 'POST',
                    contentType: 'application/json',
                    headers: { 'RequestVerificationToken': token },
                    data: JSON.stringify(item)
                });
            }
        },

        fields: [
            { name: 'id', type: 'text', visible: false },
            { name: 'taxPercentage', title: 'Impuesto (%)', type: 'number', width: 130, validate: 'required' },
            { name: 'exchangeRate', title: 'Tipo de Cambio', type: 'number', width: 130, validate: 'required' },
            { name: 'exchangeRateMargin', title: 'Margen T.C.', type: 'number', width: 120 },
            {
                name: 'localCurrencyId', title: 'Moneda Local', type: 'select',
                items: currencyItems, valueField: 'id', textField: 'text', width: 120
            },
            {
                name: 'orderCurrencyIdDefault', title: 'Moneda Orden', type: 'select',
                items: currencyItems, valueField: 'id', textField: 'text', width: 120
            },
            { type: 'control', deleteButton: false }
        ]
    });

    $('#jsGrid').jsGrid('loadData');
}

$(function () {
    $.get('/Currencies?handler=Load', function (data) {
        currencyItems = data.map(function (c) { return { id: c.id, text: c.abbreviation }; });
        initGrid();
    });
});
