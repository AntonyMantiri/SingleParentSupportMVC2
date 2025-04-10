// User management JavaScript for frontend-backend integration
$(document).ready(function () {
    // Initialize DataTable for better user list display
    if ($.fn.DataTable) {
        $('#userTable').DataTable({
            responsive: true,
            order: [[3, 'desc']], // Sort by join date by default
            language: {
                search: "Search users:",
                lengthMenu: "Show _MENU_ users per page",
                info: "Showing _START_ to _END_ of _TOTAL_ users"
            }
        });
    }

    // Handle role checkbox changes
    $('.role-checkbox').on('change', function () {
        const userId = $(this).data('user-id');
        const role = $(this).data('role');
        const isChecked = $(this).is(':checked');

        // Call API to update user roles
        $.ajax({
            url: `/api/UserApi/${userId}/roles`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                role: role,
                add: isChecked
            }),
            success: function (response) {
                showToast('Role updated successfully', 'success');
            },
            error: function (xhr) {
                showToast('Error updating role', 'danger');
                console.error('Error updating role:', xhr.responseText);
                // Revert checkbox state on error
                $(this).prop('checked', !isChecked);
            }
        });
    });

    // Handle user deletion via AJAX
    $('.delete-user-btn').on('click', function (e) {
        e.preventDefault();
        const userId = $(this).data('user-id');
        const userName = $(this).data('user-name');

        if (confirm(`Are you sure you want to delete ${userName}? This action cannot be undone.`)) {
            $.ajax({
                url: `/api/UserApi/${userId}`,
                type: 'DELETE',
                success: function () {
                    showToast('User deleted successfully', 'success');
                    // Remove row from table
                    $(`tr[data-user-id="${userId}"]`).fadeOut(function () {
                        $(this).remove();
                    });
                },
                error: function (xhr) {
                    showToast('Error deleting user', 'danger');
                    console.error('Error deleting user:', xhr.responseText);
                }
            });
        }
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

    // Toggle volunteer fields visibility based on IsVolunteer checkbox
    function toggleVolunteerFields() {
        if ($("#IsVolunteer").is(":checked")) {
            $(".volunteer-fields").show();
        } else {
            $(".volunteer-fields").hide();
        }
    }

    // Initial state
    toggleVolunteerFields();

    // On change
    $("#IsVolunteer").change(function () {
        toggleVolunteerFields();
    });

    // Form validation for user edit
    $("#userEditForm").validate({
        rules: {
            Email: {
                required: true,
                email: true
            },
            FirstName: "required",
            LastName: "required"
        },
        messages: {
            Email: {
                required: "Please enter an email address",
                email: "Please enter a valid email address"
            },
            FirstName: "Please enter a first name",
            LastName: "Please enter a last name"
        },
        errorElement: "div",
        errorClass: "invalid-feedback",
        highlight: function (element) {
            $(element).addClass("is-invalid").removeClass("is-valid");
        },
        unhighlight: function (element) {
            $(element).addClass("is-valid").removeClass("is-invalid");
        },
        errorPlacement: function (error, element) {
            error.insertAfter(element);
        }
    });
});
