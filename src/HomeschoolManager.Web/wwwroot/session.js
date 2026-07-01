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

window.homeschoolFolderBackups = (() => {
    let selectedDirectoryHandle = null;

    async function base64ToBlob(contentType, base64Content) {
        const response = await fetch(`data:${contentType};base64,${base64Content}`);
        return await response.blob();
    }

    async function hasWritePermission(handle) {
        const options = { mode: "readwrite" };
        if ((await handle.queryPermission(options)) === "granted") {
            return true;
        }

        return (await handle.requestPermission(options)) === "granted";
    }

    return {
        async chooseDirectory() {
            if (!("showDirectoryPicker" in window)) {
                return {
                    succeeded: false,
                    error: "This browser does not support choosing a folder directly. Use the download backup button instead."
                };
            }

            try {
                selectedDirectoryHandle = await window.showDirectoryPicker({ mode: "readwrite" });
                if (!(await hasWritePermission(selectedDirectoryHandle))) {
                    selectedDirectoryHandle = null;
                    return {
                        succeeded: false,
                        error: "Homeschool Manager needs permission to save files in that folder."
                    };
                }

                return {
                    succeeded: true,
                    directoryName: selectedDirectoryHandle.name || "Selected folder"
                };
            } catch (error) {
                if (error?.name === "AbortError") {
                    return {
                        succeeded: false,
                        canceled: true,
                        error: "Folder selection canceled."
                    };
                }

                return {
                    succeeded: false,
                    error: error?.message || "The folder could not be selected."
                };
            }
        },

        async saveBase64File(fileName, contentType, base64Content) {
            if (!selectedDirectoryHandle) {
                return {
                    succeeded: false,
                    error: "Choose a synced backup folder first."
                };
            }

            try {
                if (!(await hasWritePermission(selectedDirectoryHandle))) {
                    return {
                        succeeded: false,
                        error: "Homeschool Manager no longer has permission to save files in that folder. Choose the folder again."
                    };
                }

                const fileHandle = await selectedDirectoryHandle.getFileHandle(fileName, { create: true });
                const writable = await fileHandle.createWritable();
                await writable.write(await base64ToBlob(contentType, base64Content));
                await writable.close();

                return {
                    succeeded: true,
                    fileName,
                    directoryName: selectedDirectoryHandle.name || "selected folder"
                };
            } catch (error) {
                return {
                    succeeded: false,
                    error: error?.message || "The backup file could not be saved to that folder."
                };
            }
        }
    };
})();

window.homeschoolScroll = {
    intoViewById(id) {
        document.getElementById(id)?.scrollIntoView({ block: "start" });
    }
};

window.homeschoolFonts = {
    async availableFamilies() {
        const fallback = [
            "Arial",
            "Baskerville",
            "Calibri",
            "Cambria",
            "Courier New",
            "Garamond",
            "Georgia",
            "Palatino Linotype",
            "Segoe UI",
            "Times New Roman",
            "Verdana"
        ];

        if (!("queryLocalFonts" in window)) {
            return fallback;
        }

        try {
            const fonts = await window.queryLocalFonts();
            const families = [...new Set(fonts.map(font => font.family).filter(Boolean))].sort((a, b) => a.localeCompare(b));
            return families.length ? families : fallback;
        } catch {
            return fallback;
        }
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
