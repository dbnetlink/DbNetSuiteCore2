declare class SearchDialog extends Dialog {
    lookupDialog: LookupDialog;
    constructor(dialog: HTMLDialogElement, componentControl: ComponentControl);
    bindSearchButton(): void;
    operatorSelected(event: Event): void;
    isVisible(el: any): boolean;
    valueEntered(event: Event): void;
    showLookup(event: Event): void;
    clear(): void;
    openLookup(tr: HTMLTableRowElement): void;
}
