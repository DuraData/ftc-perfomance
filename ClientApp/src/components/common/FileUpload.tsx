import React, { useState, useRef, useCallback } from 'react';
import { Upload, File, X, Download, Eye, Image, FileText, FileSpreadsheet, RefreshCw } from 'lucide-react';

interface UploadedFile {
  id: string;
  publicId?: string;
  evidenceBlobPublicId?: string;
  name: string;
  size: number;
  type: string;
  progress?: number;
  uploadedAt?: string;
  uploadedBy?: string;
  documentType?: string;
  url?: string;
  scanStatus?: string;
  isQuarantined?: boolean;
  scanDetail?: string;
  assessments?: { publicId: string; outcome: 'Accepted' | 'Rejected' | 'NeedsClarification'; comment?: string | null; assessedByName?: string | null; assessedByUserId: string; assessedAt: string; correlationId: string }[];
  rowVersion?: string;
  replacementOf?: { publicId: string; supersededEvidencePublicId: string; supersededFileName: string; replacementEvidencePublicId: string; replacementFileName: string; reason: string; replacedByUserId: string; replacedByName?: string | null; replacedAt: string; correlationId: string } | null;
  legalHolds?: { holdId: string; holdReference: string; isActive: boolean; placedReason: string; placedByUserId: string; placedByName?: string | null; placedAt: string; releasedReason?: string | null; releasedByName?: string | null; releasedAt?: string | null }[];
  isActive?: boolean;
  retainUntil?: string | null;
  disposals?: { disposalId: string; status: 'Pending' | 'Completed' | 'Failed'; approvalReference: string; reason: string; requestedByUserId: string; requestedByName?: string | null; requestedAt: string; completedAt?: string | null; failedAt?: string | null; detail?: string | null }[];
  isContentDeleted?: boolean;
}

interface FileUploadProps {
  onUpload?: (files: File[]) => void;
  onRemove?: (fileId: string) => void;
  onRescan?: (fileId: string) => void;
  onAssess?: (fileId: string, outcome: 1 | 2 | 3, comment?: string) => void;
  onReplace?: (fileId: string, replacementPublicId: string, reason: string, supersededRowVersion: string, replacementRowVersion: string) => void;
  onPlaceHold?: (fileId: string, holdReference: string, reason: string) => void;
  onReleaseHold?: (fileId: string, holdId: string, reason: string) => void;
  onDispose?: (fileId: string, approvalReference: string, reason: string, rowVersion: string) => void;
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
  onRescan,
  onAssess,
  onReplace,
  onPlaceHold,
  onReleaseHold,
  onDispose,
  existingFiles = [],
  maxFiles,
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
  const [assessmentOutcomes, setAssessmentOutcomes] = useState<Record<string, 1 | 2 | 3>>({});
  const [assessmentComments, setAssessmentComments] = useState<Record<string, string>>({});
  const [replacementIds, setReplacementIds] = useState<Record<string, string>>({});
  const [replacementReasons, setReplacementReasons] = useState<Record<string, string>>({});
  const [holdReferences, setHoldReferences] = useState<Record<string, string>>({});
  const [holdReasons, setHoldReasons] = useState<Record<string, string>>({});
  const [releaseReasons, setReleaseReasons] = useState<Record<string, string>>({});
  const [disposalApprovals, setDisposalApprovals] = useState<Record<string, string>>({});
  const [disposalReasons, setDisposalReasons] = useState<Record<string, string>>({});
  const fileInputRef = useRef<HTMLInputElement>(null);

  React.useEffect(() => {
    setFiles(existingFiles);
  }, [existingFiles]);

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
    const currentCount = files.length;

    if (maxFiles !== undefined && currentCount + fileArray.length > maxFiles) {
      alert(`Maximum ${maxFiles} files allowed`);
      return;
    }

    fileArray.forEach(file => {
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
              {maxFiles === undefined ? 'Unlimited files' : `Max ${maxFiles} files`}, up to {maxSize}MB each
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
                  {file.uploadedBy && ` • by ${file.uploadedBy}`}
                </p>
                {file.scanStatus && <p className={`mt-1 text-xs font-medium ${file.isQuarantined ? 'text-warning-700 dark:text-warning-300' : 'text-success-700 dark:text-success-300'}`}>Malware scan: {file.scanStatus}{file.scanDetail ? ` · ${file.scanDetail}` : ''}</p>}
                {file.isActive === false && <p className="mt-1 text-xs font-medium text-secondary-600 dark:text-secondary-300">Retired evidence · retained until {file.retainUntil ? new Date(file.retainUntil).toLocaleDateString() : 'policy date unavailable'}</p>}
                {file.isContentDeleted && <p className="mt-1 text-xs font-medium text-error-700 dark:text-error-300">Physical content disposed; metadata and provenance retained.</p>}
                {file.assessments?.map(assessment => <p key={assessment.publicId} className="mt-1 text-xs text-secondary-600 dark:text-secondary-300"><strong>{assessment.outcome}</strong> by {assessment.assessedByName ?? assessment.assessedByUserId} · {new Date(assessment.assessedAt).toLocaleString()}{assessment.comment ? ` · ${assessment.comment}` : ''}</p>)}
                {file.replacementOf && <p className="mt-1 text-xs text-primary-700 dark:text-primary-300">Replaces {file.replacementOf.supersededFileName} · {file.replacementOf.reason} · {new Date(file.replacementOf.replacedAt).toLocaleString()}</p>}
                {file.legalHolds?.map(hold => <div key={hold.holdId} className={`mt-1 rounded border p-2 text-xs ${hold.isActive ? 'border-warning-300 bg-warning-50 text-warning-800' : 'border-secondary-200 text-secondary-600'}`}><strong>{hold.isActive ? 'Active legal hold' : 'Released legal hold'} · {hold.holdReference}</strong><span> · {hold.placedReason} · {hold.placedByName ?? hold.placedByUserId} · {new Date(hold.placedAt).toLocaleString()}</span>{hold.releasedAt && <span> · released {new Date(hold.releasedAt).toLocaleString()}{hold.releasedReason ? ` · ${hold.releasedReason}` : ''}</span>}{hold.isActive && onReleaseHold && <div className="mt-2 flex gap-2"><input aria-label={`Legal hold release reason for ${file.name} ${hold.holdReference}`} value={releaseReasons[hold.holdId] ?? ''} onChange={event => setReleaseReasons(current => ({ ...current, [hold.holdId]: event.target.value }))} placeholder="Release reason" className="min-w-48 rounded border border-secondary-300 px-2 py-1 text-xs" /><button type="button" disabled={(releaseReasons[hold.holdId]?.trim().length ?? 0) < 5} onClick={() => onReleaseHold(file.id, hold.holdId, releaseReasons[hold.holdId].trim())} className="rounded bg-secondary-700 px-2 py-1 text-white disabled:opacity-50">Release hold</button></div>}</div>)}
                {file.disposals?.map(disposal => <p key={disposal.disposalId} className={`mt-1 text-xs ${disposal.status === 'Failed' ? 'text-error-700' : 'text-secondary-600 dark:text-secondary-300'}`}><strong>Disposal {disposal.status.toLowerCase()}</strong> · {disposal.approvalReference} · {disposal.reason} · {disposal.requestedByName ?? disposal.requestedByUserId} · {new Date(disposal.requestedAt).toLocaleString()}{disposal.detail ? ` · ${disposal.detail}` : ''}</p>)}
                {onDispose && file.isActive === false && file.rowVersion && file.retainUntil && new Date(file.retainUntil) <= new Date() && !file.legalHolds?.some(hold => hold.isActive) && !file.disposals?.some(disposal => disposal.status !== 'Failed') && <div className="mt-2 flex flex-wrap gap-2"><input aria-label={`Disposal approval reference for ${file.name}`} value={disposalApprovals[file.id] ?? ''} onChange={event => setDisposalApprovals(current => ({ ...current, [file.id]: event.target.value }))} placeholder="Approval reference" className="min-w-40 rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700" /><input aria-label={`Disposal reason for ${file.name}`} value={disposalReasons[file.id] ?? ''} onChange={event => setDisposalReasons(current => ({ ...current, [file.id]: event.target.value }))} placeholder="Disposal reason" className="min-w-48 rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700" /><button type="button" aria-label={`Request disposal for ${file.name}`} disabled={(disposalApprovals[file.id]?.trim().length ?? 0) < 3 || (disposalReasons[file.id]?.trim().length ?? 0) < 5} onClick={() => onDispose(file.id, disposalApprovals[file.id].trim(), disposalReasons[file.id].trim(), file.rowVersion!)} className="rounded bg-error-700 px-2 py-1 text-xs font-medium text-white disabled:opacity-50">Request disposal</button></div>}
                {onPlaceHold && !file.disposals?.some(disposal => disposal.status === 'Completed') && <div className="mt-2 flex flex-wrap gap-2"><input aria-label={`Legal hold reference for ${file.name}`} value={holdReferences[file.id] ?? ''} onChange={event => setHoldReferences(current => ({ ...current, [file.id]: event.target.value }))} placeholder="Hold reference" className="min-w-40 rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700" /><input aria-label={`Legal hold reason for ${file.name}`} value={holdReasons[file.id] ?? ''} onChange={event => setHoldReasons(current => ({ ...current, [file.id]: event.target.value }))} placeholder="Hold reason" className="min-w-48 rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700" /><button type="button" aria-label={`Place legal hold on ${file.name}`} disabled={(holdReferences[file.id]?.trim().length ?? 0) < 3 || (holdReasons[file.id]?.trim().length ?? 0) < 5} onClick={() => onPlaceHold(file.id, holdReferences[file.id].trim(), holdReasons[file.id].trim())} className="rounded bg-warning-700 px-2 py-1 text-xs font-medium text-white disabled:opacity-50">Place legal hold</button></div>}
                {onAssess && file.isActive !== false && file.scanStatus === 'Clean' && !file.isQuarantined && <div className="mt-2 flex flex-wrap gap-2"><select aria-label={`Assessment outcome for ${file.name}`} value={assessmentOutcomes[file.id] ?? 1} onChange={event => setAssessmentOutcomes(current => ({ ...current, [file.id]: Number(event.target.value) as 1 | 2 | 3 }))} className="rounded border border-secondary-300 bg-white px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700"><option value={1}>Accepted</option><option value={2}>Rejected</option><option value={3}>Needs clarification</option></select><input aria-label={`Assessment comment for ${file.name}`} value={assessmentComments[file.id] ?? ''} onChange={event => setAssessmentComments(current => ({ ...current, [file.id]: event.target.value }))} placeholder="Assessment comment" className="min-w-48 rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700" /><button type="button" onClick={() => onAssess(file.id, assessmentOutcomes[file.id] ?? 1, assessmentComments[file.id]?.trim() || undefined)} className="rounded bg-primary-600 px-2 py-1 text-xs font-medium text-white hover:bg-primary-700">Record assessment</button></div>}
                {onReplace && file.isActive !== false && file.rowVersion && files.some(candidate => candidate.id !== file.id && candidate.isActive !== false && candidate.publicId && candidate.rowVersion && candidate.scanStatus === 'Clean' && !candidate.isQuarantined && !candidate.replacementOf) && <div className="mt-2 flex flex-wrap gap-2">
                  <select aria-label={`Replacement evidence for ${file.name}`} value={replacementIds[file.id] ?? ''} onChange={event => setReplacementIds(current => ({ ...current, [file.id]: event.target.value }))} className="rounded border border-secondary-300 bg-white px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700"><option value="">Select uploaded replacement…</option>{files.filter(candidate => candidate.id !== file.id && candidate.isActive !== false && candidate.publicId && candidate.rowVersion && candidate.scanStatus === 'Clean' && !candidate.isQuarantined && !candidate.replacementOf).map(candidate => <option key={candidate.publicId} value={candidate.publicId}>{candidate.name}</option>)}</select>
                  <input aria-label={`Replacement reason for ${file.name}`} value={replacementReasons[file.id] ?? ''} onChange={event => setReplacementReasons(current => ({ ...current, [file.id]: event.target.value }))} placeholder="Replacement reason" className="min-w-48 rounded border border-secondary-300 px-2 py-1 text-xs dark:border-secondary-600 dark:bg-secondary-700" />
                  <button type="button" aria-label={`Record replacement for ${file.name}`} disabled={!replacementIds[file.id] || (replacementReasons[file.id]?.trim().length ?? 0) < 5} onClick={() => { const candidate = files.find(item => item.publicId === replacementIds[file.id]); if (candidate?.publicId && candidate.rowVersion) onReplace(file.id, candidate.publicId, replacementReasons[file.id].trim(), file.rowVersion!, candidate.rowVersion); }} className="rounded bg-warning-600 px-2 py-1 text-xs font-medium text-white hover:bg-warning-700 disabled:opacity-50">Record replacement</button>
                </div>}

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
                    {file.isActive !== false && file.isQuarantined && onRescan && <button type="button" title="Rescan quarantined evidence" onClick={() => onRescan(file.id)} className="p-1.5 rounded-lg hover:bg-secondary-100 dark:hover:bg-secondary-700 transition-colors"><RefreshCw className="w-4 h-4 text-warning-600" /></button>}
                  </>
                )}
                {file.isActive !== false && <button
                  onClick={() => handleRemove(file.id)}
                  className="p-1.5 rounded-lg hover:bg-error-50 dark:hover:bg-error-900/20 transition-colors"
                >
                  <X className="w-4 h-4 text-error-500" />
                </button>}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
