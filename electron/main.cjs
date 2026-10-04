const { app, BrowserWindow, dialog, ipcMain } = require('electron');
const fs = require('node:fs/promises');
const path = require('node:path');
const { createWorker } = require('tesseract.js');

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

ipcMain.handle('files:open-images', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile', 'multiSelections'],
    filters: [{ name: '课程表图片', extensions: ['png', 'jpg', 'jpeg', 'bmp', 'webp'] }],
  });
  if (result.canceled) return [];

  const dataPackagePath = require.resolve('@tesseract.js-data/chi_sim');
  const tesseractPackagePath = path.dirname(require.resolve('tesseract.js'));
  const corePackagePath = require.resolve('tesseract.js-core', { paths: [tesseractPackagePath] });
  const worker = await createWorker('chi_sim', 1, {
    corePath: path.dirname(corePackagePath),
    langPath: path.join(path.dirname(dataPackagePath), '4.0.0'),
    gzip: true,
    cacheMethod: 'none',
  });
  try {
    const files = [];
    for (const filePath of result.filePaths) {
      const { data } = await worker.recognize(filePath, {}, { text: true, tsv: true });
      files.push({ path: filePath, name: path.basename(filePath), text: data.text, tsv: data.tsv || undefined });
    }
    return files;
  } finally {
    await worker.terminate();
  }
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
