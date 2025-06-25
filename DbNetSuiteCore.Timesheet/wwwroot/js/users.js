"use strict";
class UsersControl {
    saveGridReference(gridControl, args) {
        this.usersRolesGrid = gridControl;
    }
    userSelected(formControl, args) {
        this.currentUserId = formControl.formBody.dataset.id;
        const url = `/api/user/getuserroles?id=${formControl.formBody.dataset.id}`;
        fetch(url)
            .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
            .then(data => this.selectRoles(data))
            .catch(error => console.error('There was a problem with the fetch operation:', error));
    }
    selectRoles(roleIds) {
        this.usersRolesGrid.form.querySelectorAll("input.multi-select").forEach(e => { e.checked = false; e.replaceWith(e.cloneNode(true)); });
        roleIds.forEach(id => this.checkRole(id));
        this.usersRolesGrid.form.querySelectorAll("input.multi-select").forEach(e => { e.addEventListener('click', this.roleUpdated); });
    }
    checkRole(id) {
        var row = this.usersRolesGrid.form.querySelector(`tr[data-id='${id}']`);
        var checkbox = row.querySelector("input.multi-select");
        checkbox.click();
    }
    roleUpdated(e) {
        const url = `/api/user/updateuserrole`;
        const checkbox = e.target;
        const requestOptions = {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: this.currentUserId, roleSelected: checkbox.checked, roleId: checkbox.closest('tr').dataset.id })
        };
        fetch(url, requestOptions)
            .then(response => response.json())
            .then(data => console.log(data.id))
            .catch(error => console.error('Error:', error));
    }
}
var userControl = new UsersControl();
