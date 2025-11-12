export function formatTimestamp(value: string) {
    try {
        return new Intl.DateTimeFormat(undefined, {
            dateStyle: 'medium',
            timeStyle: 'short',
        }).format(new Date(value));
    } catch {
        return value;
    }
}

export function toInputDate(value: string) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return new Date().toISOString().slice(0, 16);
    }
    const iso = date.toISOString();
    return iso.slice(0, 16);
}
