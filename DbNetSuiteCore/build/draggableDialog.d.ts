declare class DraggableDialog {
    private dialog;
    private dragHandle;
    private isDragging;
    private initialX;
    private initialY;
    private xOffset;
    private yOffset;
    constructor(dialogId: string, dragHandleClass?: string);
    private initDragEvents;
    private startDragging;
    private drag;
    private stopDragging;
    private setTranslate;
}
