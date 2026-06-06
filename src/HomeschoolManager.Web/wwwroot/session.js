window.homeschoolSession = {
    key: "homeschool-manager-session",
    save(role, displayName) {
        localStorage.setItem(this.key, JSON.stringify({ role, displayName }));
    },
    load() {
        const value = localStorage.getItem(this.key);
        if (!value) {
            return null;
        }

        try {
            return JSON.parse(value);
        } catch {
            localStorage.removeItem(this.key);
            return null;
        }
    },
    clear() {
        localStorage.removeItem(this.key);
    }
};
