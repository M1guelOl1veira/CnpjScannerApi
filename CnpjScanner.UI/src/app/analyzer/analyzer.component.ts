import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Component, effect, HostListener, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Pagination } from '../shared/models/pagination';
import { RepoInfo } from '../shared/models/repoInfo';
import * as XLSX from 'xlsx';
import * as FileSaver from 'file-saver';

@Component({
  selector: 'app-analyzer',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbPaginationModule],
  templateUrl: './analyzer.component.html',
  styleUrls: ['./analyzer.component.css'],
})
export class AnalyzerComponent {
  private http = inject(HttpClient);
  repo = '';
  dirToClone = '';
  languageOptions = [
    { label: 'C#', value: 'cs' },
    { label: 'VB.NET', value: 'vb' },
    { label: 'TypeScript', value: 'ts' },
  ];
  selectedLanguages: string[] = [];
  results: RepoInfo[] = [];
  page = 1;
  pageSize = 15;
  analyzed = false;
  exportSaved = false;
  existingWorkbook: XLSX.WorkBook | null = null;
  selectedRows: RepoInfo[] = [];
  repoName = '';
  selectedType: string = '';
  availableTypes: string[] = [];
  filteredResults: RepoInfo[] = [];
  loading = false;
  lastSelectedIndex: number | null = null;

  isSelected(row: RepoInfo): boolean {
    return this.selectedRows.some(
      (r) => r.filePath === row.filePath && r.lineNumber === row.lineNumber
    );
  }

  onCheckboxToggle(row: RepoInfo, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.selectedRows.push(row);
    } else {
      this.selectedRows = this.selectedRows.filter(
        (r) => !(r.filePath === row.filePath && r.lineNumber === row.lineNumber)
      );
    }
  }
  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: any) {
    if (!this.exportSaved) {
      $event.preventDefault();
      $event.returnValue = 'You have unsaved changes!';
    }
  }
  exportToExcel(): void {
    const fileName = 'cnpj-scanner.xlsx';

    const newData = this.selectedRows.map((row) => ({
      'Descrição Completa': `${row.filePath} - ${
        row.type
      } - Line ${row.lineNumber}: ${row.declaration}`,
      Objeto: this.repoName,
      Tipo: row.type,
      Linha: row.lineNumber,
      'Caminho do Arquivo': row.filePath,
      'Declaração': row.declaration
    }));

    if (this.existingWorkbook) {
      const sheetName = this.existingWorkbook.SheetNames[0];
      const existingSheet = this.existingWorkbook.Sheets[sheetName];
      const existingData: any[] = XLSX.utils.sheet_to_json(existingSheet);

      const existingKeys = new Set(
        existingData.map((row) => row['Descrição Completa'])
      );
      const uniqueNewRows = newData.filter(
        (row) => !existingKeys.has(row['Descrição Completa'])
      );

      const updatedData = [...existingData, ...uniqueNewRows];
      const newSheet = XLSX.utils.json_to_sheet(updatedData);
      this.existingWorkbook.Sheets[sheetName] = newSheet;

      const wbout = XLSX.write(this.existingWorkbook, {
        bookType: 'xlsx',
        type: 'array',
      });

      const blob = new Blob([wbout], { type: 'application/octet-stream' });
      FileSaver.saveAs(blob, fileName);
    } else {
      const worksheet = XLSX.utils.json_to_sheet(newData);
      const workbook = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(workbook, worksheet, 'CNPJ');

      const wbout = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' });
      const blob = new Blob([wbout], { type: 'application/octet-stream' });

      FileSaver.saveAs(blob, fileName);
      this.existingWorkbook = workbook;
    }

    this.exportSaved = true;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const reader = new FileReader();

    reader.onload = (e: any) => {
      const data = new Uint8Array(e.target.result);
      const workbook = XLSX.read(data, { type: 'array' });
      this.existingWorkbook = workbook;
    };

    reader.readAsArrayBuffer(file);
  }

  onCheckboxChange(event: any) {
    const value = event.target.value;
    if (event.target.checked) {
      if (!this.selectedLanguages.includes(value)) {
        this.selectedLanguages.push(value);
      }
    } else {
      this.selectedLanguages = this.selectedLanguages.filter(
        (v) => v !== value
      );
    }
    console.log(this.selectedLanguages);
  }
  onSubmit(event: Event) {
    event.preventDefault();
    this.analyzed = false;
    this.repoName = this.repo.split('/').pop()?.replace('.git', '') ?? '';
    this.loadPage(this.page);
    this.loading = true;
  }

  areAllSelected(): boolean {
    return (
      this.paginatedResults().length > 0 &&
      this.paginatedResults().every((row) => this.isSelected(row))
    );
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.paginatedResults().forEach((row) => {
        if (!this.isSelected(row)) {
          this.selectedRows.push(row);
        }
      });
    } else {
      this.selectedRows = this.selectedRows.filter(
        (row) =>
          !this.paginatedResults().some(
            (r) =>
              r.filePath === row.filePath && r.lineNumber === row.lineNumber
          )
      );
    }
  }
  loadPage(page: number) {
    let params = new HttpParams()
      .set('repo', this.repo)
      .set('dirToClone', this.dirToClone);
    this.selectedLanguages.forEach((ext) => {
      params = params.append('extensions', ext);
    });

    this.http
      .get<RepoInfo[]>('https://localhost:7109/api/analyze/repo', {
        params,
      })
      .subscribe({
        next: (data) => {
              data = data.map(item => ({
            ...item,
            filePath: this.getShortPath(item.filePath)
          }));
          this.results = data;
          this.selectedRows.push(
            ...data.filter(r => r.looksLikeCnpj && !this.selectedRows.some(s => s.filePath === r.filePath && s.lineNumber === r.lineNumber))
          );
          this.analyzed = true;
          const typesSet = new Set(data.map((item) => item.type));
          this.availableTypes = Array.from(typesSet);
          this.filterByType();
          this.loading = false;
        },
        error: (err) => {
          console.error('Error fetching analysis results:', err);
          this.analyzed = false;
          this.loading = false;
        },
      });
  }

  filterByType(): void {
    const allItems = this.results;
    this.page = 1;
    if (this.selectedType) {
      this.filteredResults = allItems.filter(
        (item) => item.type === this.selectedType
      );
    } else {
      this.filteredResults = allItems;
    }
  }

  goBack() {
    console.log(this.analyzed);
    this.analyzed = !this.analyzed;
  }

  paginatedResults(): RepoInfo[] {
    const start = (this.page - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.filteredResults.slice(start, end);
  }
  onPageChange(pageNumber: number) {
    this.page = pageNumber;
  }

  getShortPath(fullPath: string): string {
    const normalizedPath = fullPath.replace(/\\/g, '/');
    const prefix = `${this.dirToClone.replace(/\\/g, '/')}/${this.repoName}`;
    return normalizedPath.startsWith(prefix)
      ? normalizedPath.replace(prefix + '/', '')
      : fullPath;
  }
  onCheckboxClick(event: MouseEvent, match: RepoInfo, index: number): void {
    const input = event.target as HTMLInputElement;
    const isChecked = input.checked;

    if (event.shiftKey && this.lastSelectedIndex !== null) {
      const start = Math.min(this.lastSelectedIndex, index);
      const end = Math.max(this.lastSelectedIndex, index);
      const rowsInPage = this.paginatedResults();

      for (let i = start; i <= end; i++) {
        const row = rowsInPage[i];
        if (isChecked && !this.isSelected(row)) {
          this.selectedRows.push(row);
        } else if (!isChecked) {
          this.selectedRows = this.selectedRows.filter(
            (r) =>
              !(r.filePath === row.filePath && r.lineNumber === row.lineNumber)
          );
        }
      }
    } else {
      // Normal click
      if (isChecked) {
        this.selectedRows.push(match);
      } else {
        this.selectedRows = this.selectedRows.filter(
          (r) =>
            !(
              r.filePath === match.filePath && r.lineNumber === match.lineNumber
            )
        );
      }
    }

    this.lastSelectedIndex = index;
  } 
}
