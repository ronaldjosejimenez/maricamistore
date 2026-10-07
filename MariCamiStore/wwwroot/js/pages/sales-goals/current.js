var token = $('input[name="__RequestVerificationToken"]').val();
var currentGoal = null;
var currencies = [];

var monthNames = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio',
    'agosto', 'setiembre', 'octubre', 'noviembre', 'diciembre'];

function fmt(n) {
    return Number(n || 0).toLocaleString('es-CR', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
}

function fmtPct(n) {
    return Number(n || 0).toLocaleString('es-CR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%';
}

function crToday() {
    return new Date().toLocaleDateString('en-GB', { timeZone: 'America/Costa_Rica' });
}

function showError(msg) {
    $('#goal-error').text(msg).show();
}

function clearError() {
    $('#goal-error').hide().text('');
}

function post(handler, data) {
    return $.ajax({
        url: '?handler=' + handler, method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(data)
    });
}

function statusClass(status) {
    if (status === 'red') return 'goal-red';
    if (status === 'yellow') return 'goal-yellow';
    if (status === 'green') return 'goal-green';
    return '';
}

function render(goal) {
    currentGoal = goal;
    clearError();
    $('#goal-panel').show();
    $('#goal-title').text('Meta de ' + monthNames[goal.month - 1] + ' ' + goal.year);

    var sign = goal.currencySign || '';
    $('#currency-select').val(goal.currencyId);
    $('#goal-input').val(goal.goalAmount);
    $('#actual-total').text(sign + ' ' + fmt(goal.actualAmount));
    $('#compliance-total').text(fmtPct(goal.compliancePercentage));
    $('#today-total').text(fmtPct(goal.todayPercentage))
        .closest('.info-box').removeClass('goal-red goal-yellow goal-green bg-light')
        .addClass(statusClass(goal.todayStatus) || 'bg-light');

    var today = crToday();
    var $tbody = $('#days-table tbody').empty();
    goal.days.forEach(function (d) {
        var adjusted = d.goalAmount !== d.proposedAmount;
        var $tr = $('<tr>').toggleClass('today-row', goal.isCurrentMonth && d.date === today);
        $tr.append($('<td>').text(d.dayOfMonth + ' - ' + d.weekday));
        $tr.append($('<td>').text(d.date));
        $tr.append($('<td class="text-right">').text(fmt(d.proposedAmount)));
        $tr.append($('<td class="text-right">').append(cellInput(d, 'goalAmount')));
        $tr.append($('<td>').html(adjusted ? '<span class="badge badge-warning">Ajustado</span>' : ''));
        $tr.append($('<td class="text-right">').append(cellInput(d, 'actualAmount')));
        $tr.append($('<td class="text-right">').text(fmtPct(d.compliancePercentage)));
        $tbody.append($tr);
    });
}

function cellInput(day, field) {
    var $input = $('<input type="number" step="0.01" min="0" class="form-control form-control-sm cell-input">')
        .val(day[field])
        .prop('disabled', !currentGoal.isCurrentMonth);
    $input.on('change', function () {
        var value = $input.val();
        if (value === '' || isNaN(Number(value)) || Number(value) < 0) {
            render(currentGoal);
            showError('El monto debe ser un número mayor o igual a cero.');
            return;
        }
        var payload = { dayId: day.id };
        payload[field] = Number(value);
        send('UpdateDay', payload);
    });
    return $input;
}

function send(handler, payload) {
    post(handler, payload)
        .done(function (r) {
            if (r && r.success === false) {
                handleRejected(r.error);
            } else {
                render(r);
            }
        })
        .fail(function (xhr) {
            handleRejected(xhr.responseText || 'Error inesperado.');
        });
}

function handleRejected(message) {
    // Revert the screen to the server state; a stale month reloads the current one (FR-026).
    var salespersonId = $('#salesperson-select').val();
    loadGoal(salespersonId, function () { showError(message); });
}

function loadGoal(salespersonId, done) {
    $.get('?handler=Goal&salespersonId=' + salespersonId, function (r) {
        if (r && r.success === false) {
            $('#goal-panel').hide();
            showError(r.error);
            return;
        }
        render(r);
        if (done) done();
    });
}

function sendHeader() {
    if (!currentGoal) return;
    var goalAmount = Number($('#goal-input').val());
    if (isNaN(goalAmount) || goalAmount <= 0) {
        render(currentGoal);
        showError('La meta debe ser mayor a cero.');
        return;
    }
    send('UpdateHeader', {
        goalId: currentGoal.id,
        goalAmount: goalAmount,
        currencyId: $('#currency-select').val()
    });
}

$(function () {
    $.when($.get('/Currencies?handler=Load'), $.get('?handler=Salespeople')).done(function (cur, sp) {
        currencies = cur[0];
        var people = sp[0];

        if (!people.length) {
            $('#goal-empty').show();
            return;
        }

        currencies.forEach(function (c) {
            $('#currency-select').append($('<option>').val(c.id).text(c.abbreviation));
        });
        people.forEach(function (p) {
            $('#salesperson-select').append($('<option>').val(p.id).text(p.text));
        });

        $('#salesperson-select').on('change', function () { loadGoal($(this).val()); });
        $('#goal-input').on('change', sendHeader);
        $('#currency-select').on('change', sendHeader);

        loadGoal(people[0].id);
    });
});
