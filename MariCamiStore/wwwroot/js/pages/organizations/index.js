var token = $('input[name="__RequestVerificationToken"]').val();

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

$(function () {
    $('#jsGrid').jsGrid({
        height: 'auto', width: '100%',
        filtering: false, editing: true, inserting: true, sorting: true,
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
                ajaxPost('Delete', { id: item.id }, function (r) {
                    if (r.success) { d.resolve(); }
                    else { alert(r.error); d.reject(); }
                });
                return d.promise();
            }
        },

        fields: [
            { name: 'id', type: 'text', visible: false },
            { name: 'name', title: 'Nombre', type: 'text', width: 300, validate: 'required' },
            {
                title: 'Activa', width: 100, editing: false, inserting: false,
                itemTemplate: function (val, item) {
                    return $('<button class="btn btn-xs btn-success">Usar</button>')
                        .on('click', function () {
                            ajaxPost('SetActive', { organizationId: item.id }, function () {
                                location.reload();
                            });
                        });
                }
            },
            { type: 'control' }
        ]
    });
});
