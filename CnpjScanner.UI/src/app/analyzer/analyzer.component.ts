import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Pagination } from '../shared/models/pagination';
import { RepoInfo } from '../shared/models/repoInfo';

@Component({
  selector: 'app-analyzer',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbPaginationModule],
  templateUrl: './analyzer.component.html',
  styleUrls: ['./analyzer.component.css'],
})
export class AnalyzerComponent {
  private http = inject(HttpClient);
  repoUrl = '';
  dirToClone = '';
  languageOptions = [
    { label: 'C#', value: 'cs' },
    { label: 'VB.NET', value: 'vb' },
    { label: 'TypeScript', value: 'ts' },
  ];
  selectedLanguages: string[] = [];
  results: Pagination<RepoInfo> = {
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    items: [],
  };
  page = 1;
  pageSize = 15;
  analyzed = false;

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
    this.loadPage(this.page);
  }

  loadPage(page: number) {
    const apiUrl = `https://localhost:7109/api/analyze/repo?repoUrl=${encodeURIComponent(
      this.repoUrl
    )}&pageNumber=${page}&pageSize=${this.pageSize}&dirToClone=${
      this.dirToClone
    }&extensions=${this.selectedLanguages}`;
    let params = new HttpParams()
    .set('repoUrl', this.repoUrl)
    .set('dirToClone', this.dirToClone)
    .set('pageNumber', page.toString())
    .set('pageSize', this.pageSize.toString());
    this.selectedLanguages.forEach((ext) => {
      params = params.append('extensions', ext);
    });

    this.http.get<Pagination<RepoInfo>>('https://localhost:7109/api/analyze/repo', { params }).subscribe({
      next: (data) => {
        this.results = data;
        this.page = data.pageNumber;
        this.analyzed = true;
        console.log(this.results);
      },
      error: (err) => {
        console.error('Error fetching analysis results:', err);
        this.analyzed = false;
      },
    });
  }

  goBack() {
    console.log(this.analyzed);
    this.analyzed = !this.analyzed;
  }

  paginatedResults(): RepoInfo[] {
    const start = (this.page - 1) * this.pageSize;
    return this.results.items.slice(start, start + this.pageSize);
  }
}
