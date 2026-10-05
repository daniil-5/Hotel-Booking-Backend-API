// ============================================================
// BookingSystem — минималистичный SPA (vanilla JS, i18n)
// Языки интерфейса: RU / EN / PL / BE
// Каталог → отель → бронирование; мои брони; админ: юзеры + логи
// ============================================================

const API = '';

let state = {
  token: localStorage.getItem('token') || null,
  user: JSON.parse(localStorage.getItem('user') || 'null'),
  view: 'hotels',
  hotelId: null,
};


// ---------- локализация ----------

const I18N = {
  ru: {
    langName: 'RU',
    hotels: 'Отели', bookings: 'Мои брони', admin: 'Админ',
    login: 'Войти', logout: 'Выйти',
    find: 'Найти', reset: 'Сброс',
    searchPlaceholder: 'Город или место', ratingAny: 'Рейтинг: любой', ratingFrom: 'от {v}',
    maxPrice: 'Цена до, руб.', guests: 'Гостей',
    perNight: '/ ночь', fromPrice: 'от', roomTypesCount: 'типов номеров',
    emptyCatalog: 'Ничего не найдено', emptyBookings: 'Бронирований пока нет — выберите отель в каталоге',
    backToCatalog: '← Каталог', rooms: 'Номера',
    guestsUpTo: 'до', areaUnit: 'м²', book: 'Забронировать',
    bookingTitle: 'Бронирование', checkIn: 'Дата заезда', checkOut: 'Дата выезда',
    guestsLabel: 'Гостей (максимум {n})', basePriceNote: 'Базовая цена: {p} руб./ночь; итог считается по календарю сезонных цен.',
    bookAction: 'Забронировать', loginTab: 'Вход', registerTab: 'Регистрация',
    email: 'E-mail', password: 'Пароль', username: 'Логин', firstName: 'Имя', lastName: 'Фамилия',
    phone: 'Телефон', createAccount: 'Создать аккаунт', passwordHint: 'Пароль (≥6 символов)',
    demoHint: 'Демо: user1@booking.local / User123! · admin@booking.local / Admin123!',
    welcome: 'Добро пожаловать, {u}!', accountCreated: 'Аккаунт создан',
    loginRequired: 'Сначала войдите в систему', needLogin: 'Требуется вход',
    booked: 'Забронировано! Стоимость: {p} руб.',
    datesInvalid: 'Дата выезда должна быть позже заезда',
    capacityInvalid: 'Число гостей превышает вместимость',
    myBookingsTitle: 'Мои бронирования',
    colNum: '№', colHotel: 'Отель', colRoom: 'Номер', colIn: 'Заезд', colOut: 'Выезд',
    colGuests: 'Гостей', colPrice: 'Стоимость', colStatus: 'Статус', cancel: 'Отменить',
    cancelTitle: 'Отмена бронирования', cancelText: 'Отменить бронирование №{id}? Даты станут доступны другим гостям.',
    keep: 'Оставить', cancelBookingBtn: 'Отменить бронь', bookingCancelled: 'Бронирование отменено',
    usersTitle: 'Пользователи', auditTitle: 'Журнал аудита (последние 30)',
    colID: 'ID', colLogin: 'Логин', colEmail: 'E-mail', colName: 'Имя', colRole: 'Роль', colCreated: 'Создан',
    colTime: 'Время', colUser: 'Пользователь', colEvent: 'Событие', colSuccess: 'Успех', colMessage: 'Сообщение',
    role1: 'Гость', role2: 'Менеджер', role3: 'Администратор',
    st1: 'Ожидает', st2: 'Активно', st3: 'Подтверждено', st4: 'Отменено', st5: 'Завершено',
    adminOnly: 'Раздел доступен администратору', loading: 'Загрузка…', currency: 'руб.',
  },
  en: {
    langName: 'EN',
    hotels: 'Hotels', bookings: 'My bookings', admin: 'Admin',
    login: 'Sign in', logout: 'Sign out',
    find: 'Search', reset: 'Reset',
    searchPlaceholder: 'City or place', ratingAny: 'Rating: any', ratingFrom: 'from {v}',
    maxPrice: 'Max price', guests: 'Guests',
    perNight: '/ night', fromPrice: 'from', roomTypesCount: 'room types',
    emptyCatalog: 'Nothing found', emptyBookings: 'No bookings yet — pick a hotel in the catalog',
    backToCatalog: '← Catalog', rooms: 'Rooms',
    guestsUpTo: 'up to', areaUnit: 'm²', book: 'Book',
    bookingTitle: 'Booking', checkIn: 'Check-in', checkOut: 'Check-out',
    guestsLabel: 'Guests (max {n})', basePriceNote: 'Base price: {p} BYN per night; total uses the seasonal price calendar.',
    bookAction: 'Book now', loginTab: 'Sign in', registerTab: 'Sign up',
    email: 'E-mail', password: 'Password', username: 'Username', firstName: 'First name', lastName: 'Last name',
    phone: 'Phone', createAccount: 'Create account', passwordHint: 'Password (≥6 chars)',
    demoHint: 'Demo: user1@booking.local / User123! · admin@booking.local / Admin123!',
    welcome: 'Welcome, {u}!', accountCreated: 'Account created',
    loginRequired: 'Please sign in first', needLogin: 'Sign in required',
    booked: 'Booked! Total: {p} BYN',
    datesInvalid: 'Check-out must be after check-in',
    capacityInvalid: 'Guest count exceeds room capacity',
    myBookingsTitle: 'My bookings',
    colNum: 'No.', colHotel: 'Hotel', colRoom: 'Room', colIn: 'Check-in', colOut: 'Check-out',
    colGuests: 'Guests', colPrice: 'Total', colStatus: 'Status', cancel: 'Cancel',
    cancelTitle: 'Cancel booking', cancelText: 'Cancel booking #{id}? The dates will be released for other guests.',
    keep: 'Keep', cancelBookingBtn: 'Cancel booking', bookingCancelled: 'Booking cancelled',
    usersTitle: 'Users', auditTitle: 'Audit log (latest 30)',
    colID: 'ID', colLogin: 'Username', colEmail: 'E-mail', colName: 'Name', colRole: 'Role', colCreated: 'Created',
    colTime: 'Time', colUser: 'User', colEvent: 'Event', colSuccess: 'Success', colMessage: 'Message',
    role1: 'Guest', role2: 'Manager', role3: 'Administrator',
    st1: 'Pending', st2: 'Active', st3: 'Confirmed', st4: 'Cancelled', st5: 'Completed',
    adminOnly: 'This section is for administrators', loading: 'Loading…', currency: 'BYN',
  },
  pl: {
    langName: 'PL',
    hotels: 'Hotele', bookings: 'Moje rezerwacje', admin: 'Admin',
    login: 'Zaloguj się', logout: 'Wyloguj się',
    find: 'Szukaj', reset: 'Reset',
    searchPlaceholder: 'Miasto lub miejsce', ratingAny: 'Ocena: dowolna', ratingFrom: 'od {v}',
    maxPrice: 'Cena do', guests: 'Gości',
    perNight: '/ noc', fromPrice: 'od', roomTypesCount: 'typów pokoi',
    emptyCatalog: 'Nic nie znaleziono', emptyBookings: 'Brak rezerwacji — wybierz hotel w katalogu',
    backToCatalog: '← Katalog', rooms: 'Pokoje',
    guestsUpTo: 'do', areaUnit: 'm²', book: 'Zarezerwuj',
    bookingTitle: 'Rezerwacja', checkIn: 'Przyjazd', checkOut: 'Wyjazd',
    guestsLabel: 'Gości (maks. {n})', basePriceNote: 'Cena bazowa: {p} BYN/noc; suma wg kalendarza cen sezonowych.',
    bookAction: 'Zarezerwuj', loginTab: 'Logowanie', registerTab: 'Rejestracja',
    email: 'E-mail', password: 'Hasło', username: 'Login', firstName: 'Imię', lastName: 'Nazwisko',
    phone: 'Telefon', createAccount: 'Utwórz konto', passwordHint: 'Hasło (≥6 znaków)',
    demoHint: 'Demo: user1@booking.local / User123! · admin@booking.local / Admin123!',
    welcome: 'Witaj, {u}!', accountCreated: 'Konto utworzone',
    loginRequired: 'Najpierw zaloguj się', needLogin: 'Wymagane logowanie',
    booked: 'Zarezerwowano! Kwota: {p} BYN',
    datesInvalid: 'Data wyjazdu musi być po dacie przyjazdu',
    capacityInvalid: 'Liczba gości przekracza pojemność pokoju',
    myBookingsTitle: 'Moje rezerwacje',
    colNum: 'Nr', colHotel: 'Hotel', colRoom: 'Pokój', colIn: 'Przyjazd', colOut: 'Wyjazd',
    colGuests: 'Gości', colPrice: 'Kwota', colStatus: 'Status', cancel: 'Anuluj',
    cancelTitle: 'Anulowanie rezerwacji', cancelText: 'Anulować rezerwację nr {id}? Terminy staną się dostępne dla innych gości.',
    keep: 'Zostaw', cancelBookingBtn: 'Anuluj rezerwację', bookingCancelled: 'Rezerwacja anulowana',
    usersTitle: 'Użytkownicy', auditTitle: 'Dziennik audytu (ostatnie 30)',
    colID: 'ID', colLogin: 'Login', colEmail: 'E-mail', colName: 'Imię', colRole: 'Rola', colCreated: 'Utworzono',
    colTime: 'Czas', colUser: 'Użytkownik', colEvent: 'Zdarzenie', colSuccess: 'Sukces', colMessage: 'Komunikat',
    role1: 'Gość', role2: 'Menedżer', role3: 'Administrator',
    st1: 'Oczekuje', st2: 'Aktywna', st3: 'Potwierdzona', st4: 'Anulowana', st5: 'Zakończona',
    adminOnly: 'Sekcja dostępna dla administratora', loading: 'Ładowanie…', currency: 'BYN',
  },
  be: {
    langName: 'BE',
    hotels: 'Гасцініцы', bookings: 'Мае браніраванні', admin: 'Адмін',
    login: 'Увайсці', logout: 'Выйсці',
    find: 'Знайсці', reset: 'Скінуць',
    searchPlaceholder: 'Горад або месца', ratingAny: 'Рэйтынг: любы', ratingFrom: 'ад {v}',
    maxPrice: 'Цана да, руб.', guests: 'Гасцей',
    perNight: '/ ноч', fromPrice: 'ад', roomTypesCount: 'тыпаў нумароў',
    emptyCatalog: 'Нічога не знойдзена', emptyBookings: 'Браніраванняў пакуль няма — выберыце гасцініцу ў каталогу',
    backToCatalog: '← Каталог', rooms: 'Нумары',
    guestsUpTo: 'да', areaUnit: 'м²', book: 'Забраніраваць',
    bookingTitle: 'Браніраванне', checkIn: 'Дата прыезду', checkOut: 'Дата выезду',
    guestsLabel: 'Гасцей (максімум {n})', basePriceNote: 'Базавая цана: {p} руб./ночь; вынік лічыцца па каляндары сезонных цэн.',
    bookAction: 'Забраніраваць', loginTab: 'Уваход', registerTab: 'Рэгістрацыя',
    email: 'E-mail', password: 'Пароль', username: 'Лагін', firstName: 'Імя', lastName: 'Прозвішча',
    phone: 'Тэлефон', createAccount: 'Стварыць акаўнт', passwordHint: 'Пароль (≥6 знакаў)',
    demoHint: 'Дэма: user1@booking.local / User123! · admin@booking.local / Admin123!',
    welcome: 'Вітаем, {u}!', accountCreated: 'Акаўнт створаны',
    loginRequired: 'Спачатку ўвайдзіце ў сістэму', needLogin: 'Патрэбны ўваход',
    booked: 'Забраніравана! Кошт: {p} руб.',
    datesInvalid: 'Дата выезду павінна быць пазней прыезду',
    capacityInvalid: 'Колькасць гасцей перавышае ўмяшчальнасць',
    myBookingsTitle: 'Мае браніраванні',
    colNum: '№', colHotel: 'Гасцініца', colRoom: 'Нумар', colIn: 'Прыезд', colOut: 'Выезд',
    colGuests: 'Гасцей', colPrice: 'Кошт', colStatus: 'Статус', cancel: 'Адмяніць',
    cancelTitle: 'Адмена браніравання', cancelText: 'Адмяніць браніраванне №{id}? Даты стануць даступныя іншым гасцям.',
    keep: 'Пакінуць', cancelBookingBtn: 'Адмяніць брань', bookingCancelled: 'Браніраванне адменена',
    usersTitle: 'Карыстальнікі', auditTitle: 'Журнал аудыту (апошнія 30)',
    colID: 'ID', colLogin: 'Лагін', colEmail: 'E-mail', colName: 'Імя', colRole: 'Роля', colCreated: 'Створана',
    colTime: 'Час', colUser: 'Карыстальнік', colEvent: 'Падзея', colSuccess: 'Паспех', colMessage: 'Паведамленне',
    role1: 'Госць', role2: 'Менеджар', role3: 'Адміністратар',
    st1: 'Чакае', st2: 'Актыўна', st3: 'Пацверджана', st4: 'Адменена', st5: 'Завершана',
    adminOnly: 'Раздзел даступны адміністратару', loading: 'Загрузка…', currency: 'руб.',
  },
};

let lang = localStorage.getItem('lang') || 'ru';
if (!I18N[lang]) lang = 'ru';

function t(key, params = {}) {
  let s = (I18N[lang] && I18N[lang][key]) ?? I18N.ru[key] ?? key;
  for (const [k, v] of Object.entries(params)) s = s.replace('{' + k + '}', v);
  return s;
}

/* exported setLang */
function setLang(l) {
  if (!I18N[l]) return;
  lang = l;
  localStorage.setItem('lang', l);
  renderUserbox();
  go(state.view, state.hotelId);
}

// статусы брони: код/строка → [перевод, класс бейджа]
const STATUS_BADGE = {
  1: 'b1', 2: 'b2', 3: 'b3', 4: 'b4', 5: 'b5',
  Pending: 'b1', Active: 'b2', Confirmed: 'b3', Cancelled: 'b4', Completed: 'b5',
};
const STATUS_NUM = { Pending: 1, Active: 2, Confirmed: 3, Cancelled: 4, Completed: 5 };
function statusInfo(s) {
  const num = typeof s === 'number' ? s : STATUS_NUM[s];
  return [t('st' + num), STATUS_BADGE[s] || 'b4'];
}
const ROLE_NAME = r => t('role' + r);

// локальные фотографии (реальные белорусские отели, отдаются приложением)
function localPhoto(seed = 0) { return '/photos/h' + (1 + Math.abs(Math.trunc(seed)) % 9) + '.jpg'; }

// ---------- инфраструктура ----------

async function api(path, opts = {}) {
  const headers = { 'Content-Type': 'application/json', ...(opts.headers || {}) };
  if (state.token) headers['Authorization'] = 'Bearer ' + state.token;
  const res = await fetch(API + path, { ...opts, headers });
  if (res.status === 401) { logout(); throw new Error(t('needLogin')); }
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!res.ok) {
    const detail = data?.message || data?.title || data || res.statusText;
    throw new Error(typeof detail === 'object' ? JSON.stringify(detail) : detail);
  }
  return data;
}

function toast(msg, isErr = false) {
  const el = document.getElementById('toast');
  el.textContent = msg;
  el.className = 'toast show' + (isErr ? ' err' : '');
  clearTimeout(el._h);
  el._h = setTimeout(() => el.className = 'toast', 3500);
}

function esc(s) { return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }

function saveAuth(auth) {
  state.token = auth.token;
  state.user = { id: auth.id, username: auth.username, role: roleFromToken(auth.token) };
  localStorage.setItem('token', auth.token);
  localStorage.setItem('user', JSON.stringify(state.user));
  renderUserbox();
}

function roleFromToken(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    const raw = payload.role
      || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'];
    if (typeof raw === 'number') return raw;
    return { Guest: 1, Manager: 2, Admin: 3 }[raw] || 1;
  } catch { return 1; }
}

/* exported logout */
function logout() {
  state.token = null; state.user = null;
  localStorage.removeItem('token'); localStorage.removeItem('user');
  renderUserbox(); go('hotels');
}

function langSwitch() {
  return `<select class="lang" onchange="setLang(this.value)" title="Language / Мова / Język">
    ${Object.keys(I18N).map(l => `<option value="${l}" ${l === lang ? 'selected' : ''}>${I18N[l].langName}</option>`).join('')}
  </select>`;
}

function renderUserbox() {
  const box = document.getElementById('userbox');
  document.querySelectorAll('.auth-only').forEach(e => e.style.display = state.token ? '' : 'none');
  document.querySelectorAll('.admin-only').forEach(e => e.style.display = state.user?.role >= 3 ? '' : 'none');
  document.getElementById('nav-hotels').textContent = t('hotels');
  document.getElementById('nav-bookings').textContent = t('bookings');
  document.getElementById('nav-admin').textContent = t('admin');
  box.innerHTML = (state.token
    ? `<span class="who">${esc(state.user.username)} · ${ROLE_NAME(state.user.role)}</span>
       <button class="ghost" onclick="logout()">${t('logout')}</button>`
    : `<button class="ghost" onclick="authModal()">${t('login')}</button>`)
    + langSwitch();
}

/* exported go */
function go(view, hotelId = null) {
  state.view = view; state.hotelId = hotelId;
  const v = document.getElementById('view');
  v.innerHTML = `<div class="empty">${t('loading')}</div>`;
  ({ hotels: viewHotels, hotel: viewHotel, bookings: viewBookings, admin: viewAdmin }[view] || viewHotels)()
    .catch(e => v.innerHTML = `<div class="empty">${esc(e.message)}</div>`);
}

// ---------- каталог ----------

async function viewHotels() {
  document.getElementById('view').innerHTML = `
    <form class="searchbar" onsubmit="searchHotels(event)">
      <input id="s-loc" placeholder="${t('searchPlaceholder')}">
      <select id="s-rating">
        <option value="">${t('ratingAny')}</option>
        <option value="3.5">${t('ratingFrom', { v: '3.5' })}</option>
        <option value="4">${t('ratingFrom', { v: '4.0' })}</option>
        <option value="4.5">${t('ratingFrom', { v: '4.5' })}</option>
      </select>
      <input id="s-maxprice" type="number" min="0" placeholder="${t('maxPrice')}">
      <input id="s-guests" type="number" min="1" placeholder="${t('guests')}">
      <button type="submit">${t('find')}</button>
      <button type="button" class="ghost" onclick="fill('');document.querySelector('form.searchbar').requestSubmit()">${t('reset')}</button>
    </form>
    <div class="grid" id="hotels-grid"><div class="empty">${t('loading')}</div></div>`;
  searchHotels();
}

/* exported fill */
function fill(v) {
  ['s-loc', 's-rating', 's-maxprice', 's-guests'].forEach(id => document.getElementById(id).value = v);
}

/* exported searchHotels */
async function searchHotels(ev) {
  if (ev) ev.preventDefault();
  const q = new URLSearchParams();
  const loc = document.getElementById('s-loc')?.value.trim();
  const rating = document.getElementById('s-rating')?.value;
  const price = document.getElementById('s-maxprice')?.value;
  q.set('PageSize', '50');
  q.set('SortBy', 'Rating');
  q.set('SortDescending', 'true');
  if (loc) q.set('Location', loc);
  if (rating) q.set('MinRating', rating);
  if (price) q.set('MaxPrice', price);

  const grid = document.getElementById('hotels-grid');
  try {
    const r = await api('/api/hotels/search?' + q.toString());
    const list = r.hotels || r || [];
    if (!list.length) { grid.innerHTML = `<div class="empty">${t('emptyCatalog')}</div>`; return; }
    grid.innerHTML = list.map(hotelCard).join('');
  } catch (e) {
    const all = await api('/api/hotels');
    const list = Array.isArray(all) ? all : (all.hotels || []);
    grid.innerHTML = list.map(hotelCard).join('') || `<div class="empty">${t('emptyCatalog')}</div>`;
  }
}

function hotelCard(h) {
  const photo = (h.photos && (h.photos[0]?.url || h.photos[0])) || localPhoto(h.id);
  const amenities = (h.amenities || []).slice(0, 3).map(a => `<span class="chip">${esc(a)}</span>`).join('');
  return `<div class="hotel-card" onclick="go('hotel', ${h.id})">
    <img src="${esc(photo)}" alt="" loading="lazy" onerror="this.src='${localPhoto(h.id)}'">
    <div class="body">
      <h3>${esc(h.name)}</h3>
      <div class="loc">📍 ${esc(h.location || '')}</div>
      <div><span class="stars">★ ${Number(h.rating).toFixed(1)}</span> ${h.roomTypes?.length ? `<span class="muted">· ${h.roomTypes.length} ${t('roomTypesCount')}</span>` : ''}</div>
      ${amenities ? `<div class="chips">${amenities}</div>` : ''}
      <div class="price">${t('fromPrice')} ${Number(h.basePrice ?? 0).toFixed(0)} ${t('currency')} <small>${t('perNight')}</small></div>
    </div>
  </div>`;
}

// ---------- отель ----------

async function viewHotel() {
  const id = state.hotelId;
  const h = await api('/api/hotels/' + id);
  const photos = (h.photos?.length ? h.photos : await api('/api/hotelphotos/hotel/' + id).catch(() => [])) || [];
  const roomTypes = (h.roomTypes?.length ? [...h.roomTypes] : await api('/api/room-types/by-hotel/' + id).catch(() => [])) || [];

  const photoUrls = photos.slice(0, 3).map(p => p.url || p);
  while (photoUrls.length < 3) photoUrls.push(localPhoto(id + photoUrls.length * 3));

  document.getElementById('view').innerHTML = `
    <div class="page-head"><span class="back" onclick="go('hotels')">${t('backToCatalog')}</span></div>
    <h2 style="margin:0 0 6px">${esc(h.name)}</h2>
    <div class="muted" style="margin-bottom:14px">📍 ${esc(h.location || '')} · <span class="stars">★ ${Number(h.rating).toFixed(1)}</span></div>
    <div class="photos">${photoUrls.map(u => `<img src="${esc(u)}" onerror="this.src='${localPhoto(id)}'">`).join('')}</div>
    <div class="card">
      <p style="margin:0">${esc(h.description || '')}</p>
      ${(h.amenities || []).length ? `<div class="chips mt">${h.amenities.map(a => `<span class="chip">${esc(a)}</span>`).join('')}</div>` : ''}
    </div>
    <div class="card">
      <h3 style="margin-top:0">${t('rooms')}</h3>
      <table>
        <thead><tr><th>${t('colRoom')}</th><th>${t('colGuests')}</th><th>${t('areaUnit')}</th><th>${t('colPrice')}</th><th></th></tr></thead>
        <tbody>
          ${roomTypes.map(rt => `
            <tr>
              <td><b>${esc(rt.name)}</b><div class="muted" style="font-size:12.5px">${(rt.amenities || []).join(', ')}</div></td>
              <td>${t('guestsUpTo')} ${rt.capacity}</td>
              <td>${rt.area ?? '—'} ${t('areaUnit')}</td>
              <td><b>${Number(rt.basePrice).toFixed(0)}</b> ${t('currency')}</td>
              <td><button onclick="bookModal(${h.id}, ${rt.id}, ${rt.capacity}, ${rt.basePrice})">${t('book')}</button></td>
            </tr>`).join('')}
        </tbody>
      </table>
    </div>`;
}

// ---------- бронирование ----------

/* exported bookModal */
function bookModal(hotelId, roomTypeId, capacity, basePrice) {
  if (!state.token) { toast(t('loginRequired'), true); authModal(); return; }
  const today = new Date().toISOString().slice(0, 10);
  const tomorrow = new Date(Date.now() + 864e5).toISOString().slice(0, 10);
  openModal(`
    <h3>${t('bookingTitle')}</h3>
    <form onsubmit="createBooking(event, ${hotelId}, ${roomTypeId}, ${capacity})">
      <div class="row"><label>${t('checkIn')}</label><input type="date" id="b-in" value="${today}" min="${today}" required></div>
      <div class="row"><label>${t('checkOut')}</label><input type="date" id="b-out" value="${tomorrow}" min="${tomorrow}" required></div>
      <div class="row"><label>${t('guestsLabel', { n: capacity })}</label><input type="number" id="b-guests" min="1" max="${capacity}" value="1" required></div>
      <div class="muted">${t('basePriceNote', { p: Number(basePrice).toFixed(0) })}</div>
      <button type="submit" style="width:100%;margin-top:14px">${t('bookAction')}</button>
    </form>`);
}

/* exported createBooking */
async function createBooking(ev, hotelId, roomTypeId, capacity) {
  ev.preventDefault();
  const checkIn = document.getElementById('b-in').value;
  const checkOut = document.getElementById('b-out').value;
  const guests = +document.getElementById('b-guests').value;
  if (new Date(checkOut) <= new Date(checkIn)) return toast(t('datesInvalid'), true);
  if (guests > capacity) return toast(t('capacityInvalid'), true);
  try {
    const b = await api('/api/bookings', {
      method: 'POST',
      body: JSON.stringify({
        hotelId, roomTypeId, userId: state.user.id,
        checkInDate: checkIn, checkOutDate: checkOut, guestCount: guests,
      }),
    });
    closeModal();
    toast(t('booked', { p: Number(b.totalPrice ?? 0).toFixed(0) }));
    go('bookings');
  } catch (e) { toast(e.message, true); }
}

async function viewBookings() {
  if (!state.token) { authModal(); throw new Error(''); }
  const list = await api('/api/bookings/my');
  const arr = Array.isArray(list) ? list : (list?.$values || []);
  document.getElementById('view').innerHTML = `
    <div class="page-head"><h2>${t('myBookingsTitle')}</h2></div>
    <div class="card">
      ${arr.length ? `<table>
        <thead><tr><th>${t('colNum')}</th><th>${t('colHotel')}</th><th>${t('colRoom')}</th><th>${t('colIn')}</th><th>${t('colOut')}</th><th>${t('colGuests')}</th><th>${t('colPrice')}</th><th>${t('colStatus')}</th><th></th></tr></thead>
        <tbody>${arr.map(bookingRow).join('')}</tbody>
      </table>` : `<div class="empty">${t('emptyBookings')}</div>`}
    </div>`;
}

function bookingRow(b) {
  const [name, cls] = statusInfo(b.status);
  const active = ['Pending', 'Active', 'Confirmed'].includes(b.status) || [1, 2, 3].includes(b.status);
  return `<tr>
    <td>${b.id}</td>
    <td><b>${esc(b.hotelName || '')}</b></td>
    <td>${esc(b.roomTypeName || '')}</td>
    <td>${(b.checkInDate || '').slice(0, 10)}</td>
    <td>${(b.checkOutDate || '').slice(0, 10)}</td>
    <td>${b.guestCount ?? ''}</td>
    <td><b>${Number(b.totalPrice ?? 0).toFixed(0)}</b> ${t('currency')}</td>
    <td><span class="badge ${cls}">${name}</span></td>
    <td>${active ? `<button class="ghost" onclick="cancelBooking(${b.id})">${t('cancel')}</button>` : ''}</td>
  </tr>`;
}

function confirmModal(title, text, onConfirm, confirmLabel) {
  openModal(`
    <h3>${esc(title)}</h3>
    <p class="muted" style="margin-top:0">${esc(text)}</p>
    <div class="row-flex" style="justify-content:flex-end">
      <button class="ghost" onclick="closeModal()">${t('keep')}</button>
      <button class="danger" id="confirm-yes">${esc(confirmLabel || t('cancelBookingBtn'))}</button>
    </div>`);
  document.getElementById('confirm-yes').onclick = async () => {
    document.getElementById('confirm-yes').disabled = true;
    await onConfirm();
    closeModal();
  };
}

/* exported cancelBooking */
async function cancelBooking(id) {
  confirmModal(t('cancelTitle'), t('cancelText', { id }), async () => {
    try { await api('/api/bookings/' + id, { method: 'DELETE' }); toast(t('bookingCancelled')); go('bookings'); }
    catch (e) { toast(e.message, true); }
  });
}

// ---------- админ ----------

async function viewAdmin() {
  if (!state.user || state.user.role < 3) throw new Error(t('adminOnly'));
  const [users, logs] = await Promise.all([
    api('/api/users'),
    api('/api/logs').catch(() => []),
  ]);
  const u = Array.isArray(users) ? users : (users?.$values || []);
  const l = Array.isArray(logs) ? logs : (logs?.$values || []);
  document.getElementById('view').innerHTML = `
    <div class="page-head"><h2>${t('usersTitle')}</h2></div>
    <div class="card"><table>
      <thead><tr><th>${t('colID')}</th><th>${t('colLogin')}</th><th>${t('colEmail')}</th><th>${t('colName')}</th><th>${t('colRole')}</th><th>${t('colCreated')}</th></tr></thead>
      <tbody>${u.map(x => `<tr>
        <td>${x.id}</td><td><b>${esc(x.username)}</b></td><td>${esc(x.email)}</td>
        <td>${esc((x.firstName || '') + ' ' + (x.lastName || ''))}</td>
        <td><span class="badge ${x.role === 3 ? 'b3' : x.role === 2 ? 'b2' : 'b4'}">${esc(ROLE_NAME(x.role) || x.role)}</span></td>
        <td class="muted">${(x.createdAt || '').slice(0, 10)}</td>
      </tr>`).join('')}</tbody>
    </table></div>
    <div class="page-head"><h2>${t('auditTitle')}</h2></div>
    <div class="card"><table>
      <thead><tr><th>${t('colTime')}</th><th>${t('colUser')}</th><th>${t('colEvent')}</th><th>${t('colSuccess')}</th><th>${t('colMessage')}</th></tr></thead>
      <tbody>${l.slice(0, 30).map(x => `<tr>
        <td class="muted">${(x.timestamp || x.createdAt || '').slice(0, 19).replace('T', ' ')}</td>
        <td>${x.userId ?? '—'}</td>
        <td><span class="chip">${esc(x.eventType || x.userActionType || '—')}</span></td>
        <td>${x.isSuccess ?? '—'}</td>
        <td class="muted">${esc((x.message || '').slice(0, 60))}</td>
      </tr>`).join('')}</tbody>
    </table></div>`;
}

// ---------- аутентификация ----------

/* exported authModal */
function authModal(tab = 'login') {
  openModal(`
    <div class="tabs">
      <button class="${tab === 'login' ? 'active' : ''}" onclick="authModal('login')">${t('loginTab')}</button>
      <button class="${tab === 'reg' ? 'active' : ''}" onclick="authModal('reg')">${t('registerTab')}</button>
    </div>
    ${tab === 'login' ? `
      <form onsubmit="doLogin(event)">
        <div class="row"><label>${t('email')}</label><input type="email" id="l-email" value="user1@booking.local" required></div>
        <div class="row"><label>${t('password')}</label><input type="password" id="l-pass" value="User123!" required></div>
        <button type="submit" style="width:100%">${t('login')}</button>
      </form>
      <p class="muted" style="font-size:12.5px;margin-bottom:0">${t('demoHint')}</p>
    ` : `
      <form onsubmit="doRegister(event)">
        <div class="row"><label>${t('username')}</label><input id="r-username" required></div>
        <div class="row"><label>${t('email')}</label><input type="email" id="r-email" required></div>
        <div class="row"><label>${t('passwordHint')}</label><input type="password" id="r-pass" minlength="6" required></div>
        <div class="row"><label>${t('firstName')}</label><input id="r-first"></div>
        <div class="row"><label>${t('lastName')}</label><input id="r-last"></div>
        <div class="row"><label>${t('phone')}</label><input id="r-phone" placeholder="+375..."></div>
        <button type="submit" style="width:100%">${t('createAccount')}</button>
      </form>`}`);
}

/* exported doLogin */
async function doLogin(ev) {
  ev.preventDefault();
  try {
    const auth = await api('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({
        email: document.getElementById('l-email').value,
        password: document.getElementById('l-pass').value,
      }),
    });
    saveAuth(auth);
    closeModal();
    toast(t('welcome', { u: auth.username }));
    go(state.view, state.hotelId);
  } catch (e) { toast(e.message, true); }
}

/* exported doRegister */
async function doRegister(ev) {
  ev.preventDefault();
  try {
    const auth = await api('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({
        username: document.getElementById('r-username').value,
        email: document.getElementById('r-email').value,
        password: document.getElementById('r-pass').value,
        firstName: document.getElementById('r-first').value,
        lastName: document.getElementById('r-last').value,
        phoneNumber: document.getElementById('r-phone').value,
      }),
    });
    saveAuth(auth);
    closeModal();
    toast(t('accountCreated'));
    go(state.view, state.hotelId);
  } catch (e) { toast(e.message, true); }
}

// ---------- модальное окно ----------

function openModal(html) {
  document.getElementById('modal-body').innerHTML = html;
  document.getElementById('modal').classList.remove('hidden');
}
/* exported closeModal */
function closeModal() { document.getElementById('modal').classList.add('hidden'); }

// ---------- старт ----------

// роль всегда берём из токена (localStorage мог остаться со старой)
if (state.token && state.user) state.user.role = roleFromToken(state.token);
localStorage.setItem('user', JSON.stringify(state.user));

renderUserbox();
go('hotels');
