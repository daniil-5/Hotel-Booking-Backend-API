-- ============================================================
-- Наполнение тестовыми данными
-- 10 пользователей, 40 отелей, ~160 типов номеров,
-- календарь цен на 60 дней, бронирования, фото, журнал аудита
-- ============================================================

-- Пароли: admin@booking.local / Admin123!, manager* / Manager123!,
--         user* / User123! (BCrypt)
INSERT INTO users (id, username, email, password_hash, first_name, last_name, phone_number, role) VALUES
(1,  'admin',    'admin@booking.local',    '$2a$11$/5qUbY.V3SlpOaJwvitJw.hfsB4D18YQRBbnNGnS6ykGGCuOPlkwG', 'Евгения',  'Кожемяко',  '+375291000001', 3),
(2,  'manager1', 'manager1@booking.local', '$2a$11$Y5afFV48a7zoaJQYzNRDXeIiylnvy3d0dEZpelo3YhFZY7thtNZ5K', 'Ольга',     'Менеджерова', '+375291000002', 2),
(3,  'manager2', 'manager2@booking.local', '$2a$11$Y5afFV48a7zoaJQYzNRDXeIiylnvy3d0dEZpelo3YhFZY7thtNZ5K', 'Пётр',      'Смирнов',     '+375291000003', 2),
(4,  'user1',    'user1@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Даниил',    'Марьин',      '+375291000004', 1),
(5,  'user2',    'user2@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Дмитрий',   'Себелев',     '+375291000005', 1),
(6,  'user3',    'user3@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Анна',      'Кузнецова',   '+375291000006', 1),
(7,  'user4',    'user4@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Иван',      'Петров',      '+375291000007', 1),
(8,  'user5',    'user5@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Мария',     'Иванова',     '+375291000008', 1),
(9,  'user6',    'user6@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Сергей',    'Волков',      '+375291000009', 1),
(10, 'user7',    'user7@booking.local',    '$2a$11$i12f3yc/qtPZsvRWaOxfPO1p3/sFjTdlfvXgyrZTKOD/aimovsrhm', 'Елена',     'Новикова',    '+375291000010', 1);
SELECT setval('users_id_seq', 10);

-- 40 отелей в 15 городах
INSERT INTO hotels (id, name, description, location, rating, base_price, amenities)
SELECT
    s,
    (ARRAY['Grand', 'Royal', 'Park', 'City', 'Golden', 'Blue', 'Northern', 'Sun', 'River', 'Old'])[1 + (s - 1) % 10]
        || ' '
        || (ARRAY['Plaza', 'Palace', 'Inn', 'Hotel & Spa', 'Suites', 'Tower', 'Garden', 'View', 'House', 'Crown'])[1 + (s + (s - 1) / 10) % 10],
    'Комфортный отель в самом центре города, принимает гостей круглосуточно, завтрак включён.',
    (ARRAY['Минск', 'Гомель', 'Брест', 'Витебск', 'Гродно', 'Могилёв',
           'Мозырь', 'Бобруйск', 'Барановичи', 'Борисов', 'Орша', 'Пинск',
           'Полоцк', 'Лида', 'Солигорск'])[1 + (s - 1) % 15],
    ROUND((3.4 + ((s * 7) % 17) * 0.1)::numeric, 2),
    60 + ((s * 37) % 41) * 5,
    (ARRAY[
        '["Wi-Fi", "Парковка", "Завтрак"]',
        '["Wi-Fi", "Бассейн", "СПА", "Фитнес"]',
        '["Wi-Fi", "Прокат авто", "Конференц-зал"]',
        '["Wi-Fi", "Ресторан", "Бар", "Терраса"]',
        '["Wi-Fi", "Домашние животные", "Парковка", "Завтрак"]'
     ]::jsonb[])[1 + (s - 1) % 5]
FROM generate_series(1, 40) AS s;
SELECT setval('hotels_id_seq', 40);

-- Фотографии: по 3 на отель, первая — главная
INSERT INTO hotel_photos (hotel_id, url, public_id, description, is_main)
SELECT h.id,
       (ARRAY[    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228682/booking_demo/h1.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228687/booking_demo/h2.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228689/booking_demo/h3.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228691/booking_demo/h4.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228693/booking_demo/h5.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228695/booking_demo/h6.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228697/booking_demo/h7.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228699/booking_demo/h8.jpg',
    'https://res.cloudinary.com/dcnfujewt/image/upload/v1791228701/booking_demo/h9.jpg'
       ])[1 + (h.id * 7 + p) % 9],
       (ARRAY[    'booking_demo/h1',
    'booking_demo/h2',
    'booking_demo/h3',
    'booking_demo/h4',
    'booking_demo/h5',
    'booking_demo/h6',
    'booking_demo/h7',
    'booking_demo/h8',
    'booking_demo/h9'
       ])[1 + (h.id * 7 + p) % 9],
       'Фото отеля №' || p,
       (p = 1)
FROM hotels h
CROSS JOIN generate_series(1, 3) AS p;

-- Типы номеров: 4 на каждый отель
INSERT INTO room_types (name, description, capacity, area, floor, hotel_id, base_price, amenities)
SELECT
    t.name,
    t.name || ': просторный номер с всеми удобствами',
    t.capacity + (h.id % 2),
    t.area + (h.id % 5),
    2 + ((h.id * 3) % 12),
    h.id,
    ROUND((h.base_price * t.mult)::numeric, 2),
    t.amen
FROM hotels h
CROSS JOIN (VALUES
    ('Стандарт',  2, 22.0, 1.0,  '["Wi-Fi", "ТВ", "Кондиционер"]'::jsonb),
    ('Бизнес',    2, 28.0, 1.4,  '["Wi-Fi", "Рабочее место", "Мини-бар"]'::jsonb),
    ('Люкс',      3, 38.0, 2.0,  '["Wi-Fi", "Джакузи", "Гостиная", "Мини-бар"]'::jsonb),
    ('Семейный',  4, 48.0, 1.75, '["Wi-Fi", "2 комнаты", "Кухня"]'::jsonb)
) AS t(name, capacity, area, mult, amen);

-- Календарь цен на 60 дней вперёд: выходные дороже
INSERT INTO room_pricings (room_type_id, date, price)
SELECT rt.id,
       CURRENT_DATE + d,
       ROUND((rt.base_price
              * CASE WHEN EXTRACT(ISODOW FROM CURRENT_DATE + d) IN (6, 7)
                     THEN 1.25 ELSE 0.95 + ((rt.id + d) % 4) * 0.05 END
             )::numeric, 2)
FROM room_types rt
CROSS JOIN generate_series(0, 59) AS d;

-- Активные бронирования: два непересекающихся окна у каждой третьей комнаты
INSERT INTO bookings (user_id, hotel_id, room_type_id,
                      check_in_date, check_out_date, guest_count,
                      total_price, status, created_at)
SELECT 4 + (rt.id % 7),
       rt.hotel_id,
       rt.id,
       CURRENT_DATE + 3,
       CURRENT_DATE + 3 + 4 + (rt.id % 3),
       1 + (rt.id % 2),
       calc_booking_price(rt.id,
                          CURRENT_DATE + 3,
                          CURRENT_DATE + 7 + (rt.id % 3)),
       1 + (rt.id % 3),
       CURRENT_TIMESTAMP - (rt.id % 14) * INTERVAL '1 day'
FROM room_types rt
WHERE rt.id % 3 = 0;

INSERT INTO bookings (user_id, hotel_id, room_type_id,
                      check_in_date, check_out_date, guest_count,
                      total_price, status, created_at)
SELECT 4 + (rt.id % 7),
       rt.hotel_id,
       rt.id,
       CURRENT_DATE + 20,
       CURRENT_DATE + 24 + (rt.id % 3),
       2,
       calc_booking_price(rt.id,
                          CURRENT_DATE + 20,
                          CURRENT_DATE + 24 + (rt.id % 3)),
       1,
       CURRENT_TIMESTAMP - (rt.id % 10) * INTERVAL '1 day'
FROM room_types rt
WHERE rt.id % 4 = 1;

-- Исторические бронирования (завершённые и отменённые)
INSERT INTO bookings (user_id, hotel_id, room_type_id,
                      check_in_date, check_out_date, guest_count,
                      total_price, status, created_at)
SELECT 4 + (rt.id % 7),
       rt.hotel_id,
       rt.id,
       CURRENT_DATE - 60 - (rt.id % 20),
       CURRENT_DATE - 55 - (rt.id % 20),
       1 + (rt.id % 3),
       ROUND((rt.base_price * 5)::numeric, 2),
       CASE WHEN rt.id % 2 = 0 THEN 5 ELSE 4 END,
       CURRENT_TIMESTAMP - INTERVAL '70 days'
FROM room_types rt
WHERE rt.id % 5 IN (0, 2);

-- Журнал аудита: примеры событий
INSERT INTO user_action_audit (user_id, user_action_type, is_success, created_at)
SELECT 1 + (i % 10),
       (ARRAY['UserLogin', 'UserCreated', 'UserUpdated', 'HotelCreated',
              'HotelUpdated', 'BookingCreated', 'BookingUpdated',
              'BookingDeleted', 'RoomTypesCreated', 'RoomPricingsUpdated',
              'ChangePassword', 'UserLogout']::user_action_type[])[1 + (i % 12)],
       (i % 9) <> 0,
       CURRENT_TIMESTAMP - (i % 30) * INTERVAL '1 day'
FROM generate_series(1, 60) AS i;
