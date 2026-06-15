import React, { useState, useRef, useCallback } from 'react';
import { Upload, File, X, Download, Eye, Image, FileText, FileSpreadsheet, Loader2 } from 'lucide-react';
import { Button } from '../ui';

interface UploadedFile {
  id: string;
  name: string;
  size: number;
  type: string;
  progress?: number;
  uploadedAt?: string;
  uploadedBy?: string;
  documentType?: string;
  url?: string;
}

interface FileUploadProps {
  onUpload?: (files: File[]) => void;
  onRemove?: (fileId: string) => void;
  existingFiles?: UploadedFile[];
  maxFiles?: number;
  maxSize?: number; // in MB
  acceptedTypes?: string[];
  showDocumentType?: boolean;
  documentTypes?: { value: string; label: string }[];
  previewable?: boolean;
  disabled?: boolean;
}

const defaultDocumentTypes = [
  { value: 'evidence', label: 'Evidence Document' },
  { value: 'report', label: 'Report' },
  { value: 'photo', label: 'Photograph' },
  { value: 'invoice', label: 'Invoice' },
  { value: 'contract', label: 'Contract' },
  { value: 'certificate', label: 'Certificate' },
  { value: 'other', label: 'Other' },
];

function getFileIcon(type: string) {
  if (type.startsWith('image/')) return <Image className="w-8 h-8 text-blue-500" />;
  if (type.includes('spreadsheet') || type.includes('excel')) return <FileSpreadsheet className="w-8 h-8 text-green-500" />;
  if (type.includes('pdf') || type.includes('document')) return <FileText className="w-8 h-8 text-red-500" />;
  return <File className="w-8 h-8 text-secondary-400" />;
}

function formatFileSize(bytes: number) {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

export function FileUpload({
  onUpload,
  onRemove,
  existingFiles = [],
  maxFiles = 10,
  maxSize = 10,
  acceptedTypes,
  showDocumentType = true,
  documentTypes = defaultDocumentTypes,
  previewable = true,
  disabled = false,
}: FileUploadProps) {
  const [isDragging, setIsDragging] = useState(false);
  const [files, setFiles] = useState<UploadedFile[]>(existingFiles);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!disabled) setIsDragging(true);
  }, [disabled]);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const processFiles = useCallback((newFiles: FileList | File[]) => {
    const fileArray = Array.from(newFiles);

    fileArray.forEach(file => {
      if (files.length + fileArray.length > maxFiles) {
        alert(`Maximum ${maxFiles} files allowed`);
        return;
      }

      if (file.size > maxSize * 1024 * 1024) {
        alert(`File ${file.name} exceeds maximum size of ${maxSize}MB`);
        return;
      }

      if (acceptedTypes && !acceptedTypes.some(type => file.type.match(type))) {
        alert(`File type ${file.type} not allowed`);
        return;
      }

      const uploadedFile: UploadedFile = {
        id: `temp-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        name: file.name,
        size: file.size,
        type: file.type,
        progress: 0,
      };

      setFiles(prev => [...prev, uploadedFile]);

      // Simulate upload progress
      let progress = 0;
      const interval = setInterval(() => {
        progress += Math.random() * 30;
        if (progress >= 100) {
          progress = 100;
          clearInterval(interval);
          setFiles(prev =>
            prev.map(f =>
              f.id === uploadedFile.id
                ? { ...f, progress: 100, uploadedAt: new Date().toISOString() }
                : f
            )
          );
        } else {
          setUploadProgress(prev => ({
            ...prev,
            [uploadedFile.id]: progress,
          }));
        }
      }, 200);
    });

    onUpload?.(fileArray);
  }, [files.length, maxFiles, maxSize, acceptedTypes, onUpload]);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    if (disabled) return;
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      processFiles(e.dataTransfer.files);
    }
  }, [disabled, processFiles]);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      processFiles(e.target.files);
    }
  };

  const handleRemove = (fileId: string) => {
    setFiles(prev => prev.filter(f => f.id !== fileId));
    onRemove?.(fileId);
  };

  const handleDocumentTypeChange = (fileId: string, documentType: string) => {
    setFiles(prev =>
      prev.map(f => (f.id === fileId ? { ...f, documentType } : f))
    );
  };

  return (
    <div className="space-y-4">
      {/* Drop zone */}
      <div
        onDragEnter={handleDragEnter}
        onDragLeave={handleDragLeave}
        onDragOver={handleDragOver}
        onDrop={handleDrop}
        className={`relative border-2 border-dashed rounded-xl p-8 text-center transition-all ${
          isDragging
            ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
            : 'border-secondary-300 dark:border-secondary-600 hover:border-primary-400 hover:bg-secondary-50 dark:hover:bg-secondary-800'
        } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
      >
        <input
          ref={fileInputRef}
          type="file"
          multiple
          onChange={handleFileSelect}
          disabled={disabled}
          className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
          accept={acceptedTypes?.join(',')}
        />
        <div className="space-y-3">
          <div className="flex justify-center">
            <div className={`p-4 rounded-full ${isDragging ? 'bg-primary-100 dark:bg-primary-900' : 'bg-secondary-100 dark:bg-secondary-800'}`}>
              <Upload className={`w-6 h-6 ${isDragging ? 'text-primary-600' : 'text-secondary-400'}`} />
            </div>
          </div>
          <div>
            <p className="text-sm font-medium text-secondary-700 dark:text-secondary-300">
              Drop files here or{' '}
              <span className="text-primary-600 dark:text-primary-400">browse</span>
            </p>
            <p className="text-xs text-secondary-500 dark:text-secondary-400 mt-1">
              Max {maxFiles} files, up to {maxSize}MB each
            </p>
          </div>
        </div>
      </div>

      {/* File list */}
      {files.length > 0 && (
        <div className="space-y-2">
          {files.map(file => (
            <div
              key={file.id}
              className="flex items-start gap-4 p-3 bg-white dark:bg-secondary-800 rounded-lg border border-secondary-200 dark:border-secondary-700"
            >
              {/* File icon */}
              <div className="flex-shrink-0 mt-1">
                {getFileIcon(file.type)}
              </div>

              {/* File info */}
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-secondary-900 dark:text-white truncate">
                  {file.name}
                </p>
                <p className="text-xs text-secondary-500 dark:text-secondary-400">
                  {formatFileSize(file.size)}
                  {file.uploadedAt && ` • Uploaded ${new Date(file.uploadedAt).toLocaleString()}`}
                </p>

                {/* Progress bar */}
                {(file.progress ?? uploadProgress[file.id]) !== undefined &&
                  (file.progress ?? uploadProgress[file.id]) < 100 && (
                  <div className="mt-2">
                    <div className="h-1 bg-secondary-200 dark:bg-secondary-700 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-primary-600 rounded-full transition-all duration-300"
                        style={{ width: `${file.progress ?? uploadProgress[file.id] ?? 0}%` }}
                      />
                    </div>
                    <p className="text-xs text-secondary-500 mt-1">
                      Uploading... {Math.round(file.progress ?? uploadProgress[file.id] ?? 0)}%
                    </p>
                  </div>
                )}

                {/* Document type selector */}
                {showDocumentType && (file.progress ?? 100) >= 100 && (
                  <div className="mt-2">
                    <select
                      value={file.documentType || ''}
                      onChange={(e) => handleDocumentTypeChange(file.id, e.target.value)}
                      className="text-xs py-1 px-2 border border-secondary-200 dark:border-secondary-600 rounded bg-white dark:bg-secondary-700 text-secondary-700 dark:text-secondary-300"
                    >
                      <option value="">Select document type...</option>
                      {documentTypes.map(dt => (
                        <option key={dt.value} value={dt.value}>
                          {dt.label}
                        </option>
                      ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex items-center gap-2">
                {(file.progress ?? 100) >= 100 && (
                  <>
                    {previewable && file.type.startsWith('image/') && (
                      <button className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors">
                        <Eye className="w-4 h-4 text-secondary-400" />
                      </button>
                    )}
                    {file.url && (
                      <button className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors">
                        <Download className="w-4 h-4 text-secondary-400" />
                      </button>
                    )}
                  </>
                )}
                <button
                  onClick={() => handleRemove(file.id)}
                  className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20 transition-colors"
                >
                  <X className="w-4 h-4 text-error-500" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
