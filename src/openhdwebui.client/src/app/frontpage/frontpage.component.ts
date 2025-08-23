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
    password: ['', [Validators.required]]
  });

  constructor(private fb: FormBuilder, private http: HttpClient) {}

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
}
