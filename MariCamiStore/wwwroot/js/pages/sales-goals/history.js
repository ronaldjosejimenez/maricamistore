var monthNames = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio',
    'agosto', 'setiembre', 'octubre', 'noviembre', 'diciembre'];

function fmt(n) {
    return Number(n || 0).toLocaleString('es-CR', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
}

function fmtPct(n) {
    return Number(n || 0).toLocaleString('es-CR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%';
}

function loadList(salespersonId) {
    $('#history-detail-card').hide();
    $('#history-list-card').show();
    $.get('?handler=List&salespersonId=' + salespersonId, function (rows) {
        var $tbody = $('#history-table tbody').empty();
        $('#history-empty').toggle(rows.length === 0);
        $('#history-table').toggle(rows.length > 0);
        rows.forEach(function (r) {
            var $tr = $('<tr>');
            $tr.append($('<td>').text(monthNames[r.month - 1]));
            $tr.append($('<td>').text(r.year));
            $tr.append($('<td class="text-right">').text(r.currencySign + ' ' + fmt(r.goalAmount)));
            $tr.append($('<td class="text-right">').text(r.currencySign + ' ' + fmt(r.actualAmount)));
            $tr.append($('<td class="text-right">').text(fmtPct(r.compliancePercentage)));
            $tr.append($('<td class="text-right">').append(
                $('<button class="btn btn-xs btn-info">Ver detalle</button>').on('click', function () {
                    loadDetail(r.id);
                })));
            $tbody.append($tr);
        });
    });
}

function loadDetail(goalId) {
    $.get('?handler=Detail&goalId=' + goalId, function (g) {
        if (g && g.success === false) {
            $('#history-error').text(g.error).show();
            return;
        }
        $('#history-error').hide();
        $('#detail-title').text('Meta de ' + monthNames[g.month - 1] + ' ' + g.year + ' - Meta ' +
            g.currencySign + ' ' + fmt(g.goalAmount) + ' / Real ' + g.currencySign + ' ' + fmt(g.actualAmount) +
            ' (' + fmtPct(g.compliancePercentage) + ')');
        var $tbody = $('#detail-table tbody').empty();
        g.days.forEach(function (d) {
            var $tr = $('<tr>');
            $tr.append($('<td>').text(d.dayOfMonth + ' - ' + d.weekday));
            $tr.append($('<td>').text(d.date));
            $tr.append($('<td class="text-right">').text(fmt(d.proposedAmount)));
            $tr.append($('<td class="text-right">').text(fmt(d.goalAmount)));
            $tr.append($('<td class="text-right">').text(fmt(d.actualAmount)));
            $tr.append($('<td class="text-right">').text(fmtPct(d.compliancePercentage)));
            $tr.append($('<td class="text-center">').html(d.goalAmount !== d.proposedAmount ? '<span class="badge badge-warning">Ajustado</span>' : ''));
            $tbody.append($tr);
        });
        $('#history-list-card').hide();
        $('#history-detail-card').show();
    });
}

$(function () {
    $.get('?handler=Salespeople', function (people) {
        if (!people.length) {
            $('#history-empty-people').show();
            return;
        }
        people.forEach(function (p) {
            $('#salesperson-select').append($('<option>').val(p.id).text(p.text));
        });
        $('#history-panel').show();
        $('#salesperson-select').on('change', function () { loadList($(this).val()); });
        $('#btn-back').on('click', function () { loadList($('#salesperson-select').val()); });
        loadList(people[0].id);
    });
});
