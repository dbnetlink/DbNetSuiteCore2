declare class Dialog {
    dialog: HTMLDialogElement;
    dependentDialog: Dialog;
    control: ComponentControl;
    draggableDialog: DraggableDialog | null;
    constructor(dialog: HTMLDialogElement, control: ComponentControl);
    show(draggable?: boolean, modal?: boolean): void;
    close(): void;
}
