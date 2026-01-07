declare class TreeControl extends ComponentControl {
    tree: HTMLSelectElement;
    constructor(selectId: any);
    afterRequest(evt: any): void;
    private initialise;
    private selectChanged;
    private updateLinkedChildControls;
    private checkForError;
    getSelectedOptions(): HTMLOptionElement[];
}
