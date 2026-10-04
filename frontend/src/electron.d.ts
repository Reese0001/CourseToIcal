export interface OpenedCourseFile {
  path: string;
  name: string;
  content: string;
}

export interface CourseToIcalBridge {
  openFiles: () => Promise<OpenedCourseFile[]>;
  saveText: (request: { suggestedName: string; content: string }) => Promise<string | null>;
  saveBinary: (request: { suggestedName: string; content: string }) => Promise<string | null>;
  getAppVersion: () => Promise<string>;
}

declare global {
  interface Window {
    courseToIcal?: CourseToIcalBridge;
  }
}

export {};
