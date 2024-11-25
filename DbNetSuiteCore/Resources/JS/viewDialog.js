class ViewDialog {
    constructor(dialog, gridControl) {
        this.draggableDialog = null;
        this.dialog = dialog;
        this.gridControl = gridControl;
        let closeButtons = this.dialog.querySelectorAll(this.gridControl.buttonSelector("close"));
        closeButtons.forEach((e) => {
            e.addEventListener("click", () => this.dialog.close());
        });
        this.dialog.querySelector(this.gridControl.buttonSelector("previous")).addEventListener("click", () => this.gridControl.previousRow());
        this.dialog.querySelector(this.gridControl.buttonSelector("next")).addEventListener("click", () => this.gridControl.nextRow());
        this.gridControl.getButton("view").addEventListener("click", this.open.bind(this));
    }
    open() {
        this.getRecord();
    }
    show() {
        this.dialog.show();
        if (!this.draggableDialog) {
            this.draggableDialog = new DraggableDialog(this.dialog.id, "dialog-nav", this.gridControl.gridControlElement("tbody"));
        }
    }
    update() {
        if (this.dialog && this.dialog.open) {
            this.getRecord();
        }
    }
    getRecord() {
        let input = this.dialog.querySelector("input[hx-post]");
        input.value = this.gridControl.selectedRow.dataset.id;
        htmx.trigger(input, "changed");
    }
    configureNavigation(tr) {
        this.configureButton(tr.previousElementSibling, "previous");
        this.configureButton(tr.nextElementSibling, "next");
    }
    configureButton(sibling, buttonType) {
        this.dialog.querySelector(this.gridControl.buttonSelector(buttonType)).disabled = (!sibling || sibling.classList.contains("grid-row") == false);
    }
    columnCell(columnName) {
        return this.dialog.querySelector(`div[data-columnname='${columnName.toLowerCase()}']`);
    }
    columnValue(columnName) {
        let div = this.columnCell(columnName);
        return div ? div.dataset.value : null;
    }
}
