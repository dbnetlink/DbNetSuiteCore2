class Dialog {
    constructor(dialog, control) {
        this.draggableDialog = null;
        this.dialog = dialog;
        this.dialog.style.margin = '0';
        this.dialog.style.position = 'fixed';
        this.control = control;
        let closeButtons = this.dialog.querySelectorAll(this.control.buttonSelector("close"));
        closeButtons.forEach((e) => {
            e.addEventListener("click", () => this.close());
        });
    }
    show(draggable = true, modal = false) {
        if (modal) {
            this.dialog.showModal();
        }
        else {
            this.dialog.show();
        }
        if (this.dialog.style.transform == '') {
            this.dialog.style.transform = `translate(-50%, -50%)`;
            this.dialog.style.left = '50%';
            this.dialog.style.top = '50%';
        }
        if (draggable && !this.draggableDialog) {
            this.draggableDialog = new DraggableDialog(this.dialog.id, "dialog-nav");
        }
    }
    close() {
        if (this.dependentDialog) {
            this.dependentDialog.close();
        }
        if (this.dialog && this.dialog.open) {
            this.dialog.close();
        }
    }
}
