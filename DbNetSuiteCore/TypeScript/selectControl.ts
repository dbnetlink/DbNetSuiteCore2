class SelectControl extends ComponentControl {
    select: HTMLSelectElement | null = null;
    selectedOptions: HTMLCollectionOf<HTMLOptionElement> | undefined; 
    constructor(selectId:string) {
        super(selectId)
    }

    afterRequest(evt:any) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return
        }

        this.select = this.controlElement("select") as HTMLSelectElement;

        let selectElements = this.controlElements("select");
        this.select.innerHTML = selectElements[1].innerHTML;
        selectElements[1].remove();

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.invokeEventHandler('OptionsLoaded');
        this.selectChanged(this.select);
        this.checkForError();
    }

    private initialise() {
        this.loaded = true;
        this.controlElement("select").addEventListener("change", (ev: Event) => {
            this.selectChanged(ev.target as HTMLSelectElement);
        })
        this.invokeEventHandler('Initialised');
    }

    private selectChanged(target: HTMLSelectElement) {
        let url = '';
        if (target.selectedOptions.length) {
            var dataset = target.selectedOptions[0].dataset
            url = this.dataSourceIsFileSystem() && dataset.isdirectory && dataset.isdirectory.toLowerCase() == "true" ? dataset.path as string : ''
        }
        this.updateLinkedChildControls(target.selectedIndex.toString(), url);
        this.selectedOptions = target.selectedOptions;
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }

    private updateLinkedChildControls(selectedIndex: string, url:string) {
        this.updateLinkedControls(this.getLinkedControlIds(), selectedIndex, url)
    }

    private checkForError() {
        const error = (this.select as HTMLSelectElement).querySelector("div");
        if (error) {
            (this.select as HTMLSelectElement)?.parentElement?.nextElementSibling?.after(error)
        }
    }

    public getSelectedOptions(): HTMLOptionElement[] {
        return Array.from(this.select!.selectedOptions);
    }

    public updateFixedFilterParameters(params: any) {
        this.updateFixedFilterParams(params);
    }

    public updateApiRequestParameters(params: any) {
        this.updateApiRequestParams(params);
    }
}