import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: false
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  hidePassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  onSubmit(): void {
    if (!this.loginForm.valid) {
      Object.keys(this.loginForm.controls).forEach(key => {
        this.loginForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.router.navigate(['/map']);
      },
      error: (err) => {
        console.error('Login error:', err);
        if (err.status === 401) {
          // The server message distinguishes pending/rejected membership from bad credentials
          this.errorMessage.set(err.error?.message || 'Invalid email or password');
        } else if (err.status === 422) {
          this.errorMessage.set('Please check your input and try again');
        } else {
          this.errorMessage.set('Login failed. Please try again.');
        }
        this.isLoading.set(false);
      }
    });
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }
}
