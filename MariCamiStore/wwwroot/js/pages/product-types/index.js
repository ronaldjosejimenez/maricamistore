var token = $('input[name="__RequestVerificationToken"]').val();
var currencyItems = [];

function ajaxPost(handler, data, done) {
    $.ajax({
        url: '?handler=' + handler, method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(data),
        success: done,
        error: function (xhr) { alert('Error: ' + xhr.responseText); }
    });
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
                ajaxPost('Insert', item, function (r) { d.resolve(r); });
                return d.promise();
            },
            updateItem: function (item) {
                var d = $.Deferred();
                ajaxPost('Update', item, function (r) { d.resolve(r); });
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
            { name: 'name', title: 'Nombre', type: 'text', width: 180, validate: 'required' },
            { name: 'description', title: 'Descripción', type: 'text', width: 220 },
            {
                name: 'estimateShipping', title: 'Envío Estimado', type: 'number', width: 130, validate: 'required',
                itemTemplate: function (val, item) {
                    var curr = currencyItems.find(function (c) { return c.id === item.currencyId; });
                    return formatMoney(val, curr ? curr.sign : '');
                }
            },
            {
                name: 'serviceFeeInLocal', title: 'Servicio (Local)', type: 'number', width: 130, validate: 'required',
                itemTemplate: function (val) { return formatMoney(val, localCurrencySign); }
            },
            {
                name: 'currencyId', title: 'Moneda', type: 'select',
                items: currencyItems, valueField: 'id', textField: 'text', width: 100
            },
            { type: 'control' }
        ]
    });
}

$(function () {
    $.get('/Currencies?handler=Load', function (data) {
        currencyItems = data.map(function (c) { return { id: c.id, text: c.abbreviation, sign: c.sign || '' }; });
        initGrid();
    });
});
