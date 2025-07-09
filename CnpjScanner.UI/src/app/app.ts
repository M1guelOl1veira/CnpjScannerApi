import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AnalyzerComponent } from './components/analyzer/analyzer';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AnalyzerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected title = 'CnpjScanner.UI';
}
