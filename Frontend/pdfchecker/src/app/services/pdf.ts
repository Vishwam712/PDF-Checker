import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PdfService {
  private apiUrl = 'http://localhost:5195/api';

  constructor(private http: HttpClient) { }

  // --- PDF ---
  uploadPdf(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/pdf/upload`, formData);
  }

  checkPdf(fileId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/pdf/check/${fileId}`);
  }

  // --- Validations ---
  getValidations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/validation`);
  }
}
