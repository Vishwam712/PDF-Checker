import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PdfService } from '../../services/pdf';

interface ValidationError {
  page?: number | string;
  text?: string;
  message: string;
  rect?: any;
}

@Component({
  selector: 'app-pdf-checker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pdf-checker.html',
  styleUrls: ['./pdf-checker.css']
})
export class PdfCheckerComponent implements OnInit {
  // PDF state
  selectedFile: File | null = null;
  fileId: number | null = null;
  downloadUrl: string | null = null;
  successMessage: string | null = null;
  errors: ValidationError[] = [];

  // Validations state
  validations: any[] = [];

  // UI state
  uploading = false;
  checking = false;

  constructor(private pdfService: PdfService) {}

  ngOnInit(): void {
    this.loadValidations();
  }

  // ------------------ PDF Actions ------------------

  onFileSelected(event: any) {
    this.selectedFile = event.target.files?.[0] ?? null;
  }

  uploadFile() {
    if (!this.selectedFile) return;
    this.uploading = true;
    this.pdfService.uploadPdf(this.selectedFile).subscribe({
      next: (res) => {
        this.fileId = res.fileId;
        this.successMessage = 'File uploaded successfully!';
        this.errors = [];
        this.downloadUrl = null;
      },
      error: (err) => {
        console.error(err);
        alert('Upload failed.');
      },
      complete: () => (this.uploading = false)
    });
  }

  checkFile() {
    if (!this.fileId) {
      alert('Please upload a file first.');
      return;
    }
    this.checking = true;
    this.pdfService.checkPdf(this.fileId).subscribe({
      next: (res) => {
        const raw = res?.errors ?? [];
        // Support both string[] and object[] from backend
        this.errors = Array.isArray(raw)
          ? (typeof raw[0] === 'string'
              ? (raw as string[]).map((m) => ({ message: m }))
              : raw)
          : [];

        this.downloadUrl = res?.downloadUrl ?? null;

        if (!this.errors.length) {
          this.successMessage = res?.message || 'PDF passed all validation rules ✅';
        } else {
          this.successMessage = null;
        }
      },
      error: (err) => {
        console.error(err);
        alert('Check failed.');
      },
      complete: () => (this.checking = false)
    });
  }

  // ------------------ Validation Load ------------------

  loadValidations() {
    this.pdfService.getValidations().subscribe({
      next: (res) => (this.validations = res || []),
      error: (err) => console.error(err)
    });
  }
}
