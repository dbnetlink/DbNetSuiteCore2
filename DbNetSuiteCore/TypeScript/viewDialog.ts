class ViewDialog extends Dialog {
    gridControl:GridControl
    constructor(dialog: HTMLDialogElement, gridControl: GridControl) {
        super(dialog, gridControl);
        this.gridControl = gridControl;
        this.dialog.querySelector(this.control.buttonSelector("previous")).addEventListener("click", () => gridControl.previousRow());
        this.dialog.querySelector(this.control.buttonSelector("next")).addEventListener("click", () => gridControl.nextRow());
        this.control.getButton("view").addEventListener("click", this.open.bind(this))
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
        let input = this.dialog.querySelector("input[hx-post]") as HTMLInputElement;
        input.value = this.gridControl.selectedRow.dataset.idx;
        htmx.trigger(input, "changed");
    }

    configureNavigation(tr: HTMLTableRowElement) {
        this.configureButton(tr.previousElementSibling as HTMLTableRowElement, "previous");
        this.configureButton(tr.nextElementSibling as HTMLTableRowElement, "next");
    }

    configureButton(sibling: HTMLTableRowElement, buttonType: string) {
        (this.dialog.querySelector(this.gridControl.buttonSelector(buttonType)) as HTMLButtonElement).disabled = (!sibling || sibling.classList.contains("grid-row") == false)  
    }

    columnCell(columnName: string) :HTMLDivElement {
        return this.dialog.querySelector(`div[data-columnname='${columnName.toLowerCase()}']`);
    }

    columnValue(columnName: string) {
        let div = this.columnCell(columnName)
        return div ? div.dataset.value : null;
    }
}