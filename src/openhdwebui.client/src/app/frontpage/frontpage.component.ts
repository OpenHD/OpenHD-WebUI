import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-frontpage',
  templateUrl: './frontpage.component.html',
  styleUrls: ['./frontpage.component.css']
})
export class FrontpageComponent implements OnInit {
  isLoginOpen = false;
  docLink = 'https://openhdfpv.org/introduction/';
  private readonly localDocsLink = '/docs/introduction/index.html';

  loginForm = this.fb.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  constructor(private fb: FormBuilder, private http: HttpClient) {}

  ngOnInit(): void {
    this.resolveDocsLink();
  }

  toggleLogin() { this.isLoginOpen = !this.isLoginOpen; }

  submitLogin() {
    if (this.loginForm.invalid) { return; }
    const { username, password } = this.loginForm.value;
    this.http.post('/api/auth/login', { username, password }, { responseType: 'text' })
      .subscribe({
        next: () => {
          console.log('login success');
          this.isLoginOpen = false;
        },
        error: () => {
          console.log('login failed');
        }
      });
  }

  private resolveDocsLink() {
    this.http.get<{ url: string }>('/api/docs/link').subscribe({
      next: (response) => {
        const resolvedUrl = response?.url?.trim();
        this.docLink = resolvedUrl ? resolvedUrl : this.localDocsLink;
      },
      error: () => {
        this.docLink = this.localDocsLink;
      }
    });
  }
}
