import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap'
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
  extensionFilter = 'cs,vb,ts';
  results: Pagination<RepoInfo> = {
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    items: []
  };
  page = 1;
  pageSize = 10;
  analyzed = false;



  onSubmit(event: Event) {
    event.preventDefault();
    this.analyzed = false;
    this.loadPage(this.page);
  }

  loadPage(page: number) {
    const apiUrl = `https://localhost:7109/api/analyze/repo?repoUrl=${encodeURIComponent(this.repoUrl)}&pageNumber=${page}&pageSize=${this.pageSize}`;

    this.http.get<Pagination<RepoInfo>>(apiUrl).subscribe({
      next: data => {
        this.results = data;
        this.page = data.pageNumber;
        this.analyzed = true;
        console.log(this.results)
      },
      error: err => {
        console.error('Error fetching analysis results:', err);
        this.analyzed = true;
      }
    });
  }

  paginatedResults(): RepoInfo[] {
    const start = (this.page - 1) * this.pageSize;
    return this.results.items.slice(start, start + this.pageSize);
  }
}
