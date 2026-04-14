declare class SelectControl extends ComponentControl {
    select: HTMLSelectElement;
    constructor(selectId: any);
    afterRequest(evt: any): void;
    private initialise;
    private selectChanged;
    private updateLinkedChildControls;
    private checkForError;
}
