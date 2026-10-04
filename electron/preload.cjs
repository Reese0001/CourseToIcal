const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('courseToIcal', {
  openFiles: () => ipcRenderer.invoke('files:open'),
  saveText: (request) => ipcRenderer.invoke('files:save-text', request),
  saveBinary: (request) => ipcRenderer.invoke('files:save-binary', request),
  getAppVersion: () => ipcRenderer.invoke('app:version'),
});
