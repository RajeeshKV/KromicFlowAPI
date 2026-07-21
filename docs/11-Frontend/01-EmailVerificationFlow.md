# Email Verification - Advanced Frontend Patterns

Advanced implementation patterns and code examples for email verification.

**For basic integration, see: [00-EmailVerificationIntegration.md](./00-EmailVerificationIntegration.md)**

---

## React Hook Pattern

Reusable hook for email verification logic:

```typescript
import { useState, useCallback } from 'react';

interface EmailVerificationState {
  isVerified: boolean;
  loading: boolean;
  error: string | null;
  lastEmail: string | null;
}

export function useEmailVerification(accessToken: string) {
  const [state, setState] = useState<EmailVerificationState>({
    isVerified: false,
    loading: false,
    error: null,
    lastEmail: null
  });
  
  const checkVerification = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true }));
    try {
      const response = await fetch('/api/v1/users/profile', {
        headers: { 'Authorization': `Bearer ${accessToken}` }
      });
      
      const profile = await response.json();
      setState(prev => ({
        ...prev,
        isVerified: profile.emailVerified,
        error: null,
        loading: false
      }));
      return profile.emailVerified;
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error.message,
        loading: false
      }));
      return false;
    }
  }, [accessToken]);
  
  const sendEmail = useCallback(async (email: string) => {
    setState(prev => ({ ...prev, loading: true, error: null }));
    try {
      const response = await fetch('/api/v1/users/verify-email', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${accessToken}`
        },
        body: JSON.stringify({ email })
      });
      
      const data = await response.json();
      
      if (!response.ok) {
        throw new Error(data.error);
      }
      
      setState(prev => ({
        ...prev,
        lastEmail: email,
        error: null,
        loading: false
      }));
      return { success: true, message: data.message };
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error.message,
        loading: false
      }));
      throw error;
    }
  }, [accessToken]);
  
  return { ...state, checkVerification, sendEmail };
}

// Usage
function App() {
  const verification = useEmailVerification(accessToken);
  
  useEffect(() => {
    verification.checkVerification();
  }, [verification]);
  
  if (verification.isVerified) {
    return <Dashboard />;
  }
  
  return (
    <EmailVerificationModal 
      onSend={verification.sendEmail}
      loading={verification.loading}
      error={verification.error}
    />
  );
}
```

---

## React Component with State Management

```typescript
import React, { useState, useEffect } from 'react';

interface EmailVerificationModalProps {
  isOpen: boolean;
  onSuccess: () => void;
  accessToken: string;
}

export function EmailVerificationModal({ 
  isOpen, 
  onSuccess, 
  accessToken 
}: EmailVerificationModalProps) {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);
  const [resendCountdown, setResendCountdown] = useState(0);
  const [message, setMessage] = useState<{
    text: string;
    type: 'success' | 'error';
  } | null>(null);
  
  // Countdown for resend button
  useEffect(() => {
    if (resendCountdown <= 0) return;
    
    const timer = setTimeout(() => {
      setResendCountdown(prev => prev - 1);
    }, 1000);
    
    return () => clearTimeout(timer);
  }, [resendCountdown]);
  
  const handleSendEmail = async () => {
    if (!email || !isValidEmail(email)) {
      setMessage({ text: 'Please enter a valid email', type: 'error' });
      return;
    }
    
    setLoading(true);
    try {
      const response = await fetch('/api/v1/users/verify-email', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${accessToken}`
        },
        body: JSON.stringify({ email })
      });
      
      const data = await response.json();
      
      if (!response.ok) {
        setMessage({ text: data.error, type: 'error' });
        return;
      }
      
      setSent(true);
      setMessage({ text: data.message, type: 'success' });
      setResendCountdown(60); // 60 second cooldown
    } catch (error) {
      setMessage({ text: 'Failed to send email', type: 'error' });
    } finally {
      setLoading(false);
    }
  };
  
  if (!isOpen) return null;
  
  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h2>Verify Your Email</h2>
        <p>We need to verify your email to activate automations.</p>
        
        {!sent ? (
          <>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter your email"
              disabled={loading}
            />
            
            <button onClick={handleSendEmail} disabled={loading}>
              {loading ? 'Sending...' : 'Send Verification Link'}
            </button>
          </>
        ) : (
          <>
            <p className="confirmation">
              ✓ Verification link sent to {email}
            </p>
            
            {resendCountdown > 0 ? (
              <p className="countdown">
                Resend available in {resendCountdown}s
              </p>
            ) : (
              <button onClick={() => setSent(false)}>
                Didn't receive it? Resend
              </button>
            )}
          </>
        )}
        
        {message && (
          <p className={`message ${message.type}`}>
            {message.text}
          </p>
        )}
      </div>
    </div>
  );
}

function isValidEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}
```

---

## Rate Limit Handler

Prevent rapid-fire requests to the API:

```typescript
class RateLimitManager {
  private requestTimestamps: number[] = [];
  private readonly maxRequests = 3;
  private readonly windowMs = 3600000; // 1 hour
  
  canMakeRequest(): boolean {
    const now = Date.now();
    
    // Remove old requests outside the window
    this.requestTimestamps = this.requestTimestamps.filter(
      ts => now - ts < this.windowMs
    );
    
    return this.requestTimestamps.length < this.maxRequests;
  }
  
  recordRequest(): void {
    this.requestTimestamps.push(Date.now());
  }
  
  getTimeUntilNextRequest(): number {
    if (this.requestTimestamps.length === 0) return 0;
    
    const oldestRequest = this.requestTimestamps[0];
    const timeUntilExpiry = this.windowMs - (Date.now() - oldestRequest);
    
    return Math.max(0, timeUntilExpiry);
  }
  
  getFormattedWaitTime(): string {
    const ms = this.getTimeUntilNextRequest();
    const minutes = Math.ceil(ms / 60000);
    return minutes > 0 ? `${minutes} minute(s)` : 'a few seconds';
  }
}

// Usage
const rateLimiter = new RateLimitManager();

async function sendEmailWithRateLimit(email: string, accessToken: string) {
  if (!rateLimiter.canMakeRequest()) {
    const waitTime = rateLimiter.getFormattedWaitTime();
    throw new Error(`Too many requests. Please try again in ${waitTime}`);
  }
  
  try {
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
      },
      body: JSON.stringify({ email })
    });
    
    const data = await response.json();
    
    if (!response.ok) {
      throw new Error(data.error);
    }
    
    rateLimiter.recordRequest();
    return data;
  } catch (error) {
    throw error;
  }
}
```

---

## Protected Automation Route

```typescript
import { Navigate } from 'react-router-dom';
import { useEffect, useState } from 'react';

interface ProtectedRouteProps {
  element: React.ReactElement;
  accessToken: string;
  requireEmailVerification?: boolean;
}

export function ProtectedAutomationRoute({
  element,
  accessToken,
  requireEmailVerification = true
}: ProtectedRouteProps) {
  const [isVerified, setIsVerified] = useState<boolean | null>(null);
  
  useEffect(() => {
    const checkVerification = async () => {
      try {
        const response = await fetch('/api/v1/users/profile', {
          headers: { 'Authorization': `Bearer ${accessToken}` }
        });
        
        const profile = await response.json();
        setIsVerified(profile.emailVerified);
      } catch (error) {
        console.error('Error:', error);
        setIsVerified(false);
      }
    };
    
    if (requireEmailVerification) {
      checkVerification();
    }
  }, [accessToken, requireEmailVerification]);
  
  if (requireEmailVerification && isVerified === null) {
    return <div>Loading...</div>;
  }
  
  if (requireEmailVerification && !isVerified) {
    return <Navigate to="/email-verification" replace />;
  }
  
  return element;
}

// Usage in routing
<Routes>
  <Route
    path="/automations"
    element={
      <ProtectedAutomationRoute
        element={<Automations />}
        accessToken={accessToken}
        requireEmailVerification
      />
    }
  />
</Routes>
```

---

## Error Boundary

Handle errors gracefully:

```typescript
import React, { ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class EmailVerificationErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }
  
  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }
  
  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Email verification error:', error, errorInfo);
  }
  
  render() {
    if (this.state.hasError) {
      return (
        <div className="error-container">
          <h2>Email Verification Error</h2>
          <p>{this.state.error?.message}</p>
          <button onClick={() => this.setState({ hasError: false })}>
            Try Again
          </button>
        </div>
      );
    }
    
    return this.props.children;
  }
}

// Usage
<EmailVerificationErrorBoundary>
  <EmailVerificationModal {...props} />
</EmailVerificationErrorBoundary>
```

---

## Testing Examples

### Jest + React Testing Library

```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { EmailVerificationModal } from './EmailVerificationModal';

describe('EmailVerificationModal', () => {
  const mockFetch = jest.fn();
  global.fetch = mockFetch;
  
  beforeEach(() => {
    mockFetch.mockClear();
  });
  
  it('should validate email format', async () => {
    render(
      <EmailVerificationModal
        isOpen
        onSuccess={jest.fn()}
        accessToken="test-token"
      />
    );
    
    const input = screen.getByPlaceholderText('Enter your email');
    const button = screen.getByText('Send Verification Link');
    
    fireEvent.change(input, { target: { value: 'invalid-email' } });
    fireEvent.click(button);
    
    await waitFor(() => {
      expect(screen.getByText(/enter a valid email/i)).toBeInTheDocument();
    });
    
    expect(mockFetch).not.toHaveBeenCalled();
  });
  
  it('should send email with valid format', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({
        success: true,
        message: 'Email sent'
      })
    });
    
    render(
      <EmailVerificationModal
        isOpen
        onSuccess={jest.fn()}
        accessToken="test-token"
      />
    );
    
    const input = screen.getByPlaceholderText('Enter your email');
    const button = screen.getByText('Send Verification Link');
    
    fireEvent.change(input, { target: { value: 'user@example.com' } });
    fireEvent.click(button);
    
    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        '/api/v1/users/verify-email',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ email: 'user@example.com' })
        })
      );
    });
  });
  
  it('should handle rate limit error', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: () => Promise.resolve({
        error: 'Too many verification requests. Please try again in 1 hour'
      })
    });
    
    render(
      <EmailVerificationModal
        isOpen
        onSuccess={jest.fn()}
        accessToken="test-token"
      />
    );
    
    const input = screen.getByPlaceholderText('Enter your email');
    const button = screen.getByText('Send Verification Link');
    
    fireEvent.change(input, { target: { value: 'user@example.com' } });
    fireEvent.click(button);
    
    await waitFor(() => {
      expect(screen.getByText(/too many.*try again/i)).toBeInTheDocument();
    });
  });
});
```

---

## See Also

- [Main Integration Guide](./00-EmailVerificationIntegration.md)
- [Backend Configuration](../10-Email/01-ConfigurationGuide.md)
- [Email Templates](../10-Email/02-TemplateSpecifications.md)

