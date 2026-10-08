-- アプリ起動時に毎回実行されるテーブル定義 (何度流しても同じ結果になるように書く)
-- Classic ASP 時代なら Enterprise Manager や Access で手作業していた部分。

CREATE TABLE IF NOT EXISTS todos (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title       varchar(200) NOT NULL,
    due_date    date,
    is_done     boolean      NOT NULL DEFAULT false,
    created_at  timestamptz  NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS departments (
    id    integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code  varchar(10) NOT NULL UNIQUE,
    name  varchar(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS employees (
    id             integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_no    varchar(10)  NOT NULL UNIQUE,
    name           varchar(50)  NOT NULL,
    name_kana      varchar(50)  NOT NULL,
    department_id  integer      NOT NULL REFERENCES departments (id),
    email          varchar(254),
    hired_on       date,
    is_retired     boolean      NOT NULL DEFAULT false,
    note           varchar(1000),
    updated_by     varchar(50),
    updated_at     timestamptz  NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_employees_department ON employees (department_id);

-- 初期データ (部署が 1 件も無いときだけ投入する。氏名はすべて架空)
INSERT INTO departments (code, name)
SELECT v.code, v.name
FROM (VALUES ('100', '総務部'), ('200', '営業部'), ('300', '開発部'), ('400', '経理部')) AS v (code, name)
WHERE NOT EXISTS (SELECT 1 FROM departments);

INSERT INTO employees (employee_no, name, name_kana, department_id, email, hired_on, is_retired, note, updated_by)
SELECT v.employee_no, v.name, v.name_kana, d.id, v.email, v.hired_on::date, v.is_retired, v.note, 'system'
FROM (VALUES
    ('E0001', '山田 太郎',   'ヤマダ タロウ',     '100', 'yamada@example.co.jp',    '1998-04-01', false, '総務部長'),
    ('E0002', '佐藤 花子',   'サトウ ハナコ',     '200', 'sato@example.co.jp',      '2001-04-01', false, NULL),
    ('E0003', '鈴木 一郎',   'スズキ イチロウ',   '300', 'suzuki@example.co.jp',    '2003-10-01', false, 'ASP から Razor Pages への移行担当'),
    ('E0004', '高橋 美咲',   'タカハシ ミサキ',   '300', 'takahashi@example.co.jp', '2010-04-01', false, NULL),
    ('E0005', '田中 健',     'タナカ ケン',       '400', 'tanaka@example.co.jp',    '2005-07-16', false, NULL),
    ('E0006', '伊藤 さくら', 'イトウ サクラ',     '200', 'ito@example.co.jp',       '2015-04-01', false, NULL),
    ('E0007', '渡辺 誠',     'ワタナベ マコト',   '100', NULL,                      '1995-04-01', true,  '2020年3月 定年退職'),
    ('E0008', '小林 直子',   'コバヤシ ナオコ',   '300', 'kobayashi@example.co.jp', '2019-04-01', false, NULL),
    ('E0009', '加藤 翔',     'カトウ ショウ',     '200', 'kato@example.co.jp',      '2021-04-01', false, NULL),
    ('E0010', '吉田 恵',     'ヨシダ メグミ',     '400', 'yoshida@example.co.jp',   '2012-09-01', false, NULL),
    ('E0011', '山本 大輔',   'ヤマモト ダイスケ', '300', 'yamamoto@example.co.jp',  '2008-04-01', false, NULL),
    ('E0012', '中村 由美',   'ナカムラ ユミ',     '100', 'nakamura@example.co.jp',  '2023-04-01', false, NULL)
) AS v (employee_no, name, name_kana, dept_code, email, hired_on, is_retired, note)
JOIN departments d ON d.code = v.dept_code
WHERE NOT EXISTS (SELECT 1 FROM employees)
ORDER BY v.employee_no;

INSERT INTO todos (title, due_date, is_done)
SELECT v.title, v.due_date::date, v.is_done
FROM (VALUES
    ('Classic ASP の画面を棚卸しする',            NULL,         true),
    ('docker compose up で動作確認する',          NULL,         false),
    ('従業員マスタの CSV を Excel で開いてみる',  NULL,         false)
) AS v (title, due_date, is_done)
WHERE NOT EXISTS (SELECT 1 FROM todos);
