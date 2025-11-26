declare class HtmlEditor {
    textarea: HTMLTextAreaElement;
    formControl: FormControl;
    editorInstance: any;
    static TinyMCE: string;
    static CKEditor: string;
    static Froala: string;
    constructor(textarea: HTMLTextAreaElement, formControl: FormControl);
    reset(textarea: HTMLTextAreaElement): void;
    initialise(): void;
    assignContent(evt: any): void;
    private initTinyMceEditor;
    private updateConfiguration;
    private initCKEditorEditor;
    private initFroalaEditor;
    private static editorName;
    static removeElement(textarea: HTMLTextAreaElement): void;
    static editor(editor: string): any;
}
