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

window.homeschoolFiles = {
    downloadBase64(fileName, contentType, base64Content) {
        const link = document.createElement("a");
        link.download = fileName;
        link.href = `data:${contentType};base64,${base64Content}`;
        document.body.appendChild(link);
        link.click();
        link.remove();
    }
};

window.homeschoolScroll = {
    intoViewById(id) {
        document.getElementById(id)?.scrollIntoView({ block: "start" });
    }
};
