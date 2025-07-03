class UsersControl {
    private usersRolesGrid: GridControl|undefined = undefined;
    private currentUserId: String | undefined = undefined;

    constructor() {
    }

    public saveGridReference(gridControl: GridControl, args: any) {
        this.usersRolesGrid = gridControl;
    }
    public userSelected(formControl: FormControl, args: any) {
        this.currentUserId = formControl.formBody.dataset.id;
        const url = `/api/user/getuserroles?id=${this.currentUserId}`;
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

    private selectRoles(roleIds: Array<string>) {
        this.usersRolesGrid!.form.querySelectorAll("input.multi-select").forEach(e => { (e as HTMLInputElement).checked = false; e.replaceWith(e.cloneNode(true)); });
        roleIds.forEach(id => this.checkRole(id));
        this.usersRolesGrid!.form.querySelectorAll("input.multi-select").forEach(e => { e.addEventListener('click', (e) => this.roleUpdated(e))});
    }

    private checkRole(id: string) {
        var row = this.usersRolesGrid!.form.querySelector(`tr[data-id='${id}']`) as HTMLTableRowElement;
        var checkbox = row.querySelector("input.multi-select") as HTMLInputElement;
        checkbox.click();
    }

    public roleUpdated(e:Event) {
        const url = `/api/user/updateuserrole`;
        const checkbox = (e.target as HTMLInputElement);
        const requestOptions = {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ userId: this.currentUserId, roleSelected: checkbox.checked, roleId: checkbox.closest('tr')!.dataset.id })
        };

        fetch(url, requestOptions)
            .then(response => response.json())
            .then(data => this.selectRoles(data))
            .catch(error => console.error('Error:', error));
    }
}

var userControl = new UsersControl();
