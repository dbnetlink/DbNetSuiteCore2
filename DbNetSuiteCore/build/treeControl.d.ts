declare class TreeControl extends ComponentControl {
    tree: HTMLSelectElement;
    treeContainer: HTMLSelectElement;
    searchEnabled: boolean;
    selectionLabel: HTMLSelectElement;
    constructor(selectId: any);
    afterRequest(evt: any): void;
    private toggleDropdown;
    private toggleNode;
    private selectLeaf;
    private selectNode;
    private selectParentNodes;
    private reset;
    private search;
    updateFixedFilterParameters(params: any): void;
    private initialise;
    closeDropDown(event: any): void;
    getLeafElements(): NodeListOf<any>;
    getNodeElements(): NodeListOf<any>;
}
