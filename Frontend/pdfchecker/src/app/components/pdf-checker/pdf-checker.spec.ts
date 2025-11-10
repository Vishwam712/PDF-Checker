import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PdfChecker } from './pdf-checker';

describe('PdfChecker', () => {
  let component: PdfChecker;
  let fixture: ComponentFixture<PdfChecker>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PdfChecker]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PdfChecker);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
