class ConfirmDialog {
    dialog: HTMLDialogElement;
    control: ComponentControl;
    event: any;

    constructor(control: ComponentControl) {
        this.control = control;
        this.dialog = this.control.controlElement(".confirm-dialog");

        let closeButtons = this.dialog.querySelectorAll(this.control.buttonSelector("close"));
        closeButtons.forEach((e) => {
            e.addEventListener("click", () => this.dialog.close());
        });
        this.dialog.querySelector(this.control.buttonSelector("confirm")).addEventListener("click", () => this.confirm());
        this.dialog.querySelector(this.control.buttonSelector("cancel")).addEventListener("click", () => this.cancel());
    }

    public show(event: any, container: HTMLElement) {
        this.event = event;
        this.dialog.show();

        this.dialog.style.left = this.coordinate(container.offsetLeft, container.clientWidth, this.dialog.clientWidth); 
        this.dialog.style.top = this.coordinate(container.offsetTop, container.clientHeight, this.dialog.clientHeight); 

    }

    private coordinate(offset, container, dialog) {
        let adj = container > dialog ? ((container - dialog) * 0.5) : 0;
        return `${offset + (dialog * 0.5) + adj}px`;
    }

    confirm() {
        this.event.detail.issueRequest(true);
        this.dialog.close();
    }

    cancel() {
        this.dialog.close();
    }
}