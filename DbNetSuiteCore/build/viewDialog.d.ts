declare class ViewDialog extends Dialog {
    gridControl: GridControl;
    constructor(dialog: HTMLDialogElement, gridControl: GridControl);
    open(): void;
    update(): void;
    getRecord(): void;
    configureNavigation(tr: HTMLTableRowElement): void;
    configureButton(sibling: HTMLTableRowElement, buttonType: string): void;
    columnCell(columnName: string): HTMLDivElement;
    columnValue(columnName: string): string;
}
