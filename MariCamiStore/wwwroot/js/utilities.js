function ParseDataSourceToJson(dataSource) {
    return JSON.parse(JSON.stringify(dataSource));
}

/**
 * Formats a monetary amount with an optional currency sign.
 * Returns "{sign} {amount}" when sign is non-empty, else just the formatted amount.
 * Uses es-CR locale (e.g. 1.000,00).
 * @param {number} amount
 * @param {string} sign
 * @returns {string}
 */
function formatMoney(amount, sign) {
    var formatted = (parseFloat(amount) || 0).toLocaleString('es-CR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    return sign ? sign + ' ' + formatted : formatted;
}