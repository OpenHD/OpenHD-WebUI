import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-frontpage',
  templateUrl: './frontpage.component.html',
  styleUrls: ['./frontpage.component.css']
})
export class FrontpageComponent {
  isLoginOpen = false;

  loginForm = this.fb.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
    remember: [true]
  });

  constructor(private fb: FormBuilder, private http: HttpClient) {}

  openLogin() { this.isLoginOpen = true; }
  closeLogin() { this.isLoginOpen = false; }

  submitLogin() {
    if (this.loginForm.invalid) { return; }
    const { username, password } = this.loginForm.value;
    this.http.post('/api/auth/login', { username, password }, { responseType: 'text' })
      .subscribe({
        next: () => {
          console.log('login success');
          this.closeLogin();
        },
        error: () => {
          console.log('login failed');
        }
      });
  }
}
