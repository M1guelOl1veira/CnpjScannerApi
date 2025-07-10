import { Component } from '@angular/core';
import { AnalyzerComponent } from './analyzer/analyzer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AnalyzerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'CnpjScanner.UI';
}
