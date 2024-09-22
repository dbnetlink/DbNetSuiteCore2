class DraggableDialog {
    private dialog: HTMLDialogElement;
    private dragHandle: HTMLElement;
    private isDragging: boolean = false;
    private currentX: number = 0;
    private currentY: number = 0;
    private initialX: number = 0;
    private initialY: number = 0;
    private xOffset: number = 0;
    private yOffset: number = 0;

    constructor(dialogId: string, dragHandleClass: string = 'dialog-header') {
        this.dialog = document.getElementById(dialogId) as HTMLDialogElement;
        if (!this.dialog) {
            throw new Error(`Dialog with id "${dialogId}" not found`);
        }

        this.dragHandle = this.dialog.querySelector(`.${dragHandleClass}`) as HTMLElement;
        if (!this.dragHandle) {
            throw new Error(`Drag handle with class "${dragHandleClass}" not found in the dialog`);
        }

        this.initDragEvents();
    }

    private initDragEvents(): void {
        this.dragHandle.addEventListener('mousedown', this.startDragging.bind(this));
        document.addEventListener('mousemove', this.drag.bind(this));
        document.addEventListener('mouseup', this.stopDragging.bind(this));
    }

    private startDragging(e: MouseEvent): void {
        if ((e.target as HTMLElement).closest(`.${this.dragHandle.className}`) === this.dragHandle) {
            this.isDragging = true;
            this.initialX = e.clientX - this.xOffset;
            this.initialY = e.clientY - this.yOffset;
            this.dragHandle.style.cursor = 'grabbing';
            document.body.style.userSelect = 'none'; // Prevent text selection during drag
        }
    }

    private drag(e: MouseEvent): void {
        if (this.isDragging) {
            e.preventDefault();
            this.currentX = e.clientX - this.initialX;
            this.currentY = e.clientY - this.initialY;

            this.xOffset = this.currentX;
            this.yOffset = this.currentY;

            this.setTranslate(this.currentX, this.currentY);
        }
    }

    private stopDragging(): void {
        this.isDragging = false;
        this.dragHandle.style.cursor = '';
        document.body.style.userSelect = ''; // Re-enable text selection

        // Remove mousemove and mouseup listeners
        document.removeEventListener('mousemove', this.drag);
        document.removeEventListener('mouseup', this.stopDragging);

    }

    private setTranslate(xPos: number, yPos: number): void {
        requestAnimationFrame(() => {
            this.dialog.style.transform = `translate3d(${xPos}px, ${yPos}px, 0)`;
        });
    }
}