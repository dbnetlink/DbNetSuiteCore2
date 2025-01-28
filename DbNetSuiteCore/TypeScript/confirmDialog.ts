class ConfirmDialog extends Dialog{
    event: any;

    constructor(control: ComponentControl, prompt: string) {
        super(control.controlElement(".confirm-dialog"),control )
        this.dialog.querySelector(".prompt").innerHTML = prompt;

        this.dialog.querySelector(this.control.buttonSelector("confirm")).addEventListener("click", () => this.confirm());
        this.dialog.querySelector(this.control.buttonSelector("cancel")).addEventListener("click", () => this.cancel());
    }

    public open(event: any, container: HTMLElement) {
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