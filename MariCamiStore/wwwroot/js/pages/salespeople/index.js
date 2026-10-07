var token = $('input[name="__RequestVerificationToken"]').val();

function ajaxPost(handler, data) {
    var d = $.Deferred();
    $.ajax({
        url: '?handler=' + handler, method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(data),
        success: function (r) { d.resolve(r); },
        error: function (xhr) {
            var msg = xhr.responseJSON && xhr.responseJSON.error ? xhr.responseJSON.error : xhr.responseText;
            alert('Error: ' + msg);
            d.reject();
        }
    });
    return d.promise();
}

$(function () {
    $('#jsGrid').jsGrid({
        height: 'auto', width: '100%',
        editing: true, inserting: true, sorting: true,
        paging: true, pageSize: 20, autoload: true,

        controller: {
            loadData: function () { return $.get('?handler=Load'); },
            insertItem: function (item) { return ajaxPost('Insert', item); },
            updateItem: function (item) { return ajaxPost('Update', item); }
        },

        fields: [
            { name: 'id', type: 'text', visible: false },
            { name: 'name', title: 'Nombre', type: 'text', width: 200, validate: 'required' },
            { name: 'nickName', title: 'Apodo', type: 'text', width: 140 },
            { name: 'phoneNumber', title: 'Teléfono', type: 'text', width: 120 },
            { name: 'email', title: 'Email', type: 'text', width: 200 },
            {
                name: 'isActive', title: 'Activo', type: 'checkbox', width: 70,
                // insertValue is a method in jsGrid; new salespeople start active by pre-checking the insert control
                insertTemplate: function () {
                    var $control = jsGrid.fields.checkbox.prototype.insertTemplate.call(this);
                    $control.prop('checked', true);
                    return $control;
                }
            },
            { type: 'control', deleteButton: false }
        ]
    });
});
