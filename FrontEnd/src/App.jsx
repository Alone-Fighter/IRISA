import { useEffect, useState } from 'react'
import PropTypes from 'prop-types'
import './App.css'

function toLatinDigits(value) {
  return value
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632))
}

async function request(path, options = {}) {
  const response = await fetch(path, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

  if (response.status === 204) return null

  const body = await response.json().catch(() => null)
  if (!response.ok) {
    const error = new Error(
      body?.detail || body?.title || 'ارتباط با سرور با خطا مواجه شد.',
    )
    error.status = response.status
    throw error
  }

  return body
}

function formatDate(value) {
  return new Intl.DateTimeFormat('fa-IR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function Login({ onLogin }) {
  const [email, setEmail] = useState('admin@irisa.local')
  const [password, setPassword] = useState('IrisaDemo!2026')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setLoading(true)
    try {
      const user = await request('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })
      onLogin(user)
    } catch (loginError) {
      setError(loginError.message)
    } finally {
      setLoading(false)
    }
    Login.propTypes = {
      onLogin: PropTypes.func.isRequired,
    }
  }

  return (
    <main className="login-shell">
      <section className="login-card">
        <div className="brand-mark">ا</div>
        <p className="eyebrow">سامانهٔ آزمایشگاه</p>
        <h1>ورود به ایریسا</h1>
        <p className="muted">برای مشاهدهٔ اطلاعات، وارد حساب کاربری شوید.</p>
        <form className="login-form" onSubmit={handleSubmit}>
          <label>
            ایمیل
            <input
              autoComplete="username"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </label>
          <label>
            گذرواژه
            <input
              autoComplete="current-password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </label>
          {error && <p className="alert error">{error}</p>}
          <button className="primary-button full-width" disabled={loading}>
            {loading ? 'در حال ورود…' : 'ورود به سامانه'}
          </button>
        </form>
        <div className="demo-note">
          <strong>حساب نمایشی توسعه</strong>
          <span>admin@irisa.local</span>
          <span dir="ltr">IrisaDemo!2026</span>
        </div>
      </section>
    </main>
  )
}

function Dashboard({ user, onLogout }) {
  const [nationalCode, setNationalCode] = useState('')
  const [person, setPerson] = useState(null)
  const [samples, setSamples] = useState([])
  const [error, setError] = useState('')
  const [refreshMessage, setRefreshMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [refreshing, setRefreshing] = useState(false)
  const [logoutLoading, setLogoutLoading] = useState(false)

  useEffect(() => {
    request('/api/demo/records')
      .then(setSamples)
      .catch(() => setSamples([]))
  }, [])

  async function handleSearch(event) {
    event.preventDefault()
    setError('')
    setRefreshMessage('')
    setPerson(null)
    setLoading(true)
    try {
      const result = await request(
        `/api/people/${encodeURIComponent(nationalCode.trim())}`,
      )
      setPerson(result)
    } catch (searchError) {
      setError(searchError.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleRefresh() {
    if (!nationalCode.trim()) {
      setError('ابتدا یک کد ملی را وارد کنید.')
      return
    }

    setError('')
    setRefreshMessage('')
    setRefreshing(true)
    try {
      const result = await request(
        `/api/people/${encodeURIComponent(nationalCode.trim())}/refresh`,
        { method: 'POST' },
      )
      setPerson(result)
      setRefreshMessage(
        result.isStale
          ? 'منبع اصلی هنوز در دسترس نیست؛ همان نسخهٔ ذخیره‌شده نمایش داده می‌شود.'
          : 'اطلاعات با موفقیت از منبع اصلی دریافت و به‌روز شد.',
      )
    } catch (refreshError) {
      setError(
        refreshError.status === 405
          ? 'نسخهٔ بک‌اند در حال اجرا قدیمی است. بک‌اند را متوقف و دوباره اجرا کنید تا مسیر به‌روزرسانی فعال شود.'
          : refreshError.message,
      )
    } finally {
      setRefreshing(false)
    }
  }

  async function handleLogout() {
    setLogoutLoading(true)
    try {
      await request('/api/auth/logout', { method: 'POST' })
      onLogout()
    } catch (logoutError) {
      setError(logoutError.message)
      setLogoutLoading(false)
    }
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <a className="brand" href="/" aria-label="ایریسا">
          <span className="brand-mark small">ا</span>
          <span>ایریسا</span>
        </a>
        <div className="user-menu">
          <span className="user-email">{user.email}</span>
          <button className="text-button" onClick={handleLogout} disabled={logoutLoading}>
            {logoutLoading ? '…' : 'خروج'}
          </button>
        </div>
      </header>

      <section className="welcome">
        <p className="eyebrow">پنل جست‌وجوی اطلاعات</p>
        <h1>جست‌وجوی پروندهٔ آزمایشگاهی</h1>
        <p className="muted">
          کد ملی را وارد کنید تا اطلاعات از منبع اصلی دریافت و برای دسترسی هنگام
          قطعی، در سامانه ذخیره شود.
        </p>
      </section>

      <section className="search-card">
        <form className="search-form" onSubmit={handleSearch}>
          <label htmlFor="national-code">کد ملی</label>
          <div className="search-controls">
            <input
              id="national-code"
              inputMode="numeric"
              maxLength={10}
              placeholder="مثلاً ۱۱۱۱۱۱۱۱۱۱"
              value={nationalCode}
              onChange={(event) =>
                setNationalCode(toLatinDigits(event.target.value).replace(/\D/g, ''))
              }
              required
            />
            <button className="primary-button" disabled={loading}>
              {loading ? 'در حال جست‌وجو…' : 'جست‌وجو'}
            </button>
          </div>
          {error && <p className="alert error" role="alert">{error}</p>}
        </form>

        {samples.length > 0 && (
          <div className="sample-list">
            <span>کدهای ملی نمایشی:</span>
            {samples.map((sample) => (
              <button
                className="sample-code"
                key={sample.nationalCode}
                onClick={() => setNationalCode(sample.nationalCode)}
                type="button"
              >
                {sample.nationalCode}
              </button>
            ))}
          </div>
        )}
      </section>

      {person && (
        <section className="results-card" aria-live="polite">
          <div className="results-heading">
            <div>
              <p className="eyebrow">پروندهٔ بیمار</p>
              <h2>{person.fullName}</h2>
              <p className="muted">کد ملی: {person.nationalCode}</p>
              {(person.gender || person.birthDate) && (
                <p className="muted person-demographics">
                  {person.gender && `جنسیت: ${person.gender}`}
                  {person.gender && person.birthDate && ' · '}
                  {person.birthDate &&
                    `تاریخ تولد: ${new Intl.DateTimeFormat('fa-IR').format(
                      new Date(`${person.birthDate}T00:00:00`),
                    )}`}
                </p>
              )}
            </div>
            <span className={`status-badge ${person.isStale ? 'stale' : 'fresh'}`}>
              {person.isStale ? 'نسخهٔ ذخیره‌شده' : 'دریافت از منبع اصلی'}
            </span>
          </div>
          <div className="results-meta">
            <p className="sync-time">
              آخرین دریافت موفق: {formatDate(person.lastSyncedAtUtc)}
              {person.isStale && ' · سرویس منبع در دسترس نیست؛ داده ممکن است به‌روز نباشد.'}
            </p>
            {person.isStale && (
              <button
                className="secondary-button"
                type="button"
                onClick={handleRefresh}
                disabled={refreshing}
              >
                {refreshing ? 'در حال تلاش برای منبع اصلی…' : 'تلاش مجدد برای منبع اصلی'}
              </button>
            )}
          </div>
          {refreshMessage && <p className="refresh-message" role="status">{refreshMessage}</p>}
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>آزمایش</th>
                  <th>نتیجه</th>
                  <th>واحد</th>
                  <th>محدودهٔ مرجع</th>
                  <th>تاریخ انجام</th>
                </tr>
              </thead>
              <tbody>
                {person.labResults.length ? person.labResults.map((result, index) => (
                  <tr key={`${result.testName}-${index}`}>
                    <td>{result.testName}</td>
                    <td>{result.resultValue || '—'}</td>
                    <td>{result.unit || '—'}</td>
                    <td>{result.referenceRange || '—'}</td>
                    <td>{formatDate(result.performedAtUtc)}</td>
                  </tr>
                )) : (
                  <tr><td className="empty-row" colSpan="5">برای این پرونده نتیجهٔ آزمایشی ثبت نشده است.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      )}

      <footer className="footer-note">
        این نسخه برای تست با اطلاعات ساختگی است؛ اطلاعات واقعی بیمار در آن وارد نکنید.
      </footer>
    </main>
  )
}
Dashboard.propTypes = {
  user: PropTypes.shape({ email: PropTypes.string.isRequired }).isRequired,
  onLogout: PropTypes.func.isRequired,
}

function App() {
  const [user, setUser] = useState(null)
  const [checkingSession, setCheckingSession] = useState(true)
  const [serverError, setServerError] = useState('')

  useEffect(() => {
    request('/api/auth/me')
      .then(setUser)
      .catch((error) => {
        if (error.status !== 401) {
          setServerError('ارتباط با بک‌اند برقرار نشد. ابتدا سرور بک‌اند را اجرا کنید.')
        }
      })
      .finally(() => setCheckingSession(false))
  }, [])

  if (checkingSession) {
    return <main className="loading-screen">در حال اتصال به سامانه…</main>
  }

  if (serverError) {
    return (
      <main className="loading-screen">
        <p>{serverError}</p>
        <button className="primary-button" onClick={() => window.location.reload()}>
          تلاش دوباره
        </button>
      </main>
    )
  }

  return user
    ? <Dashboard user={user} onLogout={() => setUser(null)} />
    : <Login onLogin={setUser} />
}

export default App
