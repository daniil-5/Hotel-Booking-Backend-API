-- ============================================================
-- Система бронирования отелей — схема базы данных
-- СУБД: PostgreSQL 15+
-- Статусы бронирования: 1 ожидает, 2 активно, 3 подтверждено,
--                        4 отменено, 5 завершено
-- Роли: 1 гость, 2 менеджер, 3 администратор
-- ============================================================

CREATE EXTENSION IF NOT EXISTS btree_gist;

DROP VIEW IF EXISTS v_hotel_statistics, v_active_bookings;
DROP TABLE IF EXISTS user_action_audit, bookings, hotel_photos,
                     room_pricings, room_types, hotels, users, role CASCADE;
DROP TYPE IF EXISTS user_action_type;

-- Перечисление типов действий пользователя (журнал аудита)
CREATE TYPE user_action_type AS ENUM (
    'UserLogin', 'UserCreated', 'UserUpdated', 'UserDeleted',
    'UserLogout', 'ChangePassword', 'HotelCreated', 'HotelUpdated',
    'HotelDeleted', 'BookingCreated', 'BookingUpdated', 'BookingDeleted',
    'RoomTypesCreated', 'RoomTypesUpdated', 'RoomTypesDeleted',
    'RoomPricingsCreated', 'RoomPricingsUpdated', 'RoomPricingsDeleted',
    'Unknown'
);

-- Справочник ролей (значения соответствуют перечислению приложения)
CREATE TABLE role (
    id        INTEGER PRIMARY KEY,
    role_name TEXT NOT NULL
);

-- Пользователи
CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    username      TEXT NOT NULL UNIQUE,
    email         TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    first_name    TEXT,
    last_name     TEXT,
    phone_number  TEXT,
    role          INTEGER NOT NULL DEFAULT 1
                  REFERENCES role (id),
    created_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted    BOOLEAN DEFAULT FALSE
);

-- Отели
CREATE TABLE hotels (
    id          SERIAL PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT,
    location    TEXT,
    rating      NUMERIC(3, 2) CHECK (rating >= 0 AND rating <= 5),
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted  BOOLEAN DEFAULT FALSE,
    base_price  NUMERIC(10, 2),
    amenities   JSONB
);

-- Типы номеров
CREATE TABLE room_types (
    id          SERIAL PRIMARY KEY,
    name        TEXT NOT NULL,
    description TEXT,
    capacity    INTEGER CHECK (capacity > 0),
    area        NUMERIC(6, 2),
    floor       INTEGER,
    hotel_id    INTEGER NOT NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted  BOOLEAN DEFAULT FALSE,
    base_price  NUMERIC(10, 2),
    amenities   JSONB,
    CONSTRAINT fk_room_type_hotel FOREIGN KEY (hotel_id)
        REFERENCES hotels (id) ON DELETE CASCADE
);

-- Календарь сезонных цен
CREATE TABLE room_pricings (
    id           SERIAL PRIMARY KEY,
    date         TIMESTAMP NOT NULL,
    price        NUMERIC(10, 2) NOT NULL CHECK (price >= 0),
    created_at   TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at   TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted   BOOLEAN DEFAULT FALSE,
    room_type_id INTEGER NOT NULL,
    CONSTRAINT fk_room_pricing_room_type FOREIGN KEY (room_type_id)
        REFERENCES room_types (id) ON DELETE CASCADE,
    CONSTRAINT unique_room_pricing_date UNIQUE (room_type_id, date)
);

-- Фотографии отелей
CREATE TABLE hotel_photos (
    id          SERIAL PRIMARY KEY,
    hotel_id    INTEGER NOT NULL,
    url         TEXT NOT NULL,
    description TEXT,
    is_main     BOOLEAN DEFAULT FALSE,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted  BOOLEAN DEFAULT FALSE,
    public_id   TEXT,
    CONSTRAINT fk_hotel_photo_hotel FOREIGN KEY (hotel_id)
        REFERENCES hotels (id) ON DELETE CASCADE
);

-- Бронирования
CREATE TABLE bookings (
    id             SERIAL PRIMARY KEY,
    user_id        INTEGER NOT NULL,
    check_in_date  TIMESTAMP NOT NULL,
    check_out_date TIMESTAMP NOT NULL,
    guest_count    INTEGER CHECK (guest_count > 0),
    total_price    NUMERIC(10, 2) NOT NULL CHECK (total_price >= 0),
    status         INTEGER NOT NULL DEFAULT 1
                   CHECK (status BETWEEN 1 AND 5),
    created_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted     BOOLEAN DEFAULT FALSE,
    hotel_id       INTEGER NOT NULL,
    room_type_id   INTEGER NOT NULL,
    CONSTRAINT fk_booking_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_hotel FOREIGN KEY (hotel_id)
        REFERENCES hotels (id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_room_type FOREIGN KEY (room_type_id)
        REFERENCES room_types (id) ON DELETE CASCADE,
    CONSTRAINT check_dates CHECK (check_out_date > check_in_date),
    -- Запрет пересечения активных бронирований одного типа номера
    CONSTRAINT no_overlap_active_bookings EXCLUDE USING gist (
        room_type_id WITH =,
        daterange(check_in_date::date, check_out_date::date) WITH &&
    ) WHERE (is_deleted = FALSE AND status IN (1, 2, 3))
);

-- Журнал аудита действий пользователей
CREATE TABLE user_action_audit (
    id               SERIAL PRIMARY KEY,
    user_id          INTEGER NOT NULL,
    user_action_type user_action_type NOT NULL DEFAULT 'Unknown',
    is_success       BOOLEAN DEFAULT FALSE,
    created_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted       BOOLEAN DEFAULT FALSE,
    CONSTRAINT fk_user_action_user FOREIGN KEY (user_id)
        REFERENCES users (id) ON DELETE CASCADE
);

-- Индексы
CREATE INDEX idx_users_email               ON users (email);
CREATE INDEX idx_users_username            ON users (username);
CREATE INDEX idx_users_role_active         ON users (role) WHERE is_deleted = FALSE;
CREATE INDEX idx_room_types_hotel_id       ON room_types (hotel_id);
CREATE INDEX idx_room_pricings_room_type   ON room_pricings (room_type_id);
CREATE INDEX idx_room_pricings_date        ON room_pricings (date);
CREATE INDEX idx_hotel_photos_hotel_id     ON hotel_photos (hotel_id);
CREATE INDEX idx_bookings_user_id          ON bookings (user_id);
CREATE INDEX idx_bookings_hotel_id         ON bookings (hotel_id);
CREATE INDEX idx_bookings_room_type_id     ON bookings (room_type_id);
CREATE INDEX idx_bookings_dates            ON bookings (check_in_date, check_out_date);
CREATE INDEX idx_user_action_audit_user_id ON user_action_audit (user_id);
CREATE INDEX idx_hotels_location_active    ON hotels (location) WHERE is_deleted = FALSE;
CREATE INDEX idx_hotels_rating_active      ON hotels (rating DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_hotels_name_active        ON hotels (name) WHERE is_deleted = FALSE;

-- ============================================================
-- Триггеры
-- ============================================================

-- Автоматическое обновление updated_at
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_hotels_updated_at
    BEFORE UPDATE ON hotels
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_room_types_updated_at
    BEFORE UPDATE ON room_types
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_room_pricings_updated_at
    BEFORE UPDATE ON room_pricings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_hotel_photos_updated_at
    BEFORE UPDATE ON hotel_photos
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_bookings_updated_at
    BEFORE UPDATE ON bookings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_user_action_audit_updated_at
    BEFORE UPDATE ON user_action_audit
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- Аудит изменений бронирований
CREATE OR REPLACE FUNCTION audit_booking_change()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO user_action_audit (user_id, user_action_type, is_success)
        VALUES (NEW.user_id, 'BookingCreated', TRUE);
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' AND OLD.status <> NEW.status THEN
        INSERT INTO user_action_audit (user_id, user_action_type, is_success)
        VALUES (NEW.user_id, 'BookingUpdated', TRUE);
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO user_action_audit (user_id, user_action_type, is_success)
        VALUES (OLD.user_id, 'BookingDeleted', TRUE);
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_bookings_audit
    AFTER INSERT OR UPDATE OR DELETE ON bookings
    FOR EACH ROW EXECUTE FUNCTION audit_booking_change();

-- Единственность главной фотографии отеля
CREATE OR REPLACE FUNCTION check_single_main_photo()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.is_main THEN
        UPDATE hotel_photos
        SET is_main = FALSE
        WHERE hotel_id = NEW.hotel_id AND id <> NEW.id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_hotel_photos_single_main
    BEFORE INSERT OR UPDATE ON hotel_photos
    FOR EACH ROW EXECUTE FUNCTION check_single_main_photo();

-- ============================================================
-- Хранимые процедуры и функции
-- ============================================================

-- Стоимость бронирования: сезонные цены, при отсутствии — базовая
CREATE OR REPLACE FUNCTION calc_booking_price(
    p_room_type_id INTEGER,
    p_check_in     DATE,
    p_check_out    DATE
) RETURNS NUMERIC AS $$
DECLARE
    v_base_price NUMERIC;
    v_total      NUMERIC := 0;
    v_seasonal   NUMERIC;
    d            DATE;
BEGIN
    SELECT base_price INTO v_base_price
    FROM room_types
    WHERE id = p_room_type_id AND is_deleted = FALSE;

    IF v_base_price IS NULL THEN
        RAISE EXCEPTION 'Тип номера % не найден', p_room_type_id;
    END IF;

    d := p_check_in;
    WHILE d < p_check_out LOOP
        SELECT price INTO v_seasonal
        FROM room_pricings
        WHERE room_type_id = p_room_type_id
          AND date::date = d
          AND is_deleted = FALSE;

        v_total := v_total + COALESCE(v_seasonal, v_base_price);
        d := d + 1;
    END LOOP;

    RETURN v_total;
END;
$$ LANGUAGE plpgsql;

-- Создание бронирования с проверками бизнес-правил
CREATE OR REPLACE PROCEDURE sp_create_booking(
    p_user_id      INTEGER,
    p_hotel_id     INTEGER,
    p_room_type_id INTEGER,
    p_check_in     DATE,
    p_check_out    DATE,
    p_guest_count  INTEGER,
    INOUT p_booking_id INTEGER DEFAULT NULL
) AS $$
DECLARE
    v_capacity INTEGER;
    v_hotel_of INTEGER;
    v_overlap  INTEGER;
    v_price    NUMERIC;
BEGIN
    IF p_check_out <= p_check_in THEN
        RAISE EXCEPTION 'Дата выезда должна быть позже даты заезда';
    END IF;

    SELECT hotel_id, capacity INTO v_hotel_of, v_capacity
    FROM room_types
    WHERE id = p_room_type_id AND is_deleted = FALSE;

    IF v_hotel_of IS NULL THEN
        RAISE EXCEPTION 'Тип номера % не найден', p_room_type_id;
    END IF;
    IF v_hotel_of <> p_hotel_id THEN
        RAISE EXCEPTION 'Тип номера не принадлежит отелю %', p_hotel_id;
    END IF;
    IF p_guest_count > v_capacity THEN
        RAISE EXCEPTION 'Вместимость номера — % гостей', v_capacity;
    END IF;

    SELECT COUNT(*) INTO v_overlap
    FROM bookings b
    WHERE b.room_type_id = p_room_type_id
      AND b.is_deleted = FALSE
      AND b.status IN (1, 2, 3)
      AND daterange(p_check_in, p_check_out) && daterange(
              b.check_in_date::date, b.check_out_date::date);

    IF v_overlap > 0 THEN
        RAISE EXCEPTION 'Номер занят на выбранные даты';
    END IF;

    v_price := calc_booking_price(p_room_type_id, p_check_in, p_check_out);

    INSERT INTO bookings (user_id, hotel_id, room_type_id,
                          check_in_date, check_out_date,
                          guest_count, total_price, status)
    VALUES (p_user_id, p_hotel_id, p_room_type_id,
            p_check_in, p_check_out,
            p_guest_count, v_price, 1)
    RETURNING id INTO p_booking_id;
END;
$$ LANGUAGE plpgsql;

-- Отмена бронирования владельцем или администратором
CREATE OR REPLACE PROCEDURE sp_cancel_booking(
    p_booking_id INTEGER,
    p_user_id    INTEGER,
    p_is_admin   BOOLEAN DEFAULT FALSE
) AS $$
DECLARE
    v_owner  INTEGER;
    v_status INTEGER;
BEGIN
    SELECT user_id, status INTO v_owner, v_status
    FROM bookings
    WHERE id = p_booking_id AND is_deleted = FALSE;

    IF v_owner IS NULL THEN
        RAISE EXCEPTION 'Бронирование % не найдено', p_booking_id;
    END IF;
    IF NOT p_is_admin AND v_owner <> p_user_id THEN
        RAISE EXCEPTION 'Нет прав на отмену бронирования';
    END IF;
    IF v_status = 4 THEN
        RAISE EXCEPTION 'Бронирование уже отменено';
    END IF;

    UPDATE bookings SET status = 4 WHERE id = p_booking_id;
END;
$$ LANGUAGE plpgsql;

-- Статистика по отелям
CREATE OR REPLACE FUNCTION get_hotel_statistics()
RETURNS TABLE (
    hotel_id           INTEGER,
    hotel_name         TEXT,
    location           TEXT,
    total_bookings     BIGINT,
    confirmed_bookings BIGINT,
    cancelled_bookings BIGINT,
    total_revenue      NUMERIC,
    average_price      NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT h.id, h.name, h.location,
           COUNT(b.id),
           COUNT(CASE WHEN b.status = 3 THEN 1 END),
           COUNT(CASE WHEN b.status = 4 THEN 1 END),
           COALESCE(SUM(b.total_price), 0),
           COALESCE(ROUND(AVG(b.total_price), 2), 0)
    FROM hotels h
    LEFT JOIN bookings b
        ON b.hotel_id = h.id AND b.is_deleted = FALSE
    WHERE h.is_deleted = FALSE
    GROUP BY h.id, h.name, h.location
    ORDER BY total_revenue DESC;
END;
$$ LANGUAGE plpgsql;

-- История бронирования пользователя
CREATE OR REPLACE FUNCTION get_user_booking_history(
    p_user_id INTEGER
) RETURNS TABLE (
    user_id               INTEGER,
    username              TEXT,
    full_name             TEXT,
    total_bookings        BIGINT,
    cancelled_bookings    BIGINT,
    total_spent           NUMERIC,
    average_booking_value NUMERIC,
    first_booking_date    TIMESTAMP,
    last_booking_date     TIMESTAMP,
    unique_hotels         BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.username,
           COALESCE(u.first_name, '') || ' ' || COALESCE(u.last_name, ''),
           COUNT(b.id),
           COUNT(CASE WHEN b.status = 4 THEN 1 END),
           COALESCE(SUM(b.total_price), 0),
           COALESCE(ROUND(AVG(b.total_price), 2), 0),
           MIN(b.created_at),
           MAX(b.created_at),
           COUNT(DISTINCT b.hotel_id)
    FROM users u
    LEFT JOIN bookings b
        ON b.user_id = u.id AND b.is_deleted = FALSE
    WHERE u.id = p_user_id AND u.is_deleted = FALSE
    GROUP BY u.id, u.username, u.first_name, u.last_name;
END;
$$ LANGUAGE plpgsql;

-- Поиск отелей по параметрам
CREATE OR REPLACE FUNCTION search_hotels(
    p_location   TEXT    DEFAULT NULL,
    p_min_rating NUMERIC DEFAULT NULL,
    p_max_price  NUMERIC DEFAULT NULL,
    p_guests     INTEGER DEFAULT NULL
) RETURNS TABLE (
    hotel_id    INTEGER,
    hotel_name  TEXT,
    loc         TEXT,
    rating      NUMERIC,
    min_price   NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT h.id, h.name, h.location, h.rating,
           MIN(rt.base_price)
    FROM hotels h
    JOIN room_types rt
        ON rt.hotel_id = h.id AND rt.is_deleted = FALSE
    WHERE h.is_deleted = FALSE
      AND (p_location IS NULL OR h.location ILIKE '%' || p_location || '%')
      AND (p_min_rating IS NULL OR h.rating >= p_min_rating)
      AND (p_max_price IS NULL OR rt.base_price <= p_max_price)
      AND (p_guests IS NULL OR rt.capacity >= p_guests)
    GROUP BY h.id, h.name, h.location, h.rating
    ORDER BY rating DESC;
END;
$$ LANGUAGE plpgsql;

-- Представления
CREATE VIEW v_hotel_statistics AS
SELECT h.id, h.name, h.location, h.rating,
       COUNT(b.id) AS total_bookings,
       COALESCE(SUM(b.total_price), 0) AS total_revenue
FROM hotels h
LEFT JOIN bookings b ON b.hotel_id = h.id AND b.is_deleted = FALSE
WHERE h.is_deleted = FALSE
GROUP BY h.id, h.name, h.location, h.rating;

CREATE VIEW v_active_bookings AS
SELECT b.id, u.username, h.name AS hotel_name,
       rt.name AS room_type_name,
       b.check_in_date, b.check_out_date, b.total_price
FROM bookings b
JOIN users u ON u.id = b.user_id
JOIN hotels h ON h.id = b.hotel_id
JOIN room_types rt ON rt.id = b.room_type_id
WHERE b.is_deleted = FALSE AND b.status IN (1, 2, 3);

-- Начальные данные
INSERT INTO role (id, role_name) VALUES
    (1, 'Guest'),
    (2, 'Manager'),
    (3, 'Admin');
