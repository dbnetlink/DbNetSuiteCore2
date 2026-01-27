class SelectControl extends ComponentControl {
    select: HTMLSelectElement;
    constructor(selectId) {
        super(selectId)
    }

    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return
        }

        this.select = this.controlElement("select");

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
        this.controlElement("select").addEventListener("change", (ev: Event) => {
            this.selectChanged(ev.target as HTMLSelectElement);
        })
        this.invokeEventHandler('Initialised');
    }

    private selectChanged(target: HTMLSelectElement) {
        let url = '';
        if (target.selectedOptions.length) {
            var dataset = target.selectedOptions[0].dataset
            url = this.dataSourceIsFileSystem() && dataset.isdirectory && dataset.isdirectory.toLowerCase() == "true" ? dataset.path : ''
        }
        this.updateLinkedChildControls(target.selectedIndex.toString(), url);
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }

    private updateLinkedChildControls(selectedIndex: string, url:string) {
        this.updateLinkedControls(this.getLinkedControlIds(), selectedIndex, url)
    }

    private checkForError() {
        var select = this.controlElement("select") as HTMLSelectElement;
        const error = select.querySelector("div");
        if (error) {
            select.parentElement.nextElementSibling.after(error)
        }
    }

    public getSelectedOptions(): HTMLOptionElement[] {
        return Array.from(this.select.selectedOptions);
    }

    public updateFixedFilterParameters(params: any) {
        this.updateFixedFilterParams(params);
    }

    public updateApiRequestParameters(params: any) {
        this.updateApiRequestParams(params);
    }
}