const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('courseToIcal', {
  openFiles: () => ipcRenderer.invoke('files:open'),
  openImageFiles: () => ipcRenderer.invoke('files:open-images'),
  saveText: (request) => ipcRenderer.invoke('files:save-text', request),
  saveBinary: (request) => ipcRenderer.invoke('files:save-binary', request),
  getAppVersion: () => ipcRenderer.invoke('app:version'),
});
