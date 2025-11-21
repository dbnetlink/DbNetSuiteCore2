declare class ConfirmDialog extends Dialog {
    event: any;
    constructor(control: ComponentControl, prompt: string);
    open(event: any): void;
    confirm(): void;
    cancel(): void;
}
