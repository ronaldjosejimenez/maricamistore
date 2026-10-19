var token = $('input[name="__RequestVerificationToken"]').val();

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

// Full customer catalog, loaded once and filtered client-side; reset to null to refetch.
var customerCache = null;
var SEARCH_FIELDS = ['nickName', 'name', 'phoneNumber', 'email', 'address'];

// Normalizes text for matching: no accents, lowercase, trimmed.
function norm(s) {
    return (s == null ? '' : s).toString().normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
}

// A customer passes when every non-empty column filter is contained in its field
// and the global term (if any) is contained in at least one search field.
function matchesCustomer(item, filter, term) {
    for (var i = 0; i < SEARCH_FIELDS.length; i++) {
        var f = SEARCH_FIELDS[i];
        var value = norm(filter && filter[f]);
        if (value && norm(item[f]).indexOf(value) === -1) return false;
    }
    if (!term) return true;
    return SEARCH_FIELDS.some(function (f) { return norm(item[f]).indexOf(term) !== -1; });
}

function refreshCustomers() {
    customerCache = null;
    $('#jsGrid').jsGrid('search');
}

$(function () {
    var searchTimer = null;
    $('#customer-search').on('input', function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () { $('#jsGrid').jsGrid('search'); }, 200);
    });

    $('#jsGrid').jsGrid({
        height: 'auto', width: '100%',
        filtering: true, editing: true, inserting: true, sorting: true,
        paging: true, pageSize: 20, autoload: true,
        noDataContent: 'No se encontraron clientes',

        onItemInserted: refreshCustomers,
        onItemUpdated: refreshCustomers,
        onItemDeleted: refreshCustomers,

        controller: {
            loadData: function (filter) {
                var d = $.Deferred();
                var apply = function () {
                    var term = norm($('#customer-search').val());
                    d.resolve(customerCache.filter(function (c) { return matchesCustomer(c, filter, term); }));
                };
                if (customerCache) {
                    apply();
                } else {
                    $.get('?handler=Load')
                        .done(function (data) { customerCache = data || []; apply(); })
                        .fail(function () { d.reject(); });
                }
                return d.promise();
            },
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
            { name: 'nickName', title: 'Apodo', type: 'text', width: 140, validate: 'required' },
            { name: 'name', title: 'Nombre Completo', type: 'text', width: 200 },
            { name: 'phoneNumber', title: 'Teléfono', type: 'text', width: 120 },
            { name: 'email', title: 'Email', type: 'text', width: 200 },
            { name: 'address', title: 'Dirección', type: 'text', width: 250 },
            { type: 'control' }
        ]
    });
});
