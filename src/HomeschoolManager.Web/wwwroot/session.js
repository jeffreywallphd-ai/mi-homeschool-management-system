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

window.homeschoolMenus = (() => {
    const cleanupByElement = new WeakMap();

    function disposeDetailsClickAway(details) {
        const cleanup = cleanupByElement.get(details);
        if (cleanup) {
            cleanup();
        }
    }

    function initializeDetailsClickAway(details) {
        if (!details || cleanupByElement.has(details)) {
            return;
        }

        const closeIfDetached = () => {
            if (!document.documentElement.contains(details)) {
                disposeDetailsClickAway(details);
                return true;
            }

            return false;
        };

        const onPointerDown = (event) => {
            if (closeIfDetached()) {
                return;
            }

            if (details.open && !details.contains(event.target)) {
                details.open = false;
            }
        };

        const onKeyDown = (event) => {
            if (closeIfDetached()) {
                return;
            }

            if (event.key === "Escape" && details.open) {
                details.open = false;
                details.querySelector("summary")?.focus();
            }
        };

        const cleanup = () => {
            document.removeEventListener("pointerdown", onPointerDown, true);
            document.removeEventListener("keydown", onKeyDown, true);
            cleanupByElement.delete(details);
        };

        cleanupByElement.set(details, cleanup);
        document.addEventListener("pointerdown", onPointerDown, true);
        document.addEventListener("keydown", onKeyDown, true);
    }

    return {
        initializeDetailsClickAway,
        disposeDetailsClickAway
    };
})();
