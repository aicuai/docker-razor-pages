-- Classic ASP 版が想定している SQL Server 用のテーブル定義 (参照用・未実行)
-- PostgreSQL 版の定義は apps/web/Data/schema.sql

CREATE TABLE todos (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    title       NVARCHAR(200) NOT NULL,
    due_date    DATETIME      NULL,
    is_done     BIT           NOT NULL DEFAULT 0,
    created_at  DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE departments (
    id    INT IDENTITY(1,1) PRIMARY KEY,
    code  VARCHAR(10)  NOT NULL UNIQUE,
    name  NVARCHAR(50) NOT NULL
);

CREATE TABLE employees (
    id             INT IDENTITY(1,1) PRIMARY KEY,
    employee_no    VARCHAR(10)    NOT NULL UNIQUE,
    name           NVARCHAR(50)   NOT NULL,
    name_kana      NVARCHAR(50)   NOT NULL,
    department_id  INT            NOT NULL REFERENCES departments (id),
    email          VARCHAR(254)   NULL,
    hired_on       DATETIME       NULL,
    is_retired     BIT            NOT NULL DEFAULT 0,
    note           NVARCHAR(1000) NULL,
    updated_by     NVARCHAR(50)   NULL,
    updated_at     DATETIME       NOT NULL DEFAULT GETDATE()
);
