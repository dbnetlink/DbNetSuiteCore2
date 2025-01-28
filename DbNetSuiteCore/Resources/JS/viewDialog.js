class ViewDialog extends Dialog {
    constructor(dialog, gridControl) {
        super(dialog, gridControl);
        this.gridControl = gridControl;
        this.dialog.querySelector(this.control.buttonSelector("previous")).addEventListener("click", () => gridControl.previousRow());
        this.dialog.querySelector(this.control.buttonSelector("next")).addEventListener("click", () => gridControl.nextRow());
        this.control.getButton("view").addEventListener("click", this.open.bind(this));
    }
    open() {
        this.getRecord();
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
