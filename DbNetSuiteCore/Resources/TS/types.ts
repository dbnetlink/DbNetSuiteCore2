export interface Dictionary<T> {
    [Key: string]: T;
}

export interface RowModification {
    modified: boolean;
    columns: Array<string>;
}

export interface Selection {
    value: string;
    description: string;
    parentValues: Array<string>;
    parentDescriptions: Array<string>;
}
