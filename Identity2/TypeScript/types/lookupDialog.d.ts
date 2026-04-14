declare class LookupDialog extends Dialog {
    select: HTMLSelectElement;
    input: HTMLInputElement;
    caption: string;
    constructor(dialog: HTMLDialogElement, componentControl: ComponentControl);
    open(select: HTMLSelectElement, input: HTMLInputElement, label: string): void;
    apply(): void;
}
