class ConfirmDialog extends Dialog {
    constructor(control, prompt) {
        super(control.controlElement(".confirm-dialog"), control);
        this.dialog.querySelector(".prompt").innerHTML = prompt;
        this.dialog.querySelector(this.control.buttonSelector("confirm")).addEventListener("click", () => this.confirm());
        this.dialog.querySelector(this.control.buttonSelector("cancel")).addEventListener("click", () => this.cancel());
    }
    open(event) {
        this.event = event;
        this.show(false, true);
    }
    confirm() {
        this.event.detail.issueRequest(true);
        this.dialog.close();
    }
    cancel() {
        this.dialog.close();
    }
}
