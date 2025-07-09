import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpClientModule, HttpParams } from '@angular/common/http';
import { NgxPaginationModule } from 'ngx-pagination';

@Component({
  selector: 'app-analyzer',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule, NgxPaginationModule],
  templateUrl: './analyzer.html',
  styleUrls: ['./analyzer.css'],
})
export class AnalyzerComponent {
  repoUrl = '';
  extensionFilter = 'cs,vb,ts';
  results: any[] = [];
  page = 1;
  pageSize = 10;

  constructor(private http: HttpClient) {}

  analyzeRepo() {
    let params = new HttpParams();
    params.append('repoUrl', this.repoUrl)
  
    this.http.get<any[]>('https://localhost:7109/api/analyze/repo?repoUrl=' + this.repoUrl)
      .subscribe(data => {
        this.results = data;
        this.page = 1;
        console.log(this.results)
      });
  }
}
