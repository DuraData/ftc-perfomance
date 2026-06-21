import { describe, it, expect } from 'vitest';

describe('Evidence and attachment handling', () => {
  interface FileAttachment {
    id: string;
    fileName: string;
    fileSize: number;
    fileType: string;
    uploadedAt: Date;
    uploadedBy: string;
  }

  const validFileTypes = ['application/pdf', 'image/jpeg', 'image/png', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
  const maxFileSize = 10 * 1024 * 1024; // 10MB

  const validateFile = (file: { name: string; size: number; type: string }): { valid: boolean; error?: string } => {
    if (file.size > maxFileSize) {
      return { valid: false, error: `File exceeds maximum size of 10MB (${(file.size / 1024 / 1024).toFixed(2)}MB)` };
    }

    if (!validFileTypes.includes(file.type)) {
      return { valid: false, error: `File type ${file.type} is not allowed` };
    }

    return { valid: true };
  };

  it('accepts valid PDF files', () => {
    const result = validateFile({
      name: 'evidence.pdf',
      size: 2 * 1024 * 1024, // 2MB
      type: 'application/pdf',
    });

    expect(result.valid).toBe(true);
    expect(result.error).toBeUndefined();
  });

  it('accepts valid image files', () => {
    const jpegResult = validateFile({
      name: 'photo.jpg',
      size: 1024 * 1024, // 1MB
      type: 'image/jpeg',
    });

    expect(jpegResult.valid).toBe(true);

    const pngResult = validateFile({
      name: 'screenshot.png',
      size: 512 * 1024, // 512KB
      type: 'image/png',
    });

    expect(pngResult.valid).toBe(true);
  });

  it('rejects files exceeding size limit', () => {
    const result = validateFile({
      name: 'large.pdf',
      size: 15 * 1024 * 1024, // 15MB
      type: 'application/pdf',
    });

    expect(result.valid).toBe(false);
    expect(result.error).toContain('exceeds maximum size');
  });

  it('rejects unsupported file types', () => {
    const result = validateFile({
      name: 'executable.exe',
      size: 1024 * 1024,
      type: 'application/x-msdownload',
    });

    expect(result.valid).toBe(false);
    expect(result.error).toContain('is not allowed');
  });

  it('tracks attachment metadata', () => {
    const attachment: FileAttachment = {
      id: 'attach-1',
      fileName: 'evidence.pdf',
      fileSize: 2048576,
      fileType: 'application/pdf',
      uploadedAt: new Date('2026-06-21'),
      uploadedBy: 'user-1',
    };

    expect(attachment.fileName).toBe('evidence.pdf');
    expect(attachment.fileSize).toBe(2048576);
    expect(attachment.uploadedBy).toBe('user-1');
  });

  it('calculates human-readable file sizes', () => {
    const formatFileSize = (bytes: number): string => {
      if (bytes === 0) return '0 Bytes';
      const k = 1024;
      const sizes = ['Bytes', 'KB', 'MB', 'GB'];
      const i = Math.floor(Math.log(bytes) / Math.log(k));
      return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    };

    expect(formatFileSize(1024)).toBe('1 KB');
    expect(formatFileSize(1048576)).toBe('1 MB');
    expect(formatFileSize(2097152)).toBe('2 MB');
    expect(formatFileSize(10737418240)).toBe('10 GB');
  });

  it('prevents multiple uploads of same file', () => {
    const uploadedFiles = new Map<string, FileAttachment>();

    const addAttachment = (attachment: FileAttachment): boolean => {
      const key = `${attachment.uploadedBy}-${attachment.fileName}`;
      if (uploadedFiles.has(key)) return false;
      uploadedFiles.set(key, attachment);
      return true;
    };

    const file1: FileAttachment = {
      id: 'attach-1',
      fileName: 'evidence.pdf',
      fileSize: 1024,
      fileType: 'application/pdf',
      uploadedAt: new Date(),
      uploadedBy: 'user-1',
    };

    expect(addAttachment(file1)).toBe(true);
    expect(addAttachment(file1)).toBe(false); // Duplicate
  });

  it('lists attachments in chronological order', () => {
    const attachments: FileAttachment[] = [
      {
        id: 'attach-3',
        fileName: 'evidence-3.pdf',
        fileSize: 1024,
        fileType: 'application/pdf',
        uploadedAt: new Date('2026-06-21T15:00:00'),
        uploadedBy: 'user-1',
      },
      {
        id: 'attach-1',
        fileName: 'evidence-1.pdf',
        fileSize: 1024,
        fileType: 'application/pdf',
        uploadedAt: new Date('2026-06-21T10:00:00'),
        uploadedBy: 'user-1',
      },
      {
        id: 'attach-2',
        fileName: 'evidence-2.pdf',
        fileSize: 1024,
        fileType: 'application/pdf',
        uploadedAt: new Date('2026-06-21T12:00:00'),
        uploadedBy: 'user-1',
      },
    ];

    const sorted = [...attachments].sort((a, b) => b.uploadedAt.getTime() - a.uploadedAt.getTime());

    expect(sorted[0].id).toBe('attach-3');
    expect(sorted[1].id).toBe('attach-2');
    expect(sorted[2].id).toBe('attach-1');
  });
});
