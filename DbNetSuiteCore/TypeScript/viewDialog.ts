class ViewDialog {
    dialog: HTMLDialogElement;
    gridControl: GridControl;

    constructor(dialog: HTMLDialogElement, gridControl: GridControl) {
        this.dialog = dialog;
        this.gridControl = gridControl;

        let closeButtons = this.dialog.querySelectorAll(this.gridControl.buttonSelector("close"));
        closeButtons.forEach((e) => {
            e.addEventListener("click", () => this.dialog.close());
        });
        this.dialog.querySelector(this.gridControl.buttonSelector("previous")).addEventListener("click", () => this.gridControl.previousRow());
        this.dialog.querySelector(this.gridControl.buttonSelector("next")).addEventListener("click", () => this.gridControl.nextRow());
        this.gridControl.getButton("view").addEventListener("click", this.open.bind(this))
        new DraggableDialog(this.dialog.id, "dialog-nav");
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
        input.value = this.gridControl.selectedRow.dataset.id;
        htmx.trigger(input, "changed");
    }

    close() {
        if (this.dialog && this.dialog.open) {
            this.close();
        }
    }

    configureNavigation(tr: HTMLTableRowElement) {
        this.configureButton(tr.previousElementSibling as HTMLTableRowElement, "previous");
        this.configureButton(tr.nextElementSibling as HTMLTableRowElement, "next");
    }

    configureButton(sibling: HTMLTableRowElement, buttonType: string) {
        (this.dialog.querySelector(this.gridControl.buttonSelector(buttonType)) as HTMLButtonElement).disabled = (!sibling || sibling.classList.contains("grid-row") == false)  
    }
}