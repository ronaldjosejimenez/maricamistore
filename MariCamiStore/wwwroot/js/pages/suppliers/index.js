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

$(function () {
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
            { name: 'name', title: 'Nombre', type: 'text', width: 300, validate: 'required' },
            { type: 'control' }
        ]
    });
});
