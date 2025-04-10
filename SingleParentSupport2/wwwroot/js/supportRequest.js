// Support request JavaScript for frontend-backend integration
$(document).ready(function () {
    // Initialize DataTable for better support request list display
    if ($.fn.DataTable) {
        $('#supportRequestTable').DataTable({
            responsive: true,
            order: [[1, 'desc']], // Sort by date by default
            language: {
                search: "Search requests:",
                lengthMenu: "Show _MENU_ requests per page",
                info: "Showing _START_ to _END_ of _TOTAL_ requests"
            }
        });
    }

    // Handle support request form submission via AJAX
    $('#createSupportRequestForm').on('submit', function (e) {
        e.preventDefault();

        if (!$(this).valid()) {
            return false;
        }

        const formData = {
            Name: $('#Name').val(),
            Email: $('#Email').val(),
            Phone: $('#Phone').val(),
            RequestType: $('#RequestType').val(),
            Description: $('#Description').val()
        };

        $.ajax({
            url: '/api/SupportRequestApi',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function (response) {
                window.location.href = `/SupportRequest/Confirmation/${response.id}`;
            },
            error: function (xhr) {
                showToast('Error submitting request', 'danger');
                console.error('Error submitting request:', xhr.responseText);
            }
        });
    });

    // Handle response form submission via AJAX
    $('#respondForm').on('submit', function (e) {
        e.preventDefault();

        if (!$(this).valid()) {
            return false;
        }

        const requestId = $('#Id').val();
        const formData = {
            Id: requestId,
            Status: $('#Status').val(),
            Response: $('#Response').val()
        };

        $.ajax({
            url: `/api/SupportRequestApi/${requestId}/respond`,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function () {
                showToast('Response submitted successfully', 'success');
                window.location.href = '/SupportRequest';
            },
            error: function (xhr) {
                showToast('Error submitting response', 'danger');
                console.error('Error submitting response:', xhr.responseText);
            }
        });
    });

    // Function to display toast notifications
    function showToast(message, type) {
        const toast = `
            <div class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        $('.toast-container').append(toast);
        $('.toast').toast('show');

        // Remove toast after it's hidden
        $('.toast').on('hidden.bs.toast', function () {
            $(this).remove();
        });
    }

    // Load support requests via API
    function loadSupportRequests() {
        $.ajax({
            url: '/api/SupportRequestApi',
            type: 'GET',
            success: function (data) {
                if (data.length === 0) {
                    $('#supportRequestTableContainer').html(
                        '<div class="alert alert-info">' +
                        '<i class="bi bi-info-circle"></i> You don\'t have any support requests yet. ' +
                        'Click the "New Support Request" button to create one.' +
                        '</div>'
                    );
                    return;
                }

                const table = $('#supportRequestTable').DataTable();
                table.clear();

                data.forEach(request => {
                    let statusBadge = '';
                    switch (request.status) {
                        case 'Pending':
                            statusBadge = '<span class="badge bg-warning">Pending</span>';
                            break;
                        case 'In Progress':
                            statusBadge = '<span class="badge bg-info">In Progress</span>';
                            break;
                        case 'Resolved':
                            statusBadge = '<span class="badge bg-success">Resolved</span>';
                            break;
                        case 'Closed':
                            statusBadge = '<span class="badge bg-secondary">Closed</span>';
                            break;
                        default:
                            statusBadge = `<span class="badge bg-primary">${request.status}</span>`;
                            break;
                    }

                    const actions = `
                        <div class="btn-group" role="group">
                            <a href="/SupportRequest/Details/${request.id}" class="btn btn-sm btn-outline-primary">
                                <i class="bi bi-eye"></i> View
                            </a>
                            ${isAdminOrVolunteer ?
                            `<a href="/SupportRequest/Respond/${request.id}" class="btn btn-sm btn-outline-secondary">
                                    <i class="bi bi-reply"></i> Respond
                                </a>` : ''}
                        </div>
                    `;

                    table.row.add([
                        request.requestType,
                        new Date(request.requestDate).toLocaleDateString(),
                        statusBadge,
                        actions
                    ]).draw(false);
                });
            },
            error: function (xhr) {
                console.error('Error loading support requests:', xhr.responseText);
                showToast('Error loading support requests', 'danger');
            }
        });
    }

    // Initial load if on index page
    if ($('#supportRequestTable').length) {
        loadSupportRequests();
    }
});
