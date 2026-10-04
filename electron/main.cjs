const { app, BrowserWindow, dialog, ipcMain } = require('electron');
const fs = require('node:fs/promises');
const path = require('node:path');

const isDev = !app.isPackaged;

function createWindow() {
  const window = new BrowserWindow({
    width: 1440,
    height: 920,
    minWidth: 1080,
    minHeight: 700,
    backgroundColor: '#f5f7fa',
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });
  if (isDev) window.loadURL('http://127.0.0.1:5173');
  else window.loadFile(path.join(__dirname, '..', 'dist', 'index.html'));
}

ipcMain.handle('files:open', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile', 'multiSelections'],
    filters: [{ name: '课程表', extensions: ['xls', 'xlsx', 'csv'] }],
  });
  if (result.canceled) return [];
  return Promise.all(result.filePaths.map(async (filePath) => ({
    path: filePath,
    name: path.basename(filePath),
    content: (await fs.readFile(filePath)).toString('base64'),
  })));
});

ipcMain.handle('files:save-text', async (_event, { suggestedName, content }) => {
  const result = await dialog.showSaveDialog({ defaultPath: suggestedName, filters: [{ name: 'iCalendar', extensions: ['ics'] }] });
  if (result.canceled || !result.filePath) return null;
  await fs.writeFile(result.filePath, content, 'utf8');
  return result.filePath;
});

ipcMain.handle('files:save-binary', async (_event, { suggestedName, content }) => {
  const result = await dialog.showSaveDialog({ defaultPath: suggestedName, filters: [{ name: 'Excel 工作簿', extensions: ['xlsx'] }] });
  if (result.canceled || !result.filePath) return null;
  await fs.writeFile(result.filePath, Buffer.from(content, 'base64'));
  return result.filePath;
});

ipcMain.handle('app:version', () => app.getVersion());

app.whenReady().then(() => {
  createWindow();
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});
