class TreeControl extends ComponentControl {
    tree: HTMLSelectElement;
    searchEnabled: boolean = false;
    selectionLabel: HTMLSelectElement;
    constructor(selectId) {
        super(selectId)
    }

    afterRequest(evt:any) {
        let treeId = evt.target.closest("form").id;
        if (treeId.startsWith(this.controlId) == false) {
            return
        }

        this.tree = this.controlElement("div.tree-root");

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.controlElements('span.open-icon').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.toggleNode(e)) });
        this.controlElements('span.close-icon').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.toggleNode(e)) });
        this.controlElements('span.leaf-text[selectable="true"]').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.selectLeaf(e)) });
        this.controlElements('span.node-text[selectable="true"]').forEach(div => { div.addEventListener("click", (e: MouseEvent) => this.selectNode(e)) });

        if (this.selectionLabel) {
            this.selectionLabel.innerText = this.selectionLabel.dataset.selectionplaceholder;
        }
    }

    private toggleDropdown(ev) {
        this.controlElement("#dropdownMenu").classList.toggle("show");
    }

    private toggleNode(event: MouseEvent) {
        let target = event.target as HTMLElement;
        let nodeHeader = target.closest('.node-header');
        nodeHeader.querySelectorAll('span.icon').forEach(span => { span.classList.toggle('hidden') });
        event.stopPropagation();
        const node = target.parentElement;
        node.classList.toggle('open');
        target.closest('.node').querySelector(".node-content").classList.toggle('hidden');
    }

    private selectLeaf(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const selectedLeaf = target.closest("div");
        this.selectParentNodes(selectedLeaf)
    }

    private selectNode(event: MouseEvent) {
        let target = event.target as HTMLElement;
        const selectedNode = target.closest(".node-header") as HTMLElement;
        this.selectParentNodes(selectedNode)
    }

    private selectParentNodes(selectedElement: HTMLElement) {
        let previouslySelected = this.controlElement('.selected');
        if (previouslySelected) {
            previouslySelected.classList.remove("selected");
        };
        selectedElement.classList.add("selected"); 
        selectedElement = selectedElement.closest('div[data-value]');
        let path = [selectedElement.dataset.description];
        let parentNode: HTMLDivElement = selectedElement.parentElement.parentElement.closest('.node');

        let parentValues = [];
        let parentDescriptions = [];

        while (parentNode) {
            const headerText = parentNode.dataset.description;
            path.unshift(headerText);
            parentDescriptions.push(parentNode.dataset.description);
            parentValues.push(parentNode.dataset.description);
            parentNode = parentNode.parentElement.parentElement.closest('.node');
        }

        if (this.selectionLabel) {
            this.selectionLabel.innerHTML = `<span class="path-prefix">${this.selectionLabel.dataset.selectiontitle}</span>${path.join(' &gt; ')}`;
            this.controlElement("#dropdownMenu").classList.remove("show");
        }

        this.invokeEventHandler('ItemSelected', { value: selectedElement.dataset.value, description: selectedElement.dataset.description, parentValues: parentValues, parentDescriptions: parentDescriptions });
    }

    private reset(e:MouseEvent) {
        e.stopPropagation();
        let treeSearch: HTMLInputElement = this.controlElement('#treeSearch')
        treeSearch.value = '';
        treeSearch.dispatchEvent(new Event('input'));
        if (this.selectionLabel) {
            this.selectionLabel.innerText = this.selectionLabel.dataset.selectionplaceholder;
        }
    }

    private search(e: InputEvent) {
        const filter = (e.target as HTMLInputElement).value.toLowerCase();
        const items = this.controlElements('.node, .leaf');

        this.controlElements('.close-icon').forEach(e => { e.classList.add('hidden') });
        this.controlElements('.open-icon').forEach(e => { e.classList.remove('hidden') });

        items.forEach(item => {
            const text = item.innerText.toLowerCase();
            const isMatch = text.includes(filter);

            if (!filter) {
                item.style.display = 'flex';
                if (item.classList.contains('node')) {
                    item.classList.remove('open');
                    item.querySelector('.node-content').classList.add('hidden');
                }
            } else if (isMatch) {
                item.style.display = 'flex';
                let parentContent = item.closest('.node-content');
                while (parentContent) {
                    parentContent.previousElementSibling.querySelectorAll('span.icon').forEach(span => { span.classList.toggle('hidden') });
                    parentContent.classList.remove('hidden');
                    parentContent.parentElement.classList.add('open');
                    parentContent.parentElement.style.display = 'flex';
                    parentContent = parentContent.parentElement.closest('.node-content');
                }
            } else {
                item.style.display = 'none';
            }
        });
    }

    public updateFixedFilterParameters(params: any) {
        let input = this.controlElement('input[name="fixedFilterParameters"]') as HTMLInputElement;
        if (input) {
            input.value = JSON.stringify(params);
            input.attributes["hx-target"].value = "closest div.tree-root";
            htmx.trigger(input, "changed",);
        }
    }

    private initialise() {
        this.searchEnabled = this.controlElement('.search-container') != null;

        if (this.controlElement('div.select-trigger')) {
            this.controlElement('div.select-trigger').addEventListener("click", (e: MouseEvent) => this.toggleDropdown(e));
        }

        if (this.searchEnabled) {
            this.controlElement('#treeSearch').addEventListener('input', this.debounce((e: InputEvent) => this.search(e)));
            this.controlElement('#resetBtn').addEventListener('click', (e: MouseEvent) => this.reset(e));
        }
        this.selectionLabel = this.controlElement("#selected-label");
        window.addEventListener("click", (e: MouseEvent) => { this.closeDropDown(e) });
        this.invokeEventHandler('Initialised');
    }

    public closeDropDown(event: any) {
        if (!event.target.closest('.custom-select-wrapper')) {
            this.controlElement("#dropdownMenu").classList.remove("show");
        }
    }
}