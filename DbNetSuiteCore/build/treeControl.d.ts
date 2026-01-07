declare class TreeControl extends ComponentControl {
    tree: HTMLSelectElement;
    constructor(selectId: any);
    afterRequest(evt: any): void;
    private toggleDropdown;
    private toggleNode;
    private selectLeaf;
    private selectNode;
    private selectParentNodes;
    private reset;
    private search;
    private initialise;
}
