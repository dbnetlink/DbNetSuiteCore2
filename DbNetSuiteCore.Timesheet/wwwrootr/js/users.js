function saveGridReference(gridControl, args) {
    window['usersRolesGrid'] = gridControl;
}
function userSelected(formControl, args) {
    window['currentUserId'] = formControl.formBody.dataset.id;
    const url = `/api/user/getuserroles?id=${formControl.formBody.dataset.id}`;
    fetch(url)
        .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
        .then(data => selectRoles(data))
        .catch(error => console.error('There was a problem with the fetch operation:', error));
}
function selectRoles(roleIds) {
    window['usersRolesGrid'].form.querySelectorAll("input.multi-select").forEach(e => { e.checked = false; e.replaceWith(e.cloneNode(true)); });
    roleIds.forEach(id => window['usersRolesGrid'].form.querySelector(`tr[data-id='${id}']`).querySelector("input.multi-select").click());
    window['usersRolesGrid'].form.querySelectorAll("input.multi-select").forEach(e => { e.addEventListener('click', roleUpdated); });
}
function roleUpdated(eventArgs) {
    const url = `/api/user/updateuserrole`;
    const requestOptions = {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: window['currentUserId'], roleSelected: eventArgs.target.checked, roleId: eventArgs.target.closest('tr').dataset.id })
    };
    fetch(url, requestOptions)
        .then(response => response.json())
        .then(data => console.log(data.id))
        .catch(error => console.error('Error:', error));
}
