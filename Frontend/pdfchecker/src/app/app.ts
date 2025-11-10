import { Component, signal } from '@angular/core';
// import { RouterOutlet } from '@angular/router';
import { PdfCheckerComponent } from './components/pdf-checker/pdf-checker';

@Component({
  selector: 'app-root',
  imports: [PdfCheckerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('pdfchecker');
}
