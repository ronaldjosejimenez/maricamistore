(function () {
    var token = $('input[name="__RequestVerificationToken"]').val();

    // Global AJAX loading overlay
    $(document).ajaxStart(function () {
        $('#loading-overlay').addClass('active');
    }).ajaxStop(function () {
        $('#loading-overlay').removeClass('active');
    });

    // Organization selector
    $(document).on('click', '.org-select-item', function (e) {
        e.preventDefault();
        var orgId = $(this).data('org-id');
        var orgName = $(this).text().trim();

        $.ajax({
            url: '/Organizations/Index?handler=SetActive',
            method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({ organizationId: orgId }),
            success: function (result) {
                if (result.success) {
                    $('#active-org-name').text(orgName);
                    $('.org-select-item').removeClass('active');
                    $('[data-org-id="' + orgId + '"]').addClass('active');
                    window.location.reload();
                }
            }
        });
    });
}());
