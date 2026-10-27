export interface MediaDto {
  id: string;
  fileId: string;
  mimeType: string;
  fileSize: number;
  uploadedBy: string;
  createdAt: string;
}

export interface RequestUploadRequest {
  filename: string;
  mimeType: string;
}

export interface RequestUploadResponse {
  uploadUrl: string;
  fileId: string;
  headers?: Record<string, string>;
}
